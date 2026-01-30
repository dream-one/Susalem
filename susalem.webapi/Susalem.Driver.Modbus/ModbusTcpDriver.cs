using Susalem.Core.Application.Interfaces.Services;
using Microsoft.Extensions.Logging;
using NModbus.Utility;
using NModbus;
using System.Net.Sockets;
using Susalem.Messages.Features.Channel;

namespace Susalem.Driver.Modbus;

    /// <summary>
    /// Modbus TCP连接驱动：负责与支持Modbus TCP协议的硬件（如PLC）进行物理通信
    /// </summary>
    public class ModbusTcpDriver : IMonitorDriver
    {
        private readonly ILogger _logger;
        private readonly TcpSetting _setting; // 存储IP地址和端口号等配置
        private readonly CommonSetting _commonSetting; // 存储超时时间等通用配置
        private IModbusMaster _tcpMaster; // NModbus库的核心对象，用于发送请求
        private TcpClient _tcpClient; // 底层网络连接

        /// <summary>
        /// 检查当前是否已建立连接
        /// </summary>
        public bool IsConnected
        {
            get
            {
                if (_tcpClient == null) return false;
                if (_tcpClient.Client == null) return false;
                return _tcpClient.Client.Connected;
            }
        }

    public ModbusTcpDriver(TcpSetting setting, CommonSetting commonSetting, ILogger logger)
    {
        if (setting == null) throw new ArgumentNullException(nameof(setting));
        if (commonSetting == null) throw new ArgumentNullException(nameof(commonSetting));

        _logger = logger;
        _setting = setting;
        _commonSetting = commonSetting;
    }

    /// <summary>
    /// 建立TCP连接并初始化Modbus Master
    /// </summary>
    public bool Connect()
    {
        try
        {
            _logger.LogInformation($"正在连接到设备: {_setting.Host}:{_setting.Port}");

            if (_tcpClient != null)
            {
                Disconnect(); // 如果已有连接，先断开
            }

            // 创建TCP客户端并设置读写超时
            _tcpClient = new TcpClient(_setting.Host, _setting.Port)
            {
                ReceiveTimeout = _commonSetting.ReadTimeout,
                SendTimeout = _commonSetting.WriteTimeout
            };

            // 使用NModbus工厂创建通信主站
            _tcpMaster = new ModbusFactory().CreateMaster(_tcpClient);
            return _tcpClient.Client.Connected;
        }
        catch (Exception ex)
        {
            _logger.LogError("Modbus TCP连接异常: {ex}", ex);
            return false;
        }
    }

    /// <summary>
    /// 关闭连接并释放资源
    /// </summary>
    public void Disconnect()
    {
        _logger.LogInformation($"正在断开与设备 {_setting.Host} 的连接");
        _tcpClient?.Close();
        _tcpClient?.Dispose();
    }

    /// <summary>
    /// 执行写入单个寄存器的命令（写操作）
    /// </summary>
    /// <param name="address">从站地址</param>
    /// <param name="command">命令对象，包含物理地址和值</param>
    public bool Execute(int address, EngineCommand command)
    {
        try
        {
            // 向指定寄存器写入单个值 (Function Code: 06)
            _tcpMaster.WriteSingleRegister((byte)address, command.Reg, command.Value);
            return true;
        }
        catch (IOException ioe)
        {
            _logger.LogError($"Modbus写入单个寄存器IO错误, Server: {_setting.Host}, Address: {address}, {ioe}");
            Disconnect();
            return false;
        }
        catch (SocketException ex) when (ex.ErrorCode == 10060)
        {
            _logger.LogError($"Modbus写入单个寄存器超时, Server: {_setting.Host}, Address: {address}, {ex}");
            Disconnect();
            return false;
        }
        catch (Exception e)
        {
            _logger.LogError($"Modbus写入单个寄存器未知错误, Server: {_setting.Host}, Address: {address}, {e}");
            return false;
        }
    }

    /// <summary>
    /// 执行批量写入寄存器的命令
    /// </summary>
    /// <param name="address">从站地址</param>
    /// <param name="commands">命令列表</param>
    public bool Execute(int address, IList<EngineCommand> commands)
    {
        try
        {
            // 找出这一组命令中起始的寄存器地址
            var startAddress = commands.Min(t => t.Reg);
            // 提取所有要写入的值并转换为数组
            var data = commands.Select(t => t.Value).ToArray();
            
            // 批量写入多个寄存器 (Function Code: 16)
            _tcpMaster.WriteMultipleRegisters((byte)address, startAddress, data);
            return true;
        }
        catch (IOException ioe)
        {
            _logger.LogError($"Modbus批量写入寄存器IO错误, Server: {_setting.Host}, Address: {address}, {ioe}");
            Disconnect();
            return false;
        }
        catch (SocketException ex) when (ex.ErrorCode == 10060)
        {
            _logger.LogError($"Modbus批量写入超时, Server: {_setting.Host}, Address: {address}, {ex}");
            Disconnect();
            return false;
        }
        catch (Exception e)
        {
            _logger.LogError($"Modbus批量写入未知错误, Server: {_setting.Host}, Address: {address}, {e}");
            return false;
        }
    }

    /// <summary>
    /// 读取一组点位数据（核心方法）
    /// </summary>
    /// <param name="address">从站地址</param>
    /// <param name="telemetries">待读取的点位列表</param>
    public bool Read(int address, IList<EngineTelemetry> telemetries)
    {
        try
        {
            // --- 优化策略：合并读取 ---
            // 排序并计算这组点位覆盖的起始地址和总长度
            var orderedTelemetries = telemetries.OrderBy(t => t.Reg);
            var startReg = orderedTelemetries.First().Reg; // 第一个点位的寄存器地址
            var length = (orderedTelemetries.Last().Reg + orderedTelemetries.Last().Length) - startReg; // 总共需要读取的字长度

            // 一次性读取该范围内所有的输入寄存器（Input Registers）
            // 相比于一个点位发一次请求，合并请求能大幅提升上位机采集频率
            var data = _tcpMaster.ReadInputRegisters((byte)address, startReg, (ushort)length).ToList();
            
            foreach (var engineTelemetry in telemetries)
            {
                // 从读取到的字节流中截取对应点位的数据部分
                var telemetryData = data.GetRange(engineTelemetry.Reg - startReg, engineTelemetry.Length);
                
                if (telemetryData.Count > 1)
                {
                    // 如果长度 > 1（通常是2个寄存器，32位），则认为是Float等大数据类型
                    // 使用ModbusUtility将两个16位寄存器合并成一个单精度浮点数
                    engineTelemetry.Value = Math.Round(ModbusUtility.GetSingle(telemetryData[0], telemetryData[1]), 1);
                }
                else
                {
                    // 否则认为是单个寄存器（16位整数），执行点位自身的计算逻辑
                    engineTelemetry.Cal(telemetryData.First());
                }
            }

            return true;
        }
        catch (IOException ioe)
        {
            _logger.LogError($"Modbus读取寄存器IO错误, Server: {_setting.Host}, Address: {address}, {ioe}");
            return false;
        }
        catch (SocketException ex) when (ex.ErrorCode == 10060)
        {
            _logger.LogError($"Modbus读取超时, Server: {_setting.Host}, Address: {address}, {ex}");
            Disconnect(); // 超时后建议断开连接重试
            return false;
        }
        catch (Exception e)
        {
            _logger.LogError($"Modbus读取未知错误, Server: {_setting.Host}, Address: {address}, {e}");
            return false;
        }
    }

    /// <summary>
    /// 批量同步点位状态到硬件（保持寄存器写操作）
    /// </summary>
    public bool Write(int address, IList<EngineTelemetry> telemetries)
    {
        try
        {
            // 计算点位覆盖的起始地址和长度（同样的为了合并写入提高效率）
            var orderedTelemetries = telemetries.OrderBy(t => t.Reg);
            var startReg = orderedTelemetries.First().Reg;
            var length = (orderedTelemetries.Last().Reg + orderedTelemetries.Last().Length) - startReg;
            
            var data = new ushort[length]; // 初始化待写入的缓冲区

            foreach (var engineTelemetry in orderedTelemetries)
            {
                try
                {
                    // 获取点位要写入的原始字节数据（16位无符号整数数组）
                    var writeData = engineTelemetry.GetWriteData();
                    // 将点位数据拷贝到大缓冲区的对应偏移位置
                    for (var i = 0; i < writeData.Length; i++)
                    {
                        data[engineTelemetry.Reg - startReg + i] = writeData[i];
                    }
                }
                catch (Exception e)               
                {
                    _logger.LogError($"处理待写入点位数据异常: {engineTelemetry.Key}, {e}");
                }
            }

            // 执行批量写入
            _tcpMaster.WriteMultipleRegisters((byte)address, startReg, data);
            return true;
        }
        catch (IOException ex)
        {
            _logger.LogError($"Modbus批量写入点位IO错误, Server: {_setting.Host}, Address: {address}, {ex}");
            Disconnect();
            return false;
        }
        catch (SocketException ex) when (ex.ErrorCode == 10060)
        {
            _logger.LogError($"Modbus批量写入点位超时, Server: {_setting.Host}, Address: {address}, {ex}");
            Disconnect();
            return false;
        }
        catch (Exception e)
        {
            _logger.LogError($"Modbus批量写入点位未知错误, Server: {_setting.Host}, Address: {address}, {e}");
            return false;
        }
    }

    /// <summary>
    /// 读取门限状态（特定业务点位：209地址开始读取4个寄存器）
    /// </summary>
    public bool Read(int address, IList<DoorStatus> doors)
    {
        try
        {
            // 固定读取 209 地址开始的 4 个保持寄存器
            var data = _tcpMaster.ReadHoldingRegisters((byte)address, 209, 4).ToList();
            for (var i = 0; i < data.Count; i++)
            {
                // 逻辑直觉：0表示开启，非0表示关闭（上位机常用逻辑）
                doors[i].Open = data[i] == 0;
            }

            return true;
        }
        catch (IOException ex)
        {
            _logger.LogError($"读取门状态IO错误, Server: {_setting.Host}, Address: {address}, {ex}");
            Disconnect();
            return false;
        }
        catch (SocketException ex) when (ex.ErrorCode == 10060)
        {
            _logger.LogError($"读取门状态超时, Server: {_setting.Host}, Address: {address}, {ex}");
            Disconnect();
            return false;
        }
        catch (Exception e)
        {
            _logger.LogError($"读取门状态未知错误, Server: {_setting.Host}, Address: {address}, {e}");
            return false;
        }
    }

    /// <summary>
    /// 读取调试用数据（通常直接读取保持寄存器，不做过多业务转换）
    /// </summary>
    public bool ReadDebugData(int address, IList<DebugData> datas)
    {
        try
        {
            var orderedDatas = datas.OrderBy(t => t.Reg);
            var startReg = orderedDatas.First().Reg;
            var length = (orderedDatas.Last().Reg + orderedDatas.Last().Length) - startReg;

            // 读取保持寄存器数据
            var data = _tcpMaster.ReadHoldingRegisters((byte)address, startReg, (ushort)length).ToList();

            foreach (var debugData in datas)
            {
                var telemetryData = data.GetRange(debugData.Reg - startReg, debugData.Length);
                if (telemetryData.Count > 1)
                {
                    // 同样支持多寄存器合并解析
                    debugData.Value = Math.Round(ModbusUtility.GetSingle(telemetryData[0], telemetryData[1]), 1);
                }
                else
                {
                    debugData.Value = telemetryData.First();
                }
            }

            return true;
        }
        catch (IOException ex)
        {
            _logger.LogError($"读取调试数据IO错误, Server: {_setting.Host}, Address: {address}, {ex}");
            Disconnect();
            return false;
        }
        catch (SocketException ex) when (ex.ErrorCode == 10060)
        {
            _logger.LogError($"读取调试数据超时, Server: {_setting.Host}, Address: {address}, {ex}");
            Disconnect();
            return false;
        }
        catch (Exception e)
        {
            _logger.LogError($"读取调试数据未知错误, Server: {_setting.Host}, Address: {address}, {e}");
            return false;
        }
    }

    /// <summary>
    /// 写入调试用的单个数据项
    /// </summary>
    public bool WriteDebugData(int address, DebugData data)
    {
        try
        {
            var regDatas = new ushort[data.Length];
            if (data.Length > 1)
            {
                // 如果是32位数据（长度为2），手动进行大小端转换和Float封包
                var writeData = BitConverter.GetBytes((float)data.Value);
                // 注意：这里体现了上位机常见的“大小端字节交换”操作
                regDatas[1] = BitConverter.ToUInt16(writeData, 0);
                regDatas[0] = BitConverter.ToUInt16(writeData, 2);
            }
            else
            {
                // 16位数据处理
                var intData = Convert.ToUInt32(data.Value);
                var writeData = BitConverter.GetBytes(intData);

                for (var i = 0; i < regDatas.Length; i++)
                {
                    regDatas[i] = writeData[i];
                }
            }

            _tcpMaster.WriteMultipleRegisters((byte)address, data.Reg, regDatas);
            return true;
        }
        catch (IOException ex)
        {
            _logger.LogError($"写入调试数据IO错误, Server: {_setting.Host}, Address: {address}, {ex}");
            Disconnect();
            return false;
        }
        catch (SocketException ex) when (ex.ErrorCode == 10060)
        {
            _logger.LogError($"写入调试数据超时, Server: {_setting.Host}, Address: {address}, {ex}");
            Disconnect();
            return false;
        }
        catch (Exception e)
        {
            _logger.LogError($"写入调试数据未知错误, Server: {_setting.Host}, Address: {address}, {e}");
            return false;
        }
    }

    /// <summary>
    /// 批量操作线圈（写开关量）
    /// </summary>
    public bool ExecuteCoil(int address, IList<EngineCommand> commands)
    {
        try
        {
            var startAddress = commands.Min(t => t.Reg);
            var data = commands.Select(t => t.Value).ToArray();
            var switches = new List<bool>();
            foreach (var currentData in data)
            {
                // 将数值（0/1）转换为布尔值，用于线圈操作
                switches.Add(currentData != 0);
            }
            // 批量写入多个线圈 (Function Code: 15)
            _tcpMaster.WriteMultipleCoils((byte)address, startAddress, switches.ToArray());
            return true;
        }
        catch (IOException ex)
        {
            _logger.LogError($"写入线圈IO错误, Server: {_setting.Host}, Address: {address}, {ex}");
            Disconnect();
            return false;
        }
        catch (SocketException ex) when (ex.ErrorCode == 10060)
        {
            _logger.LogError($"写入线圈超时, Server: {_setting.Host}, Address: {address}, {ex}");
            Disconnect();
            return false;
        }
        catch (Exception e)
        {
            _logger.LogError($"写入线圈未知错误, Server: {_setting.Host}, Address: {address}, {e}");
            return false;
        }
    }

    /// <summary>
    /// 读取线圈状态（读开关量，Function Code: 01）
    /// </summary>
    public bool ReadCoil(int address, IList<EngineTelemetry> telemetries)
    {
        try
        {
            var orderedTelemetries = telemetries.OrderBy(t => t.Reg);
            var startReg = orderedTelemetries.First().Reg;
            var length = (orderedTelemetries.Last().Reg + orderedTelemetries.Last().Length) - startReg;

            // 读取线圈
            var data = _tcpMaster.ReadCoils((byte)address, startReg, (ushort)length).ToList();
            // TODO: 数据解析逻辑需在此处由点位对象各自处理
            return true;
        }
        catch (IOException ex)
        {
            _logger.LogError($"读取线圈IO错误, Server: {_setting.Host}, Address: {address}, {ex}");
            Disconnect();
            return false;
        }
        catch (SocketException ex) when (ex.ErrorCode == 10060)
        {
            _logger.LogError($"读取线圈超时, Server: {_setting.Host}, Address: {address}, {ex}");
            Disconnect();
            return false;
        }
        catch (Exception e)
        {
            _logger.LogError($"读取线圈未知错误, Server: {_setting.Host}, Address: {address}, {e}");
            return false;
        }
    }
}

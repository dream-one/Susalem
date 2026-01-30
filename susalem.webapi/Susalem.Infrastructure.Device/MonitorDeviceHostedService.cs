using System;
using System.Threading;
using System.Threading.Tasks;
using Susalem.Core.Application.Interfaces;
using Susalem.Messages.Enumerations;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Susalem.Infrastructure.Device;

/// <summary>
/// 设备通信遍历数据的后台服务
/// 负责定时遍历所有已启用的通道，并对通道下的设备进行数据采集和状态监控
/// </summary>
public class MonitorDeviceHostedService : BackgroundService
{
    private readonly IEngineFactory _engineFactory;
    private readonly IChannelFactory _channelFactory;
    private readonly ILogger<MonitorDeviceHostedService> _logger;

    /// <summary>
    /// 构造函数，注入通道工厂、引擎工厂和日志组件
    /// </summary>
    public MonitorDeviceHostedService(
        IChannelFactory channelFactory,
        IEngineFactory engineFactory,
        ILogger<MonitorDeviceHostedService> logger)
    {
        _channelFactory = channelFactory;
        _engineFactory = engineFactory;
        _logger = logger;
    }

    /// <summary>
    /// 后台服务主执行方法
    /// 初始化通道和引擎工厂，遍历所有通道，为每个启用的通道开启独立线程进行设备数据采集
    /// </summary>
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // 初始化通道和引擎工厂
        await _channelFactory.InitializeAsync();
        await _engineFactory.InitializeAsync();

        _logger.LogInformation("Monitor device hosted service is running");

        // 遍历所有通道
        foreach (var channel in _channelFactory.Channels)
        {
            // 跳过未启用的通道
            if (!channel.Channel.Enable)
                continue;

            // 为每个通道开启独立线程进行设备数据采集
            new Thread(() =>
            {
                BackgroundProcessing(channel, stoppingToken);
            }).Start();
        }
    }

    /// <summary>
    /// 通道设备数据采集与监控主循环
    /// </summary>
    /// <param name="channel">通信通道</param>
    /// <param name="stoppingToken">取消令牌</param>
    private void BackgroundProcessing(ICommChannel channel, CancellationToken stoppingToken)
    {
        // 获取该通道下的所有设备引擎
        var engines = _engineFactory.GetEnginesByChannel(channel.Channel.Id);
        _logger.LogInformation("Channel: {ChannelName}, Content:{Content}, Device Count:{DeviceCount}",
            channel.Channel.Name,
            channel.Channel.Content,
            engines.Count);

        // 主循环，直到服务被取消
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // 如果通道离线，尝试重连
                if (channel.Status == ChannelStatus.Offline)
                {
                    if (!channel.MonitorDriver.Connect())
                    {
                        _logger.LogError("Monitor driver {ChannelName} connect failed, after 5s will retry connect", channel.Channel.Name);
                        Thread.Sleep(5 * 1000); // 5秒后重试
                        continue;
                    }
                }

                // 获取设备采集间隔，默认300ms
                var deviceInterval = channel.Channel.Settings.DeviceInterval;
                if (deviceInterval <= 0)
                {
                    deviceInterval = 300;
                }

                // 遍历该通道下的所有设备引擎，采集数据
                foreach (var deviceEngine in engines)
                {
                    // 如果通道离线，跳出本轮设备遍历
                    if (channel.Status == ChannelStatus.Offline)
                    {
                        break;
                    }

                    _logger.LogDebug($"Channel: {channel.Channel.Name}, Device address: {deviceEngine.BasicInfo.Address}");

                    // 更新设备遥测数据
                    if (deviceEngine.UpdateTelemetries())
                    {
                        // 打印每个遥测项的原始值和新值
                        foreach (var deviceEngineTelemetry in deviceEngine.Telemetries)
                        {
                            _logger.LogDebug($" {deviceEngineTelemetry.Key} : {deviceEngineTelemetry.OriginalValue} => {deviceEngineTelemetry.Value} ");
                        }
                    }

                    // 设备间隔采集
                    Thread.Sleep(deviceInterval);
                }

                _logger.LogDebug("End Loop Devices");
            }
            catch (Exception ex)
            {
                // 捕获异常，避免线程崩溃
                _logger.LogError($"Loop devices exception: {ex}");
            }
        }
    }

    /// <summary>
    /// 服务停止时的清理逻辑
    /// </summary>
    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Monitor Device Hosted Service is stopping.");

        // 停止所有通道的驱动连接
        foreach (var channel in _channelFactory.Channels)
        {
            channel.MonitorDriver.Disconnect();
        }

        await base.StopAsync(cancellationToken);
    }
}
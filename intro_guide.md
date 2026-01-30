# Susalem 项目进阶入门指南 (上位机初学者版)

Susalem 不仅仅是一个 MES 系统，它更像是一个基于 .NET 生态构建的**上位机/ industrial IoT (IIoT) 快速开发框架**。对于初学者来说，跳过简单的 Demo 演示，深入其核心架构是形成“上位机直觉”的最佳路径。

---

## 1. 核心架构直觉：三层结构

Susalem 的 Backend (`susalem.webapi`) 采用了典型的分层设计，其核心逻辑可以归纳为：

1.  **通信层 (Driver Layer)**：解决“如何拿到数据”的问题。
2.  **抽象层 (Thing Model / 物模型)**：解决“拿到的数据是什么”以及“如何结构化管理”的问题。
3.  **持久层 (Persistence Layer)**：解决“数据如何存，谁能改”的问题。

---

## 2. 深入：通信与驱动层 (Driver)
在上位机开发中，驱动是灵魂。Susalem 并没有过度封装，而是基于工业标准库进行了标准化的接口定义。

-   **关键组件**：`IMonitorDriver` 接口。
-   **技术栈**：
    -   **Modbus**: 使用 `NModbus` 处理标准的 TCP/RTU 通信。
    -   **高性能通信**: 引入了 `HslCommunication` (常见于高性能场景)。
-   **代码直觉**：
    -   查看 [ModbusTcpDriver.cs](file:///d:/%E4%B8%8A%E4%BD%8D%E6%9C%BA/Susalem/susalem.webapi/Susalem.Driver.Modbus/ModbusTcpDriver.cs)，你会发现它将底层的 `TcpClient` 和 `ModbusFactory` 封装，提供了统一的 `Read` 和 `Execute` 方法。这就是**屏蔽硬件差异**的第一步。

---

## 3. 灵魂：物模型 (Thing Model)
这是 Susalem 区别于简单 HMI 软件的关键。物模型将复杂的寄存器地址（如 `40001`, `40002`）转换成了人类可读的对象。

-   **直觉模型**：
    -   一个“设备”包含了多个“点位”（Telemetry/Attributes）。
    -   通过 JSON 或配置，将 `Modbus 寄存器地址` 映射到 `Property Name`。
-   **核心代码**：[ThingObject.cs](file:///d:/%E4%B8%8A%E4%BD%8D%E6%9C%BA/Susalem/susalem.webapi/Susalem.Infrastructure.ThingModel/Model/ThingObject.cs) 定义了这些抽象关系。

---

## 4. 架构设计：多上下文数据库 (Persistence)
Susalem 在持久层做了清晰的职责分离，这不是过度设计，而是为了**高并发和安全性**。

-   **`IdentityDbContext`**: 专门负责用户、权限、RBAC（基于角色的访问控制）。
-   **`ApplicationDbContext`**: 负责物模型配置、设备配置等静态/准静态元数据。
-   **`RecordDbContext`**: 负责由于设备上行产生的大量“运行数据”，这类数据写入频繁，适合单独优化。
-   **直觉提示**：当你发现系统跑不动时，分离出的 `Record` 库可以更容易地迁移到更高效的数据库或进行分表，而不影响用户登录。

---

## 5. 后端技术栈速查
-   **框架**: .NET 6 (LTS)
-   **ORM**: Entity Framework Core (EF Core)
-   **消息**: 内部使用 `MediatR` 或类似的 Event Bus 进行解耦（参见 `Susalem.Shared.Messages`）。
-   **扩展性**: 包含 `Notification` 模块（支持邮件、Webhook），用于报警推送。

---

## 6. 如何阅读源码建议
1.  **从驱动开始**：看 `Susalem.Driver.Modbus`，理解底层封包。
2.  **看物模型解析**：看 `Susalem.Infrastructure.ThingModel`，看系统如何把 Byte 数据变成 Object。
3.  **看业务流转**：寻找 [Susalem.Core.Application](file:///d:/%E4%B8%8A%E4%BD%8D%E6%9C%BA/Susalem/susalem.webapi/Susalem.Core.Application) 中的 Command 和 Query 处理器。

---

## 7. 目录结构大盘点
### 7.1 项目根目录
| 目录名 | 作用 |
| :--- | :--- |
| **susalem.webapi** | **核心后端**：基于 .NET 6 的现代 API 服务器，负责逻辑、数据、驱动。 |
| **susalem.wpf** | **桌面客户端**：基于 WPF 实现的上位机界面。 |
| **susalem.avalonia** | **跨平台桌面端**：基于 Avalonia 实现，支持 Linux/Windows/macOS（目前维护频率较低）。 |
| **doc** | **文档**：存储项目相关的设计图、协议文档等。 |
| **susalem.webapi-old** | **旧版后端**：供参考的旧版本代码，非核心。 |

### 7.2 后端核心目录 (`susalem.webapi`)
为了让你能快速通过文件名定位代码功能，这里是核心模块的作用说明：

| 目录/项目名 | 作用说明 | 直觉定位 |
| :--- | :--- | :--- |
| **Susalem.API** | **入口层** | Web 服务的启动头，Controller 所在地，处理来自前端的 HTTP 请求。 |
| **Susalem.Core.Application** | **业务逻辑层** | 系统的“大脑”。包含接口定义、Commands(写操作) 和 Queries(读操作)。 |
| **Susalem.Infrastructure** | **底层支撑** | 包含通用的服务实现、中间件、工具类以及与外部系统的对接逻辑。 |
| **Susalem.Persistence** | **数据库交互** | 所有的实体类（Entity）、DbContext 和数据库迁移脚本都在这里。 |
| **Susalem.Driver.Modbus** | **协议通信** | 专门负责 Modbus 协议的底层封包与解析，直接操作网络/串口。 |
| **Susalem.Infrastructure.ThingModel** | **物模型核心** | 定义如何将“寄存器地址”转换成“属性对象”，是系统的核心抽象逻辑。 |
| **Susalem.Shared.Messages** | **契约协议** | 跨模块通信的消息定义，确保不同子系统说的是“同一种语言”。 |
| **Susalem.Notification** | **报警通知** | 独立的子项目，负责邮件、Webhook 等报警信息的下发。 |

---

## 8. 前端项目说明

- **susalem.wpf**: 传统的 Windows 客户端，适合本地控制站、HMI 场景。
- **susalem.avalonia**: 跨平台 UI 框架实现，同一个 UI 可以跑在 Linux/Windows 上（目前暂时停止更新）。
- **Susalem.Vue**: (外部仓库) 现代化的 Web 管理后台，适合 MES 看板和远程管理。

---

## 9. 如何模拟通信 (无硬件调试)

上位机开发的一大痛点是“手头没 PLC”。你可以使用仿真工具快速开始调试：

### 推荐工具：Modbus Slave
这是上位机开发者的必备神器，可以模拟一个标准的 PLC。

1. **设置仿真：**
   - 下载并安装 **Modbus Slave**。
   - 点击 **Connection** -> **Connect**，选择 **Modbus TCP/IP**。
   - Port 保持默认 **502** (如果提示权限不足可改为 5020)。
2. **模拟数据：**
   - 点击 **Setup** -> **Slave Definition**。
   - 根据 [Demo.json](file:///d:/%E4%B8%8A%E4%BD%8D%E6%9C%BA/Susalem/susalem.webapi/Susalem.Infrastructure.ThingModel/Demo.json) 的定义：
     - **Function Code 04 (Input Registers)**：地址设为 **30001**。
     - **Function Code 03 (Holding Registers)**：地址设为 **40001**。
   - 在表中双击单元格，手动输入数值（如 `255`）。
3. **程序中配置：**
   - 在 Susalem 的配置文件（或数据库）中，将设备的 IP 设为 `127.0.0.1`，Port 设为仿真器的端口。
   - 运行程序，你就可以在上位机界面上看到刚才输入的 `25.5`（如果是 Temp `raw / 10.0`）。

---

## 10. 运行与调试实战

代码看百遍，不如运行一遍。以下是标准的启动调试流程：

### 10.1 第一步：启动后端 (The Brain)
后端负责所有的数据采集和逻辑处理，必须先启动。
1. 使用 Visual Studio 打开 `susalem.webapi/susalem.api.sln`。
2. 将 **`Susalem.API`** 设为启动项目。
3. 按 **F5** 运行。
   - 成功标志：浏览器自动弹出 **Swagger** 页面 (API 文档界面)。
   - **注意**：此时后端已经开始尝试连接配置文件中的 PLC (或你的仿真器) 了。

### 10.2 第二步：启动客户端 (The Face)
1. 使用另一个 Visual Studio 实例打开 `susalem.wpf/susalem.wpf.sln`。
2. 将 **`susalem.wpf`** (注意不是 EasyDemo) 设为启动项目。
3. 按 **F5** 运行。
   - 登录界面通常默认账号为 `admin` / `123456` (具体需查看数据库或源码配置)。

### 10.3 调试通信程序 (The Connection)
想要亲眼看到数据怎么流转？
1. **打断点**：
   - 回到后端的 VS，打开我们刚才注释过的 `ModbusTcpDriver.cs`。
   - 在 `Read` 方法内部打一个断点 (例如 `var data = _tcpMaster.ReadInputRegisters(...)` 这一行)。
2. **观察数据**：
   - 当断点命中时，把鼠标悬停在 `data` 变量上。
   - 你看到的数组值，应该和你 Modbus Slave 仿真器里填的值一致。
3. **查看日志**：
   - 如果通信失败，不用猜。直接看 VS 的 **输出 (Output)** 窗口，或者查看运行目录下生成的 `logs` 文件夹。Susalem 记录了详细的连接错误日志。

---

> [!TIP]
> 上位机学习的本质不是写 UI，而是**数据的流转**：从 PLC 寄存器 $\rightarrow$ 驱动解包 $\rightarrow$ 物模型抽象 $\rightarrow$ 业务逻辑处理 $\rightarrow$ 数据库持久化/前端展示。

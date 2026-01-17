using System;
using System.IO;
using System.Threading.Tasks;
using Susalem.Infrastructure.Services.DbInitializerService;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;

namespace Susalem.Api
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            // CreateHostBuilder(args) 定义了服务器怎么配置（用什么日志、怎么启动等）
            var host = CreateHostBuilder(args).Build();

            // 创建一个新的依赖注入作用域 (Scope)
            using (var scope = host.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                // 获取日志服务，用于记录启动时的信息
                var logger = services.GetRequiredService<ILogger<Program>>();

                try
                {
                    // 从容器中获取 IDbInitializerService 接口的实现
                    // 在整洁架构中，这个服务通常位于 Infrastructure 层
                    var dbInitializer = services.GetRequiredService<IDbInitializerService>();
                    logger.LogInformation($"Running database migration/seed");
                    // 关键步骤 A: 应用迁移
                    // 这通常会调用 dbContext.Database.Migrate()
                    // 作用：如果数据库不存在，就创建；如果存在但由旧版本，就更新表结构。
                    dbInitializer.Migrate();
                    // 关键步骤 B: 填充种子数据
                    // 作用：写入默认用户、默认配置、字典表数据等。
                    await dbInitializer.SeedAsync();
                }
                catch (Exception e)
                {
                    logger.LogError(e, "An error occurred while running database migration.");
                }
            }
            host.Run();
        }


        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)// 加载默认配置 (appsettings.json, 环境变量等)
                .UseSerilog()// 替换默认日志系统
                // 使用 Serilog 库来接管日志。Serilog 能够将日志结构化输出到文件、Elasticsearch 或控制台，比默认日志更强大。

                .ConfigureWebHostDefaults(webBuilder =>
                {
                    // 2. 指定启动配置类
                    // 告诉 Host，详细的中间件管道(Middleware)和依赖注入配置在 Startup.cs 中
                    webBuilder.UseStartup<Startup>();
                })
                .UseWindowsService(); // 3. Windows 服务支持
        // 允许这个 Web API 像一个 Windows Service (后台服务) 一样运行，
        // 如果你在 IIS 或 Linux Docker 中运行，这行代码通常会被忽略，不影响功能。
    }
}

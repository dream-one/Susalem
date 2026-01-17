using Prism.Ioc;
using Prism.Regions;
using Prism.Unity;
using susalem.EasyDemo.Repository;
using susalem.EasyDemo.Services;
using susalem.EasyDemo.Services.ServicesImpl;
using susalem.EasyDemo.ViewModels;
using susalem.EasyDemo.ViewModels.Dialogs;
using susalem.EasyDemo.Views;
using susalem.EasyDemo.Views.Dialogs;
using System.Configuration;
using System.Data;
using System.Windows;

namespace susalem.EasyDemo
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : PrismApplication
    {
        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {
            containerRegistry.Register<IUserService, UserService>();
            containerRegistry.Register<IRoleService, RoleService>();
            containerRegistry.Register<IHistoryService, HistoryService>();
            containerRegistry.Register<IChamParaService, ChamParaService>();
            containerRegistry.Register<ICabinetInfoService, CabinetInfoService>();

            containerRegistry.RegisterForNavigation<MainWindow, MainWindowViewModel>();

            containerRegistry.RegisterForNavigation<AlarmRecordView, AlarmRecordViewModel>();
            containerRegistry.RegisterForNavigation<ChambrierenView, ChambrierenViewModel>();
            containerRegistry.RegisterForNavigation<HistoryRecordView, HistoryRecordViewModel>();
            containerRegistry.RegisterForNavigation<LoginRecordView, LoginRecordViewModel>();
            containerRegistry.RegisterForNavigation<OperateMachineView, OperateMachineViewModel>();
            containerRegistry.RegisterForNavigation<ParameterSettingView, ParameterSettingViewModel>();
            containerRegistry.RegisterForNavigation<CurrentCabinetView, CurrentCabinetViewModel>();
            containerRegistry.RegisterForNavigation<AddCabinetView, AddCabinetViewModel>();


            containerRegistry.RegisterDialog<ErrorView, ErrorViewModel>();
            containerRegistry.RegisterDialog<WarningView, WarningViewModel>();
            containerRegistry.RegisterDialog<AddUserView, AddUserViewModel>();
            containerRegistry.RegisterDialog<MessageView, MessageViewModel>();

        }
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // === 数据库初始化逻辑放在这里，只运行一次 ===
            try 
            {
                using (var context = new JccRepository())
                {
                    // 自动创建文件夹（如果 JccRepository 内部没处理，这里最好补上）
                    // context.Database.EnsureCreated() 会自动处理文件创建，
                    // 但父文件夹不存在有时会报错，建议保留之前的文件夹检查逻辑
                    
                    // 触发建库和种子数据写入
                    context.Database.EnsureCreated();
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"数据库初始化失败: {ex.Message}");
            }
            // ===========================================
        }
    }

}

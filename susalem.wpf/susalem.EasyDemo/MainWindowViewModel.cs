using HslCommunication.ModBus;
using Prism.Commands;
using Prism.Events;
using Prism.Ioc;
using Prism.Mvvm;
using Prism.Regions;
using Prism.Services.Dialogs;
using susalem.EasyDemo.Entities;
using susalem.EasyDemo.Services;
using susalem.EasyDemo.Share;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace susalem.EasyDemo
{
    public class MainWindowViewModel : BindableBase
    {
        private readonly IRegionManager _regionManager;
        private readonly IDialogService _dialogService;
        private readonly ICabinetInfoService _cabinetInfoService;
        private CancellationTokenSource cts = new CancellationTokenSource();
        private static string _username;
        public static event EventHandler<PropertyChangedEventArgs> StaticPropertyChanged;
        public static string Username
        {
            get { return _username; }
            set { _username = value; StaticPropertyChanged?.Invoke(null, new PropertyChangedEventArgs(nameof(Username))); }
        }
        private string _buttonText = "账号登录"; // 初始文字
        public string ButtonText
        {
            get { return _buttonText; }
            set { SetProperty(ref _buttonText, value); } // 当值改变时通知 UI 更新
        }

        // 定义一个命令，用于点击按钮时修改文字
        public DelegateCommand ChangeTextCommand { get; }

        private void ExecuteChangeText()
        {
            // 动态逻辑判断
            if (OverAllContext.User != null)
            {
                ButtonText = "登出";
            }
            else
            {
                ButtonText = "账号登录";
            }
        }
        public MainWindowViewModel(IRegionManager regionManager, ICabinetInfoService cabinetInfoService, IDialogService dialogService, IEventAggregator eventAggregator)
        {
            _regionManager = regionManager;
            _dialogService = dialogService;
            //OverAllContext.modbusTcpServer = new ModbusTcpServer();
            //OverAllContext.modbusTcpServer.ServerStart(502, true);
            ChangeTextCommand = new DelegateCommand(ExecuteChangeText);

            OverAllContext.ModbusTcpLock = new ModbusTcpNet("192.168.1.102", 502);
            OverAllContext.ModbusTcpStatusLight = new ModbusTcpNet("192.168.1.101", 502);
            OverAllContext.ModbusTcpDoor = new ModbusTcpNet("192.168.1.100", 502);
            _cabinetInfoService = cabinetInfoService;
            Username = "当前无登录账户";

            RefreshLight();
            RefreshIsTemperaturing();

            eventAggregator.GetEvent<LoginStatusChangedEvent>().Subscribe(UpdateLoginStatus);
        }

        public void UpdateLoginStatus(bool isLogin)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (isLogin)
                {
                    Username = "当前登录账户：" + OverAllContext.User.UserName;
                    ButtonText = "登出";
                    //切换页面
                    NavigationParameters keyValuePairs = new NavigationParameters();
                    //keyValuePairs.Add("Menu", menuItem);
                    _regionManager.Regions["MainRegion"].RequestNavigate("HistoryRecordView");
                }
                else
                {
                    Username = "当前无登录账户";
                    ButtonText = "账号登录";
                    var region = _regionManager.Regions["MainRegion"];
                    //给region添加事件，当视图跳转完成触发
                    region.NavigationService.Navigated += OnNavigated;
                    _regionManager.Regions["MainRegion"].RequestNavigate("LoginRecordView");
                }
            });
        }
        /// <summary>
        /// 30s更新灯信号
        /// </summary>
        private void RefreshLight()
        {
            Task.Factory.StartNew(async () =>
            {
                // 柜子空的时候不亮灯，回温中红灯，回温结束绿灯
                // 遍历所有柜子的回温信息，如果回温完成写入灯的状态
                // 如果过保质期需要弹窗，回温结束也弹窗
                while (!cts.IsCancellationRequested)
                {
                    try
                    {
                        //TODO 优化方案： 根据历史柜子操作记录来更新灯信号
                        List<CabinetInfoModel> cabinetInfoModelList = _cabinetInfoService.FindAllCabinetInfos();
                        if (cabinetInfoModelList == null || cabinetInfoModelList.Count == 0)
                        {
                            await Task.Delay(30 * 1000);
                            continue;
                        }

                        foreach (var cabinetInfoModel in cabinetInfoModelList)
                        {
                            //TODO 如何熄灭灯
                            if (cabinetInfoModel.IsNull)
                            {
                                await OverAllContext.ModbusTcpLock.WriteAsync(cabinetInfoModel.GreenLightAddress, false);
                            }
                            else
                            {
                                if (cabinetInfoModel.IsTemperaturing)
                                {
                                    await OverAllContext.ModbusTcpLock.WriteAsync(cabinetInfoModel.RedLightAddress, true);
                                }
                                else
                                {
                                    await OverAllContext.ModbusTcpLock.WriteAsync(cabinetInfoModel.GreenLightAddress, true);
                                }
                            }
                        }


                    }
                    catch (Exception ex)
                    {

                    }
                    await Task.Delay(30 * 1000);
                }
            }, cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        /// <summary>
        /// 30s刷新回温状态
        /// </summary>
        private void RefreshIsTemperaturing()
        {
            Task.Factory.StartNew(async () =>
            {
                while (!cts.IsCancellationRequested)
                {
                    try
                    {
                        List<CabinetInfoModel> cabinetInfoModelList = _cabinetInfoService.FindAllCabinetInfos().Where(p => !p.IsNull).ToList();
                        if (cabinetInfoModelList == null || cabinetInfoModelList.Count == 0)
                        {
                            await Task.Delay(30 * 1000);
                            continue;
                        }

                        foreach (var cabinetInfoModel in cabinetInfoModelList)
                        {

                            if (cabinetInfoModel.IsTemperaturing && DateTime.Now >= cabinetInfoModel.TemperatureEndTime)
                            {
                                cabinetInfoModel.IsTemperaturing = false;
                            }
                        }

                        await _cabinetInfoService.EditCabinetInfoList(cabinetInfoModelList);
                    }
                    catch (Exception ex)
                    {

                    }

                    await Task.Delay(30 * 1000);
                }
            }, cts.Token, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        /// <summary>
        /// 页面加载跳转实时柜子界面
        /// </summary>
        public ICommand PageLoaded
        {
            get => new DelegateCommand(() =>
            {
                var region = _regionManager.Regions["MainRegion"];
                //给region添加事件，当视图跳转完成触发
                region.NavigationService.Navigated += OnNavigated;
                _regionManager.Regions["MainRegion"].RequestNavigate("LoginRecordView");

            });
        }

        public ICommand ParaSettingCommand
        {
            get => new DelegateCommand(() =>
            {
                //NavigationParameters keyValuePairs = new NavigationParameters();
                //keyValuePairs.Add("Menu", menuItem);
                // 导航时传递参数
                var parameters = new NavigationParameters { { "ClearValidation", true } };
                _regionManager.RequestNavigate("MainRegion", "ParameterSettingView", parameters);
                //_regionManager.Regions["MainRegion"].RequestNavigate("ParameterSettingView", keyValuePairs);
            });
        }

        public ICommand CabinetCommand
        {
            get => new DelegateCommand(() =>
            {
                var parameters = new NavigationParameters();
                _regionManager.RequestNavigate("MainRegion", "AddCabinetView", parameters);
            });
        }

        public ICommand LoginCommand
        {
            get => new DelegateCommand(() =>
            {
                NavigationParameters keyValuePairs = new NavigationParameters();
                //keyValuePairs.Add("Menu", menuItem);

                if (ButtonText == "登出" && OverAllContext.User != null)
                {
                    DialogParameters p = new DialogParameters();
                    p.Add("Content", "您确定要退出当前账号吗？"); // 传给弹窗显示的话
                    _dialogService.ShowDialog("MessageView", p, result =>
                    {
                        if (result.Result == ButtonResult.OK)
                        {
                            UpdateLoginStatus(false);
                        }
                    });
                }
            });
        }

        // 定义一个命令属性，供 XAML 中的 Button 绑定
        public ICommand ChambrierenCommand
        {
            // get => new ... 这是一个“表达式体”，每次访问属性都会执行 => 后面的代码
            // DelegateCommand 是 Prism 提供的类，它把一个 C# 方法包装成 XAML 能认的 Command
            get => new DelegateCommand(() =>
            {
                // 准备“行李” (参数包)
                // NavigationParameters 就像一个字典 Dictionary<string, object>
                NavigationParameters keyValuePairs = new NavigationParameters();

                // 这行被注释了，说明原本想传点数据过去，但现在决定“空手去”
                //keyValuePairs.Add("Menu", menuItem);

                // 执行导航 (核心动作)
                // _regionManager: 区域经理
                // .Regions["MainRegion"]: 找到名叫 "MainRegion" 的那块屏幕 (ContentControl)
                // .RequestNavigate(...): 发出指令——“请把画面切到 ChambrierenView，并带上 keyValuePairs 这些行李”
                _regionManager.Regions["MainRegion"].RequestNavigate("ChambrierenView", keyValuePairs);
            });
        }

        public ICommand OperateCommand
        {
            get => new DelegateCommand(() =>
            {
                NavigationParameters keyValuePairs = new NavigationParameters();
                //keyValuePairs.Add("Menu", menuItem);
                _regionManager.Regions["MainRegion"].RequestNavigate("OperateMachineView", keyValuePairs);
            });
        }

        public ICommand HistoryCommand
        {
            get => new DelegateCommand(() =>
            {
                NavigationParameters keyValuePairs = new NavigationParameters();
                //keyValuePairs.Add("Menu", menuItem);
                _regionManager.Regions["MainRegion"].RequestNavigate("HistoryRecordView", keyValuePairs);
            });
        }

        public ICommand AlarmCommand
        {
            get => new DelegateCommand(() =>
            {
                NavigationParameters keyValuePairs = new NavigationParameters();
                //keyValuePairs.Add("Menu", menuItem);
                //_regionManager.Regions["MainRegion"].RequestNavigate("AlarmRecordView", keyValuePairs);
                _regionManager.Regions["MainRegion"].RequestNavigate("CurrentCabinetView", keyValuePairs);
            });
        }




        private void OnNavigated(object sender, RegionNavigationEventArgs e)
        {
            // 检查导航跳转条件
            if (ShouldCancelNavigation(e.Uri))
            {
                NavigationParameters keyValuePairs = new NavigationParameters();
                _regionManager.Regions["MainRegion"].RequestNavigate("LoginRecordView", keyValuePairs);
                _dialogService.ShowDialog("MessageView", new DialogParameters() { { "Content", "请登录账户!" } }, null);
                //e.Cancel = true; // 取消导航
                // 可以在此处添加提示逻辑
            }

        }


        private bool ShouldCancelNavigation(Uri uri)
        {
            // 账号为空并且uri不是登录界面时，取消导航
            if (OverAllContext.User == null && !uri.OriginalString.Equals("LoginRecordView"))
            {
                return true;
            }
            return false;
        }


    }
}

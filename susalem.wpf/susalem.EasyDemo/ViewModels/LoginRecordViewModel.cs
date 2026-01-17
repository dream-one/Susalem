using susalem.EasyDemo.Entities;
using susalem.EasyDemo.Models;
using HslCommunication.Core.Net;
using HslCommunication.Secs.Types;
using Microsoft.Extensions.Logging;
using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using susalem.EasyDemo.Services;
using Prism.Events;
using susalem.EasyDemo.Share;

namespace susalem.EasyDemo.ViewModels
{
    public class LoginRecordViewModel : BindableBase, IDialogAware
    {
        private readonly IRegionManager _regionManager;
        private readonly IUserService _userService;
        private readonly IDialogService _dialogService;
        private readonly IRoleService _roleService;
        private readonly IEventAggregator _eventAggregator;

        public LoginRecordViewModel(IDialogService dialogService, IRegionManager regionManager,
            IUserService userService, IRoleService roleService, IEventAggregator eventAggregator)
        {
            _dialogService = dialogService;
            _regionManager = regionManager;
            _userService = userService;
            _roleService = roleService;
            _eventAggregator = eventAggregator;
        }

        private ObservableCollection<RoleModel> roleModels = new ObservableCollection<RoleModel>();

        public ObservableCollection<RoleModel> RoleModels
        {
            get { return roleModels; }
            set
            {
                roleModels = value;
                RaisePropertyChanged();
            }
        }

        private ObservableCollection<UserModel>? _allUser = new ObservableCollection<UserModel>();

        public ObservableCollection<UserModel>? AllUser
        {
            get { return _allUser; }
            set { _allUser = value; RaisePropertyChanged(); }
        }

        private ObservableCollection<string> _users = new ObservableCollection<string>();

        public ObservableCollection<string> Users
        {
            get { return _users; }
            set { _users = value; RaisePropertyChanged(); }
        }

        private string? _userName;

        public string? UserName
        {
            get { return _userName; }
            set { _userName = value; RaisePropertyChanged(); }
        }

        private string? _password;

        public string? Password
        {
            get { return _password; }
            set { _password = value; RaisePropertyChanged(); }
        }

        private string? _errorMessage;

        public event Action<IDialogResult> RequestClose;

        public string? ErrorMessage
        {
            get { return _errorMessage; }
            set { _errorMessage = value; RaisePropertyChanged(); }
        }

        private async Task LoadUser()
        {
            await Task.Run(() =>
            {
                var _result = _userService.FindAllUser();

                if (_result != null && _result.Count != 0)
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        AllUser.Clear();
                        Users.Clear();
                        foreach (var user in _result)
                        {
                            if (!string.IsNullOrWhiteSpace(user.UserName))
                            {
                                Users?.Add(user.UserName);
                                AllUser.Add(user);
                            }
                        }
                    });
                    
                }
            });
        }

        private async Task LoadAllRoles()
        {
            await Task.Run(() =>
            {
                var _result = _roleService.FindAllRole();

                if (_result != null && _result.Count != 0)
                {
                    RoleModels.Clear();
                    foreach (var role in _result)
                    {
                        if (!string.IsNullOrWhiteSpace(role.RoleName))
                        {
                            RoleModels?.Add(role);
                        }
                    }
                }
            });
           
        }

        private void Login()
        {
            ErrorMessage = "";
            if (string.IsNullOrEmpty(UserName))
            {
                ErrorMessage = "UserName is empty";
                return;
            }
            if (string.IsNullOrEmpty(Password))
            {
                ErrorMessage = "Password is empty";
                return;
            }

            Task.Run(() =>
            {
                try
                {
                    UserModel resultModel = _userService.Login(UserName, Password,false);
                    if (resultModel!=null)
                    {
                        OverAllContext.User = resultModel!;
                        MainWindowViewModel.Username= "当前登录账户："+OverAllContext.User.UserName;
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            _dialogService.ShowDialog("MessageView", new DialogParameters() { { "Content", "登录成功!" } }, null);
                            // 构建返回结果 (OK)
                            DialogResult dialogResult = new DialogResult(ButtonResult.OK);
                            
                            // 触发 RequestClose 事件，通知 Prism 关闭当前的 LoginView
                            RequestClose?.Invoke(dialogResult);

                            _eventAggregator.GetEvent<LoginStatusChangedEvent>().Publish(true);
                        });

                        //Application.Current.Dispat-cher.Invoke(() => RequestClose?.Invoke(new DialogResult(ButtonResult.OK)));
                        //AppSession.IsLogon = false;
                    }
                    else
                    {
                        ErrorMessage = "登录失败";
                    }
                }
                catch (Exception ex)
                {
                    ErrorMessage = ex.Message;
                    //logger.Error(ex.Message);
                }
            });
        }

        //IDialogAware 接口要求的方法：是否允许关闭
        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {
          
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            
        }

        public ICommand RegisterCommand
        {
            get => new DelegateCommand(() =>
            {
                DialogParameters keyValuePairs = new DialogParameters();
                keyValuePairs.Add("Users", AllUser);
                keyValuePairs.Add("Roles", RoleModels);
                _dialogService.ShowDialog("AddUserView", keyValuePairs, callback =>
                {
                    if (callback.Result == ButtonResult.OK)
                    {
                        LoadUser();
                    }
                });
            });
        }

        public ICommand LoginCommand
        {
            get => new DelegateCommand(() =>
            {
                Login();
            });
        }

        public ICommand PageLoaded
        {
            get => new DelegateCommand(async() =>
            {
                await LoadUser();
                await LoadAllRoles();
            });
        }

        public ICommand PageUnLoaded
        {
            get => new DelegateCommand(() =>
            {
                //Login();
            });
        }

        public string Title => "系统登录";
    }
}

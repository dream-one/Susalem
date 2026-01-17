using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Commands;
using System.Windows.Input;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using susalem.EasyDemo.Entities;
using susalem.EasyDemo.Services;

namespace susalem.EasyDemo.ViewModels
{
    internal class AddCabinetViewModel : BindableBase
    {
        private readonly ICabinetInfoService _cabinetInfoService;
        private readonly IDialogService _dialogService;
        public AddCabinetViewModel(ICabinetInfoService cabinetInfoService, IDialogService dialogService)
        {
            _cabinetInfoService = cabinetInfoService;
            _dialogService = dialogService;
            NewCabinet = new CabinetInfoModel
            {
                IsNull = true,            // 刚添加的柜子默认是空的
                IsTemperaturing = false,  // 没在回温
                ChamName = ""             // 让用户填
            };
        }

        private CabinetInfoModel? cabinetInfo;

        public CabinetInfoModel? NewCabinet
        {
            get { return cabinetInfo; }
            set { cabinetInfo = value; RaisePropertyChanged(); }
        }

        public ICommand AddCommand
        {
            get => new DelegateCommand(async () =>
            {

                if (!NewCabinet.IsValidated)
                {
                    NewCabinet.IsFormValid = true;
                    _dialogService.ShowDialog("MessageView", new DialogParameters() { { "Content", "请检查输入项" } }, null);
                    return;
                }
                int ret = _cabinetInfoService.AddCabinetInfo(NewCabinet);

                if (ret >= 0)
                {
                    _dialogService.ShowDialog("MessageView", new DialogParameters() { { "Content", "操作成功!" } }, null);
                   //重新初始化
                    NewCabinet = new CabinetInfoModel
                    {
                        IsNull = true,            // 刚添加的柜子默认是空的
                        IsTemperaturing = false,  // 没在回温
                        ChamName = ""             // 让用户填
                    };
                }
            });
        }

        public ICommand ClearCommand
        {
            get => new DelegateCommand(() =>
            {
                NewCabinet = new CabinetInfoModel
                {
                    IsNull = true,            // 刚添加的柜子默认是空的
                    IsTemperaturing = false,  // 没在回温
                    ChamName = ""             // 让用户填
                };
            });
        }
    }
}

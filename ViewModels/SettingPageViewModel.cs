using Prism.Windows.Mvvm;
using Prism.Windows.Navigation;
using Prism.Commands;
using Microsoft.Practices.Unity;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using FooEditor.UWP.Services;
using FooEditor.UWP.Models;
using System;
using System.Threading.Tasks;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;
using EncodeDetect;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.AccessCache;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;
using Windows.Graphics.Printing;
using Windows.System.Threading;

namespace FooEditor.UWP.ViewModels
{
    class SettingPageViewModel : ViewModelBase
    {
        INavigationService NavigationService;

        [InjectionConstructor]
        public SettingPageViewModel(INavigationService navigationService)
        {
            this.NavigationService = navigationService;
        }

        public DelegateCommand<object> GlobalSettingCommand
        {
            get
            {
                return new DelegateCommand<object>((s) => {
                    NavigationService.Navigate("GlobalSetting", null);
                });
            }
        }

        public DelegateCommand<object> FileTypeSettingCommand
        {
            get
            {
                return new DelegateCommand<object>((s) => {
                    NavigationService.Navigate("FileTypes", null);
                });
            }
        }

        public DelegateCommand<object> PrintSettingCommand
        {
            get
            {
                return new DelegateCommand<object>((s) => {
                    NavigationService.Navigate("PrintSettings", null);
                });
            }
        }

        public DelegateCommand<object> AboutPageCommand
        {
            get
            {
                return new DelegateCommand<object>((s) => {
                    NavigationService.Navigate("About", null);
                });
            }
        }

    }
}

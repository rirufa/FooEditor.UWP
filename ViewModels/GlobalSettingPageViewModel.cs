using System.Collections.ObjectModel;
using System.Windows.Input;
using Prism.Windows.Mvvm;
using FooEditEngine;
using System;
using CommunityToolkit.Mvvm.Input;
using FooEditor.UWP.Models;

namespace FooEditor.UWP.ViewModels
{
    class GlobalSettingPageViewModel : ViewModelBase
    {

        public ICommand OpenConfigureFolderCommand
        {
            get
            {
                return new RelayCommand<object>(async (e) =>
                {
                    await Windows.System.Launcher.LaunchFolderAsync(Windows.Storage.ApplicationData.Current.LocalFolder);
                });
            }
        }

        ObservableCollection<string> _FontFamilyList;
        public ObservableCollection<string> FontFamilyList
        {
            get
            {
                if (this._FontFamilyList == null)
                    this._FontFamilyList = new ObservableCollection<string>(FontFamillyCollection.GetFonts());
                return this._FontFamilyList;
            }
        }

        ObservableCollection<LineBreakMethod> _LineBreakMethodList;
        public ObservableCollection<LineBreakMethod> LineBreakMethodList
        {
            get
            {
                if(this._LineBreakMethodList == null)
                {
                    this._LineBreakMethodList = new ObservableCollection<LineBreakMethod>(EnumListGenerator.GetList<LineBreakMethod>());
                }
                return _LineBreakMethodList;
            }
        }

        public ObservableCollection<System.Text.Encoding> EncodeCollection
        {
            get
            {
                return AppSettings.SupportEncodeCollection;
            }
        }

        AppSettings _Setting = AppSettings.Current;
        public AppSettings Setting
        {
            get
            {
                return this._Setting;
            }
            set
            {
                this._Setting = value;
                this.OnPropertyChanged();
            }
        }

    }
}

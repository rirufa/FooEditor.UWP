using System.Collections.ObjectModel;
using System.Windows.Input;
using Prism.Windows.Mvvm;
using Prism.Commands;
using FooEditEngine;
using FooEditor.UWP.Models;
using Prism.Windows.Navigation;
using System.Collections.Generic;
using System.Linq;

namespace FooEditor.UWP.ViewModels
{
    class FileTypeDetailPageViewModel : ViewModelBase
    {
        INavigationService NavigationService;

        public FileTypeDetailPageViewModel(INavigationService navigationService)
        {
            this.NavigationService = navigationService;
        }

        FileType _FileType;
        public FileType FileType
        {
            get
            {
                return this._FileType;
            }
            set
            {
                this._FileType = value;
                this.RaisePropertyChanged();
            }
        }

        string _NewExtension;
        public string NewExtension
        {
            get
            {
                return this._NewExtension;
            }
            set
            {
                this._NewExtension = value;
                this.RaisePropertyChanged();
            }
        }

        public ICommand RemoveCommand
        {
            get
            {
                return new DelegateCommand<string>((e) =>
                {
                    this.FileType.ExtensionCollection.Remove(e);
                });
            }
        }

        public ICommand AddCommand
        {
            get
            {
                return new DelegateCommand<string>((e) =>
                {
                    this.FileType.ExtensionCollection.Add(this.NewExtension);
                });
            }
        }

        ObservableCollection<LineBreakMethod> _LineBreakMethodList;
        public ObservableCollection<LineBreakMethod> LineBreakMethodList
        {
            get
            {
                if (this._LineBreakMethodList == null)
                {
                    this._LineBreakMethodList = new ObservableCollection<LineBreakMethod>(EnumListGenerator.GetList<LineBreakMethod>());
                }
                return _LineBreakMethodList;
            }
        }

        public override void OnNavigatedTo(NavigatedToEventArgs e, Dictionary<string, object> viewModelState)
        {
            base.OnNavigatedTo(e, viewModelState);
            string doctype = e.Parameter as string;
            this._FileType = AppSettings.Current.FileTypeCollection.Where((s) => { return s.DocumentTypeName == doctype; }).First();
        }
    }
}

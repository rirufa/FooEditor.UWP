using System.Collections.ObjectModel;
using System.Windows.Input;
using Prism.Windows.Mvvm;
using Prism.Commands;
using FooEditor.UWP.Models;
using Prism.Windows.Navigation;

namespace FooEditor.UWP.ViewModels
{
    class FileTypesPageViewModel : ViewModelBase
    {
        INavigationService NavigationService;

        public FileTypesPageViewModel(INavigationService navigationService)
        {
            this.NavigationService = navigationService;
        }

        ObservableCollection<FileType> _FileTypeCollection = AppSettings.Current.FileTypeCollection;
        public ObservableCollection<FileType> FileTypeCollection
        {
            get
            {
                return this._FileTypeCollection;
            }
            set
            {
                this._FileTypeCollection = value;
                this.RaisePropertyChanged();
            }
        }

        public ICommand ShowDetailCommand
        {
            get
            {
                return new DelegateCommand<FileType>((e) =>
                {
                    NavigationService.Navigate("FileTypeDetail", e.DocumentTypeName);
                });
            }
        }
    }
}

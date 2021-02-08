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
    class DocumentInfoPageViewModel : ViewModelBase
    {
        INavigationService NavigationService;
        IMainViewService MainViewService;
        DocumentCollection _doc_list;
        public DocumentInfoPageViewModel(INavigationService navigationService, IMainViewService mainViewService)
        {
            this._doc_list = DocumentCollection.Instance;
            this.NavigationService = navigationService;
            this.MainViewService = mainViewService;
        }

        public DocumentCollection DocumentList
        {
            get
            {
                return this._doc_list;
            }
            set
            {
                SetProperty(ref this._doc_list, value);
            }
        }

        public ObservableCollection<FileType> FileTypeCollection
        {
            get
            {
                return AppSettings.Current.FileTypeCollection;
            }
        }

        public ObservableCollection<Encoding> EncodeCollection
        {
            get
            {
                return AppSettings.SupportEncodeCollection;
            }
        }

        static ObservableCollection<LineFeedType> lineFeedCollection = new ObservableCollection<LineFeedType>() {
            LineFeedType.CR,
            LineFeedType.CRLF,
            LineFeedType.LF
        };
        public ObservableCollection<LineFeedType> LineFeedCollection
        {
            get
            {
                return lineFeedCollection;
            }
        }

        public Encoding DocumentEncode
        {
            get
            {
                return this._doc_list.Current.Encode;
            }
            set
            {
                if (this._doc_list.Current.FilePath == null)
                {
                    this._doc_list.Current.Encode = value;
                    this.RaisePropertyChanged();
                    return;
                }
                if(this._doc_list.Current.Encode.WebName != value.WebName)
                {
                    var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
                    var taskScheduler = TaskScheduler.FromCurrentSynchronizationContext();
                    var task = this.MainViewService.Confirm(
                        loader.GetString("ReopenConfirm"),
                        loader.GetString("YesButton"),
                        loader.GetString("NoButton"));
                    task.ContinueWith(async (s) => {
                        if (await s == true)
                            await this._doc_list.Current.ReloadFileAsync(value);
                        //ファイルを再読み込みしなかった場合でも呼び出さないといけないらしい
                        this.RaisePropertyChanged();
                    }, taskScheduler);
                }
            }
        }

        public FileType DocumentType
        {
            get
            {
                return this._doc_list.Current.DocumentModel.DocumentType;
            }
            set
            {
                var taskScheduler = TaskScheduler.FromCurrentSynchronizationContext();
                var task = this._doc_list.Current.DocumentModel.SetDocumentType(value);
                task.ContinueWith((s) => {
                    this.RaisePropertyChanged();
                }, taskScheduler);
            }
        }
    }
}

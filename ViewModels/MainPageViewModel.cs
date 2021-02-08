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
    public class MainPageViewModel : ViewModelBase
    {
        INavigationService NavigationService;
        IMainViewService MainViewService;
        DispatcherTimer timer;
        bool IsRequierDelayCleanStatusMessage = false;

        [InjectionConstructor]
        public MainPageViewModel(INavigationService navigationService,IMainViewService mainViewService)
        {
            this.DocumentList = DocumentCollection.Instance;
            this.NavigationService = navigationService;
            this.MainViewService = mainViewService;
        }

        public async Task Init(object param,bool require_restore , Dictionary<string, object> viewModelState)
        {
            this.DocumentList.ActiveDocumentChanged += DocumentList_ActiveDocumentChanged;

            //復元する必要がある
            if (require_restore)
            {
                if (viewModelState != null && viewModelState.Count > 0)
                {
                    if (await this.MainViewService.ConfirmRestoreUserState())
                        await RestoreUserDocument(viewModelState);
                }
            }

            await this.OpenFromArgs(param);

            if (this.DocumentList.Count == 0)
                this.DocumentList.AddNewDocument();
            await this.OnLoadCategories();

            //前回保存したときのごみが残っていることがある
            await DocumentCollection.CleanUp();

            this.timer = new DispatcherTimer();
            this.timer.Interval = new TimeSpan(0, 0, DocumentCollection.TimerTickInterval);
            this.timer.Tick += Timer_Tick;
            this.timer.Start();

        }

        public async Task OpenFromArgs(object args)
        {
            if (args != null)
            {
                ObjectToXmlConverter conv = new ObjectToXmlConverter();
                var files = conv.ConvertBack(args, typeof(string[]), null, null) as string[];
                if (files != null)
                {
                    foreach (string filepath in files)
                    {
                        var file = await FileModel.GetFileModel(FileModelBuildType.AbsolutePath, filepath);
                        await this.DocumentList.AddFromFile(file);
                    }
                }
            }
        }

        private async Task RestoreUserDocument(Dictionary<string, object> viewModelState)
        {
            try
            {
                FileModel file = await FileModel.GetFileModel(FileModelBuildType.LocalFolder, "DocumentCollection.xml");
                await this.DocumentList.RestoreFrom(file);
                if (viewModelState != null && viewModelState.Count > 0)
                {
                    var selIndex = viewModelState["CurrentDocumentIndex"] as int?;
                    if (selIndex != null && selIndex < this.DocumentList.Count)
                        this.DocumentList.ActiveDocument(selIndex.Value);
                }
                await file.DeleteAsync();
                System.Diagnostics.Debug.WriteLine("restored previous document");
            }
            catch (FileNotFoundException)
            {

            }
        }

        private async void Timer_Tick(object sender, object e)
        {
            if(AppSettings.Current.EnableAutoSave)
            {
                //再入されるとまずい
                this.timer.Stop();
                await this.DocumentList.SaveDocumentCollection();
                this.timer.Start();
            }
            if (this.IsRequierDelayCleanStatusMessage)
            {
                this.StatusMessage = string.Empty;
                this.IsRequierDelayCleanStatusMessage = false;
            }
        }

        public async Task Suspend(Dictionary<string, object> viewModelState, bool suspending)
        {
            if (suspending)
            {
                viewModelState["CurrentDocumentIndex"] = this.DocumentList.CurrentDocumentIndex;    //選択中のドキュメントは別途保存する必要がある

                await AppSettings.Current.Save();
            }

            this.DocumentList.ActiveDocumentChanged -= DocumentList_ActiveDocumentChanged;
            if (this.timer != null)
            {
                this.timer.Stop();
                this.timer.Tick -= Timer_Tick;
                this.timer = null;
            }

        }

        private void DocumentList_ActiveDocumentChanged(object sender, DocumentCollectionEventArgs e)
        {
            this.RaisePropertyChanged("CurrentDocument");
            this.RaisePropertyChanged("MaxRow");
            this.RaisePropertyChanged("DocumentType");
            this.RaisePropertyChanged("Encode");
            this.RaisePropertyChanged("LineFeed");
        }

        DocumentCollection _doc_list;
        public DocumentCollection DocumentList
        {
            get
            {
                return this._doc_list;
            }
            set
            {
                this._FindModel.DocumentCollection = value;
                SetProperty(ref this._doc_list, value);
            }
        }

        public DocumentInfoViewModel CurrentDocument
        {
            get
            {
                if (_doc_list == null)
                    return null;
                return this._doc_list.Current;
            }
            set
            {
                if (_doc_list == null)
                    return;
                this._doc_list.ActiveDocument(value);
                this.RaisePropertyChanged();
            }
        }

        public DelegateCommand<object> AddDocumentCommand
        {
            get
            {
                return new DelegateCommand<object>((s) => {
                    this._doc_list.AddNewDocument();
                });
            }
        }

        public DelegateCommand<object> RemoveDocumentCommand
        {
            get
            {
                return new DelegateCommand<object>((s) => {
                    if(this._doc_list.Count > 1)
                    {
                        DocumentInfoViewModel doc = s as DocumentInfoViewModel;
                        this._doc_list.RemoveDocument(doc);
                    }
                });
            }
        }

        public DelegateCommand<System.Text.Encoding> OpenFileCommand
        {
            get
            {
                return new DelegateCommand<System.Text.Encoding>(async (s) => {
                    try
                    {
                        await this._doc_list.AddFromFilePicker(s);
                    }catch(Exception ex)
                    {
                        await this.MainViewService.MakeMessageBox(ex.Message);
                    }
                });
            }
        }

        public DelegateCommand<IEnumerable<string>> OpenFilePathCommand
        {
            get
            {
                return new DelegateCommand<IEnumerable<string>>(async (s) => {
                    if (s == null)
                        return;
                    try
                    {
                        foreach (var file_path in s)
                        {
                            var file = await FileModel.GetFileModel(FileModelBuildType.AbsolutePath, file_path);
                            await this.DocumentList.AddFromFile(file);
                        }
                    }
                    catch (Exception ex)
                    {
                        await this.MainViewService.MakeMessageBox(ex.Message);
                    }
                });
            }
        }

        public DelegateCommand<IEnumerable<IStorageItem>> OpenFilesCommand
        {
            get
            {
                return new DelegateCommand<IEnumerable<IStorageItem>>(async (s) => {
                    if (s == null)
                        return;
                    try
                    {
                        foreach (StorageFile file in s)
                        {
                            await this.DocumentList.AddFromFile(file);
                        }
                    }
                    catch (Exception ex)
                    {
                        await this.MainViewService.MakeMessageBox(ex.Message);
                    }
                });
            }
        }

        public DelegateCommand<object> SaveCommand
        {
            get
            {
                return new DelegateCommand<object>(async (s) => {
                    if (this._doc_list.Current.DocumentModel.CurrentFilePath == null)
                    {
                        await SaveAs(null);
                        return;
                    }
                    try
                    {
                        var fileModel = await FileModel.GetFileModel(FileModelBuildType.AbsolutePath, this._doc_list.Current.DocumentModel.CurrentFilePath);
                        if (fileModel != null)
                        {
                            if (fileModel.IsNeedUserActionToSave())
                                await this.SaveAs(fileModel);
                            else
                            {
                                await this._doc_list.Current.DocumentModel.SaveFile(fileModel, null);
                                var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
                                var str = string.Format(loader.GetString("NotifySaveCompleteText"), fileModel.Name);
                                this.StatusMessage = str;
                                this.IsRequierDelayCleanStatusMessage = true;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        await this.MainViewService.MakeMessageBox(ex.Message);
                    }
                });
            }
        }

        public DelegateCommand<System.Text.Encoding> SaveAsCommand
        {
            get
            {
                return new DelegateCommand<System.Text.Encoding>(async (enc) => {
                    if (this._doc_list.Current.DocumentModel.CurrentFilePath == null)
                    {
                        await SaveAs(null, enc);
                    }
                    else
                    {
                        var fileModel = await FileModel.GetFileModel(FileModelBuildType.AbsolutePath, this._doc_list.Current.DocumentModel.CurrentFilePath);
                        await SaveAs(fileModel, enc);
                    }
                });
            }
        }

        private async Task SaveAs(FileModel suggestFile, System.Text.Encoding enc = null)
        {
            FileSavePicker savePicker = new FileSavePicker();

            //これをつけないとファイルダイアログで拡張子を変えることができなくなる
            List<string> currentFileTypes = new List<string> { "." };
            if (suggestFile == null)
            {
                savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            }
            else
            {
                savePicker.SuggestedSaveFile = suggestFile;
                //現在開いているファイルの拡張子を追加すると未知の拡張子でもファイルダイアログに表示される
                currentFileTypes.Add(suggestFile.Extension);
            }
            savePicker.FileTypeChoices.Add("Current File Type", currentFileTypes);

            ObservableCollection<FileType> collection = AppSettings.Current.FileTypeCollection;
            foreach (FileType type in collection)
            {
                savePicker.FileTypeChoices.Add(type.DocumentTypeName, type.ExtensionCollection);
            }

            StorageFile file = await savePicker.PickSaveFileAsync();
            if (file != null)
            {
                FileModel fileModel = await FileModel.GetFileModel(file);
                this.timer.Stop();
                await this._doc_list.Current.DocumentModel.SaveFile(fileModel,enc);
                this.timer.Start();
                this._doc_list.Current.Title = file.Name;
                if(enc != null)
                    this._doc_list.Current.Encode = enc;
                StorageApplicationPermissions.MostRecentlyUsedList.Add(file, file.Name);

                var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
                var str = string.Format(loader.GetString("NotifySaveCompleteText"), file.Name);
                this.StatusMessage = str;
                this.IsRequierDelayCleanStatusMessage = true;
            }
        }

        public DelegateCommand<object> UndoCommand
        {
            get
            {
                return new DelegateCommand<object>((param) =>
                {
                    this._doc_list.Current.DocumentModel.Document.UndoManager.undo();
                    this._doc_list.Current.DocumentModel.Document.RequestRedraw();
                });
            }
        }

        public DelegateCommand<object> RedoCommand
        {
            get
            {
                return new DelegateCommand<object>((param) =>
                {
                    this._doc_list.Current.DocumentModel.Document.UndoManager.redo();
                    this._doc_list.Current.DocumentModel.Document.RequestRedraw();
                });
            }
        }

        public DelegateCommand<object> GlobalSettingCommand
        {
            get
            {
                return new DelegateCommand<object>((s) => {
                    NavigationService.Navigate("GlobalSetting", null);
                    this.IsNavPaneOpen = true;
                });
            }
        }

        public DelegateCommand<object> FileTypeSettingCommand
        {
            get
            {
                return new DelegateCommand<object>((s) => {
                    NavigationService.Navigate("FileTypes", null);
                    this.IsNavPaneOpen = true;
                });
            }
        }

        public DelegateCommand<object> PrintSettingCommand
        {
            get
            {
                return new DelegateCommand<object>((s) => {
                    NavigationService.Navigate("PrintSettings", null);
                    this.IsNavPaneOpen = true;
                });
            }
        }

        public DelegateCommand<object> AboutPageCommand
        {
            get
            {
                return new DelegateCommand<object>((s) => {
                    NavigationService.Navigate("About", null);
                    this.IsNavPaneOpen = true;
                });
            }
        }

        public DelegateCommand<object> PrintCommand
        {
            get
            {
                return new DelegateCommand<object>(async (s) => {
                    try
                    {
                        await PrintManager.ShowPrintUIAsync();
                    }
                    catch
                    {
                        System.Diagnostics.Debug.WriteLine("Printer is not stanby");
                    }
                });
            }
        }

        public DelegateCommand<object> CloseSideBarCommand
        {
            get
            {
                return new DelegateCommand<object>(async (s) => {
                    this.IsNavPaneOpen = false;
                });
            }
        }

        string _Result;
        public string Result
        {
            get
            {
                return this._Result;
            }
            set
            {
                SetProperty(ref this._Result, value);
            }
        }

        string _StatusMessage;
        public string StatusMessage
        {
            get
            {
                return this._StatusMessage;
            }
            set
            {
                SetProperty(ref this._StatusMessage, value);
            }
        }

        #region FindAndReplace
        FindModel _FindModel = new FindModel();

        string _FindPattern;
        public string FindPattern
        {
            get
            {
                return this._FindPattern;
            }
            set
            {
                this._FindPattern = value;
                this.RaisePropertyChanged();
            }
        }

        string _SelectedFindPattern;
        public string SelectedFindPattern
        {
            get
            {
                return this._SelectedFindPattern;
            }
            set
            {
                this._SelectedFindPattern = value;
                this.FindPattern = value;
                this.RaisePropertyChanged();
            }
        }

        ObservableCollection<string> _FindHistroy;
        public ObservableCollection<string> FindHistroy
        {
            get
            {
                return this._FindHistroy;
            }
            set
            {
                this._FindHistroy = value;
                this.RaisePropertyChanged();
            }
        }

        string _ReplacePattern;
        public string ReplacePattern
        {
            get
            {
                return this._ReplacePattern;
            }
            set
            {
                this._ReplacePattern = value;
                this.RaisePropertyChanged();
            }
        }

        bool _UseRegEx;
        public bool UseRegEx
        {
            get
            {
                return this._UseRegEx;
            }
            set
            {
                this._UseRegEx = value;
                this._FindModel.Reset();
                this.RaisePropertyChanged();
            }
        }

        bool _RestrictSearch;
        public bool RestrictSearch
        {
            get
            {
                return this._RestrictSearch;
            }
            set
            {
                this._RestrictSearch = value;
                this._FindModel.Reset();
                this.RaisePropertyChanged();
            }
        }

        bool _UseGroup;
        public bool UseGroup
        {
            get
            {
                return this._UseGroup;
            }
            set
            {
                this._UseGroup = value;
                this.RaisePropertyChanged();
            }
        }

        public bool AllDocuments
        {
            get
            {
                return this._FindModel.AllDocuments;
            }
            set
            {
                this._FindModel.AllDocuments = value;
                this.RaisePropertyChanged();
            }
        }

        public DelegateCommand<object> FindNextCommand
        {
            get
            {
                return new DelegateCommand<object>((s) =>
                {
                    this.Result = string.Empty;
                    try
                    {
                        this.AddFindHistory(this.FindPattern);

                        RegexOptions opt = this.RestrictSearch ? RegexOptions.None : RegexOptions.IgnoreCase;
                        this._FindModel.FindNext(this.FindPattern, this.UseRegEx, opt);
                    }
                    catch(NotFoundExepction)
                    {
                        var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
                        this.Result = loader.GetString("NotFoundInDocument");
                    }
                    catch (Exception e)
                    {
                        this.Result = e.Message;
                    }
                });
            }
        }

        public DelegateCommand<object> ReplaceNextCommand
        {
            get
            {
                return new DelegateCommand<object>((s) =>
                {
                    this.Result = string.Empty;
                    try
                    {
                        this._FindModel.Replace(this.ReplacePattern, this.UseGroup);
                        RegexOptions opt = this.RestrictSearch ? RegexOptions.None : RegexOptions.IgnoreCase;
                        this._FindModel.FindNext(this.FindPattern, this.UseRegEx, opt);
                    }
                    catch (NotFoundExepction)
                    {
                        var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
                        this.Result = loader.GetString("NotFoundInDocument");
                    }
                    catch (Exception e)
                    {
                        this.Result = e.Message;
                    }
                });
            }
        }

        public DelegateCommand<object> ReplaceAllCommand
        {
            get
            {
                return new DelegateCommand<object>((s) => {
                    this.Result = string.Empty;
                    try
                    {
                        this.AddFindHistory(this.FindPattern);

                        RegexOptions opt = this.RestrictSearch ? RegexOptions.None : RegexOptions.IgnoreCase;
                        this._FindModel.ReplaceAll(this.FindPattern, this.ReplacePattern, this.UseGroup, this.UseRegEx, opt);
                    }
                    catch (Exception e)
                    {
                        this.Result = e.Message;
                    }
                });
            }
        }

        void AddFindHistory(string pattern)
        {
            if (this.FindHistroy == null)
                return;
            if (!this.FindHistroy.Contains(this.FindPattern))
                this.FindHistroy.Add(this.FindPattern);
        }
        #endregion

        #region GoTo
        int _ToRow;
        public int ToRow
        {
            get
            {
                return this._ToRow;
            }
            set
            {
                this._ToRow = value;
                this.RaisePropertyChanged();
            }
        }

        public int MaxRow
        {
            get
            {
                return this._doc_list.Current.DocumentModel.Document.LayoutLines.Count;
            }
        }

        public DelegateCommand<object> JumpLineCommand
        {
            get
            {
                return new DelegateCommand<object>((s) =>
                {
                    var newPostion = new FooEditEngine.TextPoint() ;
                    newPostion.row = this.ToRow;
                    newPostion.col = 0;
                    var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
                    if (this.ToRow > MaxRow)
                    {
                        this.Result = string.Format(loader.GetString("LineNumberOutOutOfRange"), 1, this.MaxRow);
                        return;
                    }
                    this._doc_list.Current.DocumentModel.Document.CaretPostion = newPostion;
                    this._doc_list.Current.DocumentModel.Document.RequestRedraw();
                });
            }
        }
        #endregion

        #region DocumentProperty
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
                },taskScheduler);
            }
        }

        #endregion

        #region Panel
        bool _IsOutlineOpen;
        public bool IsNavPaneOpen
        {
            get
            {
                return this._IsOutlineOpen;
            }
            set
            {
                SetProperty(ref this._IsOutlineOpen, value);
            }
        }
        #endregion

        #region Snippet
        ObservableCollection<SnippetCategoryViewModel> _CategoryList;
        public ObservableCollection<SnippetCategoryViewModel> CategoryList
        {
            get
            {
                return this._CategoryList;
            }
            set
            {
                SetProperty(ref _CategoryList, value);
            }
        }

        SnippetCategoryViewModel _CurrentCategory;
        public SnippetCategoryViewModel CurrentCategory
        {
            get
            {
                return this._CurrentCategory;
            }
            set
            {
                SetProperty(ref _CurrentCategory, value);
                this.OnChangeCategory();
            }
        }

        ObservableCollection<SnippetViewModel> _Snippets;
        public ObservableCollection<SnippetViewModel> Snippets
        {
            get
            {
                return this._Snippets;
            }
            set
            {
                SetProperty(ref this._Snippets, value);
            }
        }

        SnippetViewModel _SelectSnippet;
        public SnippetViewModel SelectSnippet
        {
            get
            {
                return this._SelectSnippet;
            }
            set
            {
                SetProperty(ref _SelectSnippet, value);
            }
        }

        private async Task OnLoadCategories()
        {
            this.CategoryList = await SnipeetLoader.LoadCategory();
        }

        private async void OnChangeCategory()
        {
            this.Snippets = await SnipeetLoader.LoadSnippets(this.CurrentCategory.FilePath);
        }

        public DelegateCommand<object> InsertSnippetCommand
        {
            get
            {
                return new DelegateCommand<object>((s) => {
                    this.SelectSnippet.InsetToDocument(this.DocumentList.Current.DocumentModel);
                });
            }
        }
        #endregion
    }
}

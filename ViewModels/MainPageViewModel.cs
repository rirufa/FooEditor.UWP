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

        public const string KeywordFolderName = "Keywords";

        public async Task Init(object param,bool require_restore , Dictionary<string, object> viewModelState)
        {
            this.DocumentList.ActiveDocumentChanged += DocumentList_ActiveDocumentChanged;

            await FolderModel.CopyFilesFromInstalledFolderToLocalSetting(KeywordFolderName);

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
                await this.DocumentList.SaveDocumentCollection();
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
            this.DocumentList.Current.OnActivate();
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
                    try
                    {
                        if (this._doc_list.Current.DocumentModel.CurrentFilePath == null)
                        {
                            await SaveAs(null, enc);
                        }
                        else
                        {
                            var fileModel = await FileModel.GetFileModel(FileModelBuildType.AbsolutePath, this._doc_list.Current.DocumentModel.CurrentFilePath);
                            await SaveAs(fileModel, enc);
                        }
                    }
                    catch (Exception ex)
                    {
                        await this.MainViewService.MakeMessageBox(ex.Message);
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
        public DelegateCommand<object> OpenSettingPageCommand
        {
            get
            {
                return new DelegateCommand<object>((s) => {
                    NavigationService.Navigate("Setting", null);
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
                return new DelegateCommand<object>((s) => {
                    this.NavigationService.ClearHistory();
                    this.IsNavPaneOpen = false;
                });
            }
        }

        public DelegateCommand<object> OpenOutlineCommand
        {
            get
            {
                return new DelegateCommand<object>((s) => {
                    this.NavigationService.Navigate("Outline", null);
                    this.IsNavPaneOpen = true;
                });
            }
        }

        public DelegateCommand<object> OpenFindAndReplaceCommand
        {
            get
            {
                return new DelegateCommand<object>((s) => {
                    this.NavigationService.Navigate("FindReplace", null);
                    this.IsNavPaneOpen = true;
                });
            }
        }

        public DelegateCommand<object> OpenGoToCommand
        {
            get
            {
                return new DelegateCommand<object>((s) => {
                    this.NavigationService.Navigate("Goto", null);
                    this.IsNavPaneOpen = true;
                });
            }
        }

        public DelegateCommand<object> OpenSnipeetCommand
        {
            get
            {
                return new DelegateCommand<object>((s) => {
                    this.NavigationService.Navigate("Snipeet", null);
                    this.IsNavPaneOpen = true;
                });
            }
        }

        public DelegateCommand<object> OpenDocumentInfoCommand
        {
            get
            {
                return new DelegateCommand<object>((s) => {
                    this.NavigationService.Navigate("DocumentInfo", null);
                    this.IsNavPaneOpen = true;
                });
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

    }
}

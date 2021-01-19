using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.AccessCache;
using Windows.Storage.Pickers;
using FooEditor.UWP.ViewModels;

namespace FooEditor.UWP.Models
{
    public class DocumentCollectionEventArgs
    {
        public int ActiveIndex;
        public DocumentCollectionEventArgs(int index)
        {
            this.ActiveIndex = index;
        }
    }

    [CollectionDataContract]
    public class DocumentCollection : ObservableCollection<DocumentInfoViewModel>
    {
        int _CurrentDocumentIndex;

        private DocumentCollection()
        {
            this.Initialize();
        }

        private DocumentCollection(IEnumerable<DocumentInfoViewModel> models)
            : base(models)
        {
            this.Initialize();
        }

        private void Initialize()
        {
            this.ActiveDocumentChanged += (s, e) => { };
            this.CollectionChanged += (s, e) => { };
            this.PropertyChanged += (s, e) => { };
        }

        static DocumentCollection _Instance;
        public static DocumentCollection Instance
        {
            get
            {
                if (_Instance == null)
                    _Instance = new DocumentCollection();
                return _Instance;
            }
        }

        public DocumentInfoViewModel Current
        {
            get
            {
                if (this.Count == 0)
                    return null;
                else
                    return this[_CurrentDocumentIndex];
            }
        }

        public int CurrentDocumentIndex
        {
            get
            {
                return _CurrentDocumentIndex;
            }
        }

        public const int TimerTickInterval = 3;

        public bool hasDirtyDoc
        {
            get
            {
                var nowTick = DateTime.Now.Ticks;
                var dirtyDocuments = this
                    .Select((s) => { return s; })
                    .Where((s) => {
                        return s.DocumentModel.hasUnAutoSavedDocument && nowTick - s.DocumentModel.lastUpdatedTickCount >= TimerTickInterval * 10000000;
                    });
                return dirtyDocuments.Count() > 0;
            }
        }

        public event EventHandler<DocumentCollectionEventArgs> ActiveDocumentChanged;

        public async Task AddFromFile(StorageFile file,System.Text.Encoding enc = null)
        {
            if (file != null)
            {
                if (this.ActiveDocument(file.Path))
                    return;

                DocumentInfoViewModel info = new DocumentInfoViewModel(file.Name);
                this.AddDocument(info);
                await info.DocumentModel.LoadFile(await FileModel.GetFileModel(file), enc);
                StorageApplicationPermissions.MostRecentlyUsedList.Add(file, "mrufile");
            }
        }

        public async Task AddFromFilePicker(System.Text.Encoding enc = null)
        {
            FileOpenPicker openPicker = new FileOpenPicker();

            openPicker.ViewMode = PickerViewMode.List;

            openPicker.FileTypeFilter.Add("*");

            openPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            StorageFile file = await openPicker.PickSingleFileAsync();
            if (file != null)
            {
                await this.AddFromFile(file, enc);
            }
        }

        public static async Task CleanUp()
        {
            var files = await ApplicationData.Current.LocalFolder.GetFilesAsync();
            foreach (var file in files)
            {
                if (file.Name.IndexOf(DocumentModel.prefixFileName) != -1 || file.Name == collection_name)
                    await file.DeleteAsync();
            }
        }

        public void RemoveDocument(DocumentInfoViewModel param)
        {
            int index = this.IndexOf(param);
            if (index != -1)
            {
                param.DocumentModel.Document.Dispose();

                this.RemoveAt(index);

                if (this.Count <= 0)
                    return;

                int new_active_index = this.CurrentDocumentIndex;

                //削除しようとしたドキュメントが現在アクティブもしくはアクティブなドキュメントより前にある
                if (index <= this.CurrentDocumentIndex)
                    new_active_index = this.CurrentDocumentIndex - 1;

                //計算の結果、負の値になったら、先頭に設定する
                if (new_active_index < 0)
                    new_active_index = 0;

                this.ActiveDocument(new_active_index);
            }
        }

        public void AddDocument(DocumentInfoViewModel info)
        {
            this.Add(info);
            this.ActiveDocument(info);
        }

        public void AddNewDocument()
        {
            var doc = new DocumentInfoViewModel();
            doc.Title = string.Format("Untitled{0}", this.Count + 1);
            this.AddDocument(doc);
        }

        public void Next()
        {
            int selIndex = this._CurrentDocumentIndex + 1;
            if (selIndex > this.Count - 1)
                selIndex = 0;
            this.ActiveDocument(selIndex);
        }

        public void Prev()
        {
            int selIndex = this._CurrentDocumentIndex - 1;
            if (selIndex < 0)
                selIndex = this.Count - 1;
            this.ActiveDocument(selIndex);
        }

        public void ActiveDocument(int index)
        {
            if (index == -1)
                return;
            DocumentInfoViewModel info = this[index];
            info.ApplyCurrentSetting();
            _CurrentDocumentIndex = index;
            this.ActiveDocumentChanged(this, new DocumentCollectionEventArgs(index));
        }

        public void ActiveDocument(DocumentInfoViewModel info)
        {
            this.ActiveDocument(this.IndexOf(info));
        }

        public bool ActiveDocument(string file_path)
        {
            foreach (DocumentInfoViewModel doc_info in this)
            {
                if (doc_info.FilePath == file_path)
                {
                    this.ActiveDocument(doc_info);
                    return true;
                }
            }
            return false;
        }

        public static string collection_name = "DocumentCollection.xml";

        public async Task SaveDocumentCollection()
        {
            if (this.Count > 0 && this.hasDirtyDoc)
            {
                FileModel file = await FileModel.CreateFileModel(collection_name, true);
                using (Stream fs = await file.GetWriteStreamAsync())
                {
                    //タイミングによってはスナップショットを取らないと落ちる
                    var snapshot = new DocumentCollection(this);
                    DataContractSerializer serializer = new DataContractSerializer(typeof(DocumentCollection));
                    serializer.WriteObject(fs, snapshot);
                    foreach (var doc in this)
                    {
                        await doc.DocumentModel.SaveCurrentFile();
                    }
                    System.Diagnostics.Debug.WriteLine("AutoSaved");
                }
            }
        }

        public async Task RestoreFrom(FileModel file)
        {
            this.Clear();
            using (Stream fs = await file.GetReadStreamAsync())
            {
                DataContractSerializer serializer = new DataContractSerializer(typeof(DocumentCollection));
                DocumentCollection newDocumentCollection = (DocumentCollection)serializer.ReadObject(fs);
                foreach (var doc in newDocumentCollection)
                {
                    await doc.DocumentModel.RestoreCurrentFile();
                    this.Add(doc);
                }
            }
        }
    }
}

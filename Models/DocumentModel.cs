using System;
using System.IO;
using System.Collections.ObjectModel;
using System.Text;
using System.Threading.Tasks;
using FooEditEngine;
using FooEditEngine.UWP;
using EncodeDetect;
using Prism.Mvvm;

namespace FooEditor.UWP.Models
{
    public class DocumentTypeEventArg
    {
        /// <summary>
        /// 適用すべきハイライター。nullならクリアする必要がある
        /// </summary>
        public IHilighter hilighter;
        /// <summary>
        /// 適用すべき折り畳み。nullならクリアする必要がある
        /// </summary>
        public IFoldingStrategy folding;
        /// <summary>
        /// 新しく設定されたファイルタイプ
        /// </summary>
        public FileType newFileType;

        public DocumentTypeEventArg(FileType newFileType)
        {
            this.newFileType = newFileType;
        }
    }

    public class DocumentModel : BindableBase
    {
        public string CurrentFilePath
        {
            get;
            set;
        }
        public string Title
        {
            get;
            set;
        }

        bool _IsProgressNow;
        public bool IsProgressNow
        {
            get
            {
                return this._IsProgressNow;
            }
            set
            {
                this._IsProgressNow = value;
                this.RaisePropertyChanged();
            }
        }

        public bool IsDirty
        {
            get
            {
                return this.Document.Dirty;
            }
            private set
            {
                this.Document.Dirty = value;
                this.RaisePropertyChanged();
            }
        }

        public Document Document
        {
            get;
            set;
        }

        public FileType DocumentType
        {
            get;
            private set;
        }

        public Encoding Encode
        {
            get;
            set;
        }

        public LineFeedType LineFeed
        {
            get;
            set;
        }

        public long lastUpdatedTickCount
        {
            get;
            private set;
        }

        public bool hasUnAutoSavedDocument
        {
            get;
            private set;
        }

        public DocumentModel()
        {
            this.Document = new Document();
            this.Document.InsertMode = true;
            this.Document.Update += (s, e) => {
                this.hasUnAutoSavedDocument = true;
                this.IsDirty = true;
                this.lastUpdatedTickCount = DateTime.Now.Ticks;
            };
            this.Document.AutoIndentHook += (s, e) =>
            {
                this.autoIndent.InsertIndent(this.Document);
            };
            this.Encode = AppSettings.Current.DefaultEncoding;
            this.LineFeed = LineFeedType.CRLF;
            this.hasUnAutoSavedDocument = true;
            this.IsDirty = false;
            this.IsProgressNow = false;
            this.DocumentTypeChanged += (s, e) => { };
        }

        public event EventHandler<DocumentTypeEventArg> DocumentTypeChanged;

        AutoIndent autoIndent = new AutoIndent();

        string KeywordFolderName = "Keywords";

        public async Task SetDocumentType(FileType type)
        {
            this.DocumentType = type;

            this.hasUnAutoSavedDocument = true; //メタデータを更新する必要がある

            var arg = new DocumentTypeEventArg(type);

            if (type == null)
            {
                this.DocumentTypeChanged(this, arg);
                return;
            }else if(string.IsNullOrEmpty(type.DocumentType))
            {
                if (type.DocumentTypeName == AppSettings.TextType && type.EnableGenerateFolding)
                    arg.folding = new WZTextFoldingGenerator();
                this.DocumentTypeChanged(this, arg);
                return;
            }

            await FolderModel.CopyFilesFromInstalledFolderToLocalSetting(KeywordFolderName);

            FileModel file;
            FolderModel keywordFolder = await FolderModel.TryGetFolderFromAppSetting(KeywordFolderName);
            if (keywordFolder == null)
            {
                this.DocumentTypeChanged(this, new DocumentTypeEventArg(type));
                return;
            }
            file = await keywordFolder.GetFile(type.DocumentType);

            var tuple = EditorHelper.GetFoldingAndHilight(file.Path);
            if (type.EnableSyntaxHilight)
                arg.hilighter = tuple.Item2;
            if (type.EnableGenerateFolding)
                arg.folding = tuple.Item1;


            GenericHilighter genericHilighter = arg.hilighter as GenericHilighter;
            if (genericHilighter != null)
            {
                var syntax_def = genericHilighter.KeywordManager;
                this.autoIndent.IndentStart = syntax_def.IntendStart;
                this.autoIndent.IndentEnd = syntax_def.IntendEnd;

                var AutoComplete = new AutoCompleteBox(this.Document);
                AutoComplete.Items = new CompleteCollection<ICompleteItem>();
                AutoComplete.Enabled = AppSettings.Current.EnableAutoComplete;
                GenericCompleter.LoadKeywords(AutoComplete, syntax_def);
                this.Document.AutoComplete = AutoComplete;
            }

            XmlHilighter xmlHilighter = arg.hilighter as XmlHilighter;
            if(xmlHilighter != null)
            {
                var syntax_def = xmlHilighter.KeywordManager;
                var AutoComplete = new AutoCompleteBox(this.Document);
                AutoComplete.Items = new CompleteCollection<ICompleteItem>();
                AutoComplete.Enabled = AppSettings.Current.EnableAutoComplete;
                XmlCompleter.LoadKeywords(AutoComplete, syntax_def);
                this.Document.AutoComplete = AutoComplete;
            }

            this.DocumentTypeChanged(this, arg);
        }

        private FileType GetFileType(string file)
        {
            ObservableCollection<FileType> collection = AppSettings.Current.FileTypeCollection;
            foreach (FileType type in collection)
                foreach (string ext in type.ExtensionCollection)
                    if (Path.GetExtension(file) == ext)
                        return type;
            return null;
        }

        public async Task LoadFile(FileModel file, Encoding enc = null)
        {
            this.IsProgressNow = true;
            try
            {
                await this.LoadFileImpl(file, enc);
            }
            finally
            {
                this.CurrentFilePath = file.Path;
                this.IsProgressNow = false;
            }
        }

        private async Task LoadFileImpl(FileModel file, Encoding enc = null)
        {
            if (enc == null)
            {
                using (Stream stream = await file.GetReadStreamAsync())
                {
                    enc = await DectingEncode.GetEncodingAsync(stream);
                }
                if (enc == null)
                {
                    enc = AppSettings.Current.DefaultEncoding;
                }
                else if(enc == Encoding.ASCII)
                {
                    var default_enc = AppSettings.Current.DefaultEncoding;
                    //UTF32とUTF16以外はASCIIコードを含むので上位互換の奴で読み込んでも問題ない
                    if(default_enc != Encoding.UTF32 && default_enc != Encoding.Unicode)
                        enc = AppSettings.Current.DefaultEncoding;
                    else
                        enc = Encoding.Default;
                }
            }
            this.Encode = enc;

            using (Stream stream = await file.GetReadStreamAsync())
            {
                this.LineFeed = await LineFeedHelper.GetLineFeedAsync(stream, enc);
            }

            await this.SetDocumentType(GetFileType(file.Name));

            using (var stream = await file.GetReadStreamAsync())
            using (var sr = new StreamReader(stream, enc))
            {
                await this.Document.LoadAsync(sr, null, (int)file.Length);
            }

            this.IsDirty = false;
            this.hasUnAutoSavedDocument = true; //フラグを立てないと中断した時に保存されない
#if TEST_PROGRESS
                await Task.Delay(10000);
#endif
        }

        public async Task ReloadFile(Encoding enc)
        {
            if (this.CurrentFilePath == null)
                return;
            this.Encode = enc;
            var currentFile = await FileModel.GetFileModel(FileModelBuildType.AbsolutePath,this.CurrentFilePath);
            using (var stream = await currentFile.GetReadStreamAsync())
            using (var sr = new StreamReader(stream, enc))
            {
                await this.Document.LoadAsync(sr);
            }
            this.IsDirty = false;
        }

        public async Task SaveFile(FileModel file,Encoding enc = null)
        {
            using (var stream = await file.GetWriteStreamAsync())
            using (var sw = new StreamWriter(stream, enc == null ? this.Encode : enc))
            {
                stream.SetLength(0);    //ゴミが残るのを回避する
                sw.NewLine = LineFeedHelper.ToString(this.LineFeed);
                await this.Document.SaveAsync(sw);
            }
            this.CurrentFilePath = file.Path;
            this.IsDirty = false;
            this.hasUnAutoSavedDocument = true; //メタデータを更新する必要がある
        }

        public const string prefixFileName = "save_";

        private string stateFileName
        {
            get
            {
                return (prefixFileName + this.CurrentFilePath + this.Title).Replace(':','_').Replace('\\', '_');
            }
        }

        public async Task RestoreCurrentFile()
        {
            FileModel file = await FileModel.GetFileModel(FileModelBuildType.LocalFolder,stateFileName);
            await this.LoadFileImpl(file);
            await file.DeleteAsync();
            this.Document.RequestRedraw();
            //ハイライトなどの情報をまだ反映されていないので反映させる
            await this.SetDocumentType(this.DocumentType);
        }

        public async Task SaveCurrentFile()
        {
            if (!hasUnAutoSavedDocument)
                return;
            FileModel file = await FileModel.CreateFileModel(stateFileName, true);
            using (var stream = await file.GetWriteStreamAsync())
            using (var sw = new StreamWriter(stream, this.Encode))
            {
                sw.NewLine = LineFeedHelper.ToString(this.LineFeed);
                await this.Document.SaveAsync(sw);
            }
            hasUnAutoSavedDocument = false;
        }

        public const int FoundMarkerID = 4;

        public void ApplyCurrentSetting()
        {
            AppSettings setting = AppSettings.Current;
            setting.FileType = this.DocumentType;   //ドキュメントが切り替わった場合にも呼ばれるのでドキュメントのタイプを設定しておく
            if (this.Document.HideLineMarker != !setting.ShowLineMarker)
                this.Document.HideLineMarker = !setting.ShowLineMarker;
            if (this.Document.TabStops != setting.TabChar)
                this.Document.TabStops = setting.TabChar;
            if (this.Document.RightToLeft != setting.IsRTL)
                this.Document.RightToLeft = setting.IsRTL;
            if (!setting.ShowFoundPattern)
                this.Document.MarkerPatternSet.Remove(FoundMarkerID);
            IndentMode indentmode = setting.IndentBySpace ? IndentMode.Space : IndentMode.Tab;
            if (this.Document.IndentMode != indentmode)
                this.Document.IndentMode = indentmode;
            if (this.Document.HideRuler != !setting.ShowRuler)
                this.Document.HideRuler = !setting.ShowRuler;
            if (this.Document.DrawLineNumber != setting.ShowLineNumber)
                this.Document.DrawLineNumber = setting.ShowLineNumber;
            if (this.Document.ShowFullSpace != setting.ShowFullSpace)
                this.Document.ShowFullSpace = setting.ShowFullSpace;
            if (this.Document.ShowTab != setting.ShowTab)
                this.Document.ShowTab = setting.ShowTab;
            if (this.Document.ShowLineBreak != setting.ShowLineBreak)
                this.Document.ShowLineBreak = setting.ShowLineBreak;
            if (this.Document.IndentMode != indentmode)
                this.Document.IndentMode = indentmode;
            if (this.autoIndent.Enable != setting.EnableAutoIndent)
                this.autoIndent.Enable = setting.EnableAutoIndent;
            if (this.Document.AutoComplete != null && this.Document.AutoComplete.Enabled != setting.EnableAutoComplete)
                this.Document.AutoComplete.Enabled = setting.EnableAutoComplete;
            bool rebuildLayout = false;
            if (this.Document.LineBreak != setting.LineBreakMethod)
            {
                this.Document.LineBreak = setting.LineBreakMethod;
                rebuildLayout = true;
            }
            if (this.Document.LineBreakCharCount != setting.LineBreakCount)
            {
                this.Document.LineBreakCharCount = setting.LineBreakCount;
                rebuildLayout = true;
            }
            if (rebuildLayout)
                this.Document.PerformLayout();
            this.Document.RequestRedraw();
        }

        public void ReadXml(System.Xml.XmlReader reader)
        {
            reader.ReadStartElement("DocumentModel");

            reader.ReadStartElement("Title");
            this.Title = reader.ReadContentAsString();
            reader.ReadEndElement();

            reader.ReadStartElement("CaretPostionRow");
            int row = reader.ReadContentAsInt();
            reader.ReadEndElement();

            reader.ReadStartElement("CaretPostionCol");
            int col = reader.ReadContentAsInt();
            reader.ReadEndElement();

            if (reader.IsEmptyElement)
            {
                reader.Skip();
            }
            else
            {
                reader.ReadStartElement("DocumentType");
                var documentType = reader.ReadContentAsString();
                foreach (FileType type in AppSettings.Current.FileTypeCollection)
                {
                    if (type.DocumentType == documentType)
                    {
                        this.DocumentType = type;
                    }
                }
                reader.ReadEndElement();
            }

            try
            {
                reader.ReadStartElement("FilePath");
                this.CurrentFilePath = reader.ReadContentAsString();
                reader.ReadEndElement();
            }
            catch (System.Xml.XmlException)
            {
                //存在しない可能性があるので無視する
            }
            catch(UnauthorizedAccessException)
            {
                //MRUに存在しないファイルはそもそもアクセスできないので、内容だけ復元する
            }
        }

        public void WriteXml(System.Xml.XmlWriter writer)
        {
            writer.WriteStartElement("DocumentModel");

            writer.WriteStartElement("Title");
            writer.WriteValue(this.Title);
            writer.WriteEndElement();

            writer.WriteStartElement("CaretPostionRow");
            writer.WriteValue(this.Document.CaretPostion.row);
            writer.WriteEndElement();

            writer.WriteStartElement("CaretPostionCol");
            writer.WriteValue(this.Document.CaretPostion.col);
            writer.WriteEndElement();

            writer.WriteStartElement("DocumentType");
            writer.WriteValue(this.DocumentType == null ? string.Empty : this.DocumentType.DocumentType);
            writer.WriteEndElement();

            if (this.CurrentFilePath != null)
            {
                writer.WriteStartElement("FilePath");
                writer.WriteValue(this.CurrentFilePath);
                writer.WriteEndElement();
            }

            writer.WriteEndElement();
        }
    }
}

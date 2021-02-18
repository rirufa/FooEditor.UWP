using System;
using System.Threading.Tasks;
using System.Xml.Serialization;
using FooEditEngine;
using FooEditEngine.UWP;
using FooEditor.UWP.Models;
using Prism.Windows.Mvvm;
using UI = Windows.UI;
using Windows.UI.Xaml.Media;

namespace FooEditor.UWP.ViewModels
{
    public class DocumentInfoViewModel : ViewModelBase, IXmlSerializable
    {
        public String Title
        {
            get
            {
                return this.DocumentModel.Title;
            }
            set
            {
                this.DocumentModel.Title = value;
                this.RaisePropertyChanged();
            }
        }

        public System.Text.Encoding Encode
        {
            get
            {
                return this.DocumentModel.Encode;
            }
            set
            {
                this.DocumentModel.Encode = value;
                this.RaisePropertyChanged();
            }
        }

        public EncodeDetect.LineFeedType LineFeed
        {
            get
            {
                return this.DocumentModel.LineFeed;
            }
            set
            {
                this.DocumentModel.LineFeed = value;
                this.RaisePropertyChanged();
            }
        }

        public bool Dirty
        {
            get
            {
                return this.DocumentModel.IsDirty;
            }
        }

        public String FilePath
        {
            get
            {
                return this.DocumentModel.CurrentFilePath;
            }
            set
            {
                this.DocumentModel.CurrentFilePath = value;
                this.RaisePropertyChanged();
            }
        }

        public FooEditEngine.TextPoint CaretPostion
        {
            get
            {
                return this.DocumentModel.Document.CaretPostion;
            }
        }

        DocumentModel _model;
        public DocumentModel DocumentModel
        {
            get
            {
                return this._model;
            }
            set
            {
                this._model = value;
                this.RaisePropertyChanged();
            }
        }

        double _fontSize = AppSettings.Current.FontSize;
        public double FontSize
        {
            get
            {
                return this._fontSize;
            }
            set
            {
                this._fontSize = value;
                this.RaisePropertyChanged();
            }
        }

        FontFamily _FontFamily = new FontFamily(AppSettings.Current.FontFamily);
        public FontFamily FontFamily
        {
            get
            {
                return this._FontFamily;
            }
            set
            {
                this._FontFamily = value;
                this.RaisePropertyChanged();
            }
        }

        public bool IsProgressNow
        {
            get
            {
                return this.DocumentModel.IsProgressNow;
            }
        }

        IHilighter _Hilighter;
        public IHilighter Hilighter
        {
            get
            {
                return this._Hilighter;
            }
            set
            {
                SetProperty(ref this._Hilighter, value);
            }
        }

        IFoldingStrategy _FoldingStrategy;
        public IFoldingStrategy FoldingStrategy
        {
            get
            {
                return this._FoldingStrategy;
            }
            set
            {
                SetProperty(ref this._FoldingStrategy, value);
            }
        }

        Brush _Foreground = new SolidColorBrush(UI.Colors.Black);
        public Brush Foreground
        {
            get
            {
                return _Foreground;
            }
            set
            {
                SetProperty(ref _Foreground, value);
            }
        }

        Brush _URL = new SolidColorBrush(UI.Colors.Blue);
        public Brush URL
        {
            get
            {
                return _URL;
            }
            set
            {
                SetProperty(ref _URL, value);
            }
        }

        Brush _Comment = new SolidColorBrush(UI.Colors.Green);
        public Brush Comment
        {
            get
            {
                return _Comment;
            }
            set
            {
                SetProperty(ref _Comment, value);
            }
        }

        Brush _Keyword1 = new SolidColorBrush(UI.Colors.Blue);
        public Brush Keyword1
        {
            get
            {
                return _Keyword1;
            }
            set
            {
                SetProperty(ref _Keyword1, value);
            }
        }

        Brush _Keyword2 = new SolidColorBrush(UI.Colors.DarkCyan);
        public Brush Keyword2
        {
            get
            {
                return _Keyword2;
            }
            set
            {
                SetProperty(ref _Keyword2, value);
            }
        }

        Brush _Literal = new SolidColorBrush(UI.Colors.Brown);
        public Brush Literal
        {
            get
            {
                return _Literal;
            }
            set
            {
                SetProperty(ref _Literal, value);
            }
        }

        public DocumentInfoViewModel()
        {
            this._model = new DocumentModel();
            this._model.PropertyChanged += _model_PropertyChanged;
            this._model.DocumentTypeChanged += _model_DocumentTypeChanged;
            AppSettings.Current.ChangedSetting += (s, e) =>
            {
                this.ApplyCurrentSetting();
            };
        }

        private void _model_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsDirty")
                this.RaisePropertyChanged("Dirty");
            else
                this.RaisePropertyChanged(e.PropertyName);
        }

        private void _model_DocumentTypeChanged(object sender, DocumentTypeEventArg e)
        {
            AppSettings.Current.FileType = e.newFileType;
            this.ApplyCurrentSetting();

            if (e.hilighter == null)
            {
                this.Hilighter = null;
                this._model.Document.LayoutLines.ClearHilight();
            }
            else
            {
                this.Hilighter = e.hilighter;
                this._model.Document.LayoutLines.HilightAll();
            }

            if (e.folding == null)
            {
                this.FoldingStrategy = null;
                this._model.Document.LayoutLines.ClearFolding();
            }
            else
            {
                this.FoldingStrategy = e.folding;
                this._model.Document.LayoutLines.ClearFolding();
                this._model.Document.LayoutLines.GenerateFolding();
            }
            this._model.Document.RequestRedraw();
        }

        public DocumentInfoViewModel(string title) : this()
        {
            this.Title = title;
        }

        public void ApplyCurrentSetting()
        {
            this._model.ApplyCurrentSetting();
            if(this.FontSize != AppSettings.Current.FontSize)
                this.FontSize = AppSettings.Current.FontSize;
            if(this.FontFamily == null || this.FontFamily.Source != AppSettings.Current.FontFamily)
                this.FontFamily = new FontFamily(AppSettings.Current.FontFamily);
        }

        public void OnActivate()
        {
            this.RaisePropertyChanged("Encode");
            this.RaisePropertyChanged("LineFeed");
        }

        public DocumentSource CreatePrintDocument()
        {
            var source = new DocumentSource(this.DocumentModel.Document, new FooEditEngine.Padding(20, 20, 20, 20), this.FontFamily.Source, this.FontSize);
            source.ParseHF = (s, e) => { return e.Original; };
            source.Header = AppSettings.Current.Header;
            source.Fotter = AppSettings.Current.Footer;
            source.Forground = ((SolidColorBrush)this.Foreground).Color;
            source.Keyword1 = ((SolidColorBrush)this.Keyword1).Color;
            source.Keyword2 = ((SolidColorBrush)this.Keyword2).Color;
            source.Literal = ((SolidColorBrush)this.Literal).Color;
            source.Comment = ((SolidColorBrush)this.Comment).Color;
            source.Url = ((SolidColorBrush)this.URL).Color;
            source.ParseHF = (s, e) => {
                PrintInfomation info = new PrintInfomation() { Title = this.Title, PageNumber = e.PageNumber };
                return EditorHelper.ParseHF(e.Original, info);
            };

            return source;
        }

        public async Task ReloadFileAsync(System.Text.Encoding enc)
        {
            if (this.FilePath == null)
            {
                throw new InvalidOperationException("ファイルを読み込んだ状態で実行する必要があります");
            }
            var file = await FileModel.GetFileModel(FileModelBuildType.AbsolutePath, this.FilePath);
            this.DocumentModel.Document.Clear();
            await this.DocumentModel.LoadFile(file, enc);
            //エンコードが読み込み後に変わる
            this.RaisePropertyChanged("Encode");
        }

        public System.Xml.Schema.XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(System.Xml.XmlReader reader)
        {
            reader.ReadStartElement("DocumentInfoViewModel");
            this.DocumentModel = new DocumentModel();
            this.DocumentModel.ReadXml(reader);
            this.DocumentModel.DocumentTypeChanged += _model_DocumentTypeChanged;
        }

        public void WriteXml(System.Xml.XmlWriter writer)
        {
            this.DocumentModel.WriteXml(writer);
        }
    }
}

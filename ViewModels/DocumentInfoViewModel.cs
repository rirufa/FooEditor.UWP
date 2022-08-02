using System;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Xml.Serialization;
using FooEditEngine;
using FooEditEngine.UWP;
using FooEditor.UWP.Models;
using Prism.Windows.Mvvm;
using UI = Windows.UI;
using Windows.UI.Xaml.Media;
using CommunityToolkit.Mvvm.Messaging;

namespace FooEditor.UWP.ViewModels
{
    public class DocumentInfoViewModel : ViewModelBase, IXmlSerializable
    {

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
                this.OnPropertyChanged();
            }
        }

        public AppSettings Settings
        {
            get
            {
                return AppSettings.Current;
            }
        }

        Brush _Foreground = new SolidColorBrush(AppSettings.Current.ForegroundColor);
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

        Brush _URL = new SolidColorBrush(AppSettings.Current.URLColor);
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

        Brush _Comment = new SolidColorBrush(AppSettings.Current.CommentColor);
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

        Brush _Keyword1 = new SolidColorBrush(AppSettings.Current.KeywordColor);
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

        Brush _Keyword2 = new SolidColorBrush(AppSettings.Current.Keyword2Color);
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

        Brush _Literal = new SolidColorBrush(AppSettings.Current.LiteralColor);
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

        Brush _ControlChar = new SolidColorBrush(AppSettings.Current.ControlCharColor);
        public Brush ControlChar
        {
            get
            {
                return _ControlChar;
            }
            set
            {
                SetProperty(ref _ControlChar, value);
            }
        }

        Brush _UpdateArea = new SolidColorBrush(AppSettings.Current.UpdateAreaColor);
        public Brush UpdateArea
        {
            get
            {
                return _UpdateArea;
            }
            set
            {
                SetProperty(ref _UpdateArea, value);
            }
        }

        Brush _LineMarker = new SolidColorBrush(AppSettings.Current.LineMarkerColor);
        public Brush LineMarker
        {
            get
            {
                return _LineMarker;
            }
            set
            {
                SetProperty(ref _LineMarker, value);
            }
        }

        public DocumentInfoViewModel()
        {
            this._model = new DocumentModel();
            WeakReferenceMessenger.Default.Register<PropertyChangedEventArgs>(this, (s, e) => {
                switch(e.PropertyName)
                {
                    case "ForegroundColor":
                        this.Foreground = new SolidColorBrush(AppSettings.Current.ForegroundColor);
                        break;
                    case "KeywordColor":
                        this.Keyword1 = new SolidColorBrush(AppSettings.Current.KeywordColor);
                        break;
                    case "Keyword2Color":
                        this.Keyword2 = new SolidColorBrush(AppSettings.Current.Keyword2Color);
                        break;
                    case "URLColor":
                        this.URL = new SolidColorBrush(AppSettings.Current.URLColor);
                        break;
                    case "ControlCharColor":
                        this.ControlChar = new SolidColorBrush(AppSettings.Current.ControlCharColor);
                        break;
                    case "CommentColor":
                        this.Comment = new SolidColorBrush(AppSettings.Current.CommentColor);
                        break;
                    case "LiteralColor":
                        this.Literal = new SolidColorBrush(AppSettings.Current.LiteralColor);
                        break;
                    case "UpdateAreaColor":
                        this.UpdateArea = new SolidColorBrush(AppSettings.Current.UpdateAreaColor);
                        break;
                    case "LineMarkerColor":
                        this.LineMarker = new SolidColorBrush(AppSettings.Current.LineMarkerColor);
                        break;
                }
            });
            this._model.DocumentTypeChanged += _model_DocumentTypeChanged;
            AppSettings.Current.ChangedSetting += (s, e) =>
            {
                this.ApplyCurrentSetting();
            };
        }

        private void _model_DocumentTypeChanged(object sender, DocumentTypeEventArg e)
        {
            AppSettings.Current.FileType = e.newFileType;
            this.ApplyCurrentSetting();

            if (e.hilighter == null)
            {
                this._model.Document.LayoutLines.ClearHilight();
            }
            else
            {
                this._model.Hilighter = e.hilighter;
                this._model.Document.LayoutLines.HilightAll();
            }

            if (e.folding == null)
            {
                this._model.Document.LayoutLines.ClearFolding();
            }
            else
            {
                this._model.Document.LayoutLines.ClearFolding();
                this._model.FoldingStrategy = e.folding;
                this._model.Document.LayoutLines.GenerateFolding();
            }
            this._model.Document.RequestRedraw();
        }

        public DocumentInfoViewModel(string title) : this()
        {
            this.DocumentModel.Title = title;
        }

        public void ApplyCurrentSetting()
        {
            this._model.ApplyCurrentSetting();
        }

        public void OnActivate()
        {
            this.OnPropertyChanged("Encode");
            this.OnPropertyChanged("LineFeed");
        }

        public DocumentSource CreatePrintDocument()
        {
            var source = new DocumentSource(this.DocumentModel.Document, new FooEditEngine.Padding(20, 20, 20, 20), AppSettings.Current.FontFamily, AppSettings.Current.FontSize);
            source.ParseHF = (s, e) => { return e.Original; };
            source.Header = AppSettings.Current.Header;
            source.Fotter = AppSettings.Current.Footer;
            source.Forground = AppSettings.Current.ForegroundColor;
            source.Keyword1 = AppSettings.Current.KeywordColor;
            source.Keyword2 = AppSettings.Current.KeywordColor;
            source.Literal = AppSettings.Current.LiteralColor;
            source.Comment = AppSettings.Current.CommentColor;
            source.Url = AppSettings.Current.URLColor;
            source.LineBreak = this.DocumentModel.Document.LineBreak;
            if (source.LineBreak == LineBreakMethod.None)
                source.LineBreak = LineBreakMethod.PageBound;
            source.LineBreakCount = this.DocumentModel.Document.LineBreakCharCount;
            source.ParseHF = (s, e) => {
                PrintInfomation info = new PrintInfomation() { Title = this.DocumentModel.Title, PageNumber = e.PageNumber };
                return EditorHelper.ParseHF(e.Original, info);
            };

            return source;
        }

        public async Task ReloadFileAsync(System.Text.Encoding enc)
        {
            if (this.DocumentModel.CurrentFilePath == null)
            {
                throw new InvalidOperationException("ファイルを読み込んだ状態で実行する必要があります");
            }
            var file = await FileModel.GetFileModel(FileModelBuildType.AbsolutePath, this.DocumentModel.CurrentFilePath);
            this.DocumentModel.Document.Clear();
            await this.DocumentModel.LoadFile(file, enc);
            //エンコードが読み込み後に変わる
            this.OnPropertyChanged("Encode");
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

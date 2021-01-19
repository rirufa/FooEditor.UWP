using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Runtime.Serialization;
using System.IO;
using System.Text;
using Windows.Storage;
using System.Xml;

namespace FooEditor.UWP.Models
{
    public sealed class AppSettings : AppSettingsBase
    {
        static AppSettings _thisInstance = new AppSettings();

        public static AppSettings Current
        {
            get
            {
                return _thisInstance;
            }
        }

        private AppSettings() : base()
        {
        }

        protected override void SetDefalutSetting()
        {
            if (GetGlobalEditorProperty("FontFamily") == null)
                SetGlobalEditorProperty("FontFamily", "Calibri");
            if (GetGlobalEditorProperty("FontSize") == null)
                SetGlobalEditorProperty("FontSize", 18.0);
            if (GetGlobalEditorProperty("TabChar") == null)
                SetGlobalEditorProperty("TabChar", 4);
            if (GetGlobalEditorProperty("IsRTL") == null)
                SetGlobalEditorProperty("IsRTL", false);
            if (GetGlobalEditorProperty("ShowLineBreak") == null)
                SetGlobalEditorProperty("ShowLineBreak", true);
            if (GetGlobalEditorProperty("ShowTab") == null)
                SetGlobalEditorProperty("ShowTab", true);
            if (GetGlobalEditorProperty("ShowFullSpace") == null)
                SetGlobalEditorProperty("ShowFullSpace", true);
            if (GetGlobalEditorProperty("ShowRuler") == null)
                SetGlobalEditorProperty("ShowRuler", false);
            if (GetGlobalEditorProperty("ShowLineNumber") == null)
                SetGlobalEditorProperty("ShowLineNumber", false);
            if (GetGlobalEditorProperty("ShowLineMarker") == null)
                SetGlobalEditorProperty("ShowLineMarker", true);
            if (GetGlobalEditorProperty("LineBreakMethod") == null)
                SetGlobalEditorProperty("LineBreakMethod", (int)FooEditEngine.LineBreakMethod.None);
            if (GetGlobalEditorProperty("LineBreakCount") == null)
                SetGlobalEditorProperty("LineBreakCount", 80);
            if (GetGlobalEditorProperty("ShowFoundPattern") == null)
                SetGlobalEditorProperty("ShowFoundPattern", false);
            if (GetGlobalEditorProperty("IndentBySpace") == null)
                SetGlobalEditorProperty("IndentBySpace", false);
            if (GetGlobalEditorProperty("TopMargin") == null)
                SetGlobalEditorProperty("TopMargin", 0);
            if (GetGlobalEditorProperty("RightMargin") == null)
                SetGlobalEditorProperty("RightMargin", 0);
            if (GetGlobalEditorProperty("BottomMargin") == null)
                SetGlobalEditorProperty("BottomMargin", 0);
            if (GetGlobalEditorProperty("LeftMargin") == null)
                SetGlobalEditorProperty("LeftMargin", 0);
            if (GetGlobalEditorProperty("Header") == null)
                SetGlobalEditorProperty("Header", "%f");
            if (GetGlobalEditorProperty("Footer") == null)
                SetGlobalEditorProperty("Footer", "-%p-");
            if (GetGlobalEditorProperty("EnableAutoIndent") == null)
                SetGlobalEditorProperty("EnableAutoIndent", false);
            if (GetGlobalEditorProperty("EnableAutoComplete") == null)
                SetGlobalEditorProperty("EnableAutoComplete", false);
            if (GetGlobalEditorProperty("DefaultEncoding") == null)
                SetGlobalEditorProperty("DefaultEncoding", System.Text.Encoding.Unicode.WebName);
            if (GetGlobalEditorProperty("EnableAutoSave") == null)
                SetGlobalEditorProperty("EnableAutoSave", true);
            if (GetGlobalEditorProperty("EnableSyntaxHilight") == null)
                SetGlobalEditorProperty("EnableSyntaxHilight", true);
            if (GetGlobalEditorProperty("EnableGenerateFolding") == null)
                SetGlobalEditorProperty("EnableGenerateFolding", true);
        }

        public const string TextType = "Text";

        protected override void LoadFileTypeCollection()
        {
            DataContractSerializer serializer = new DataContractSerializer(typeof(ObservableCollection<FileType>));
            Stream fs = null;
            try
            {
                Task<Stream> task = ApplicationData.Current.LocalFolder.OpenStreamForReadAsync("FileTypes.xml");
                fs = task.Result;
                _FileTypeCollection = (ObservableCollection<FileType>)serializer.ReadObject(fs);
            }
            catch (Exception)
            {
                _FileTypeCollection = new ObservableCollection<FileType>();
                FileType fileType = new FileType(TextType, "");
                fileType.ExtensionCollection.Add(".txt");
                _FileTypeCollection.Add(fileType);
                fileType = new FileType("C", "clang.xml");
                fileType.ExtensionCollection.Add(".c");
                fileType.ExtensionCollection.Add(".cs");
                fileType.ExtensionCollection.Add(".cpp");
                fileType.ExtensionCollection.Add(".h");
                _FileTypeCollection.Add(fileType);
                fileType = new FileType("CSS", "css.xml");
                fileType.ExtensionCollection.Add(".css");
                _FileTypeCollection.Add(fileType);
                fileType = new FileType("HTML", "html.xml");
                fileType.ExtensionCollection.Add(".htm");
                fileType.ExtensionCollection.Add(".html");
                fileType.ExtensionCollection.Add(".xhtml");
                _FileTypeCollection.Add(fileType);
                fileType = new FileType("Java", "java.xml");
                fileType.ExtensionCollection.Add(".java");
                _FileTypeCollection.Add(fileType);
                fileType = new FileType("JavaScript", "javascript.xml");
                fileType.ExtensionCollection.Add(".js");
                fileType = new FileType("CoffeeScript", "coffeescript.xml");
                fileType.ExtensionCollection.Add(".coffee");
                _FileTypeCollection.Add(fileType);
                fileType = new FileType("Perl", "perl.xml");
                fileType.ExtensionCollection.Add(".pl");
                _FileTypeCollection.Add(fileType);
                fileType = new FileType("PHP", "php.xml");
                fileType.ExtensionCollection.Add(".php");
                _FileTypeCollection.Add(fileType);
                fileType = new FileType("Python", "python.xml");
                fileType.ExtensionCollection.Add(".py");
                _FileTypeCollection.Add(fileType);
                fileType = new FileType("Ruby", "ruby.xml");
                fileType.ExtensionCollection.Add(".rb");
                _FileTypeCollection.Add(fileType);
                fileType = new FileType("VisualBasic", "vb.xml");
                fileType.ExtensionCollection.Add(".vb");
                fileType.ExtensionCollection.Add(".vbs");
                _FileTypeCollection.Add(fileType);
                fileType = new FileType("XML", "xml.xml");
                fileType.ExtensionCollection.Add(".xml");
                _FileTypeCollection.Add(fileType);
            }
            finally
            {
                if (fs != null)
                    fs.Dispose();
            }
        }

        public const string KeywordFolderName = "Keywords";

        public override async Task Save()
        {
            DataContractSerializer serializer = new DataContractSerializer(typeof(ObservableCollection<FileType>));
            StorageFile file = await ApplicationData.Current.LocalFolder.CreateFileAsync("FileTypes.xml", CreationCollisionOption.ReplaceExisting);
            using (Stream fs = await file.OpenStreamForWriteAsync())
            {
                var settings = new XmlWriterSettings { Indent = true };
                using (var w = XmlWriter.Create(fs, settings))
                    serializer.WriteObject(w, _FileTypeCollection);
            }
        }

        protected override object GetGlobalEditorProperty(string name)
        {
            return ApplicationData.Current.LocalSettings.Values[name];
        }

        protected override void SetGlobalEditorProperty(string name,object value)
        {
            ApplicationData.Current.LocalSettings.Values[name] = value;
        }

        public static ObservableCollection<Encoding> SupportEncodeCollection = new ObservableCollection<Encoding>() {
            Encoding.GetEncoding("utf-32"),
            Encoding.GetEncoding("utf-32BE"),
            Encoding.GetEncoding("utf-16"),
            Encoding.GetEncoding("unicodeFFFE"),
            Encoding.GetEncoding("iso-2022-jp"),
            Encoding.GetEncoding("us-ascii"),
            Encoding.GetEncoding("euc-jp"),
            Encoding.GetEncoding("shift_jis"),
            new EncodeDetect.UTF8WithoutBom(),
            new EncodeDetect.UTF8WithBom(),
        };
    }

}

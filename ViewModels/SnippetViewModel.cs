using FooEditor.UWP.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using Windows.Storage;

namespace FooEditor.UWP.ViewModels
{
    public class SnippetViewModel : INotifyPropertyChanged
    {
        Snippet snippet;    //元データ
        string _Name;
        public string Name
        {
            get
            {
                return _Name;
            }
            set
            {
                _Name = value;
                OnPropertyChanged();
            }
        }
        string _data;
        public string Data
        {
            get
            {
                return Util.Replace(this.snippet.Data,new string[] {"\\n" }, new string[] { System.Environment.NewLine });
            }
            set
            {
                _data = value;
                this.OnPropertyChanged();
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        public SnippetViewModel(Snippet snippet)
        {
            this.snippet = snippet;
            this.Name = snippet.Name;
            this.Data = snippet.Data;
        }
        public void OnPropertyChanged([CallerMemberName] string caller = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(caller));
        }
        public void InsetToDocument(DocumentModel docModel)
        {
            this.snippet.InsetToDocument(docModel);
        }
    }

    public class SnippetCategoryViewModel : INotifyPropertyChanged
    {
        string _Name;
        public string Name
        {
            get
            {
                return this._Name;
            }
            set
            {
                this._Name = value;
                OnPropertyChanged();
            }
        }
        string _FilePath;
        public string FilePath
        {
            get
            {
                return this._FilePath;
            }
            set
            {
                this._FilePath = value;
                this.OnPropertyChanged();
            }
        }
        public event PropertyChangedEventHandler PropertyChanged;
        public SnippetCategoryViewModel(string name, string path)
        {
            this.Name = name;
            this.FilePath = path;
        }
        public void OnPropertyChanged([CallerMemberName] string caller = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(caller));
        }

    }

    class SnipeetLoader
    {
        const string SnippetFolderName = "Sinppets";


        public static async Task<ObservableCollection<SnippetCategoryViewModel>> LoadCategory()
        {
            ObservableCollection<SnippetCategoryViewModel> items = new ObservableCollection<SnippetCategoryViewModel>();

            StorageFolder snippetFolder = await ApplicationData.Current.LocalFolder.TryGetItemAsync(SnippetFolderName) as StorageFolder;
            if (snippetFolder == null)
            {
                snippetFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(SnippetFolderName);
                StorageFolder appInstalledFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
                StorageFolder assets = await appInstalledFolder.GetFolderAsync(SnippetFolderName);
                foreach (var file in await assets.GetFilesAsync())
                {
                    var item = snippetFolder.TryGetItemAsync(file.Name);
                    if (item != null)
                        await file.CopyAsync(snippetFolder);
                }
            }

            foreach (var file in await snippetFolder.GetFilesAsync())
                items.Add(new SnippetCategoryViewModel(file.Name, file.Path));

            return items;
        }

        static bool HasCategory(IEnumerable<SnippetCategoryViewModel> items, string name)
        {
            foreach (SnippetCategoryViewModel category in items)
                if (category.Name == name)
                    return true;
            return false;
        }

        public static async Task<ObservableCollection<SnippetViewModel>> LoadSnippets(string path)
        {

            var file = await StorageFile.GetFileFromPathAsync(path);
            XmlDocument xml = await XmlDocument.LoadFromFileAsync(file);

            XmlNodeList nodes = xml.GetElementsByTagName("sinppet");
            ObservableCollection<SnippetViewModel> items = new ObservableCollection<SnippetViewModel>();
            foreach (var node in nodes)
            {
                string name = null, data = null;
                foreach (var child in node.ChildNodes)
                {
                    switch ((string)child.LocalName)
                    {
                        case "name":
                            name = child.InnerText;
                            break;
                        case "data":
                            //元からある改行はタブは無視する
                            data = Util.Replace(child.InnerText, new string[] { "\t", "\n", "\r" }, new string[] { "", "", "" });
                            break;
                    }
                }
                if (name == null || data == null)
                    throw new Exception(string.Format("Not found(name:{0} data:{1} path:{2})", name, data, path));
                items.Add(new SnippetViewModel(new Snippet(name, data)));
            }
            return items;
        }
    }
}

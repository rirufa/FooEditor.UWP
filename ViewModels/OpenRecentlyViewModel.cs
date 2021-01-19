using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Windows.Mvvm;
using FooEditor.UWP.Models;
using System.Collections.ObjectModel;
using Windows.Storage.AccessCache;

namespace FooEditor.UWP.ViewModels
{
    public sealed class RecentFile
    {
        /// <summary>
        /// ファイル名
        /// </summary>
        public string FileName
        {
            get;
            private set;
        }
        /// <summary>
        /// ファイルパス
        /// </summary>
        public string FilePath
        {
            get;
            private set;
        }
        /// <summary>
        /// コンストラクター
        /// </summary>
        public RecentFile(string name, string path)
        {
            this.FileName = name;
            this.FilePath = path;
        }
    }

    class OpenRecentlyViewModel : ViewModelBase
    {
        public async Task Initalize()
        {
            foreach (var item in StorageApplicationPermissions.MostRecentlyUsedList.Entries)
            {
                try
                {
                    var file = await StorageApplicationPermissions.MostRecentlyUsedList.GetFileAsync(item.Token);
                    _RecentFiles.Add(new RecentFile(FileModel.TrimFullPath(file.Path),file.Path));

                }
                catch (System.IO.IOException)
                {
                    //どうにもならないので飛ばす
                }
            }
        }

        bool _EnablePrimaryButton = false;
        public bool EnablePrimaryButton
        {
            get
            {
                return _EnablePrimaryButton;
            }
            set
            {
                SetProperty(ref _EnablePrimaryButton, value);
            }
        }

        ObservableCollection<RecentFile> _SelectedFiles = new ObservableCollection<RecentFile>();
        public ObservableCollection<RecentFile> SelectedFiles
        {
            get
            {
                return _SelectedFiles;
            }
            set
            {
                SetProperty(ref _SelectedFiles, value);
                this.EnablePrimaryButton = true;
            }
        }

        ObservableCollection<RecentFile> _RecentFiles = new ObservableCollection<RecentFile>();
        public ObservableCollection<RecentFile> RecentFiles
        {
            get
            {
                return _RecentFiles;
            }
        }
    }
}

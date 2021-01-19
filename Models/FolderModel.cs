using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;

namespace FooEditor.UWP.Models
{
    class FolderModel
    {
        private StorageFolder _StorageFolder;

        private FolderModel(StorageFolder folder)
        {
            this._StorageFolder = folder;
        }

        public static async Task<FolderModel> TryGetFolderFromAppSetting(string name)
        {
            StorageFolder folder = await ApplicationData.Current.LocalFolder.TryGetItemAsync(name) as StorageFolder;
            if (folder == null)
                return null;
            else
                return new FolderModel(folder);
        }

        public async static Task CopyFilesFromInstalledFolderToLocalSetting(string copyTo)
        {
            StorageFolder destinationFolder = await ApplicationData.Current.LocalFolder.TryGetItemAsync(copyTo) as StorageFolder;
            if(destinationFolder == null)
            {
                destinationFolder = await ApplicationData.Current.LocalFolder.CreateFolderAsync(copyTo);
                StorageFolder appInstalledFolder = Windows.ApplicationModel.Package.Current.InstalledLocation;
                StorageFolder assets = await appInstalledFolder.GetFolderAsync(copyTo);
                foreach (var file in await assets.GetFilesAsync())
                {
                    var item = destinationFolder.TryGetItemAsync(file.Name);
                    if (item != null)
                        await file.CopyAsync(destinationFolder);
                }
            }
        }

        public async Task<FileModel> GetFile(string name)
        {
            StorageFile file = await _StorageFolder.GetFileAsync(name);
            return await FileModel.GetFileModel(file);
        }
    }
}

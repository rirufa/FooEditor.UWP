using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using FooEditor.UWP.ViewModels;

// コンテンツ ダイアログの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=234238 を参照してください

namespace FooEditor.UWP.Views
{
    public sealed partial class OpenRecentlyView : ContentDialog
    {
        public OpenRecentlyView()
        {
            this.InitializeComponent();
            this.Loaded += OpenRecentlyView_Loaded;
        }

        public ObservableCollection<RecentFile> SelectedFiles
        {
            get;
            private set;
        }

        private async void OpenRecentlyView_Loaded(object sender, RoutedEventArgs e)
        {
            var vm = this.DataContext as OpenRecentlyViewModel;
            if (vm != null)
                await vm.Initalize();
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            var vm = this.DataContext as OpenRecentlyViewModel;
            this.SelectedFiles = vm.SelectedFiles;
        }

        private void ContentDialog_SecondaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
        }

        private void ListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listbox = (ListBox)sender;
            var vm = this.DataContext as OpenRecentlyViewModel;
            vm.SelectedFiles = new ObservableCollection<RecentFile>(listbox.SelectedItems.Cast<RecentFile>());
        }
    }
}

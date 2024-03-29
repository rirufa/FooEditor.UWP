﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Graphics.Printing;
using Windows.Graphics.Printing.OptionDetails;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.Storage;
using Windows.System;
using Prism.Windows.Mvvm;
using FooEditEngine.UWP;
using FooEditor.UWP.ViewModels;
using FooEditor.UWP.Models;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Xaml.Controls;
using Windows.UI.ViewManagement;

namespace FooEditor.UWP.Views
{
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
            InputPane currentView = InputPane.GetForCurrentView();
            currentView.Showing += currentView_Showing;
            currentView.Hiding += currentView_Hiding;
        }

        public void SetRootFrame(Frame frame)
        {
            this.NavigationContent.Content = frame;
        }

        void currentView_Hiding(InputPane sender, InputPaneVisibilityEventArgs args)
        {
            this.RootPanel.Margin = new Thickness(0);
            args.EnsuredFocusedElementInView = true;
        }

        void currentView_Showing(InputPane sender, InputPaneVisibilityEventArgs args)
        {
            this.RootPanel.Margin = new Thickness(0, 0, 0, args.OccludedRect.Height);
            args.EnsuredFocusedElementInView = true;
        }


        void MainPage_PrintTaskRequested(PrintManager sender, PrintTaskRequestedEventArgs args)
        {
            IAsyncAction async = Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
            {
                MainPageViewModel vm = this.DataContext as MainPageViewModel;
                var source = vm.CurrentDocument.CreatePrintDocument();
                PrintTask task = null;
                task = args.Request.CreatePrintTask(vm.CurrentDocument.DocumentModel.Title, (e) =>
                {
                    e.SetSource(source.PrintDocument);
                });
                task.Completed += (s, e) => {
                    System.Diagnostics.Debug.WriteLine("finshed printing");
                };
                PrintOptionBuilder<DocumentSource> builder = new PrintOptionBuilder<DocumentSource>(source);
                builder.BuildPrintOption(PrintTaskOptionDetails.GetFromPrintTaskOptions(task.Options));
            });
            Task t = WindowsRuntimeSystemExtensions.AsTask(async);
            t.Wait();
        }


        bool _inited = false;
        public async Task Init(object param, bool require_restore, Dictionary<string, object> viewModelState)
        {
            //VM内で追加する設定が反映されないので、ここで追加する
            MainPageViewModel vm = this.DataContext as MainPageViewModel;
            if(_inited == false)
            {
                PrintManager.GetForCurrentView().PrintTaskRequested += MainPage_PrintTaskRequested;
                Window.Current.CoreWindow.KeyUp += CoreWindow_KeyUp;
                _inited = true;
            }
            await vm.Init(param, require_restore, viewModelState);
        }

        public async Task Suspend(bool suspending, Dictionary<string, object> viewModelState)
        {
            MainPageViewModel vm = this.DataContext as MainPageViewModel;
            PrintManager.GetForCurrentView().PrintTaskRequested -= MainPage_PrintTaskRequested;
            Window.Current.CoreWindow.KeyUp -= CoreWindow_KeyUp;
            _inited = false;
            await vm.Suspend(viewModelState, suspending);            
        }

        public async void OpenFromArgs(object args)
        {
            MainPageViewModel vm = this.DataContext as MainPageViewModel;
            await vm.OpenFromArgs(args);
        }

        bool IsModiferKeyPressed(VirtualKey key)
        {
            CoreVirtualKeyStates state = Window.Current.CoreWindow.GetKeyState(key);
            return (state & CoreVirtualKeyStates.Down) == CoreVirtualKeyStates.Down;
        }

        void CoreWindow_KeyUp(CoreWindow sender, KeyEventArgs e)
        {
            MainPageViewModel vm = this.DataContext as MainPageViewModel;
            bool isCtrlPressed = IsModiferKeyPressed(VirtualKey.Control);
            bool isShiftPressed = IsModiferKeyPressed(VirtualKey.Shift);
            if (isCtrlPressed)
            {
                switch (e.VirtualKey)
                {
                    case VirtualKey.N:
                        vm.AddDocumentCommand.Execute(null);
                        break;
                    case VirtualKey.Tab:
                        if (isShiftPressed)
                            vm.DocumentList.Prev();
                        else
                            vm.DocumentList.Next();
                        break;
                    case VirtualKey.F:
                    case VirtualKey.H:
                        vm.OpenFindAndReplaceCommand.Execute(null);
                        break;
                    case VirtualKey.G:
                        vm.OpenGoToCommand.Execute(null);
                        break;
                    case VirtualKey.S:
                        vm.SaveCommand.Execute(null);
                        break;
                    case VirtualKey.O:
                        vm.OpenFileCommand.Execute(null);
                        break;
                }
            }
        }

        private void Grid_DragOver(object sender, DragEventArgs e)
        {
            e.AcceptedOperation = Windows.ApplicationModel.DataTransfer.DataPackageOperation.Copy;
            e.Handled = true;
        }

        private async void Grid_Drop(object sender, DragEventArgs e)
        {
            var d = e.GetDeferral();

            if (e.DataView.Contains(Windows.ApplicationModel.DataTransfer.StandardDataFormats.StorageItems))
            {
                MainPageViewModel vm = this.DataContext as MainPageViewModel;
                // ファイルのパス一覧を取得する
                var items = await e.DataView.GetStorageItemsAsync();
                vm.OpenFilesCommand.Execute(items);
            }

            d.Complete();
        }

        private async void OpenAsEncodeButton_Click(object sender, RoutedEventArgs e)
        {
            MainPageViewModel vm = this.DataContext as MainPageViewModel;
            var dialog = new OpenAsEncodeView();
            if(await dialog.ShowAsync() == ContentDialogResult.Primary)
                vm.OpenFileCommand.Execute(dialog.SelectedEncoding);
        }

        private async void SaveAsEncodeButton_Click(object sender, RoutedEventArgs e)
        {
            MainPageViewModel vm = this.DataContext as MainPageViewModel;
            var dialog = new SaveAsEncodeView();
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                vm.SaveAsCommand.Execute(dialog.SelectedEncoding);
        }

        private async void OpenFromMRU_Click(object sender, RoutedEventArgs e)
        {
            MainPageViewModel vm = this.DataContext as MainPageViewModel;
            var dialog = new OpenRecentlyView();
            if (await dialog.ShowAsync() == ContentDialogResult.Primary)
            {
                var file_paths = from select_file in dialog.SelectedFiles select select_file.FilePath;
                vm.OpenFilePathCommand.Execute(file_paths);
            }

        }

        private void TabViewItem_CloseRequested(Microsoft.UI.Xaml.Controls.TabViewItem sender, Microsoft.UI.Xaml.Controls.TabViewTabCloseRequestedEventArgs args)
        {
            MainPageViewModel vm = this.DataContext as MainPageViewModel;
            vm.RemoveDocumentCommand.Execute(args.Item);
        }
    }
}
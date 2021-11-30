using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using Windows.ApplicationModel.Activation;
using Prism.Unity.Windows;
using Prism.Mvvm;
using Prism.Windows;
using FooEditor.UWP.Models;
using FooEditor.UWP.Services;
using FooEditor.UWP.Views;
using FooEditor.UWP.ViewModels;
using Microsoft.Practices.Unity;
using Windows.Storage.AccessCache;
using System.Runtime.Serialization;
using Prism.Windows.Navigation;
using Prism.Windows.AppModel;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using Windows.UI.Xaml;

namespace FooEditor.UWP
{
    /// <summary>
    /// 既定の Application クラスを補完するアプリケーション固有の動作を提供します。
    /// </summary>
    sealed partial class App : PrismUnityApplication
    {
        /// <summary>
        /// 単一アプリケーション オブジェクトを初期化します。これは、実行される作成したコードの
        ///最初の行であるため、main() または WinMain() と論理的に等価です。
        /// </summary>
        public App()
        {
            this.InitializeComponent();

            //EncodeDetectを正常に動作させるために必要
            //http://www.atmarkit.co.jp/ait/articles/1509/30/news039.html
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);

            this.RestoreNavigationStateOnResume = false;    //設定画面を表示するだけなので状態を保存しなくてもいい

            ExtendedSplashScreenFactory = (splashscreen) => new ExtendedSplashScreen(splashscreen);

#if !DEBUG
            AppCenter.Start("7fc70d1f-9bf6-4da7-b9f2-aa49d827b0fe",typeof(Analytics), typeof(Crashes));
#endif
        }


        protected override Task OnInitializeAsync(IActivatedEventArgs args)
        {
            ViewModelLocationProvider.Register(
                typeof(MainPage).ToString(),
                () => {
                    return new MainPageViewModel(NavigationService, new MainViewService());
                }
                );
            ViewModelLocationProvider.Register(
                typeof(DocumentInfoPage).ToString(),
                () => {
                    return new DocumentInfoPageViewModel(NavigationService, new MainViewService());
                }
                );
            return base.OnInitializeAsync(args);
        }

        protected override UIElement CreateShell(Frame rootFrame)
        {
            var main = new MainPage();
            main.SetRootFrame(rootFrame);
            return main;
        }

        protected override async Task OnResumeApplicationAsync(IActivatedEventArgs args)
        {
            MainPage main = (MainPage)this.Shell;
            await main.Init(null, true, this.SessionStateService.SessionState);
        }

        protected override async Task OnLaunchApplicationAsync(LaunchActivatedEventArgs args)
        {
            MainPage main = (MainPage)this.Shell;
            await main.Init(null, false, this.SessionStateService.SessionState);
            //何でもいいので適当なページを遷移させておく
            this.NavigationService.Navigate("Setting", null);
        }

        protected override async Task OnActivateApplicationAsync(IActivatedEventArgs args)
        {
            MainPage main = (MainPage)this.Shell;
            var fileargs = args as FileActivatedEventArgs;
            if (fileargs != null)
            {
                var filepaths = from file in fileargs.Files
                                select file.Path;

                //MRUに追加しないと後で開けない
                foreach (var file in fileargs.Files)
                    StorageApplicationPermissions.MostRecentlyUsedList.Add(file, "mrufile");

                //そのまま渡すと中断時に落ちるので文字列に変換する
                ObjectToXmlConverter conv = new ObjectToXmlConverter();
                object param = conv.Convert(filepaths.ToArray(), typeof(string[]), null, null);
                await main.Init(param, false, this.SessionStateService.SessionState);
            }
            else
            {
                await main.Init(null, false, this.SessionStateService.SessionState);
            }

        }

        protected override void OnFileActivated(FileActivatedEventArgs args)
        {
            this.OnActivated(args);
        }

        protected override async Task OnSuspendingApplicationAsync()
        {
            MainPage main = (MainPage)this.Shell;
            await main.Suspend(true, this.SessionStateService.SessionState);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.AccessCache;
using Windows.UI.Notifications;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Navigation;
using Windows.Graphics.Printing;
using Windows.System.Threading;
using Windows​.Data​.Xml​.Dom;
using Microsoft.Toolkit.Uwp.Notifications;
using FooEditor.UWP.Models;
using FooEditor.UWP.Views;

namespace FooEditor.UWP.Services
{
    public interface IMainViewService
    {
        MainPage MainPage
        {
            get;
            set;
        }
        Task<bool> ConfirmRestoreUserState();
        Task<bool> Confirm(string s, string yes_label, string no_label);
        Task MakeMessageBox(string s);
        void MakeNotifaction(string text);
    }

    public class MainViewService : IMainViewService
    {
        public MainPage MainPage
        {
            get;
            set;
        }

        public async Task<bool> Confirm(string s, string yes_label, string no_label)
        {
            var msg = new MessageDialog(s, "");
            msg.Commands.Add(new UICommand(yes_label));
            msg.Commands.Add(new UICommand(no_label));
            var res = await msg.ShowAsync();
            return res.Label == yes_label;
        }

        public async Task<bool> ConfirmRestoreUserState()
        {
            if (!AppSettings.Current.EnableAutoSave)
                return false;
            var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
            return await this.Confirm(loader.GetString("RestoreUsareStateMessage"),
                loader.GetString("YesButton"),
                loader.GetString("NoButton")
                );
        }

        public async Task MakeMessageBox(string s)
        {
            var msg = new MessageDialog(s);
            await msg.ShowAsync();
        }

        public void MakeNotifaction(string text)
        {
            ToastContent toastContent = new ToastContent()
            {
                Scenario = ToastScenario.Default,
                Visual = new ToastVisual()
                {
                    BindingGeneric = new ToastBindingGeneric()
                    {
                        Children =
                        {
                            new AdaptiveText()
                            {
                                Text = text
                            },
                        },
                        AppLogoOverride = new ToastGenericAppLogo()
                        {
                            Source = "ms-appx:///Assets/StoreLogo.png"
                        }
                    }
                }
            };


            XmlDocument xmlDoc = new XmlDocument();
            xmlDoc.LoadXml(toastContent.GetContent());

            var toast = new ToastNotification(xmlDoc);
            ToastNotificationManager.CreateToastNotifier().Show(toast); // Display toast

        }
    }
}

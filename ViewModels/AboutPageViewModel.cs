using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Windows.Mvvm;
using System.Reflection;


namespace FooEditor.UWP.ViewModels
{
    class AboutPageViewModel : ViewModelBase
    {
        public string AppName
        {
            get
            {
                return typeof(App).GetTypeInfo().Assembly.GetName().Name;
            }
        }

        public string Version
        {
            get
            {
                return typeof(App).GetTypeInfo().Assembly.GetName().Version.ToString();
            }
        }
    }
}

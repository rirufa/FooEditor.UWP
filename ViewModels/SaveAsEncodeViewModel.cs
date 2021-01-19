using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Prism.Windows.Mvvm;
using FooEditor.UWP.Models;
using System.Collections.ObjectModel;

namespace FooEditor.UWP.ViewModels
{
    class SaveAsEncodeViewModel : ViewModelBase
    {
        Encoding _Encoding;
        public Encoding Encoding
        {
            get
            {
                return _Encoding;
            }
            set
            {
                SetProperty(ref _Encoding, value);
            }
        }

        public ObservableCollection<Encoding> EncodeCollection
        {
            get
            {
                return AppSettings.SupportEncodeCollection;
            }
        }
    }
}

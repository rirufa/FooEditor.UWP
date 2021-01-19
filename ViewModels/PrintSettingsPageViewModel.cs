using System;
using System.Globalization;
using FooEditor.UWP.Models;
using Prism.Windows.Mvvm;

namespace FooEditor.UWP.ViewModels
{
    sealed class PrintSettingsPageViewModel : ViewModelBase
    {
        public bool IsMetric
        {
            get
            {
                return RegionInfo.CurrentRegion.IsMetric;
            }
        }
        public float TopMargin
        {
            get
            {
                return PrintModel.GetUnit(AppSettings.Current.TopMargin, this.IsMetric);
            }
            set
            {
                AppSettings.Current.TopMargin = PrintModel.GetPixel(value, this.IsMetric);
                this.RaisePropertyChanged();
            }
        }
        public float RightMargin
        {
            get
            {
                return PrintModel.GetUnit(AppSettings.Current.RightMargin, this.IsMetric);
            }
            set
            {
                AppSettings.Current.RightMargin = PrintModel.GetPixel(value, this.IsMetric);
                this.RaisePropertyChanged();
            }
        }
        public float BottomMargin
        {
            get
            {
                return PrintModel.GetUnit(AppSettings.Current.BottomMargin, this.IsMetric);
            }
            set
            {
                AppSettings.Current.BottomMargin = PrintModel.GetPixel(value, this.IsMetric);
                this.RaisePropertyChanged();
            }
        }
        public float LeftMargin
        {
            get
            {
                return PrintModel.GetUnit(AppSettings.Current.LeftMargin, this.IsMetric);
            }
            set
            {
                AppSettings.Current.LeftMargin = PrintModel.GetPixel(value, this.IsMetric);
                this.RaisePropertyChanged();
            }
        }
        public string Header
        {
            get
            {
                return AppSettings.Current.Header;
            }
            set
            {
                AppSettings.Current.Header = value;
                this.RaisePropertyChanged();
            }
        }
        public string Footer
        {
            get
            {
                return AppSettings.Current.Footer;
            }
            set
            {
                AppSettings.Current.Footer = value;
                this.RaisePropertyChanged();
            }
        }
        public string UnitNoticeText
        {
            get
            {
                var loader = new Windows.ApplicationModel.Resources.ResourceLoader();
                if (this.IsMetric)
                    return loader.GetString("MetricLabel");
                else
                    return loader.GetString("InchiLabel");
            }
        }
    }
    sealed class PrintModel
    {
        static public int GetPixel(float n, bool ismetric)
        {
            float dpi = Windows.Graphics.Display.DisplayInformation.GetForCurrentView().LogicalDpi;
            if (ismetric)
                return (int)Math.Round(n / 25.4f * dpi);
            else
                return (int)Math.Round(n * dpi + 0.5);
        }
        static public float GetUnit(float px, bool ismetric)
        {
            float dpi = Windows.Graphics.Display.DisplayInformation.GetForCurrentView().LogicalDpi;
            if (ismetric)
                return (float)Math.Round(px * 25.4f / dpi);
            else
                return (float)Math.Round(px / dpi);

        }
    }
}

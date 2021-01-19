using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;
using FooEditEngine;
using Windows.ApplicationModel.Resources;
using EncodeDetect;
using System.IO;
using System.Runtime.Serialization;

namespace FooEditor.UWP
{
    public class ObjectToXmlConverter : IValueConverter
    {
        public object Convert(object obj, Type targetType, object param, string language)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            using (StreamReader reader = new StreamReader(memoryStream))
            {
                DataContractSerializer serializer = new DataContractSerializer(obj.GetType());
                serializer.WriteObject(memoryStream, obj);
                memoryStream.Position = 0;
                return reader.ReadToEnd();
            }
        }

        public object ConvertBack(object value, Type toType, object param, string language)
        {
            string xml = (string)value;
            using (Stream stream = new MemoryStream())
            {
                byte[] data = System.Text.Encoding.UTF8.GetBytes(xml);
                stream.Write(data, 0, data.Length);
                stream.Position = 0;
                DataContractSerializer deserializer = new DataContractSerializer(toType);
                return deserializer.ReadObject(stream);
            }
        }
    }

    public class TextPointConverter : IValueConverter
    {
        ResourceLoader loader = new ResourceLoader();
        public object Convert(object value, System.Type targetType, object parameter, string language)
        {
            TextPoint tp = (TextPoint)value;
            return string.Format(loader.GetString("TextPointFormat"), tp.row + 1, tp.col + 1);
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public sealed class LineFeedConverter : IValueConverter
    {
        public object Convert(object value, System.Type targetType, object parameter, string language)
        {
            LineFeedType type = (LineFeedType)value;
            switch (type)
            {
                case LineFeedType.CR:
                    return "CR";
                case LineFeedType.CRLF:
                    return "CRLF";
                case LineFeedType.LF:
                    return "LF";
            }
            return string.Empty;
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, string language)
        {
            string str = (string)value;
            switch (str)
            {
                case "CR":
                    return LineFeedType.CR;
                case "LF":
                    return LineFeedType.LF;
                case "CRLF":
                    return LineFeedType.CRLF;
            }
            throw new ArgumentOutOfRangeException();
        }
    }

    public class RateConverter : IValueConverter
    {
        ResourceLoader loader = new ResourceLoader();
        public object Convert(object value, System.Type targetType, object parameter, string language)
        {
            double v = (double)value;
            return string.Format(loader.GetString("MagnificationPowerFormat"), (int)(v * 100));
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class LineBreakMethodConverter : IValueConverter
    {
        ResourceLoader loader = new ResourceLoader();

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            LineBreakMethod method = (LineBreakMethod)value;
            switch (method)
            {
                case LineBreakMethod.None:
                    return loader.GetString("LineBreakMethodNone");
                case LineBreakMethod.CharUnit:
                    return loader.GetString("LineBreakMethodCharUnit");
                case LineBreakMethod.PageBound:
                    return loader.GetString("LineBreakMethodPageBound");
            }
            throw new ArgumentOutOfRangeException();
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            string name = (string)value;
            if (name == loader.GetString("LineBreakMethodNone"))
                return LineBreakMethod.None;
            if (name == loader.GetString("LineBreakMethodCharUnit"))
                return LineBreakMethod.CharUnit;
            if (name == loader.GetString("LineBreakMethodPageBound"))
                return LineBreakMethod.PageBound;
            throw new ArgumentOutOfRangeException();
        }
    }
    /// <summary>
    /// Converts a Boolean into a Visibility.
    /// </summary>
    public class BooleanToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// If set to True, conversion is reversed: True will become Collapsed.
        /// </summary>
        public bool IsReversed { get; set; }

        public object Convert(object value, System.Type targetType, object parameter, string language)
        {
            var val = System.Convert.ToBoolean(value);
            if (this.IsReversed)
            {
                val = !val;
            }

            if (val)
            {
                return Visibility.Visible;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, string language)
        {
            throw new System.NotImplementedException();
        }
    }
}

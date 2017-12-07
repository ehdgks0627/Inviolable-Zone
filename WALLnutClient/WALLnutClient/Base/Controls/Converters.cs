using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WALLnutClient
{
    public static class TreeViewItemExtensions
    {
        public static int GetDepth(this TreeViewItem item)
        {
            TreeViewItem parent;
            while ((parent = GetParent(item)) != null)
            {
                return GetDepth(parent) + 1;
            }
            return 0;
        }

        private static TreeViewItem GetParent(TreeViewItem item)
        {
            var parent = VisualTreeHelper.GetParent(item);
            while (!(parent is TreeViewItem || parent is TreeView))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }
            return parent as TreeViewItem;
        }
    }


    public class ItemsControlHeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            Double intReturn = 0;
            Double Distribute = 0;
            try
            {
                Double.TryParse((value != null) ? value.ToString() : "", out intReturn);
                Double.TryParse((parameter != null) ? parameter.ToString() : "", out Distribute);

                return intReturn - Distribute;
            }
            catch (Exception) { }
            return intReturn;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return Double.NaN;
        }
    }

    public class LeftMarginMultiplierConverter : IValueConverter
    {
        public double Length { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var item = value as TreeViewItem;
            if (item == null)
                return new Thickness(0);

            return new Thickness(Length * item.GetDepth(), 0, 0, 0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    [ValueConversion(typeof(string), typeof(bool))]
    public class HeaderToImageConverter : IValueConverter
    {
        public static HeaderToImageConverter Instance = new HeaderToImageConverter();

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            BitmapImage source = null;
            ChildTreeNode node = (value as ChildTreeNode);
            if (node == null)
                return source;

            try
            {
                switch (node.NodeType)
                {
                    case NodeTypes.ROOT:
                        source = Utilitys.BitMapToBitmapImage(Properties.Resources.MyComputer, System.Drawing.Imaging.ImageFormat.Png);
                        return source;
                    case NodeTypes.DRIVE:
                        source = Utilitys.BitMapToBitmapImage(Properties.Resources.diskdrive, System.Drawing.Imaging.ImageFormat.Png);
                        return source;
                    case NodeTypes.FOLDER:
                        source = Utilitys.BitMapToBitmapImage(Properties.Resources.folder, System.Drawing.Imaging.ImageFormat.Png);
                        break;
                    default:
                        source = Utilitys.BitMapToBitmapImage(Properties.Resources.file, System.Drawing.Imaging.ImageFormat.Png);
                        return source;
                }
            }
            catch (Exception ex)
            {
            }
            return source;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException("Cannot convert back");
        }
    }

    public class TypeToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string typeString = parameter.ToString();
            if (value != null)
                return (value.ToString() == typeString) ? System.Windows.Visibility.Collapsed : System.Windows.Visibility.Visible;
            else
                return System.Windows.Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }
}

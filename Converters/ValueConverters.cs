using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ProcMaster.Converters
{
    /// <summary>
    /// Value converter that converts a CPU percentage value into a representative highlight color
    /// (IndianRed for high load >50%, DarkOrange for medium >20%, DodgerBlue for active >5%, Gray for idle).
    /// </summary>
    public class CpuColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double cpu)
            {
                if (cpu > 50) return Brushes.IndianRed;
                if (cpu > 20) return Brushes.DarkOrange;
                if (cpu > 5) return Brushes.DodgerBlue;
            }
            return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Converts a tree depth integer into a pixel margin thickness for visual tree hierarchy indentation.
    /// </summary>
    public class HierarchyIndentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int level)
            {
                return new System.Windows.Thickness(level * 16, 0, 0, 0);
            }
            return new System.Windows.Thickness(0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

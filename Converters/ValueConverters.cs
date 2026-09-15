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
                if (cpu > 50) return new SolidColorBrush(Color.FromRgb(255, 82, 82));
                if (cpu > 20) return new SolidColorBrush(Color.FromRgb(255, 179, 0));
                if (cpu > 2) return new SolidColorBrush(Color.FromRgb(77, 208, 225));
            }
            return new SolidColorBrush(Color.FromRgb(150, 160, 170));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Generates a dynamic mini sparkline geometry for CPU usage.
    /// </summary>
    public class CpuSparklineConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            double cpu = value is double d ? d : 0;
            double peak = Math.Clamp(cpu / 100.0 * 10.0, 1.0, 11.0);
            double yPeak = 12.0 - peak;
            double midPeak = 12.0 - (peak * 0.4);

            return Geometry.Parse($"M 0,11 L 8,11 L 14,{midPeak:F1} L 18,{yPeak:F1} L 22,{midPeak:F1} L 28,11 L 36,11");
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

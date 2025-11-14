using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace WorkTimeTracker.Converters
{
    public class OverurenToBrushConverter : IValueConverter
    {
        public Brush Positief { get; set; } = new SolidColorBrush(Color.FromRgb(22, 163, 74));   // groen
        public Brush Negatief { get; set; } = new SolidColorBrush(Color.FromRgb(249, 115, 22));  // oranje
        public Brush Nul { get; set; } = Brushes.Black;

        public object Convert(object value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is double d)
            {
                if (d > 0.0001) return Positief;
                if (d < -0.0001) return Negatief;
            }
            return Nul;
        }

        public object ConvertBack(object value, Type targetType, object? parameter, CultureInfo culture)
            => Binding.DoNothing;
    }
}

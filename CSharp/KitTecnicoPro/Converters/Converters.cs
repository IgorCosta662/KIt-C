using System.Collections.Concurrent;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace KitTecnicoPro.Converters
{
    /// <summary>Converte bool para uma cor hex. ConverterParameter = "colorTrue|colorFalse"</summary>
    public class BoolToColorConverter : IValueConverter
    {
        private static readonly ConcurrentDictionary<string, SolidColorBrush> _brushCache = new(StringComparer.OrdinalIgnoreCase);
        private static readonly SolidColorBrush _fallbackBrush = CreateFrozenBrush(Colors.Gray);

        private static SolidColorBrush CreateFrozenBrush(Color color)
        {
            var brush = new SolidColorBrush(color);
            brush.Freeze();
            return brush;
        }

        private static SolidColorBrush GetOrCreateBrush(string hex)
        {
            return _brushCache.GetOrAdd(hex, h =>
            {
                try
                {
                    var color = (Color)ColorConverter.ConvertFromString(h);
                    return CreateFrozenBrush(color);
                }
                catch
                {
                    return _fallbackBrush;
                }
            });
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool b = value is bool bVal && bVal;
            string param = parameter?.ToString() ?? "#4CAF50|#607D8B";
            string[] parts = param.Split('|');
            string hex = b ? parts[0] : (parts.Length > 1 ? parts[1] : "#607D8B");

            if (targetType == typeof(Brush) || targetType == typeof(SolidColorBrush))
            {
                return GetOrCreateBrush(hex);
            }
            return hex;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => DependencyProperty.UnsetValue;
    }

    /// <summary>Converte bool para Visibility.</summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b && b ? Visibility.Visible : Visibility.Collapsed;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value is Visibility v && v == Visibility.Visible;
    }

    /// <summary>Inverte um bool.</summary>
    public class InverseBoolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b && !b;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b && !b;
    }

    /// <summary>Converte bool invertido para Visibility.</summary>
    public class InverseBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => value is bool b && !b ? Visibility.Visible : Visibility.Collapsed;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => DependencyProperty.UnsetValue;
    }

    /// <summary>Converte bool para texto. ConverterParameter = "textoTrue|textoFalse"</summary>
    public class BoolToStringConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool b = value is bool bVal && bVal;
            string param = parameter?.ToString() ?? "Sim|Não";
            string[] parts = param.Split('|');
            return b ? parts[0] : (parts.Length > 1 ? parts[1] : "Não");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => DependencyProperty.UnsetValue;
    }
}

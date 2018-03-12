using GDax.Attributes;
using GDax.Enums;
using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace GDax.Converters
{
    public class CoinToPathConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (!(value is CoinKind))
                throw new NotImplementedException();

            var type = typeof(CoinKind);
            var enumName = Enum.GetName(type, value);
            var icon = type.GetMember(enumName).FirstOrDefault()?.GetCustomAttributes(typeof(IconAttribute), false).FirstOrDefault() as IconAttribute;

            return icon?.Path;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}

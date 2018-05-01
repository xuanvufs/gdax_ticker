using GDax.Attributes;
using GDax.Enums;
using GDax.Models;
using System;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace GDax.Converters
{
    public class CurrencyIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var product = value as CurrencyPair;
            if (product == null)
                throw new NotImplementedException();

            var type = typeof(Currency);
            var icon = type.GetMember(product.Base.ToString()).FirstOrDefault()?.GetCustomAttributes(typeof(IconAttribute), false).FirstOrDefault() as IconAttribute;

            return icon?.Path;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}
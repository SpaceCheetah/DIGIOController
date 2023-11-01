using Avalonia.Data;
using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace DIGIOController.ViewModels.Converters; 

public class HexConverter : IValueConverter {
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture) {
        if (value is not string text || targetType != typeof(decimal?))
            return new BindingNotification(new InvalidCastException(), BindingErrorType.Error);
        return int.TryParse(text, NumberStyles.HexNumber, null, out int result) ? (decimal)result : 0;
    }
    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) {
        if (targetType != typeof(string) || (value is not decimal && value is not null)) {
            return new BindingNotification(new InvalidCastException(), BindingErrorType.Error);
        }
        return value is null ? "0" : $"{decimal.ToInt32((decimal)value):X}";
    }
}
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace ChatClientWPF.Converters
{
    /// <summary>
    /// 방 상태를 색상으로 변환하는 Converter
    /// </summary>
    public class StatusToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                return status switch
                {
                    "waiting" => new SolidColorBrush(Color.FromRgb(0x2a, 0x9d, 0x8f)), // Green
                    "full" => new SolidColorBrush(Color.FromRgb(0xe7, 0x4c, 0x3c)), // Red
                    "playing" => new SolidColorBrush(Color.FromRgb(0xff, 0xb7, 0x03)), // Orange
                    _ => new SolidColorBrush(Color.FromRgb(0x3d, 0x5a, 0x80))
                };
            }
            return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Bool 값을 색상으로 변환 (점유 여부)
    /// </summary>
    public class BoolToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isOccupied)
            {
                if (isOccupied)
                {
                    return Color.FromRgb(0x4a, 0x90, 0xe2); // Blue (Occupied)
                }
                else
                {
                    return Color.FromRgb(0x3d, 0x5a, 0x80); // Gray (Empty)
                }
            }
            return Colors.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Bool 값을 Brush로 변환 (준비 상태)
    /// </summary>
    public class BoolToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isReady)
            {
                if (isReady)
                {
                    return new SolidColorBrush(Color.FromRgb(0x2a, 0x9d, 0x8f)); // Green (Ready)
                }
                else
                {
                    return new SolidColorBrush(Color.FromRgb(0xff, 0xb7, 0x03)); // Orange (Not Ready)
                }
            }
            return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Bool 값을 Visibility로 변환 (True = Visible)
    /// </summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is true ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is Visibility.Visible;
        }
    }

    /// <summary>
    /// Bool 값을 역 Visibility로 변환 (True = Collapsed)
    /// </summary>
    public class InverseBoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is true ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is not Visibility.Visible;
        }
    }
    /// <summary>
    /// Bool 값을 준비 버튼 텍스트로 변환
    /// </summary>
    public class BoolToReadyTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return (value is bool isReady && isReady) ? "준비 취소" : "준비";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// Bool 값을 준비 버튼 스타일로 변환
    /// </summary>
    public class BoolToReadyButtonStyleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isReady && isReady)
            {
                return Application.Current.Resources["WarningButtonStyle"];
            }
            return Application.Current.Resources["SuccessButtonStyle"];
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

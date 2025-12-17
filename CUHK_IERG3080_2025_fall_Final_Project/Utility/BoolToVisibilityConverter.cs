using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;

namespace CUHK_IERG3080_2025_fall_Final_Project.ViewModel
{
	public class BoolToVisibilityConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value is bool b && b ? Visibility.Visible : Visibility.Collapsed;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value is Visibility v && v == Visibility.Visible;
		}
	}

	public class RelayCommand<T> : ICommand
	{
		private readonly Action<T> _execute;

		public RelayCommand(Action<T> execute) => _execute = execute;

		public event EventHandler CanExecuteChanged;
		public bool CanExecute(object parameter) => true;
		public void Execute(object parameter) => _execute((T)parameter);
	}
}
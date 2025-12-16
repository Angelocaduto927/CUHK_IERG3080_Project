using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CUHK_IERG3080_2025_fall_Final_Project.ViewModel;

namespace CUHK_IERG3080_2025_fall_Final_Project.View
{
    public partial class Setting : UserControl
    {
        private Button _listeningButton;
        private string _previousContent;
        private Brush _previousBackground;
        private Window _hostWindow;
        private SettingVM _vm;

        public Setting()
        {
            InitializeComponent();
            Loaded += Setting_Loaded;
            Unloaded += Setting_Unloaded;
        }

        private void Setting_Loaded(object sender, RoutedEventArgs e)
        {
            _vm = DataContext as SettingVM;
            if (_vm == null)
                return;

            ApplyBindingsFromVM();
        }

        private void RebindKey_Click(object sender, RoutedEventArgs e)
        {
            if (_listeningButton != null)
            {
                CancelListening();
            }

            _listeningButton = sender as Button;
            if (_listeningButton == null)
                return;

            _previousContent = _listeningButton.Content?.ToString() ?? "Unassigned";
            _previousBackground = _listeningButton.Background;

            _listeningButton.Content = "Press a key...";
            _listeningButton.Background = new SolidColorBrush(Color.FromRgb(200, 230, 255));

            _hostWindow = Window.GetWindow(this);
            if (_hostWindow != null)
            {
                _hostWindow.PreviewKeyDown += HostWindow_PreviewKeyDown;
                _hostWindow.PreviewMouseDown += HostWindow_PreviewMouseDown;
            }

            _hostWindow?.Focus();
        }

        private void HostWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (_listeningButton == null || _vm == null)
                return;

            var mods = Keyboard.Modifiers;
            var key = e.Key == Key.System ? e.SystemKey : e.Key;

            string existing = _vm.SaveBinding(_listeningButton.Name, key, mods, force: false);

            if (!string.IsNullOrEmpty(existing) && existing != _listeningButton.Name)
            {
                var conflictMsg = string.Format("The key {0} is already bound to \"{1}\".\nDo you want to overwrite it?",
                    SettingVM.FormatBindingText(key, mods), existing);

                var result = MessageBox.Show(conflictMsg, "Binding conflict", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.No)
                {
                    CancelListening();
                    e.Handled = true;
                    return;
                }

                _vm.SaveBinding(_listeningButton.Name, key, mods, force: true);

                var oldBtn = FindName(existing) as Button;
                if (oldBtn != null)
                    oldBtn.Content = "Unassigned";
            }

            _listeningButton.Content = SettingVM.FormatBindingText(key, mods);
            _listeningButton.Background = _previousBackground;

            e.Handled = true;
            StopListening();
        }

        private void HostWindow_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_listeningButton != null)
            {
                CancelListening();
            }
        }

        private void CancelListening()
        {
            if (_listeningButton == null)
                return;

            _listeningButton.Content = _previousContent;
            _listeningButton.Background = _previousBackground;
            StopListening();
        }

        private void StopListening()
        {
            if (_hostWindow != null)
            {
                _hostWindow.PreviewKeyDown -= HostWindow_PreviewKeyDown;
                _hostWindow.PreviewMouseDown -= HostWindow_PreviewMouseDown;
            }

            _listeningButton = null;
            _previousContent = null;
            _previousBackground = null;
            _hostWindow = null;
        }

        private void Setting_Unloaded(object sender, RoutedEventArgs e)
        {
            StopListening();
        }

        private void ResetDefaults_Click(object sender, RoutedEventArgs e)
        {
            _vm?.ResetToDefaults();
            ApplyBindingsFromVM();
        }

        private void ApplyBindingsFromVM()
        {
            if (_vm == null)
                return;

            var dict = _vm.GetBindings();
            foreach (var kv in dict)
            {
                if (FindName(kv.Key) is Button btn)
                {
                    btn.Content = SettingVM.FormatBindingText(kv.Value.Item1, kv.Value.Item2);
                }
            }
        }
    }
}

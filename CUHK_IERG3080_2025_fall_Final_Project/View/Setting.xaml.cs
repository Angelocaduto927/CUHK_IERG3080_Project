using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using CUHK_IERG3080_2025_fall_Final_Project.ViewModel;

namespace CUHK_IERG3080_2025_fall_Final_Project.View
{
    /// <summary>
    /// Setting.xaml interaction logic (UI listens for key and delegates state to SettingVM)
    /// </summary>
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

            // populate UI from VM bindings
            ApplyBindingsFromVM();
        }

        // click a bind button to start listening
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
                _hostWindow.PreviewMouseDown += HostWindow_PreviewMouseDown; // cancel on mouse click outside
            }

            _hostWindow?.Focus();
        }

        private void HostWindow_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (_listeningButton == null || _vm == null) return;

            var mods = Keyboard.Modifiers;
            var key = e.Key == Key.System ? e.SystemKey : e.Key;

            // Ask VM to try to save binding (non-forced). If a collision exists, vm returns the existing action name.
            string existing = _vm.SaveBinding(_listeningButton.Name, key, mods, force: false);

            if (!string.IsNullOrEmpty(existing) && existing != _listeningButton.Name)
            {
                // Prompt user — view handles UI decision
                var conflictMsg = string.Format("The key {0} is already bound to \"{1}\".\nDo you want to overwrite it?",
                    SettingVM.FormatBindingText(key, mods), existing);

                var result = MessageBox.Show(conflictMsg, "Binding conflict", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.No)
                {
                    CancelListening();
                    e.Handled = true;
                    return;
                }

                // User chose to overwrite -> call VM with force
                _vm.SaveBinding(_listeningButton.Name, key, mods, force: true);

                // Clear previous UI label for the overwritten action if present
                var oldBtn = FindName(existing) as Button;
                if (oldBtn != null)
                    oldBtn.Content = "Unassigned";
            }

            // If no existing collision, binding has already been saved by the non-forced call above.
            if (string.IsNullOrEmpty(existing))
            {
                // nothing more to do
            }

            // Update UI and restore background
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
            if (_listeningButton == null) return;

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

        // Apply bindings provided by VM to UI buttons
        private void ApplyBindingsFromVM()
        {
            if (_vm == null) return;

            var dict = _vm.GetBindings();
            foreach (var kv in dict)
            {
                var btn = FindName(kv.Key) as Button;
                if (btn != null)
                    btn.Content = SettingVM.FormatBindingText(kv.Value.Item1, kv.Value.Item2);
            }
        }
    }
}

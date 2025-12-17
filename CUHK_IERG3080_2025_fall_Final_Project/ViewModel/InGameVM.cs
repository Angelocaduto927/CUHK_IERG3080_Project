using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using CUHK_IERG3080_2025_fall_Final_Project.Model;

namespace CUHK_IERG3080_2025_fall_Final_Project.ViewModel
{
    public class InGameVM : INotifyPropertyChanged
    {
        private GameEngine _engine;
        private DispatcherTimer _timer;

        public ICommand KeyPressCommand { get; }
        public ObservableCollection<NoteVM> Player1Notes { get; } = new ObservableCollection<NoteVM>();
        public ObservableCollection<NoteVM> Player2Notes { get; } = new ObservableCollection<NoteVM>();

        public string SongName => SongManager.CurrentSong;
        public double CurrentTime => _engine?.CurrentTime / 1000.0 ?? 0;
        public System.Collections.Generic.IReadOnlyList<PlayerManager> Players => _engine?.Players;
        public bool IsMultiplayer => Players?.Count > 1;

        public InGameVM()
        {
            KeyPressCommand = new RelayCommand<string>(OnKeyPress);
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            _timer.Tick += (s, e) => Update();
        }

        public void Initialize(GameEngine engine)
        {
            _engine = engine;
            _engine.StartGame();
            _timer.Start();
            OnPropertyChanged("");
        }

        private void Update()
        {
            if (_engine?.State != GameEngine.GameState.Playing) return;

            _engine.Update();
            UpdateNotes(0, Player1Notes);
            if (IsMultiplayer) UpdateNotes(1, Player2Notes);

            OnPropertyChanged(nameof(CurrentTime));
            OnPropertyChanged(nameof(Players));

            if (_engine.IsGameFinished())
            {
                _timer.Stop();
                _engine.StopGame();
            }
        }

        private void UpdateNotes(int idx, ObservableCollection<NoteVM> notes)
        {
            var player = Players[idx];

            var visible = player.Chart
                .Where(n => n.State == Note.NoteState.Active)
                .ToList();

            notes.Clear();
            foreach (var note in visible)
            {
                notes.Add(new NoteVM
                {
                    X = note.X - 45,
                    Y = 305,
                    Inner = note.Type == Note.NoteType.Red ? Color.FromRgb(255, 120, 120) : Color.FromRgb(120, 170, 255),
                    Outer = note.Type == Note.NoteType.Red ? Color.FromRgb(180, 0, 0) : Color.FromRgb(0, 100, 220),
                    Border = note.Type == Note.NoteType.Red ? Brushes.DarkRed : Brushes.DarkBlue,
                    Icon = note.Type == Note.NoteType.Red ? "ド" : "カ"
                });
            }
        }

        private void OnKeyPress(string key)
        {
            if (_engine?.State != GameEngine.GameState.Playing) return;

            var k = (Key)Enum.Parse(typeof(Key), key);

            for (int i = 0; i < Players.Count; i++)
            {
                if (PlayerSettingsManager.GetSettings(i).KeyBindings.ContainsValue(k))
                {
                    _engine.HandleKeyPress(i, k);
                    break;
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }

    public class NoteVM
    {
        public double X { get; set; }
        public double Y { get; set; }
        public Color Inner { get; set; }
        public Color Outer { get; set; }
        public Brush Border { get; set; }
        public string Icon { get; set; }
    }

    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value is bool b && b ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value is Visibility v && v == Visibility.Visible;
        }
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;

        public RelayCommand(Action<T> execute) => _execute = execute;

        public event EventHandler CanExecuteChanged { add { } remove { } }
        public bool CanExecute(object parameter) => true;
        public void Execute(object parameter) => _execute((T)parameter);
    }
}
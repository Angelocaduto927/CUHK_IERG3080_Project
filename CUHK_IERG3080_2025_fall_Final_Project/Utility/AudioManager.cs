using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;

namespace CUHK_IERG3080_2025_fall_Final_Project.Utility
{
    public static class AudioManager
    {
        private static MediaPlayer _backgroundMusicPlayer;
        private static MediaPlayer _effectPlayer = new MediaPlayer();
        private static bool _isInitialized = false;

        // Volume settings
        private const double BackgroundVolume = 0.3; // 30% volume for background music
        private const double EffectVolume = 0.8; // 80% volume for sound effects

        public static void Initialize()
        {
            if (_isInitialized)
                return;

            _backgroundMusicPlayer = new MediaPlayer();
            _backgroundMusicPlayer.MediaEnded += OnMediaEnded;
            _isInitialized = true;
        }

        private static void OnMediaEnded(object sender, EventArgs e)
        {
            // Loop the background music
            _backgroundMusicPlayer.Position = TimeSpan.Zero;
            _backgroundMusicPlayer.Play();
        }

        public static void PlayBackgroundMusic()
        {
            if (!_isInitialized)
                Initialize();

            try
            {
                // Load the background music file
                var uri = new Uri("pack://application:,,,/CUHK_IERG3080_2025_fall_Final_Project;component/Assets/Sound/background.mp3");
                _backgroundMusicPlayer.Open(uri);
                _backgroundMusicPlayer.Volume = BackgroundVolume;
                _backgroundMusicPlayer.Play();
            }
            catch (Exception ex)
            {
                // Handle file not found or other errors gracefully
                System.Diagnostics.Debug.WriteLine($"Failed to play background music: {ex.Message}");
            }
        }

        public static void StopBackgroundMusic()
        {
            if (_backgroundMusicPlayer != null)
            {
                _backgroundMusicPlayer.Stop();
            }
        }

        public static void PauseBackgroundMusic()
        {
            if (_backgroundMusicPlayer != null)
            {
                _backgroundMusicPlayer.Pause();
            }
        }

        public static void ResumeBackgroundMusic()
        {
            if (_backgroundMusicPlayer != null)
            {
                _backgroundMusicPlayer.Play();
            }
        }

        public static void SetBackgroundVolume(double volume)
        {
            if (_backgroundMusicPlayer != null)
            {
                // Clamp volume between 0.0 and 1.0
                _backgroundMusicPlayer.Volume = Math.Max(0.0, Math.Min(1.0, volume));
            }
        }

        public static void Cleanup()
        {
            if (_backgroundMusicPlayer != null)
            {
                _backgroundMusicPlayer.Stop();
                _backgroundMusicPlayer.Close();
                _backgroundMusicPlayer = null;
            }
            if (_effectPlayer != null)
            {
                _effectPlayer.Close();
                _effectPlayer = null;
            }
            _isInitialized = false;
        }

        // --- Attached property for click sound ---
        public static readonly DependencyProperty EnableClickSoundProperty =
            DependencyProperty.RegisterAttached(
                "EnableClickSound",
                typeof(bool),
                typeof(AudioManager),
                new PropertyMetadata(false, OnEnableClickSoundChanged));

        public static void SetEnableClickSound(UIElement element, bool value)
        {
            element.SetValue(EnableClickSoundProperty, value);
        }

        public static bool GetEnableClickSound(UIElement element)
        {
            return (bool)element.GetValue(EnableClickSoundProperty);
        }

        private static void OnEnableClickSoundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Button button)
            {
                if ((bool)e.NewValue)
                {
                    button.Click += PlayClickSound;
                }
                else
                {
                    button.Click -= PlayClickSound;
                }
            }
        }

        private static void PlayClickSound(object sender, RoutedEventArgs e)
        {
            try
            {
                var uri = new Uri("pack://application:,,,/CUHK_IERG3080_2025_fall_Final_Project;component/Assets/Sound/click.wav");
                _effectPlayer.Open(uri);
                _effectPlayer.Volume = EffectVolume;
                _effectPlayer.Play();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to play click sound: {ex.Message}");
            }
        }
    }
}
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Controls;
using System.IO;

namespace CUHK_IERG3080_2025_fall_Final_Project.Utility
{
    public static class AudioManager
    {
        private static MediaPlayer _backgroundMusicPlayer;
        private static MediaPlayer _effectPlayer = new MediaPlayer();
        private static bool _isInitialized = false;
        private static bool _bgmOpened = false;

        private const double BackgroundVolume = 0.1; 
        private const double EffectVolume = 0.8; 


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
            _backgroundMusicPlayer.Position = TimeSpan.Zero;
            _backgroundMusicPlayer.Play();
        }

        public static void PlayBackgroundMusic()
        {
            if (!_isInitialized)
                Initialize();

            try
            {
                if (!_bgmOpened)
                {
                    Uri uri = new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Sound", "background.mp3"));

                    _backgroundMusicPlayer.Open(uri);
                    _backgroundMusicPlayer.Volume = BackgroundVolume;
                    _bgmOpened = true;
                }

                _backgroundMusicPlayer.Play();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to play background music: {ex.Message}");
            }
        }


        public static void StopBackgroundMusic()
        {
            if (_backgroundMusicPlayer != null)
            {
                _backgroundMusicPlayer.Stop();
                _backgroundMusicPlayer.Position = TimeSpan.Zero;
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

            _bgmOpened = false;
            _isInitialized = false;
        }

        public static void PlayFx(string fileName, bool reuseSharedPlayer = false)
        {
            try
            {
                var fxPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Sound", fileName);
                Uri uri = new Uri(fxPath);

                if (reuseSharedPlayer)
                {
                    if (_effectPlayer == null)
                    {
                        _effectPlayer = new MediaPlayer();
                    }

                    _effectPlayer.Open(uri);
                    _effectPlayer.Volume = EffectVolume;
                    _effectPlayer.Play();
                }
                else
                {
                    var player = new MediaPlayer();
                    player.Open(uri);
                    player.Volume = EffectVolume;

                    player.MediaEnded += (s, e) =>
                    {
                        try
                        {
                            player.Close();
                        }
                        catch { }
                    };

                    player.Play();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to play fx '{fileName}': {ex.Message}");
            }
        }

        private static readonly RoutedEventHandler SharedClickHandler = (s, e) =>
        {
            PlayFx("click.wav");
        };

        public static readonly DependencyProperty EnableClickSoundProperty =
            DependencyProperty.RegisterAttached(
                "EnableClickSound",
                typeof(bool),
                typeof(AudioManager),
                new PropertyMetadata(false, OnEnableClickSoundChanged));

        public static bool GetEnableClickSound(UIElement element)
        {
            return (bool)element.GetValue(EnableClickSoundProperty);
        }

        public static void SetEnableClickSound(UIElement element, bool value)
        {
            element.SetValue(EnableClickSoundProperty, value);
        }

        private static void OnEnableClickSoundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Button button)
            {
                if ((bool)e.NewValue)
                {
                    button.Click += SharedClickHandler;
                }
                else
                {
                    button.Click -= SharedClickHandler;
                }
            }
        }

    }
}

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

        // Volume settings
        private const double BackgroundVolume = 0.1; // 30% volume for background music
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


        // Unified FX player. By default it creates a dedicated MediaPlayer and closes it after playback.
        // Pass reuseSharedPlayer = true to use the shared _effectPlayer (keeps previous click behavior).
        public static void PlayFx(string fileName, bool reuseSharedPlayer = false)
        {
            try
            {
                var fxPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "Sound", fileName);
                Uri uri = new Uri(fxPath);

                if (reuseSharedPlayer)
                {
                    // Use the shared effect player (click behavior previously used this player)
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
                    // Create a dedicated player and ensure resources are released after playback
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

        // Shared click handler (replaces the previous PlayClickSound method).
        // It directly calls PlayFx. The additional parameter previously passed was redundant,
        // so we use the simpler call.
        private static readonly RoutedEventHandler SharedClickHandler = (s, e) =>
        {
            PlayFx("click.wav");
        };

        // Attached property for enabling/disabling click sound
        public static readonly DependencyProperty EnableClickSoundProperty =
            DependencyProperty.RegisterAttached(
                "EnableClickSound", // The name of the property
                typeof(bool),       // The type of the property
                typeof(AudioManager), // The class that owns the property
                new PropertyMetadata(false, OnEnableClickSoundChanged)); // Default value and callback

        // Property to get the value of the attached property
        public static bool GetEnableClickSound(UIElement element)
        {
            return (bool)element.GetValue(EnableClickSoundProperty);
        }

        // Property to set the value of the attached property
        public static void SetEnableClickSound(UIElement element, bool value)
        {
            element.SetValue(EnableClickSoundProperty, value);
        }

        // This callback is triggered when the value of EnableClickSound changes
        private static void OnEnableClickSoundChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Button button)
            {
                if ((bool)e.NewValue)
                {
                    // Subscribe to the Click event of the button to play the click sound
                    button.Click += SharedClickHandler;
                }
                else
                {
                    // Unsubscribe from the Click event of the button
                    button.Click -= SharedClickHandler;
                }
            }
        }

    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace CUHK_IERG3080_2025_fall_Final_Project.Utility
{
    public static class AudioManager
    {
        private static MediaPlayer _backgroundMusicPlayer;
        private static bool _isInitialized = false;

        // Volume settings
        private const double BackgroundVolume = 0.3; // 30% volume for background music
        private const double GameVolume = 1.0; // 100% volume for game music

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
                var uri = new Uri("pack://application:,,,/Assets/Sound/background.mp3");
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
            _isInitialized = false;
        }
    }
}

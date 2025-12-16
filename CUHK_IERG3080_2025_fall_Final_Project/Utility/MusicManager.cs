using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace CUHK_IERG3080_2025_fall_Final_Project.Utility
{
    public class MusicManager
    {
        private MediaPlayer _musicPlayer;

        // 事件：音乐结束
        public event EventHandler MusicEnded;

        public MusicManager()
        {
            _musicPlayer = new MediaPlayer();
            _musicPlayer.MediaEnded += OnMusicEnded;
        }

        public void Play(string songPath, double volume = 0.7)
        {
            if (string.IsNullOrEmpty(songPath))
                return;

            try
            {
                var uri = new Uri(songPath, UriKind.RelativeOrAbsolute);
                _musicPlayer.Open(uri);
                _musicPlayer.Volume = volume;
                _musicPlayer.Play();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to play music: {ex.Message}");
            }
        }

        public void Pause()
        {
            _musicPlayer?.Pause();
        }

        public void Resume()
        {
            _musicPlayer?.Play();
        }

        public void Stop()
        {
            _musicPlayer?.Stop();
        }

        private void OnMusicEnded(object sender, EventArgs e)
        {
            MusicEnded?.Invoke(this, e);
        }

        public void Dispose()
        {
            _musicPlayer?.Close();
            _musicPlayer = null;
        }
    }
}

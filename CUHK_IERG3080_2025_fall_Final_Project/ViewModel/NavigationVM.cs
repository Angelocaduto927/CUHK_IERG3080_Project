using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CUHK_IERG3080_2025_fall_Final_Project.Utility;
using CUHK_IERG3080_2025_fall_Final_Project.Model;
using System.Windows.Input;

namespace CUHK_IERG3080_2025_fall_Final_Project.ViewModel
{
    class NavigationVM : Utility.ViewModelBase
    {
        private object _currentViewModel;
        public object CurrentViewModel
        {
            get { return _currentViewModel; }
            set
            {
                _currentViewModel = value;
                OnPropertyChanged();

                // Handle background music based on current view  
                HandleBackgroundMusic(value);
            }
        }

        public ICommand TitleScreenCommand { get; set; }
        public ICommand SongSelectionCommand { get; set; }
        public ICommand SettingCommand { get; set; }
        public ICommand InGameCommand { get; set; }
        public ICommand GameOverCommand { get; set; }

        private void TitleScreen(object obj)
        {
            CurrentViewModel = new TitleScreenVM(() => SongSelection(null));
        }

        private void SongSelection(object obj)
        {
            //// Ensure a game mode is selected before going to song selection  
            //if (GameModeManager.CurrentMode == null)
            //{
            //    // If no mode selected, go back to title screen  
            //    TitleScreen(null);
            //    return;
            //}

            CurrentViewModel = new SongSelectionVM();
        }

        private void Setting(object obj)
        {
            // Recreate SettingVM each time to reload current speeds from model  
            CurrentViewModel = new SettingVM();
        }

        private void InGame(object obj)
        {
            //// Ensure a game mode is selected before starting game  
            //if (GameModeManager.CurrentMode == null)
            //{
            //    // If no mode selected, go back to title screen  
            //    TitleScreen(null);
            //    return;
            //}

            CurrentViewModel = new InGameVM(() => GameOver(null));
        }

        private void GameOver(object obj)
        {
            CurrentViewModel = new GameOverVM(() => TitleScreen(null));
        }

        private void HandleBackgroundMusic(object viewModel)
        {
            // Stop background music when entering InGame view  
            if (viewModel is InGameVM)
            {
                CUHK_IERG3080_2025_fall_Final_Project.Utility.AudioManager.StopBackgroundMusic();
            }
            // Resume/play background music for all other views  
            else
            {
                CUHK_IERG3080_2025_fall_Final_Project.Utility.AudioManager.PlayBackgroundMusic();
            }
        }

        public NavigationVM()
        {
            TitleScreenCommand = new RelayCommand(TitleScreen);
            SongSelectionCommand = new RelayCommand(SongSelection);
            SettingCommand = new RelayCommand(Setting);
            InGameCommand = new RelayCommand(InGame);
            GameOverCommand = new RelayCommand(GameOver);

            // Initialize audio system  
            CUHK_IERG3080_2025_fall_Final_Project.Utility.AudioManager.Initialize();

            // Start with title screen and background music  
            CurrentViewModel = new TitleScreenVM(() => SongSelection(null));
            CUHK_IERG3080_2025_fall_Final_Project.Utility.AudioManager.PlayBackgroundMusic();
        }
    }
}

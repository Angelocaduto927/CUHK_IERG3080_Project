using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CUHK_IERG3080_2025_fall_Final_Project.Utility;
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
            }
        }
        public ICommand TitleScreenCommand { get; set; }
        public ICommand SongSelectionCommand { get; set; }
        public ICommand SettingCommand { get; set; }
        public ICommand InGameCommand { get; set; }
        //public ICommand GameOverCommand { get; set; }

        private void TitleScreen(object obj)
        {
            CurrentViewModel = new TitleScreenVM();
        }
        private void SongSelection(object obj)
        {
            CurrentViewModel = new SongSelectionVM();
        }
        private void Setting(object obj)
        {
            CurrentViewModel = new SettingVM();
        }
        private void InGame(object obj)
        {
            CurrentViewModel = new InGameVM();
        }
        private void GameOver(object obj)
        {
            CurrentViewModel = new GameOverVM();
        }

        public NavigationVM()
        {
            TitleScreenCommand = new RelayCommand(TitleScreen);
            SongSelectionCommand = new RelayCommand(SongSelection);
            SettingCommand = new RelayCommand(Setting);
            InGameCommand = new RelayCommand(InGame);
            //GameOverCommand = new RelayCommand(GameOver);
            CurrentViewModel = new TitleScreenVM();
        }

    }
}

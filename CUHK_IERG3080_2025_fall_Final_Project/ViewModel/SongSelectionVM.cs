using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace CUHK_IERG3080_2025_fall_Final_Project.ViewModel
{
    internal class SongSelectionVM : INotifyPropertyChanged
    {
        // Store the selected difficulty globally (for all cards)
        private string _selectedDifficulty;
        public string SelectedDifficulty
        {
            get => _selectedDifficulty;
            set
            {
                if (_selectedDifficulty != value)
                {
                    _selectedDifficulty = value;
                    OnPropertyChanged(nameof(SelectedDifficulty));

                    // Notify all buttons to reflect the new selection
                    RaiseAll();
                }
            }
        }

        /* -------- Card A -------- */
        public bool IsEasyASelected
        {
            get => SelectedDifficulty == "A_Easy";
            set { if (value) SelectedDifficulty = "A_Easy"; }
        }

        public bool IsHardASelected
        {
            get => SelectedDifficulty == "A_Hard";
            set { if (value) SelectedDifficulty = "A_Hard"; }
        }

        /* -------- Card B -------- */
        public bool IsEasyBSelected
        {
            get => SelectedDifficulty == "B_Easy";
            set { if (value) SelectedDifficulty = "B_Easy"; }
        }

        public bool IsHardBSelected
        {
            get => SelectedDifficulty == "B_Hard";
            set { if (value) SelectedDifficulty = "B_Hard"; }
        }

        /* -------- Notify all buttons -------- */
        private void RaiseAll()
        {
            OnPropertyChanged(nameof(IsEasyASelected));
            OnPropertyChanged(nameof(IsHardASelected));
            OnPropertyChanged(nameof(IsEasyBSelected));
            OnPropertyChanged(nameof(IsHardBSelected));
        }

        // INotifyPropertyChanged implementation
        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string name)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}

using System.Collections.ObjectModel;
using System.Linq;
using FinTrackBY.Models;

namespace FinTrackBY.ViewModels
{
    public class CurrentRatesViewModel : ViewModelBase
    {
        private int _selectedCurrencyId;

        public ObservableCollection<CurrencyRateRecord> Rates { get; } = new ObservableCollection<CurrencyRateRecord>();

        public int SelectedCurrencyId
        {
            get => _selectedCurrencyId;
            set => SetProperty(ref _selectedCurrencyId, value);
        }

        public void UpdateFrom(ObservableCollection<CurrencyRateRecord> source)
        {
            Rates.Clear();
            foreach (var record in source)
                Rates.Add(record);
        }
    }
}

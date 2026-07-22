using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using FinTrackBY.Models;
using FinTrackBY.Services;

namespace FinTrackBY.ViewModels
{
    public class LoadHistoryDialogViewModel : ViewModelBase
    {
        private readonly ICurrencyRepository _currencyRepository;
        private DateTime _startDate = DateTime.Today.AddDays(-30);
        private DateTime _endDate = DateTime.Today;
        private ObservableCollection<Currency> _availableCurrencies;
        private List<int> _selectedCurrencyIds;

        public LoadHistoryDialogViewModel(ICurrencyRepository currencyRepository)
        {
            _currencyRepository = currencyRepository;
            LoadCommand = new RelayCommand(Load, CanLoad);
        }

        public DateTime StartDate
        {
            get => _startDate;
            set => SetProperty(ref _startDate, value);
        }

        public DateTime EndDate
        {
            get => _endDate;
            set => SetProperty(ref _endDate, value);
        }

        public ObservableCollection<Currency> AvailableCurrencies
        {
            get => _availableCurrencies;
            set => SetProperty(ref _availableCurrencies, value);
        }

        public List<int> SelectedCurrencyIds
        {
            get => _selectedCurrencyIds;
            set => SetProperty(ref _selectedCurrencyIds, value);
        }

        public ICommand LoadCommand { get; }

        public async Task LoadAsync()
        {
            var activeCurrencies = await _currencyRepository.GetActiveCurrenciesAsync().ConfigureAwait(false);
            if (activeCurrencies == null)
                activeCurrencies = new List<Currency>();
            await App.Current.Dispatcher.InvokeAsync(() =>
            {
                AvailableCurrencies = new ObservableCollection<Currency>(activeCurrencies);
            });
        }

        public event EventHandler RequestClose;

        private void Load(object parameter)
        {
            RequestClose?.Invoke(this, EventArgs.Empty);
        }

        private bool CanLoad(object parameter)
        {
            return StartDate <= EndDate &&
                   (SelectedCurrencyIds?.Count ?? 0) > 0 &&
                   (EndDate - StartDate).TotalDays <= 365;
        }
    }
}

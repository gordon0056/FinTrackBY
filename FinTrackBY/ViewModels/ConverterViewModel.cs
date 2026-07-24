using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using FinTrackBY.Models;
using FinTrackBY.Services;

namespace FinTrackBY.ViewModels
{
    public class ConverterViewModel : ViewModelBase
    {
        private readonly ICurrencyConverter _converter;
        private decimal _amount = 1;
        private Currency _selectedFromCurrency;
        private Currency _selectedToCurrency;
        private DateTime _selectedDate = DateTime.Today;
        private decimal? _result;
        private ObservableCollection<CurrencyRateRecord> _sourceData;
        private ObservableCollection<Currency> _availableCurrencies = new ObservableCollection<Currency>();

        public ConverterViewModel(ICurrencyConverter converter)
        {
            _converter = converter;
            RecalculateCommand = new RelayCommand(Recalculate);
        }

        public decimal Amount
        {
            get => _amount;
            set
            {
                var clamped = Math.Max(value, 0);
                if (SetProperty(ref _amount, clamped))
                    Recalculate();
            }
        }

        public Currency SelectedFromCurrency
        {
            get => _selectedFromCurrency;
            set
            {
                if (SetProperty(ref _selectedFromCurrency, value))
                {
                    if (value != null)
                        Recalculate();
                }
            }
        }

        public Currency SelectedToCurrency
        {
            get => _selectedToCurrency;
            set
            {
                if (SetProperty(ref _selectedToCurrency, value))
                {
                    if (value != null)
                        Recalculate();
                }
            }
        }

        public DateTime SelectedDate
        {
            get => _selectedDate;
            set
            {
                if (SetProperty(ref _selectedDate, value))
                    Recalculate();
            }
        }

        public decimal? Result
        {
            get => _result;
            private set
            {
                if (SetProperty(ref _result, value))
                    OnPropertyChanged(nameof(ResultText));
            }
        }

        public string ResultText
        {
            get
            {
                if (!Result.HasValue)
                    return "—";

                var fromName = SelectedFromCurrency?.Cur_Abbreviation ?? "???";
                var toName = SelectedToCurrency?.Cur_Abbreviation ?? "???";
                return $"{Amount} {fromName} = {Result.Value:F4} {toName}";
            }
        }

        private string _errorMessage;
        public string ErrorMessage
        {
            get => _errorMessage;
            private set => SetProperty(ref _errorMessage, value);
        }

        public ObservableCollection<Currency> AvailableCurrencies
        {
            get => _availableCurrencies;
            set => SetProperty(ref _availableCurrencies, value);
        }

        public ICommand RecalculateCommand { get; }

        public void SetSourceData(ObservableCollection<CurrencyRateRecord> data)
        {
            _sourceData = data;
            Recalculate();
        }

        public void SetAvailableCurrencies(ObservableCollection<Currency> currencies)
        {
            AvailableCurrencies.Clear();
            foreach (var c in currencies)
                AvailableCurrencies.Add(c);
            if (SelectedFromCurrency == null && AvailableCurrencies.Count > 0)
                SelectedFromCurrency = AvailableCurrencies.FirstOrDefault(c => c.Cur_Abbreviation == "USD") ?? AvailableCurrencies[0];

            if (SelectedToCurrency == null && AvailableCurrencies.Count > 1)
                SelectedToCurrency = AvailableCurrencies.FirstOrDefault(c => c.Cur_Abbreviation == "BYN") ?? AvailableCurrencies[1];
        }

        private void Recalculate(object parameter = null)
        {
            OnPropertyChanged(nameof(ResultText));

            if (_sourceData == null || SelectedFromCurrency == null || SelectedToCurrency == null)
                return;

            var result = _converter.Convert(SelectedFromCurrency.Cur_ID, SelectedToCurrency.Cur_ID, Amount, SelectedDate, _sourceData);
            Result = result;

            if (!result.HasValue)
                ErrorMessage = "Конвертация невозможна: нет курса на выбранную дату";
            else
                ErrorMessage = null;
        }
    }
}

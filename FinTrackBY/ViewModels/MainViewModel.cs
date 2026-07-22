using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using Microsoft.Win32;
using FinTrackBY.Models;
using FinTrackBY.Services;
using FinTrackBY.Views;

namespace FinTrackBY.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly IRatesService _ratesService;
        private readonly IJsonRatesStorage _jsonStorage;
        private readonly ICurrencyRepository _currencyRepository;
        private readonly ICurrencyConverter _currencyConverter;

        private ObservableCollection<CurrencyRateRecord> _rates;
        private int _selectedCurrencyId;
        private string _statusMessage;

        public MainViewModel(IRatesService ratesService, IJsonRatesStorage jsonStorage,
            ICurrencyRepository currencyRepository, ICurrencyConverter currencyConverter)
        {
            _ratesService = ratesService;
            _jsonStorage = jsonStorage;
            _currencyRepository = currencyRepository;
            _currencyConverter = currencyConverter;

            Rates = new ObservableCollection<CurrencyRateRecord>();
            Rates.CollectionChanged += OnRatesCollectionChanged;

            ChartViewModel = new ChartViewModel();
            ConverterViewModel = new ConverterViewModel(_currencyConverter);

            OpenFileCommand = new RelayCommand(OpenFile);
            SaveFileCommand = new RelayCommand(SaveFile, CanSaveFile);
            LoadHistoryCommand = new RelayCommand(LoadHistory);
            ToggleThemeCommand = new RelayCommand(ToggleTheme);
            _ = InitializeAsync();
        }

        public ObservableCollection<CurrencyRateRecord> Rates
        {
            get => _rates;
            set => SetProperty(ref _rates, value);
        }

        public int SelectedCurrencyId
        {
            get => _selectedCurrencyId;
            set
            {
                if (SetProperty(ref _selectedCurrencyId, value))
                {
                    ChartViewModel.SelectedCurrencyId = value;
                    var currency = ConverterViewModel.AvailableCurrencies.FirstOrDefault(c => c.Cur_ID == value);
                    if (currency != null)
                        ConverterViewModel.SelectedFromCurrency = currency;
                    OnPropertyChanged(nameof(SelectedCurrency));
                }
            }
        }

        public CurrencyRateRecord SelectedCurrency
        {
            get => Rates.FirstOrDefault(r => r.CurId == SelectedCurrencyId);
            set
            {
                if (value != null)
                    SelectedCurrencyId = value.CurId;
            }
        }

        public ChartViewModel ChartViewModel { get; }
        public ConverterViewModel ConverterViewModel { get; }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ICommand OpenFileCommand { get; }
        public ICommand SaveFileCommand { get; }
        public ICommand LoadHistoryCommand { get; }
        public ICommand ToggleThemeCommand { get; }

        private async Task InitializeAsync()
        {
            string defaultPath = null;
            try
            {
                var currencies = await _currencyRepository.GetActiveCurrenciesAsync().ConfigureAwait(false);
                await App.Current.Dispatcher.InvokeAsync(() =>
                {
                    ConverterViewModel.SetAvailableCurrencies(new ObservableCollection<Currency>(currencies));
                });
                defaultPath = Properties.Settings.Default.LastRatesFilePath;
                if (string.IsNullOrEmpty(defaultPath) ||
                    defaultPath.EndsWith("currencies_cache.json", StringComparison.OrdinalIgnoreCase))
                {
                    defaultPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "rates.json");
                    Properties.Settings.Default.LastRatesFilePath = defaultPath;
                    Properties.Settings.Default.Save();
                }

                if (System.IO.File.Exists(defaultPath))
                {
                    var records = await _jsonStorage.LoadAsync(defaultPath).ConfigureAwait(false);
                    await App.Current.Dispatcher.InvokeAsync(() =>
                    {
                        foreach (var record in records)
                            Rates.Add(record);
                    });
                }

                try
                {
                    var snapshot = await _ratesService.GetTodaySnapshotWithChangeAsync().ConfigureAwait(false);
                    await _jsonStorage.MergeAndSaveAsync(defaultPath, snapshot).ConfigureAwait(false);

                    await App.Current.Dispatcher.InvokeAsync(() =>
                    {
                        Rates.Clear();
                        foreach (var record in snapshot)
                            Rates.Add(record);

                        if (SelectedCurrencyId == 0 && Rates.Count > 0)
                            SelectedCurrencyId = Rates[0].CurId;

                        ChartViewModel.SetSourceData(Rates);
                        ConverterViewModel.SetSourceData(Rates);
                    });
                    var uniqueDates = Rates.Select(r => r.Date).Distinct().Count();
                    if (uniqueDates <= 1)
                    {
                        StatusMessage = "Загрузка истории за 1 год...";
                        var endDate = DateTime.Today;
                        var startDate = endDate.AddDays(-365);
                        var allCurIds = currencies.Select(c => c.Cur_ID)
                            .Where(id => id > 0)
                            .ToList();

                        var historyRecords = await _ratesService.GetRatesForPeriodAsync(allCurIds, startDate, endDate).ConfigureAwait(false);
                        await _jsonStorage.MergeAndSaveAsync(defaultPath, historyRecords).ConfigureAwait(false);
                        var allRecords = await _jsonStorage.LoadAsync(defaultPath).ConfigureAwait(false);
                        await App.Current.Dispatcher.InvokeAsync(() =>
                        {
                            var chartData = new ObservableCollection<CurrencyRateRecord>(allRecords);
                            ChartViewModel.SetSourceData(chartData);
                            ConverterViewModel.SetSourceData(chartData);
                            StatusMessage = "Готово";
                        });
                    }
                }
                catch (Exception)
                {
                    await App.Current.Dispatcher.InvokeAsync(() =>
                    {
                        StatusMessage = "Офлайн: показаны последние сохранённые данные";

                        if (SelectedCurrencyId == 0 && Rates.Count > 0)
                            SelectedCurrencyId = Rates[0].CurId;

                        ChartViewModel.SetSourceData(Rates);
                        ConverterViewModel.SetSourceData(Rates);
                    });
                }
            }
            catch (Exception ex)
            {
                await App.Current.Dispatcher.InvokeAsync(() =>
                {
                    StatusMessage = $"Ошибка: {ex.Message}";
                });
            }
        }

        private void OpenFile(object parameter)
        {
            var dialog = new OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*"
            };
            if (dialog.ShowDialog() == true)
            {
                _ = LoadFileAsync(dialog.FileName);
            }
        }

        private async Task LoadFileAsync(string path)
        {
            var records = await _jsonStorage.LoadAsync(path).ConfigureAwait(false);
            await App.Current.Dispatcher.InvokeAsync(() =>
            {
                Rates.Clear();
                foreach (var record in records)
                    Rates.Add(record);
                Properties.Settings.Default.LastRatesFilePath = path;
                Properties.Settings.Default.Save();
                if (SelectedCurrencyId == 0 && Rates.Count > 0)
                    SelectedCurrencyId = Rates[0].CurId;

                ChartViewModel.SetSourceData(Rates);
                ConverterViewModel.SetSourceData(Rates);
            });
        }

        private void SaveFile(object parameter)
        {
            var dialog = new SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*",
                FileName = "rates.json"
            };
            if (dialog.ShowDialog() == true)
            {
                if (dialog.FileName.Equals(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "currencies_cache.json"),
                        StringComparison.OrdinalIgnoreCase) ||
                    dialog.FileName.EndsWith("currencies_cache.json", StringComparison.OrdinalIgnoreCase))
                {
                    StatusMessage = "Нельзя сохранить курсы поверх файла справочника валют (currencies_cache.json).";
                    return;
                }
                _ = SaveFileAsync(dialog.FileName);
            }
        }

        private async Task SaveFileAsync(string path)
        {
            var records = Rates.ToList();
            await _jsonStorage.SaveAsync(path, records).ConfigureAwait(false);
            Properties.Settings.Default.LastRatesFilePath = path;
            Properties.Settings.Default.Save();
        }

        private bool CanSaveFile(object parameter) => Rates.Count > 0;

        private void LoadHistory(object parameter)
        {
            var dialog = new LoadHistoryDialogView
            {
                DataContext = new LoadHistoryDialogViewModel(_currencyRepository)
            };
            if (dialog.ShowDialog() == true)
            {
                var vm = (LoadHistoryDialogViewModel)dialog.DataContext;
                _ = LoadHistoryDataAsync(vm.SelectedCurrencyIds, vm.StartDate, vm.EndDate);
            }
        }

        private async Task LoadHistoryDataAsync(IEnumerable<int> curIds, DateTime startDate, DateTime endDate)
        {
            StatusMessage = "Загрузка данных...";
            try
            {
                var records = await _ratesService.GetRatesForPeriodAsync(curIds, startDate, endDate).ConfigureAwait(false);
                string path = Properties.Settings.Default.LastRatesFilePath;
                if (string.IsNullOrEmpty(path))
                    path = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "rates.json");

                await _jsonStorage.MergeAndSaveAsync(path, records).ConfigureAwait(false);

                var allRecords = await _jsonStorage.LoadAsync(path).ConfigureAwait(false);
                await App.Current.Dispatcher.InvokeAsync(() =>
                {
                    Rates.Clear();
                    foreach (var record in allRecords)
                        Rates.Add(record);
                    StatusMessage = "Данные загружены";
                    if (SelectedCurrencyId == 0 && Rates.Count > 0)
                        SelectedCurrencyId = Rates[0].CurId;

                    ChartViewModel.SetSourceData(Rates);
                    ConverterViewModel.SetSourceData(Rates);
                });
            }
            catch (Exception ex)
            {
                await App.Current.Dispatcher.InvokeAsync(() =>
                {
                    StatusMessage = $"Ошибка загрузки: {ex.Message}";
                });
            }
        }

        private void OnRatesCollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (CurrencyRateRecord record in e.NewItems)
                    record.PropertyChanged += OnRecordPropertyChanged;
            }
            if (e.OldItems != null)
            {
                foreach (CurrencyRateRecord record in e.OldItems)
                    record.PropertyChanged -= OnRecordPropertyChanged;
            }
        }

        private void OnRecordPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (sender is CurrencyRateRecord record &&
                (e.PropertyName == nameof(CurrencyRateRecord.Abbreviation) ||
                 e.PropertyName == nameof(CurrencyRateRecord.OfficialRate)))
            {
                var path = Properties.Settings.Default.LastRatesFilePath;
                if (!string.IsNullOrEmpty(path))
                {
                    _ = SaveRecordEditAsync(record, path);
                }
            }
        }

        private async Task SaveRecordEditAsync(CurrencyRateRecord record, string path)
        {
            try
            {
                await _jsonStorage.UpdateFieldAsync(path, record.CurId, record.Date,
                    record.Abbreviation, record.OfficialRate).ConfigureAwait(false);
                await App.Current.Dispatcher.InvokeAsync(() =>
                {
                    StatusMessage = "Изменения сохранены";
                });
            }
            catch (Exception ex)
            {
                await App.Current.Dispatcher.InvokeAsync(() =>
                {
                    StatusMessage = $"Ошибка сохранения: {ex.Message}";
                });
            }
        }

        private void ToggleTheme(object parameter)
        {
            App.ToggleTheme();
            ChartViewModel.RefreshTheme();
        }
    }
}

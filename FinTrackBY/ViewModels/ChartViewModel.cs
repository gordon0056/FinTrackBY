using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using FinTrackBY.Models;
using FinTrackBY.Services;

namespace FinTrackBY.ViewModels
{
    public class ChartViewModel : ViewModelBase
    {
        private int _selectedCurrencyId;
        private string _selectedPeriod = "1M";
        private PlotModel _plotModel;

        public ChartViewModel()
        {
            SetPeriodCommand = new RelayCommand(SetPeriod);
            PlotModel = CreateEmptyModel();
            ChartController = CreateNoZoomController();
        }

        public PlotController ChartController { get; }

        private static PlotController CreateNoZoomController()
        {
            var c = new PlotController();
            c.Unbind(new OxyMouseWheelGesture());
            return c;
        }

        public int SelectedCurrencyId
        {
            get => _selectedCurrencyId;
            set
            {
                if (SetProperty(ref _selectedCurrencyId, value))
                    UpdatePlotModel();
            }
        }

        public string SelectedPeriod
        {
            get => _selectedPeriod;
            set
            {
                if (SetProperty(ref _selectedPeriod, value))
                    UpdatePlotModel();
            }
        }

        public ICommand SetPeriodCommand { get; }

        private void SetPeriod(object parameter)
        {
            if (parameter is string period && !string.IsNullOrEmpty(period))
                SelectedPeriod = period;
        }

        public PlotModel PlotModel
        {
            get => _plotModel;
            private set => SetProperty(ref _plotModel, value);
        }

        public void SetSourceData(ObservableCollection<CurrencyRateRecord> allRecords)
        {
            _sourceData = allRecords;
            UpdatePlotModel();
        }

        public void RefreshTheme()
        {
            UpdatePlotModel();
        }

        private ObservableCollection<CurrencyRateRecord> _sourceData;

        private bool IsDarkTheme
        {
            get
            {
                try
                {
                    return FinTrackBY.Properties.Settings.Default.IsDarkTheme;
                }
                catch
                {
                    return false;
                }
            }
        }

        private OxyColor GetThemeBackgroundColor()
        {
            return IsDarkTheme ? OxyColors.Black : OxyColors.White;
        }

        private OxyColor GetThemeTextColor()
        {
            return IsDarkTheme ? OxyColors.White : OxyColors.Gray;
        }

        private OxyColor GetThemeGridColor()
        {
            return IsDarkTheme ? OxyColor.FromArgb(255, 43, 45, 51) : OxyColors.LightGray;
        }

        private OxyColor GetThemeSeriesColor()
        {
            return IsDarkTheme ? OxyColor.FromArgb(255, 111, 168, 220) : OxyColors.SteelBlue;
        }

        private OxyColor GetThemeSeriesFillColor()
        {
            return IsDarkTheme
                ? OxyColor.FromArgb(50, 111, 168, 220)
                : OxyColor.FromArgb(50, 70, 130, 180);
        }

        private void UpdatePlotModel()
        {
            if (_sourceData == null || SelectedCurrencyId == 0)
            {
                PlotModel = CreateEmptyModel();
                return;
            }

            int days = 30;
            if (SelectedPeriod == "1W") days = 7;
            else if (SelectedPeriod == "1M") days = 30;
            else if (SelectedPeriod == "3M") days = 90;
            else if (SelectedPeriod == "6M") days = 180;
            else if (SelectedPeriod == "1Y") days = 365;

            var cutoff = DateTime.Today.AddDays(-days);
            var filtered = _sourceData
                .Where(r => r.CurId == SelectedCurrencyId && r.Date >= cutoff && r.OfficialRate.HasValue)
                .OrderBy(r => r.Date)
                .ToList();

            if (filtered.Count == 0)
            {
                PlotModel = CreateEmptyModel();
                return;
            }

            var record = _sourceData.FirstOrDefault(r => r.CurId == SelectedCurrencyId);
            var currencyName = record != null ? record.Abbreviation : "";

            var model = new PlotModel
            {
                Title = currencyName,
                Background = GetThemeBackgroundColor(),
                PlotAreaBorderColor = GetThemeTextColor()
            };

            var minRate = filtered.Min(r => r.OfficialRate.Value);
            var maxRate = filtered.Max(r => r.OfficialRate.Value);
            var range = maxRate - minRate;
            var padding = range * 0.1m;
            if (padding == 0) padding = 0.01m;

            model.Axes.Add(new LinearAxis
            {
                Position = AxisPosition.Left,
                Minimum = (double)(minRate - padding),
                Maximum = (double)(maxRate + padding),
                StringFormat = "F4",
                MajorGridlineStyle = LineStyle.Solid,
                MajorGridlineColor = GetThemeGridColor(),
                TextColor = GetThemeTextColor(),
                AxislineColor = GetThemeTextColor()
            });

            model.Axes.Add(new DateTimeAxis
            {
                Position = AxisPosition.Bottom,
                StringFormat = "dd.MM",
                MajorGridlineStyle = LineStyle.None,
                TextColor = GetThemeTextColor(),
                AxislineColor = GetThemeTextColor(),
                Minimum = DateTimeAxis.ToDouble(filtered.Min(r => r.Date)),
                Maximum = DateTimeAxis.ToDouble(filtered.Max(r => r.Date))
            });

            var series = new AreaSeries
            {
                Color = GetThemeSeriesColor(),
                Fill = GetThemeSeriesFillColor(),
                MarkerType = MarkerType.None,
                StrokeThickness = 2,
                TrackerFormatString = "{0}\n{2:dd.MM.yyyy}: {4:F4}"
            };

            foreach (var r in filtered)
            {
                series.Points.Add(new DataPoint(DateTimeAxis.ToDouble(r.Date), (double)r.OfficialRate.Value));
            }

            model.Series.Add(series);
            PlotModel = model;
            PlotModel.InvalidatePlot(true);
        }

        private PlotModel CreateEmptyModel()
        {
            var model = new PlotModel
            {
                Title = "Выберите валюту",
                Background = GetThemeBackgroundColor(),
                PlotAreaBorderColor = GetThemeTextColor()
            };
            model.Axes.Add(new LinearAxis { Position = AxisPosition.Left, TextColor = GetThemeTextColor() });
            model.Axes.Add(new DateTimeAxis { Position = AxisPosition.Bottom, TextColor = GetThemeTextColor() });
            return model;
        }
    }
}

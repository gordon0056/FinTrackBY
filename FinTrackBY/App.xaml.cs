using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using FinTrackBY.Services;
using FinTrackBY.ViewModels;
using FinTrackBY.Views;

namespace FinTrackBY
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var apiClient = new NbrbApiClient();
            var currencyRepository = new CurrencyRepository(apiClient);
            var jsonStorage = new JsonRatesStorage();
            var ratesService = new RatesService(apiClient, currencyRepository, jsonStorage);
            var currencyConverter = new CurrencyConverter();

            ApplyTheme();

            var mainWindow = new MainWindow();
            mainWindow.DataContext = new MainViewModel(ratesService, jsonStorage, currencyRepository, currencyConverter);
            mainWindow.Show();
        }

        private void ApplyTheme()
        {
            bool isDark = FinTrackBY.Properties.Settings.Default.IsDarkTheme;

            var existing = Resources.MergedDictionaries
                .FirstOrDefault(d => d.Source != null &&
                    (d.Source.OriginalString.EndsWith("LightTheme.xaml") ||
                     d.Source.OriginalString.EndsWith("DarkTheme.xaml")));
            if (existing != null)
                Resources.MergedDictionaries.Remove(existing);

            var themeUri = isDark
                ? new Uri("Themes/DarkTheme.xaml", UriKind.Relative)
                : new Uri("Themes/LightTheme.xaml", UriKind.Relative);
            Resources.MergedDictionaries.Add(new ResourceDictionary { Source = themeUri });

            if (isDark)
            {
                Resources[SystemColors.WindowBrushKey] = new SolidColorBrush(Color.FromRgb(0x1E, 0x1F, 0x24));
                Resources[SystemColors.WindowTextBrushKey] = new SolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xF2));
            }
            else
            {
                if (Resources.Contains(SystemColors.WindowBrushKey))
                    Resources.Remove(SystemColors.WindowBrushKey);
                if (Resources.Contains(SystemColors.WindowTextBrushKey))
                    Resources.Remove(SystemColors.WindowTextBrushKey);
            }
        }

        public static void ToggleTheme()
        {
            var app = (App)Current;
            FinTrackBY.Properties.Settings.Default.IsDarkTheme = !FinTrackBY.Properties.Settings.Default.IsDarkTheme;
            FinTrackBY.Properties.Settings.Default.Save();
            app.ApplyTheme();
        }
    }
}

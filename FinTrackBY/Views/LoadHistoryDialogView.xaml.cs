using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FinTrackBY.Models;

namespace FinTrackBY.Views
{
    public partial class LoadHistoryDialogView : Window
    {
        public LoadHistoryDialogView()
        {
            InitializeComponent();
            Loaded += async (s, e) =>
            {
                if (DataContext is ViewModels.LoadHistoryDialogViewModel vm)
                {
                    vm.RequestClose += (sender, args) =>
                    {
                        DialogResult = true;
                        Close();
                    };

                    try
                    {
                        await vm.LoadAsync();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка загрузки валют: {ex.Message}", "Ошибка",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            };
        }

        private void CurrencyListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (DataContext is ViewModels.LoadHistoryDialogViewModel vm)
            {
                vm.SelectedCurrencyIds = currencyListBox.SelectedItems
                    .Cast<Currency>()
                    .Select(c => c.Cur_ID)
                    .ToList();
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }
}

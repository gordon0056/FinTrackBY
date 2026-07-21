using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;

namespace FinTrackBY.Models
{
    public class CurrencyRateRecord : INotifyPropertyChanged
    {
        private string _abbreviation;
        private decimal? _officialRate;

        public DateTime Date { get; set; }

        public string Abbreviation
        {
            get => _abbreviation;
            set
            {
                if (_abbreviation != value)
                {
                    _abbreviation = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Name { get; set; }

        public decimal? OfficialRate
        {
            get => _officialRate;
            set
            {
                if (_officialRate != value)
                {
                    _officialRate = value;
                    OnPropertyChanged();
                }
            }
        }

        public int CurId { get; set; }
        public int Scale { get; set; }

        [JsonIgnore]
        public decimal? ChangePercent { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

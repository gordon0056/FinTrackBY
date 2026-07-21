using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FinTrackBY.Models;

namespace FinTrackBY.Services
{
    public interface ICurrencyConverter
    {
        decimal? Convert(int fromCurId, int toCurId, decimal amount, DateTime date, IEnumerable<CurrencyRateRecord> rates);
    }
}

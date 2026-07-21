using System;
using System.Collections.Generic;
using System.Linq;
using FinTrackBY.Models;

namespace FinTrackBY.Services
{
    public class CurrencyConverter : ICurrencyConverter
    {
        public decimal? Convert(int fromCurId, int toCurId, decimal amount, DateTime date, IEnumerable<CurrencyRateRecord> rates)
        {
            if (fromCurId == -1 && toCurId == -1)
                return amount; // BYN to BYN

            var fromRecord = rates.Where(r => r.CurId == fromCurId && r.Date <= date && r.OfficialRate.HasValue)
                                   .OrderByDescending(r => r.Date)
                                   .FirstOrDefault();
            var toRecord = rates.Where(r => r.CurId == toCurId && r.Date <= date && r.OfficialRate.HasValue)
                                 .OrderByDescending(r => r.Date)
                                 .FirstOrDefault();
            if (fromCurId == -1)
            {
                if (toRecord == null || !toRecord.OfficialRate.HasValue || toRecord.OfficialRate.Value == 0)
                    return null;
                
                var bynPerUnitTo = toRecord.OfficialRate.Value / toRecord.Scale;
                return amount / bynPerUnitTo;
            }
            if (toCurId == -1)
            {
                if (fromRecord == null || !fromRecord.OfficialRate.HasValue)
                    return null;
                
                var bynPerUnitFrom = fromRecord.OfficialRate.Value / fromRecord.Scale;
                return amount * bynPerUnitFrom;
            }
            if (fromRecord == null || toRecord == null ||
                !fromRecord.OfficialRate.HasValue || !toRecord.OfficialRate.HasValue ||
                toRecord.OfficialRate.Value == 0)
            {
                return null;
            }

            var rateFrom = fromRecord.OfficialRate.Value / fromRecord.Scale;
            var rateTo = toRecord.OfficialRate.Value / toRecord.Scale;

            return amount * rateFrom / rateTo;
        }
    }
}

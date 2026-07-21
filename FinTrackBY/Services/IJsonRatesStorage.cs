using System.Collections.Generic;
using System.Threading.Tasks;
using FinTrackBY.Models;

namespace FinTrackBY.Services
{
    public interface IJsonRatesStorage
    {
        Task<List<CurrencyRateRecord>> LoadAsync(string path);
        Task SaveAsync(string path, List<CurrencyRateRecord> records);
        Task MergeAndSaveAsync(string path, List<CurrencyRateRecord> newRecords);
        Task UpdateFieldAsync(string path, int curId, System.DateTime date, string newAbbreviation = null, decimal? newOfficialRate = null);
    }
}

using System.Collections.Generic;
using System.Threading.Tasks;
using FinTrackBY.Models;

namespace FinTrackBY.Services
{
    public interface ICurrencyRepository
    {
        Task<List<Currency>> GetActiveCurrenciesAsync();
        Currency GetById(int curId);
    }
}

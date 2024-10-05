using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PowerService
{
    public interface IPowerService
    {
        IEnumerable<PowerTrade> GetTrades(DateTime date);

        Task<IEnumerable<PowerTrade>> GetTradesAsync(DateTime date);
    }
}

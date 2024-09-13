using RestSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using TdAmeritrade;
using TdAmeritrade.External;
using TdAmeritrade.Models;
using TdAmeritrade.Processors;

namespace TestAppSt
{
    class Program
    {
        static void Main(string[] args)
        {

            
            var refreshToken = "ykumg7tCwLDXjvImFegMgaCgCjCeOzNkgRi1gkbQZzXn5QbXMoxubsKy+rcfy3uZYhU7hUbHTIWvWu3+MVH/60twLevAGc3bdJo6ntHoDxQd7bi5V86Q5b0sEc7OSxyl8GKtQidCjUraF2SM+sPVeO6AOIuQdpJTo4/bRUpXbcRNkIEJJoGzLcEUYiBt5i38SniIbH8fbDWmO6UeBjmh5Rya0DbQggrRwR1RGpJkIqCh+7dy5WcRzcPUtGsEJ85bBz22KV+RbYlDizqR3192DrVfbLQk8KnwkDDE44FF4lY2hv0bzpmOVi36kYpM/cSlXK2ezVMfSjU4UiR+qFyjBZUjl51bQmANQGxzfar/f6LrkOQe3DbwKTyLAnzmW27XX9atyK1xR8YU4+TZjDJMnk2Q3boFmIwlr5CydfLZLGPJPBuO6Y5QOo+IbQM100MQuG4LYrgoVi/JHHvlUFNGfUndN01r/WUoMS32NZjdsmhCa6NT7MLcbM+Q/edIndShb6rCKVaPef+rCEPNberbmn8nRT5QjUnRyz0wNTcwLlpfYeY/4ZpVMxAntIV+pmlLi2zNjSGPbVSqggCf9N/vyQHIdnaQIvTsUmx8Pwj4Qrdr5ke8i4fuvhwAgF16hi9fwJ4h5aI3MgerxicMD2g4NN+BeQnToUfOXBxVSboeOmb9NgvRe3OPzLBco/Rkvt3KntBJ0rMZAhxoMZfxbZF9BGf42kD3vsoVn+180909m7K2N5/ZjW+3TEd0q/QLfX4PCcWwVFBSCDQ+urvYS8I5p/RwuQMut/puuEkK8b4X4huD5EXbKFU8srChQ9qtdWRW7kOWWToVZtd9UL9UcOUEoLX15UyTHT9Wy0JvxhnTrk138pVgaxdnuI4q+cPX9gd7v+FHQYfezAU=212FD3x19z9sWBHDJACbC00B75E";
            var clientId = "BUJUUYLC4DYB9H7EV8WBDAZ9JRI8JFLW";
            var tdSettings = TdAmeritradeSettings.Instance;
            tdSettings.Authenticate(clientId, String.Empty, refreshToken, Convert.ToDateTime("2020-11-15"));

            var accounts = new AccountsTrading();
            var accountsData = accounts.GetAccounts();
            var marketHoursData = accounts.GetMarketHours();
            var quote = accounts.GetInstrumentQuote("NOK");
            var accountId = accountsData.accounts[0].securitiesAccount.accountId;
            var symbol = "NOK";
            var price = 4.05;
            var quantity = 2;
            var pendingAccountOrderQuantity = 0;

            AccountManager accountManager = new AccountManager()
            {
                AccountId = accountId
            };

            //accountManager.ProcessMartetTrading(new StockTrade()
            //{
            //    Symbol = symbol,
            //    Quantity = quantity,
            //    Instruction = OrderInstruction.BUY
            //});

            accountManager.ProcessLimitTrading(new StockTrade()
            {
                Symbol = symbol,
                Quantity = quantity,
                Price = price,
                Instruction = OrderInstruction.SELL_TO_OPEN
            });

            //var transaction = accounts.GetTransaction(accountsData.accounts[0].securitiesAccount.accountId, symbol, Convert.ToDateTime("2020-11-28"), DateTime.Today);
            //var securities = accounts.GetAccount(accountsData.accounts[0].securitiesAccount.accountId);
        }
    }
}

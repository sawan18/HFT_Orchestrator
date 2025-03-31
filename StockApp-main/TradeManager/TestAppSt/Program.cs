using Microsoft.Extensions.Configuration;
using RestSharp;
using System;
using System.IO;
using System.Threading.Tasks;
using TdAmeritrade;
using TdAmeritrade.Models;

namespace TestAppSt
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Load configuration
            var config = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json")
                .Build();

            // Get Schwab credentials from config
            var schwabClientId = config["SchwabApi:ClientId"];
            var schwabRefreshToken = config["SchwabApi:RefreshToken"];
            
            // Initialize Schwab authentication
            var authenticator = new SchwabAuthenticator(schwabClientId, schwabRefreshToken);
            var accessToken = await authenticator.GetAccessTokenAsync();

            // Initialize Schwab trading client
            var schwabTrader = new SchwabTrader(authenticator, config);
            
            // Example usage
            try
            {
                // Get market hours
                var marketHours = await schwabTrader.GetMarketHoursAsync();
                if (!marketHours.equity.EQ.isOpen)
                {
                    Console.WriteLine("Market is closed!");
                    return;
                }

                // Get account information
                var accounts = await schwabTrader.GetAccountsAsync();
                var accountId = accounts.accounts[0].securitiesAccount.accountId;

                // Get stock quote
                var symbol = "NOK";
                var quote = await schwabTrader.GetInstrumentQuoteAsync(symbol);
                Console.WriteLine($"Current price of {symbol}: {quote.lastPrice}");

                // Place limit order
                var quantity = 2;
                var limitPrice = Math.Round(quote.lastPrice * 0.99m, 2); // 1% below current price
                
                await schwabTrader.TradeRegularStockAsync(accountId, new StockTrade
                {
                    Symbol = symbol,
                    Quantity = quantity,
                    Instruction = OrderInstruction.SELL_TO_OPEN,
                    Price = limitPrice
                });

                Console.WriteLine($"Successfully placed order for {quantity} shares of {symbol} at {limitPrice}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }
        }
    }
}
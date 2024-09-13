using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TdAmeritrade.Models;

namespace TdAmeritrade.Processors
{
    public class AccountsTrading : StockProcessorBase
    {
        private const string ACCOUNTS_API = "/accounts";
        private const string QUOTES_API = "/marketdata/quotes";
        private const string MARKET_HOURS_API = "/marketdata/hours";

        /// <summary>
        /// Gets the stock history for a symbol over a date period.
        /// </summary>
        /// <returns>AccountsResponse</returns>
        public AccountsResponse GetAccounts()
        {
            var parameters = new Dictionary<string, object>
            {
                { "fields", "positions,orders" }
            };
            AuthenticationProcessor.ProcessAuthentication();
           var payload = restController.GetData(AuthenticationProcessor.AccessToken, ACCOUNTS_API, parameters);
           


            return JsonConvert.DeserializeObject<AccountsResponse>(
               @"{
                Accounts:{{Payload}}
            }".Replace("{{Payload}}", payload)
            );
        }

        /// <summary>
        /// Gets the stock history for a symbol over a date period.
        /// </summary>
        /// <param name="accountId">Account id</param>
        /// <returns>Account</returns>
        public Account GetAccount(string accountId)
        {
            var apiPath = String.Format("{0}/{1}", ACCOUNTS_API, accountId);
            var parameters = new Dictionary<string, object>
            {
                { "fields", "positions,orders" }
            };
            AuthenticationProcessor.ProcessAuthentication();
            var payload = restController.GetData(AuthenticationProcessor.AccessToken, apiPath, parameters);
            return JsonConvert.DeserializeObject<Account>(
               payload
             );
        }

        /// <summary>
        /// Get account orders for a specific period.
        /// </summary>
        /// <param name="accountId">Account id</param>
        /// <returns>OrdersResponse</returns>
        public OrdersResponse GetAccountOrders(string accountId, DateTime startDate, DateTime endDate, OrderStatus status = OrderStatus.ACCEPTED, int maxResults = 100)
        {
            var apiPath = String.Format("{0}/{1}/orders", ACCOUNTS_API, accountId);

            var parameters = new Dictionary<string, object>
            {
                { "maxResults", maxResults },
                { "fromEnteredTime", startDate.ToString("yyyy-MM-dd") },
                { "toEnteredTime", endDate.ToString("yyyy-MM-dd") },
                { "status",  status}
            };

            AuthenticationProcessor.ProcessAuthentication();
            var response = restController.GetData(AuthenticationProcessor.AccessToken, apiPath, parameters);
            return JsonConvert.DeserializeObject<OrdersResponse>(
               @"{
                Orders:{{Payload}}
                 }".Replace("{{Payload}}", response)
            );
        }

        /// <summary>
        /// Gets a transaction for a specific symbol
        /// </summary>
        /// <param name="accountId">Account Id</param>
        /// <param name="symbol">Symbol</param>
        /// <param name="startDate">Start date</param>
        /// <param name="endDate">End date</param>
        /// <returns></returns>
        public TransactionsResponse GetTransaction(string accountId, string symbol, DateTime startDate, DateTime endDate)
        {
            var apiPath = String.Format("{0}/{1}/transactions", ACCOUNTS_API, accountId);

            var parameters = new Dictionary<string, object>
            {
                { "type", "ALL" },
                { "symbol", symbol },
                { "startDate", startDate.ToString("yyyy-MM-dd") },
                { "endDate", endDate.ToString("yyyy-MM-dd") }
            };

            AuthenticationProcessor.ProcessAuthentication();
            var response = restController.GetData(AuthenticationProcessor.AccessToken, apiPath, parameters);
            return JsonConvert.DeserializeObject<TransactionsResponse>(
               @"{
                Transactions:{{Payload}}
                 }".Replace("{{Payload}}", response)
            );
        }

        /// <summary>
        /// Cancels an order.
        /// </summary>
        /// <param name="accountId">Account id</param>
        /// <param name="orderId">Order id</param>
        /// <returns></returns>
        public string CancelOrder(string accountId, string orderId)
        {
            var apiPath = String.Format("{0}/{1}/orders/{2}", ACCOUNTS_API, accountId, orderId);
            AuthenticationProcessor.ProcessAuthentication();
            return restController.Delete(AuthenticationProcessor.AccessToken, apiPath);
        }

        /// <summary>
        /// Trades regular stock on a trading day.
        /// </summary>
        /// <param name="accountId">Account id</param>
        /// <param name="symbol">Stock symbol</param>
        /// <param name="instruction">Purchase Instruction</param>
        /// <param name="quantity">Stock quantity</param>
        /// <returns></returns>
        public string TradeRegularStock(string accountId, string symbol, OrderInstruction instruction, int quantity)
        {
            var trade = new StockTrade()
            {
                Instruction = instruction,
                Quantity = quantity,
                Symbol = symbol
            };

            return TradeRegularStock(accountId, new List<StockTrade>() { trade });
        }

        /// <summary>
        /// Trades regular stocks at market value.
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="trades"></param>
        /// <returns></returns>
        public string TradeRegularStock(string accountId, List<StockTrade> trades)
        {
            // We can only trade market orders during the regular trading hours.
            if (!IsMarketHours())
            {
                throw new Exception("Market orders can only be submitted during the trading hours");
            }

            var apiPath = String.Format("{0}/{1}/orders", ACCOUNTS_API, accountId);
            StockPurchaseRequest stockPurchaseRequest = new StockPurchaseRequest()
            {
                orderType = OrderType.MARKET.ToString(),
                session = OrderSession.NORMAL.ToString(),
                duration = OrderDuration.DAY.ToString(),
                orderStrategyType = OrderStrategyType.SINGLE.ToString()
            };

            stockPurchaseRequest.orderLegCollection = new List<OrderLegCollection>();
            foreach (var trade in trades)
            {
                // Set the order collection details.
                stockPurchaseRequest.orderLegCollection.Add(new OrderLegCollection()
                {
                    instruction = trade.Instruction.ToString(),
                    quantity = trade.Quantity,
                    instrument = new Instrument() { assetType = OrderAssetType.EQUITY.ToString(), symbol = trade.Symbol }
                });
            }

            return restController.Post(AuthenticationProcessor.AccessToken, apiPath, JsonConvert.SerializeObject(stockPurchaseRequest));
        }

        /// <summary>
        /// Trades a limit regular stock at a set price
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="price"></param>
        /// <param name="trade"></param>
        /// <returns></returns>
        public string TradeLimitRegularStock(string accountId, StockTrade trade)
        {
            var apiPath = String.Format("{0}/{1}/orders", ACCOUNTS_API, accountId);
            BuyLimitSingleOption stockPurchaseRequest = new BuyLimitSingleOption()
            {
                complexOrderStrategyType = ComplexOrderStrategyType.NONE.ToString(),
                orderType = OrderType.LIMIT.ToString(),
                session = OrderSession.NORMAL.ToString(),
                duration = OrderDuration.DAY.ToString(),
                orderStrategyType = OrderStrategyType.SINGLE.ToString(),
                price = trade.Price
            };

            stockPurchaseRequest.orderLegCollection = new List<OrderLegCollection>();
            // Set the order collection details.
            stockPurchaseRequest.orderLegCollection.Add(new OrderLegCollection()
            {
                instruction = trade.Instruction.ToString(),
                quantity = trade.Quantity,
                instrument = new Instrument() { assetType = OrderAssetType.EQUITY.ToString(), symbol = trade.Symbol }
            });

            return restController.Post(AuthenticationProcessor.AccessToken, apiPath, JsonConvert.SerializeObject(stockPurchaseRequest));
        }

        /// <summary>
        /// Trades multiple limit stocks.
        /// </summary>
        /// <param name="accountId"></param>
        /// <param name="trades"></param>
        /// <returns></returns>
        public bool TradeLimitRegularStock(string accountId, List<StockTrade> trades)
        {
            // We submit each symbol as a separate order.
            foreach(var trade in trades)
            {
                TradeLimitRegularStock(accountId, trade);
            }

            return true;
        }

        /// <summary>
        /// Get market hour details.
        /// </summary>
        /// <returns></returns>
        public MarketHours GetMarketHours()
        {
            var parameters = new Dictionary<string, object>
            {
                { "markets", "EQUITY"},
                { "date", DateTime.Today.ToString("yyyy-MM-dd")},
            };

            AuthenticationProcessor.ProcessAuthentication();
            var payload = restController.GetData(AuthenticationProcessor.AccessToken, MARKET_HOURS_API, parameters);

            return JsonConvert.DeserializeObject<MarketHours>(
              payload
            );
        }

        /// <summary>
        /// Gets the current quote for a stock based on its symbol.
        /// </summary>
        /// <param name="symbol">Symbol</param>
        /// <returns>StockQuote</returns>
        public StockQuote GetInstrumentQuote(string symbol)
        {            
            var parameters = new Dictionary<string, object>
            {
                { "symbol", symbol}
            };
            AuthenticationProcessor.ProcessAuthentication();
            var payload = restController.GetData(AuthenticationProcessor.AccessToken, QUOTES_API, parameters);

            //Weird payload from Td Ameritrade. We need to format the symbol section.
            payload = payload.Replace(
                "\"" + symbol.Trim().ToUpper() + "\":{"
                , "\"stock\": {");
            payload = payload.Replace("52WkHigh", "fiftytwoWkHigh");
            payload = payload.Replace("52WkLow", "fiftytwoWkLow");

            return JsonConvert.DeserializeObject<StockQuote>(
              payload
            );
        }

        /// <summary>
        /// Gets the status of the market.
        /// </summary>
        /// <returns></returns>
        public bool IsMarketHours()
        {
            var marketHours = GetMarketHours();
            return marketHours.equity.EQ.isOpen;
        }
    }
}

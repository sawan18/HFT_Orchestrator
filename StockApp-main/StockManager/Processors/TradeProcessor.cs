using StockManager.Data;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TdAmeritrade;
using TdAmeritrade.External;
using TdAmeritrade.Models;
using TdAmeritrade.Processors;

namespace StockManager.Processors
{
    public class TradeProcessor
    {        
        private AccountsTrading accountsTrading = new AccountsTrading();
        private AccountsResponse accountsResponse = new AccountsResponse();
        private MarketHours marketHours = new MarketHours();
        private Dictionary<string, string> settings = new Dictionary<string, string>();
        private static RunSettings runSettings = RunSettings.Instance;

        /// <summary>
        /// Gets and sets the test mode flag.
        /// </summary>
        public bool TestMode { get; private set; }

        /// <summary>
        /// Gets and sets the Sql server connection parameters
        /// </summary>
        public SqlServerCalls SqlServerCalls { get; private set; }

        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="sqlServercalls"></param>
        /// <param name="testMode"></param>
        public TradeProcessor(SqlServerCalls sqlServercalls, bool testMode = true)
        {
            this.SqlServerCalls = sqlServercalls;
            this.TestMode = testMode;
            settings = SqlServerCalls.GetAccountSettings();
            InitializeAccounts(settings);
        }

        private double GetBalance(SecuritiesAccount securitiesAccount)
        {
            if (securitiesAccount.type == "MARGIN")
                return securitiesAccount.currentBalances.cashBalance;
            else
                return securitiesAccount.projectedBalances.cashAvailableForTrading;

        }
        /// <summary>
        /// Process stock trades for a reference date.
        /// </summary>
        public void ProcessTrades()
        {            

            DateTime referenceDate = SqlServerCalls.GetLastTradeDate();
            List<StockTrade> triggeredTrades = GetTriggeredTrades(referenceDate);
            List<StockTrade> processedTrades = GetProcessedTrades(DateTime.Today);

            ShowBalanacesAndPositions(false);

            foreach (var triggeredTrade in triggeredTrades)
            {
                runSettings.Log.PrintGap();
                runSettings.Log.LogMessage(String.Format("Triggered trade for {0} - I:{1}, P:{2}, Q:{4}, Limit:{3}", triggeredTrade.Symbol, triggeredTrade.Instruction, triggeredTrade.Price, triggeredTrade.Limit, triggeredTrade.Quantity));

                var numbers = processedTrades.Where(x => triggeredTrade.Symbol.Equals(x.Symbol, StringComparison.CurrentCultureIgnoreCase)).Count();
                if (numbers > 0)
                {
                    runSettings.Log.LogMessage(String.Format("Trade for {0} already processed - I:{1}, P:{2}, Q:{4}, Limit:{3}", triggeredTrade.Symbol, triggeredTrade.Instruction, triggeredTrade.Price, triggeredTrade.Limit, triggeredTrade.Quantity));
                    continue;
                } 

                if (IsTradingPeriod())
                {
                    runSettings.Log.LogMessage(String.Format("Processing trade {0} - I:{1}, P:{2}, Q:{4}, Limit:{3}", triggeredTrade.Symbol, triggeredTrade.Instruction, triggeredTrade.Price, triggeredTrade.Limit, triggeredTrade.Quantity));
                    if (triggeredTrade.Instruction == OrderInstruction.BUY)
                    {
                        var algorithmPerformance = SqlServerCalls.GetAlgorithmPerformance(triggeredTrade.Symbol);
                        if (algorithmPerformance < 0.25)
                        {
                            runSettings.Log.LogMessage(String.Format("Symbol \"{0}\" purchase is cancelled because of poor performance - {1}"
                                , triggeredTrade.Symbol, (algorithmPerformance * 100).ToString("#0.00")));
                            continue;
                        }

                        var adjustedTriggeredTrade = AdjustSymbolPurchase(triggeredTrade);

                        // Place the order after adjustment.
                        PlaceBuyOrder(adjustedTriggeredTrade);
                    }                        
                    else
                        PlaceSellOrder(triggeredTrade);
                }
                else
                {
                    runSettings.Log.LogMessage("Current trade is ignored as this is outside the defined monitoring period");
                }
                
            }

            // Get the current status of the accounts.
            accountsResponse = accountsTrading.GetAccounts();
            var orderStrategies = accountsResponse.accounts[0].securitiesAccount.orderStrategies;
            if (orderStrategies == null)
            {
                runSettings.Log.LogMessage("No orders found. No trade is placed in this cycle");
                return;
            }

            var filledOrders = orderStrategies.Where(x => x.status.ToString() == OrderStatus.FILLED.ToString());

            // Process processed orders
            foreach (var order in filledOrders)
            {
                var orderLegCollection = order.orderLegCollection[0];
                var decision = orderLegCollection.instruction == "BUY" ? Model.Decision.Buy : Model.Decision.Sell;

                runSettings.Log.LogMessage(String.Format("Saving filled order {0} - ID:{1}, Time:{2}, Ins:{3}, Pos:{4}, Price: {5}, Qty: {6}"
                    , orderLegCollection.instrument.symbol
                    , order.orderId.ToString()
                    , order.enteredTime
                    , orderLegCollection.instruction
                    , orderLegCollection.positionEffect                    
                    , (double)order.price
                    , order.filledQuantity));

                // Save the filled orders
                SqlServerCalls.SaveActualTrade(orderLegCollection.instrument.symbol, order.enteredTime, decision, orderLegCollection.positionEffect, order.orderId.ToString(), (double)order.price, order.filledQuantity);
            }

            var queuedOrders = orderStrategies.Where(x => x.enteredTime.Date == DateTime.Today && x.status.ToString() == OrderStatus.QUEUED.ToString());
            foreach (var order in queuedOrders)
            {
                var orderLegCollection = order.orderLegCollection[0];
                var decision = orderLegCollection.instruction == "BUY" ? Model.Decision.Buy : Model.Decision.Sell;

                runSettings.Log.LogMessage(String.Format("Queued order {0} - ID:{1}, Time:{2}, Ins:{3}, Pos:{4}, Price: {5}, Qty: {6}, Rem Qty: {7}"
                    , orderLegCollection.instrument.symbol
                    , order.orderId.ToString()
                    , order.enteredTime
                    , orderLegCollection.instruction
                    , orderLegCollection.positionEffect
                    , (double)order.price
                    , order.quantity
                    , order.remainingQuantity
                    ));
            }            

            if (this.TestMode)
            {
                runSettings.Log.LogMessage("Test Mode Cleanup");

                var securitiesAccount = accountsResponse.accounts[0].securitiesAccount;
                var accountId = securitiesAccount.accountId;
                AccountManager accountManager = new AccountManager()
                {
                    AccountId = accountId
                };

                // Cancel all queued trades
                foreach (var triggeredTrade in triggeredTrades)
                {
                    runSettings.Log.LogMessage(String.Format("Test mode auto cancelling of trade {0} - I:{1}, P:{2}, Q:{4}, Limit:{3}", triggeredTrade.Symbol, triggeredTrade.Instruction, triggeredTrade.Price, triggeredTrade.Limit, triggeredTrade.Quantity));
                    accountManager.CancelPendingOrders(triggeredTrade);
                }
            }

            ShowBalanacesAndPositions(true);
        }

        /// <summary>
        /// Predicting stock prices.
        /// </summary>
        public void PredictStockPrices()
        {
            var dt = SqlServerCalls.GetStockDefinitions();
            runSettings.Log.PrintGap();
            runSettings.Log.LogMessage(String.Format("Predicting stock prices at {0}", DateTime.Now));

            foreach (DataRow row in dt.Rows)
            {                
                PredictStockPrice(row["Symbol"].ToString());               
            }
        }

        private double PredictStockPrice(string symbol)
        {
            // Get the quote for this stock
            var quote = accountsTrading.GetInstrumentQuote(symbol);

            // This is the last price the stock was traded.
            double referencePrice;

            // In predicting the new stock price, we review both 
            // the current bids and asks.
            if (quote.stock == null)
            {
                return 0.0;
            }

            var totalThresholdSizes = quote.stock.bidSize + quote.stock.askSize;
            var priceDiff = quote.stock.askPrice - quote.stock.bidPrice;

            /**
            Optional approach for picking the bid and ask prices.
            ====================================================
            if (quote.stock.askSize > quote.stock.bidSize)
            {
                // We tilt towards the price of the max bid size.
                referencePrice = quote.stock.bidPrice;
            }
            else
            {
                // We have more bidders for the stock. 
                // Stock will likely sell at the ask price.
                referencePrice = quote.stock.askPrice;
            }
            */
            referencePrice = Math.Round((quote.stock.askPrice - (priceDiff / totalThresholdSizes * quote.stock.bidSize)), 3);
            

            runSettings.Log.LogMessage(String.Format("Symbol: {0}: Open price: {1}, High: {2}, Low: {3}"
                , symbol
                , quote.stock.openPrice
                , quote.stock.highPrice
                , quote.stock.lowPrice)
             );

            runSettings.Log.LogMessage(String.Format("Symbol: {0}: Last price: {1}, Last size: {2}"
                , symbol
                , quote.stock.lastPrice
                , quote.stock.lastSize)
             );

            runSettings.Log.LogMessage(String.Format("Symbol: {0}: Bid: {1}@{2}, Ask: {3}@{4}"
                , symbol
                , quote.stock.bidSize
                , quote.stock.bidPrice
                , quote.stock.askSize
                , quote.stock.askPrice)
             );            

            runSettings.Log.LogMessage(String.Format("Predicted price for stock {0} @ {1}. Price: {2}", symbol, DateTime.Now, referencePrice));
            return referencePrice;
        }

        private void PlaceBuyOrder(StockTrade trade)
        {
            double referencePrice = PredictStockPrice(trade.Symbol);

            // Pr < Pmin
            // Price has dropped below our tracked minimum
            // We don't place the order since price is still dropping
            if (referencePrice < trade.Limit)
            {
                runSettings.Log.LogMessage(String.Format("Trading ignored temporarily as price is still dropping {0} => I:{1}, Q:{2}, P:{3}", trade.Symbol, trade.Instruction, trade.Quantity, trade.Price));
                return;
            }

            var tradeInstruction = OrderInstruction.BUY;
            if (!IsMarketOpen())
            {
                tradeInstruction = OrderInstruction.BUY_TO_OPEN;
            }

            // Pr >= P And Po <= Py
            // Price is coming up as predicted, we need to place the order right away
            if (referencePrice > trade.Limit && referencePrice <= trade.Price)
            {
                trade.Price = referencePrice;
            }

            if (referencePrice > trade.Price)
            {
                trade.Price += (referencePrice - trade.Price) / 2.0;
            }

            trade.Instruction = tradeInstruction;
            PlaceTrade(trade);
        }
        private void PlaceSellOrder(StockTrade trade)
        {       
            
            var referencePrice = PredictStockPrice(trade.Symbol);

            // Po > Px
            // Price has dropped below our tracked minimum
            // We don't place the order since price is still rising
            if (referencePrice > trade.Limit)
            {                
                runSettings.Log.LogMessage(String.Format("Trading ignored temporarily as price is still rising {0} => I:{1}, Q:{2}, P:{3}", trade.Symbol, trade.Instruction, trade.Quantity, trade.Price));
                return;
            }
            
            var tradeInstruction = OrderInstruction.SELL;
            if (!IsMarketOpen())
            {
                tradeInstruction = OrderInstruction.SELL_TO_OPEN;
            }

            // Po >= Px And Po <= Py
            // Price is coming up as predicted, we need to place the order right away
            if (referencePrice < trade.Limit && referencePrice >= trade.Price)
            {
                trade.Price = referencePrice;                
            }

            // Po < P
            if (referencePrice < trade.Price)
            {
                // Price is still sliding down. 
                // We will cut our losses.
                trade.Price -= (trade.Price - referencePrice) / 2.0;
            }

            trade.Instruction = tradeInstruction;

            if (!IsSellProfitable(trade))
            {
                runSettings.Log.LogMessage(String.Format("Trading ignored as the current price is below the original purchase price for {0} => I:{1}, Q:{2}, P:{3}", trade.Symbol, trade.Instruction, trade.Quantity, trade.Price));
                return;
            }

            PlaceTrade(trade);                   
        }

        private void PlaceTrade(StockTrade trade)
        {
            accountsResponse = accountsTrading.GetAccounts();
            var securitiesAccount = accountsResponse.accounts[0].securitiesAccount;
            var accountId = securitiesAccount.accountId;
            AccountManager accountManager = new AccountManager()
            {
                AccountId = accountId
            };            

            var orderStrategies = securitiesAccount.orderStrategies;
            OrderStrategy activeTrade = null;
            if (orderStrategies != null)
            {
                activeTrade = orderStrategies.Where(
                    x => x.orderLegCollection[0].instrument.symbol == trade.Symbol
                    && x.enteredTime.Date == DateTime.Today && x.status.ToString() == OrderStatus.QUEUED.ToString()
                    ).FirstOrDefault();
            }           

            if ((activeTrade != null && (DateTime.Now - activeTrade.enteredTime).TotalMinutes < 90))
            {
                runSettings.Log.LogMessage(String.Format("Trading ignored because of an active trade on queue {0} => I:{1}, Q:{2}, P:{3}", trade.Symbol, trade.Instruction, trade.Quantity, trade.Price));
                return;
            } else if(activeTrade != null)
            {
                runSettings.Log.LogMessage(String.Format("Cancelling order {0} - ID: {1}, P:{2}, Q:{3}", trade.Symbol, activeTrade.orderId, activeTrade.price, activeTrade.orderLegCollection[0].quantity));
                accountManager.CancelPendingOrders(trade);
            }        

            try
            {
                if (!TestMode)
                {

                    /**
                     * Place market orders during the trading period.
                    if (!trade.Instruction.ToString().Contains("OPEN"))
                    {
                        accountManager.ProcessMarketTrading(trade);
                    }
                    else
                    {
                        accountManager.ProcessLimitTrading(trade);
                    }
                    **/
                }
                else
                {
                    // Set the price above the margin that it cannot buy/sell
                    if (trade.Instruction.ToString().Contains("BUY"))
                    {
                        trade.Price -= trade.Price / 2.0;
                    }
                    else
                    {
                        trade.Price += trade.Price / 2.0;
                    }
                }

                if (!PerformFinalTradeValidation(accountManager, securitiesAccount, trade))
                    return;

                trade.Price = Math.Round(trade.Price, 2);
                trade.Quantity = GetPositionQuantity(trade);

                accountManager.ProcessLimitTrading(trade);
                runSettings.Log.LogMessage(String.Format("Trade successfully placed for {0} => I:{1}, Q:{2}, P:{3}, Limit: {4}", trade.Symbol, trade.Instruction, trade.Quantity, trade.Price, trade.Limit));
                

            } catch(Exception ex)
            {
                runSettings.Log.LogError(String.Format("Trade failed for {0} => I:{1}, Q:{2}, P:{3} | {4}", trade.Symbol, trade.Instruction, trade.Quantity, trade.Price, ex.Message));
            }
        }

        /// <summary>
        /// We try to sell the stock when it's profitable.
        /// </summary>
        /// <param name="trade"></param>
        /// <returns></returns>
        private bool IsSellProfitable(StockTrade trade)
        {
            return true;

            DataTable table = SqlServerCalls.GetSymbolLastBuy(trade.Symbol, DateTime.Today);
            
            if(table == null || table.Rows.Count == 0)
            {
                return true;
            }
            
            if(Convert.ToInt32(table.Rows[0]["TradeType"]) != 1)
            {
                // If the last transaction is a sell, then we can sell this position
                return true;
            }

            var purchasePrice = Convert.ToDouble(table.Rows[0]["Price"]);

            if (trade.Price > purchasePrice)
            {
                // This curve is appropriate for our position
                return true;
            }

            return false;
        }

        private StockTrade AdjustSymbolPurchase(StockTrade trade)
        {
            var securitiesAccount = accountsResponse.accounts[0].securitiesAccount;
            if (securitiesAccount.positions != null && securitiesAccount.positions.Count > 0)
            {               
                foreach (var position in securitiesAccount.positions)
                {
                    if (trade.Symbol.Equals(position.instrument.symbol, StringComparison.CurrentCultureIgnoreCase))
                    {
                        trade.Quantity = position.longQuantity >= (double) trade.Quantity ? 0 : trade.Quantity - (int) position.longQuantity;
                        break;
                    }
                }
            }

            return trade;
        }

        private void ShowBalanacesAndPositions(bool after)
        {
            runSettings.Log.LogMessage(String.Format("Account balances and positions {0}", after ? " - after trade." : "."));

            var securitiesAccount = accountsResponse.accounts[0].securitiesAccount;
            var accountId = securitiesAccount.accountId;
            AccountManager accountManager = new AccountManager()
            {
                AccountId = accountId
            };

            runSettings.Log.LogMessage("Account: " + securitiesAccount.accountId);
            runSettings.Log.LogMessage("Account type: " + securitiesAccount.type);

            if (securitiesAccount.type != "MARGIN")
            {
                runSettings.Log.LogMessage(String.Format("Account {0}. Cash available for trading: {1}", accountId, securitiesAccount.currentBalances.cashAvailableForTrading));

            }
            else
            {
                runSettings.Log.LogMessage(String.Format("Account {0}. Cash available for trading: {1}", accountId, securitiesAccount.currentBalances.cashBalance));
                runSettings.Log.LogMessage(String.Format("Account {0}. Liquidation value: {1}", accountId, securitiesAccount.currentBalances.liquidationValue));
            }

            if (securitiesAccount.positions != null  && securitiesAccount.positions.Count > 0)
            {
                runSettings.Log.LogMessage("Existing positions");
                foreach (var position in securitiesAccount.positions)
                {
                    var stock = position.instrument.symbol;
                    runSettings.Log.LogMessage(String.Format("Account position for {0} => Q:{1}, Avg. Price:{2}, Market Value:{3}", stock, position.longQuantity, position.averagePrice, position.marketValue));

                }
            }            
        }

        private int GetPositionQuantity(StockTrade trade)
        {
            var securitiesAccount = accountsResponse.accounts[0].securitiesAccount;
            if (securitiesAccount.positions != null && securitiesAccount.positions.Count > 0)
            {
                foreach (var position in securitiesAccount.positions)
                {
                    if (position.instrument.symbol.Equals(trade.Symbol, StringComparison.CurrentCultureIgnoreCase) 
                            && trade.Quantity != Convert.ToInt32(position.longQuantity))
                    {
                        runSettings.Log.LogMessage(String.Format("Trade quantity for {0} adjusted to Q:{1} and trade placed at {2}", trade.Symbol, position.longQuantity, trade.Price.ToString("#0.00")));
                        return Convert.ToInt32(position.longQuantity);
                    }
                }
            }

            return trade.Quantity;
        }

        private bool PerformFinalTradeValidation(AccountManager accountManager, SecuritiesAccount securitiesAccount, StockTrade trade)
        {
            // Check the stock position
            if (trade.Instruction.ToString().Contains("SELL"))
            {
                var position = accountManager.GetInstrumentPosition(trade.Symbol);
                if (position == null)
                {
                    runSettings.Log.LogMessage(String.Format("Trading ignored because of lack of existing position  for {0} => I:{1}, Q:{2}, P:{3}", trade.Symbol, trade.Instruction, trade.Quantity, trade.Price));
                    return false;
                }
            }
            else
            {
                // Ensure that we adjusted the quantity to fit the cash balance
                var balance = GetBalance(securitiesAccount);
                if (balance < trade.Price * trade.Quantity)
                {
                    trade.Quantity = (int)(balance / trade.Price);
                }

                if (trade.Quantity == 0)
                {
                    runSettings.Log.LogMessage(String.Format("Trading ignored because of insufficient balance {4} for {0} => I:{1}, Q:{2}, P:{3}"
                        , trade.Symbol
                        , trade.Instruction
                        , trade.Quantity
                        , trade.Price
                        , balance));
                    return false;
                }
            }

            return true;
        }
        private List<StockTrade> GetTriggeredTrades(DateTime referenceDate)
        {
            DataTable periodTrades = SqlServerCalls.GetTradeTriggers(referenceDate);
            List<TdAmeritrade.Models.StockTrade> trades = new List<TdAmeritrade.Models.StockTrade>();

            if (periodTrades != null && periodTrades.Rows.Count > 0)
            {
                foreach (DataRow row in periodTrades.Rows)
                {
                    if (Convert.ToInt32(row["Row_No"]) == 1)
                    {
                        trades.Add(
                            new TdAmeritrade.Models.StockTrade()
                            {
                                Instruction = Convert.ToInt32(row["TradeType"]) == 1 ? OrderInstruction.BUY : OrderInstruction.SELL,
                                Symbol = row["Symbol"].ToString(),
                                Quantity = Convert.ToInt32(row["Volume"]),
                                Price = Convert.ToDouble(row["Price"]),
                                Limit = Convert.ToDouble(row["Limit"])

                            }
                        );
                    }
                }
            }

            return trades;
        }

        private List<StockTrade> GetProcessedTrades(DateTime referenceDate)
        {
            DataTable periodTrades = SqlServerCalls.GetSavedActualTrades(referenceDate);
            List<TdAmeritrade.Models.StockTrade> trades = new List<TdAmeritrade.Models.StockTrade>();

            if (periodTrades != null && periodTrades.Rows.Count > 0)
            {
                foreach (DataRow row in periodTrades.Rows)
                {
                    trades.Add(
                        new TdAmeritrade.Models.StockTrade()
                        {
                            Instruction = Convert.ToInt32(row["TradeType"]) == 1 ? OrderInstruction.BUY : OrderInstruction.SELL,
                            Symbol = row["Symbol"].ToString(),
                            Quantity = Convert.ToInt32(row["Volume"]),
                            Price = Convert.ToDouble(row["Price"]),
                            OrderId = row["OrderId"].ToString()
                        }
                    );
                }
            }

            return trades;
        }

        private void InitializeAccounts(Dictionary<string,string> settings)
        {
            var refreshToken = settings["RefreshToken"];
            var clientId = settings["ClientId"];
            var refreshTokenDate = Convert.ToDateTime(settings["RefreshTokenDate"]);
            var tdSettings = TdAmeritradeSettings.Instance;

            tdSettings.Authenticate(clientId, String.Empty, refreshToken, refreshTokenDate);
            accountsResponse = accountsTrading.GetAccounts();
            marketHours = accountsTrading.GetMarketHours();
        }

        private bool IsMarketOpen()
        {
            if (marketHours.equity != null && marketHours.equity.EQ != null)
                return marketHours.equity.EQ.isOpen;
            else
            {
                var tradingStart = Convert.ToDateTime(DateTime.Today.ToString("yyyy-MM-dd") + " 09:30");
                var tradingEnd = Convert.ToDateTime(DateTime.Today.ToString("yyyy-MM-dd") + " 16:30");

                if (DateTime.Today.DayOfWeek == DayOfWeek.Saturday || DateTime.Today.DayOfWeek == DayOfWeek.Sunday)
                {
                    return false;
                }
                else if (tradingStart <= DateTime.Now && tradingEnd >= DateTime.Now)
                    return true;
                else
                    return false;
            }
        }

        private bool IsTradingPeriod()
        {
            var monitoringStart = Convert.ToDateTime(DateTime.Today.ToString("yyyy-MM-dd") + " " + settings["MonitoringStart"]);
            var monitoringEnd = Convert.ToDateTime(DateTime.Today.ToString("yyyy-MM-dd") + " " + settings["MonitoringEnd"]);

            if (monitoringStart <= DateTime.Now && monitoringEnd >= DateTime.Now)
                return true;
            else
                return false;
        }
    }
}

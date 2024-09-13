using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TdAmeritrade.Models;
using TdAmeritrade.Processors;

namespace TdAmeritrade.External
{
    public class AccountManager
    {
        private AccountsTrading accountAndTrading = new AccountsTrading();
        private SecuritiesAccount accountsData = new SecuritiesAccount();

        public string AccountId { get; set; }

        
        /// <summary>
        /// Process limit order trades.
        /// </summary>
        /// <param name="trades">Applicable trades.</param>
        public void ProcessLimitTrading(List<StockTrade> trades)
        {
            foreach(var trade in trades)
            {
                ProcessLimitTrading(trade);
            }
        }

        /// <summary>
        /// Process market order trades.
        /// </summary>
        /// <param name="trades">Applicable Trades.</param>
        public void ProcessMarketTrading(List<StockTrade> trades)
        {
            foreach (var trade in trades)
            {
                ProcessMarketTrading(trade);
            }
        }

        /// <summary>
        /// Process market trade order.
        /// </summary>
        /// <param name="trade">A specific equity trade.</param>
        public void ProcessMarketTrading(StockTrade trade)
        {
            accountsData = RefreshAccountData();
            CancelPendingOrders(trade);

            if (trade.Instruction.ToString().Contains("BUY"))
            {
                var quote = accountAndTrading.GetInstrumentQuote(trade.Symbol);
                var balance = GetBalance(accountsData);

                if (balance  >= quote.stock.lastPrice * trade.Quantity)
                {
                    // Trade and place a limit order for the symbol
                    accountAndTrading.TradeRegularStock(AccountId, trade.Symbol, trade.Instruction, trade.Quantity);
                }
                else
                {
                    throw new Exception(
                        String.Format("Stock balance for account {0} is too low (US $ {1}) to buy the equity on {2}({3} units @ US ${4})"
                        , AccountId
                        , balance
                        , trade.Symbol
                        , trade.Quantity
                        , trade.Price)
                     );
                }
            }
            else
            {
                // We are selling the stock.
                // We need to have a position for the equity before the trade can happen.
                var position = GetInstrumentPosition(trade.Symbol);
                if (position == null)
                {
                    throw new Exception(
                        String.Format("Stock account {0} has no position for this equity {1}"
                            , AccountId
                            , trade.Symbol
                         )
                    );
                }
                else if (position.longQuantity >= trade.Quantity)
                {
                    // Trade and place a limit order for the symbol
                    accountAndTrading.TradeRegularStock(AccountId, trade.Symbol, trade.Instruction, trade.Quantity);

                } else
                {
                    throw new Exception(
                        String.Format("Stock balance for account {0} is too low (qty: {1}) to buy the equity on {2}({3} units @ US ${4})"
                        , AccountId
                        , position.longQuantity
                        , trade.Symbol
                        , trade.Quantity
                        , position.marketValue)
                     );
                }
            }
        }

        /// <summary>
        /// Process limit trading - Buy to open or Sale to open.
        /// </summary>
        /// <param name="trade">A specific trade.</param>
        public void ProcessLimitTrading(StockTrade trade)
        {
            accountsData = RefreshAccountData(); 
            CancelPendingOrders(trade);

            if (trade.Instruction.ToString().Contains("BUY"))
            {
                var balance = GetBalance(accountsData); 
                //We are buying the stock
                if (balance >= trade.Price * trade.Quantity)
                {
                    // Trade and place a limit order for the symbol
                    var returnObject = accountAndTrading.TradeLimitRegularStock(AccountId, new TdAmeritrade.Models.StockTrade()
                    {
                        Instruction = trade.Instruction,
                        Quantity = trade.Quantity,
                        Price = trade.Price,
                        Symbol = trade.Symbol
                    });
                }
                else
                {
                    throw new Exception(
                        String.Format("Stock balance for account {0} is too low (US $ {1}) to buy the equity on {2}({3} units @ US ${4})"
                        , AccountId
                        , balance
                        , trade.Symbol
                        , trade.Quantity
                        , trade.Price)
                     );
                }
            }
            else
            {
                //We are selling the stock
                var position = GetInstrumentPosition(trade.Symbol);

                // Ensure that a position exists before triggering the trade.
                if (position == null)
                {
                    throw new Exception(
                        String.Format("Stock account {0} has no position for this equity {1}"
                            , AccountId
                            , trade.Symbol
                         )
                    );
                }
                else if (position.longQuantity >= trade.Quantity)
                {
                    // Trade and place a limit order for the symbol
                    var returnObject = accountAndTrading.TradeLimitRegularStock(AccountId, new TdAmeritrade.Models.StockTrade()
                    {
                        Instruction = trade.Instruction,
                        Quantity = trade.Quantity,
                        Price = trade.Price,
                        Symbol = trade.Symbol
                    });
                } else
                {
                    var returnObject = accountAndTrading.TradeLimitRegularStock(AccountId, new TdAmeritrade.Models.StockTrade()
                    {
                        Instruction = trade.Instruction,
                        Quantity = Convert.ToInt32(position.longQuantity),
                        Price = trade.Price,
                        Symbol = trade.Symbol
                    });
                }
            }            
        }

        /// <summary>
        /// Cancel orders directly linked to this trade's equity.
        /// TODO - Manage this differently in the future.
        /// </summary>
        /// <param name="trade"></param>
        public void CancelPendingOrders(StockTrade trade)
        {
            var pendingAccountOrderQuantity = 0;
            var pendingOrders = accountAndTrading.GetAccountOrders(AccountId, DateTime.Today.AddDays(-7), DateTime.Today, TdAmeritrade.Models.OrderStatus.QUEUED);

            //Cancel pending orders for this symbol
            foreach (var pendingOrder in pendingOrders.Orders)
            {
                // We only cancel those orders that are tied to the Symbol associated with this trade.
                pendingAccountOrderQuantity = pendingOrder.orderLegCollection.Where(x => trade.Symbol.Equals(x.instrument.symbol, StringComparison.CurrentCultureIgnoreCase)).Count();
                                
                if (pendingAccountOrderQuantity > 0)
                {
                    //Cancel the orders
                    accountAndTrading.CancelOrder(AccountId, pendingOrder.orderId.ToString());
                }
            }

            if (pendingAccountOrderQuantity > 0)
            {
                // Refresh the account so we have the current state of the account
                accountsData = RefreshAccountData();
            }
        }

        /// <summary>
        /// Gets the current instrument position
        /// </summary>
        /// <param name="symbol"></param>
        /// <returns></returns>
        public Position GetInstrumentPosition(string symbol)
        {
            if (accountsData.positions == null)
            {
                accountsData = RefreshAccountData();
            }

            if (accountsData.positions == null)
                return null;

            return accountsData.positions.Where(x => x.instrument.symbol.Equals(symbol, StringComparison.CurrentCultureIgnoreCase)).FirstOrDefault();
        }

        /// <summary>
        /// Refresh accounts data under this client.
        /// </summary>
        /// <returns></returns>
        private SecuritiesAccount RefreshAccountData()
        {            
            var accountData = accountAndTrading.GetAccounts();
            foreach(var account in accountData.accounts)
            {
                if (account.securitiesAccount.accountId.Equals(AccountId, StringComparison.CurrentCultureIgnoreCase))
                {
                    return account.securitiesAccount;
                }
            }

            throw new Exception(String.Format("Account {0} is invalid. No data found from TD Ameritrade", AccountId));
        }

        private double GetBalance(SecuritiesAccount securitiesAccount)
        {
            if (securitiesAccount.type == "MARGIN")
                return securitiesAccount.currentBalances.cashBalance;
            else
                return securitiesAccount.projectedBalances.cashAvailableForTrading;

        }
    }
}

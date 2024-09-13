using StockManager.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockManager.TradingComponents
{
    public class StockTrader
    {
        #region properties
        public double TradeCommission
        {
            get;
            set;
        }

        public double InitialVolume
        {
            get;
            set;
        }

        public double InitialValue
        {
            get;
            set;
        }

        public double CurrentVolume
        {
            get;
            set;
        }

        public double CurrentValue
        {
            get;
            set;
        }

        public List<StockTrade> Trades
        {
            get;
            set;
        }
        #endregion

        #region constructor
        public StockTrader(double stockValue, long stockVolume, double tradeCommission) {
            this.InitialValue = stockValue;
            this.CurrentValue = stockValue;
            this.InitialVolume = stockVolume;
            this.CurrentVolume = stockValue;
            this.TradeCommission = tradeCommission;
        }
        #endregion

        #region public
        public void ProcessDecision(string stock, string decisionRule, SimulationResult simulationResult)
        {
            if (Trades == null) { Trades = new List<StockTrade>(); }

            if (simulationResult.TradeDecision != Decision.NoAction)
            {
                Trades.Add(new StockTrade(stock, simulationResult.Period, simulationResult.TradeDecision, decisionRule, simulationResult.Price, simulationResult.Limit, simulationResult.Volume, simulationResult.Value));
            }

            this.CurrentValue = simulationResult.Value;
            this.CurrentVolume = simulationResult.Volume;  
        }

        #endregion
    }
}

using StockManager.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockManager.Processors
{
    public class Simulator
    {
        private double initalStock = 0;
        private double change = 0.0;
        /// <summary>
        /// Gets and sets the simulation results
        /// </summary>
        public List<SimulationResult> Results
        {
            get;
            set;
        }

        /// <summary>
        /// Initial volume for the stock.
        /// </summary>
        public long InitialVolume
        {
            get;
            set;
        }

        /// <summary>
        /// Initial value of the stock.
        /// </summary>
        public double InitialValue
        {
            get;
            set;
        }

        /// <summary>
        /// Current volume for the stock.
        /// </summary>
        public long CurrentVolume
        {
            get;
            set;
        }

        /// <summary>
        /// Current value of the stock.
        /// </summary>
        public double CurrentValue
        {
            get;
            set;
        }

        /// <summary>
        /// Transaction fee buying the stock
        /// </summary>
        public double BuyTransactionFees
        {
            get;
            set;
        }

        /// <summary>
        /// Transaction fees for selling the stock
        /// </summary>
        public double SaleTransactionFees
        {
            get;
            set;
        }

        public SimulationResult ProcessPeriod(DateTime period, double price, double limit, double macd, double signal, Decision decision)
        {
            if (this.Results == null || this.Results.Count == 0)
            {
                this.CurrentValue = InitialValue;
                this.CurrentVolume = InitialVolume;
                this.Results = new List<SimulationResult>();
                initalStock = InitialValue / price;
            }

            if (decision == Decision.Buy)
            {
                ProcessBuy(price);

            } else if (decision == Decision.Sell) {

                ProcessSell(price);

            } else if (this.CurrentVolume == 0)
            {
                // We sold our stock
                // Do nothing
            }
            else {

                //Calculate the current value of the stock
                this.CurrentValue = (this.CurrentVolume * price) + change;
            }

            var simulationResult = new SimulationResult(
                period
                , price
                , limit
                , CurrentVolume
                , CurrentValue
                , (initalStock * price)
                , macd
                , signal
                , decision);

            this.Results.Add(simulationResult);
            return simulationResult;
        }

        private void ProcessBuy(double price)
        {            
            if (this.CurrentValue > 0)
            {
                this.CurrentVolume = (long) ((this.CurrentValue - this.BuyTransactionFees) / price);
                change = this.CurrentValue - (this.CurrentVolume * price);

                this.CurrentValue = (this.CurrentVolume * price) + change;
            }
        }

        private void ProcessSell(double price)
        {
            if (this.CurrentVolume > 0)
            {
                this.CurrentValue = (this.CurrentVolume * price) - this.SaleTransactionFees + change;
                this.CurrentVolume = 0;
                this.change = 0;
            }
        }
    }
}

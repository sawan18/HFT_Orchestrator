using StockManager.Model;
using StockManager.TradingComponents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockManager.Processors
{
    public abstract class StockBase
    {
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
        /// Transaction fee for each trade.
        /// </summary>
        public double TransactionFee
        {
            get;
            set;
        }

        /// <summary>
        /// Tracking for this component
        /// </summary>
        public StockTrader Trader
        {
            get;
            set;
        }


        /// <summary>
        /// Simulator for testing an algorithm.
        /// </summary>
        public Simulator Simulator
        {
            get;
            set;
        }


        /// <summary>
        /// Captures the period calculations
        /// </summary>
        public List<PeriodAttributes> PeriodCalculations { get; set; }

        public MacdDecisionMode DecisionMode { get; set; }

        /// <summary>
        /// Seed data to the algorithm processor
        /// </summary>
        /// <param name="data"></param>
        public abstract void SeedData(StockFeed data);

        /// <summary>
        /// Reset data on stock processor.
        /// </summary>
        public abstract void Reset();

    }
}

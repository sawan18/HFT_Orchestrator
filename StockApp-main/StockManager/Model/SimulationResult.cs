using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockManager.Model
{
    public struct SimulationResult
    {
        public SimulationResult(
            DateTime period
            , double price
            , double limit
            , double volume
            , double value
            , double control
            , double macd
            , double signal
            , Decision tradeDecision)
        {
            this.Period = period;
            this.Volume = volume;
            this.Value = value;
            this.Price = price;
            this.Limit = limit;
            this.Control = control;
            this.Macd = macd;
            this.Signal = signal;
            this.TradeDecision = tradeDecision;
        }

        public DateTime Period { get; set; }
        public double Price { get; set; }

        public double Limit { get; set; }
        public double Volume { get; set; }
        public double Value { get; set; }

        public double Control { get; set; }
        
        public double Macd { get; set; }

        public double Signal { get; set; }
        public Decision TradeDecision { get; set; }
    }
}

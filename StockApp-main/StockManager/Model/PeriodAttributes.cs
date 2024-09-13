using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockManager.Model
{

    public struct PeriodAttributes
    {
        public int Index
        {
            get;
            set;
        }
        public DateTime Period
        {
            get;
            set;
        }

        public int MacdLongerPeriodLength
        {
            get;
            set;
        }

        public int MacdShorterPeriodLength
        {
            get;
            set;
        }

        public int MacdSignalPeriodLength
        {
            get;
            set;
        }

        public double EmaLonger
        {
            get;
            set;
        }

        public double EmaShorter
        {
            get;
            set;
        }

        public double Signal
        {
            get;
            set;
        }

        public int Smoothing
        {
            get;
            set;
        }

        public double Macd
        {
            get;
            set;
        }

        public double Histogram
        {
            get;
            set;
        }

        public int Trend
        {
            get;
            set;
        }

        public int TrendRepeats
        {
            get;
            set;
        }

        public double Price
        {
            get;
            set;
        }

        public double Limit
        {
            get;
            set;
        }

        public double HighPrice { get; set; }
        public int HighPriceTrend { get; set; }

        public double LowPrice { get; set; }
        public int LowPriceTrend { get; set; }

        public Decision PeriodDecision
        {
            get;
            set;
        }

        public String PeriodDecisionRule
        {
            get;
            set;
        }

        public DateTime ProcessTime
        {
            get;
            set;
        }

        public int ActualTrend
        {
            get;
            set;
        }

        public double ActualChange
        {
            get;
            set;
        }
    }
}

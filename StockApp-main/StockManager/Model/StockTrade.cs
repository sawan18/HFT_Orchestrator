using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockManager.Model
{
    public struct StockTrade
    {
        public StockTrade(string stock, DateTime period, Decision tradeType, string rule, double price, double limit, double volume = 0, double value = 0)
        {
            this.Stock = stock;
            this.TradeType = tradeType;
            this.Period = period;
            this.Price = price;
            this.Limit = limit;
            this.Volume = volume;
            this.Value = value; 
            this.TradeDate = DateTime.Now;
            this.Rule = rule;
        }

        /// <summary>
        /// Gets and Sets the Stock.
        /// </summary>
        public string Stock
        {
            get;
            set;
        }


        /// <summary>
        /// Gets and sets the type of trade.
        /// </summary>
        public Decision TradeType
        {
            get;
            set;
        }

        /// <summary>
        /// Gets and sets the applicable rule.
        /// </summary>
        public string Rule
        {
            get;
            set;
        }

        /// <summary>
        /// Gets and sets the period for this data feed.
        /// </summary>
        public DateTime Period
        {
            get;
            set;
        }

        /// <summary>
        /// Gets and sets the price for the stock.
        /// </summary>
        public double Price
        {
            get;
            set;
        }

        /// <summary>
        /// Gets and sets the limit for the trade.
        /// </summary>
        public double Limit
        {
            get;
            set;
        }

        /// <summary>
        /// Gets and sets the volume of this stock.
        /// </summary>
        public double Volume
        {
            get;
            set;
        }

        /// <summary>
        /// Gets and sets the value of this stock.
        /// </summary>
        public double Value
        {
            get;
            set;
        }

        public DateTime TradeDate
        {
            get;
            set;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockManager.Model
{
    /// <summary>
    /// Model for the data feed for financial data processing.
    /// </summary>
    public struct StockFeed
    {
        public StockFeed(int index, string stock, DateTime period, double price, long volume = 0)
        {
            this.Index = index;
            this.Stock = stock;
            this.Period = period;
            this.Price = price;
            this.Volume = volume;
        }

        /// <summary>
        /// Gets and sets the period Index
        /// </summary>
        public int Index
        {
            get;
            set;
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
        /// Gets and sets the volume of this stock.
        /// </summary>
        public double Volume
        {
            get;
            set;
        }
    }
}

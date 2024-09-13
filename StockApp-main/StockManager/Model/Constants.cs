using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockManager.Model
{
    public class Constants
    {
        public const string PRICETRENDFIRSTBUY = "PriceTrendFirstBuy";
        public const string CROSSOVERUPFIRSTBUY = "CrossOverFirstBuy";
        public const string CROSSOVERUP = "CrossOverUpTrend";
        public const string CROSSOVERDOWN = "CrossOverDownTrend";
        public const string SELLONMAJORPRICEDROP = "SellOnMajorPriceDrop";
        public const string STRONGUPWARDTRENDINGSTOCK = "StrongUpwardTrendingStock";
        public const string STRONGDOWNARDTRENDINGSTOCK = "StrongDownTrendingStock";
        public const string STRONGTRENDPEAKING = "StrongTrendPeaking";
        public const string STRONGTRENDTROUGH = "StrongTrendTrough";
        public const string BESTTREND = "BestTrendRetro";
        public const string PRICETREND = "PriceTrendRetro";

        public const int MIN_REPEATS = 2;
        public const int MAX_REPEATS = 6;
        public const int INVALID_VALUE = -9999;
    }
}

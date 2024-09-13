using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockManager.Rest
{
    public enum YahooInterval
    {
        OneMinute = 0,
        ThreeMinutes = 1,
        FiveMinutes = 2,
        ThirtyMinutes = 3,
        OneDay = 4,
        ThreeMonths = 5
    }

    public class YahooDataPull
    {
        #region properties
        public string Symbol { get; set; }
        public YahooInterval Interval { get; set; }
        public DateTime StartPeriod { get; set; }
        public DateTime EndPeriod { get; set; }
        #endregion

        /// <summary>
        /// Sample Yahoo finance URL: https://query1.finance.yahoo.com/v7/finance/download/AAPL?period1=1570987842&period2=1602610242&interval=1d&events=history&includeAdjustedClose=true
        /// </summary>
        // Sample 

        private const string BASE_URL = "https://query1.finance.yahoo.com/v7/finance/download/{0}?period1={1}&period2={2}&interval={3}&events=history&includeAdjustedClose=true";

        public void Download()
        {
            var url = String.Format(BASE_URL, Symbol, GetUnixTimestamp(StartPeriod), GetUnixTimestamp(EndPeriod), GetPeriod());
            var restClient = new RESTClient()
            {
                EndPointUrl = url,
                ContentType = "txt/csv"
            };

            var task = Task.Run(() => restClient.SendAsync());

            var dataFolder = RunSettings.Instance.MetaDataFolder + "\\Data";
            if (!Directory.Exists(dataFolder))
            {
                Directory.CreateDirectory(dataFolder);
            };

            File.WriteAllText(String.Format("{0}\\{1}.csv", dataFolder, Symbol), task.Result);
        }

        public string GetPeriod()
        {
            switch (this.Interval)
            {
                case YahooInterval.OneMinute:
                    return "1m";
                case YahooInterval.ThreeMinutes:
                    return "3m";
                case YahooInterval.FiveMinutes:
                    return "5m";
                case YahooInterval.OneDay:
                    return "1d";
                case YahooInterval.ThreeMonths:
                    return "3mo";
                default:
                    return "1d";
            }
        }
        public double GetUnixTimestamp(DateTime datetime)
        {
            return Convert.ToUInt64(datetime.Subtract(new DateTime(1970, 1, 1)).TotalSeconds);
        }
    }
}

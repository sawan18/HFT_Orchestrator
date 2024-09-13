using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TdAmeritrade.Models;

namespace TdAmeritrade.Processors
{
    public class StockHistory : StockProcessorBase
    {

        private const string HISTORY_API = "/marketdata/{0}/pricehistory";

        /// <summary>
        /// Gets the stock history for a symbol over a date period.
        /// </summary>
        /// <param name="symbol"></param>
        /// <param name="startDate"></param>
        /// <param name="endDate"></param>
        /// <returns></returns>
        public List<Candle> GetStockHistory(string symbol, DateTime startDate, DateTime endDate)
        {
            var startEpoch = TimeToEpoch(startDate);
            var endEpoch = TimeToEpoch(endDate);
            var parameters = new Dictionary<string, object>
            {
                { "startDate", startEpoch },
                { "endDate", endEpoch }
            };

            AuthenticationProcessor.ProcessAuthentication();
            var stockHistory = JsonConvert.DeserializeObject<HistoryResponse>(restController.GetData(AuthenticationProcessor.AccessToken, string.Format(HISTORY_API, symbol), parameters));

            var candles = new List<Candle>();
            
            //Reverse the date from epoch values
            foreach (var candle in stockHistory.candles)
            {
                candle.datetime = EpochToTime((long)candle.datetime);
                candles.Add(candle);
            }

            return candles;
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TdAmeritrade.Models
{
    
    public class Candle
    {
        public double open { get; set; }
        public double high { get; set; }
        public double low { get; set; }
        public double close { get; set; }
        public int volume { get; set; }
        public object datetime { get; set; }
    }

    public class HistoryResponse
    {
        public List<Candle> candles { get; set; }
        public string symbol { get; set; }
        public bool empty { get; set; }
    }


}

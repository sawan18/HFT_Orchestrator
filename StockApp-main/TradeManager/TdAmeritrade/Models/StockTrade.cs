using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TdAmeritrade.Models
{
    public class StockTrade
    {
        public string OrderId { get; set; }
        public OrderInstruction Instruction { get; set; }
        public int Quantity { get; set; }
        public string Symbol { get; set; }

        public double Price { get; set; }

        public double Limit { get; set; }
    }
}

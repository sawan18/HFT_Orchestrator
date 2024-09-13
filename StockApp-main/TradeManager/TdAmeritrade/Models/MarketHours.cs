using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TdAmeritrade.Models
{
    public class PreMarket
    {
        public DateTime start { get; set; }
        public DateTime end { get; set; }
    }

    public class RegularMarket
    {
        public DateTime start { get; set; }
        public DateTime end { get; set; }
    }

    public class PostMarket
    {
        public DateTime start { get; set; }
        public DateTime end { get; set; }
    }

    public class SessionHours
    {
        public List<PreMarket> preMarket { get; set; }
        public List<RegularMarket> regularMarket { get; set; }
        public List<PostMarket> postMarket { get; set; }
    }

    public class EQ
    {
        public string date { get; set; }
        public string marketType { get; set; }
        public string exchange { get; set; }
        public string category { get; set; }
        public string product { get; set; }
        public string productName { get; set; }
        public bool isOpen { get; set; }
        public SessionHours sessionHours { get; set; }
    }

    public class Equity
    {
        public EQ EQ { get; set; }
    }

    public class MarketHours
    {
        public Equity equity { get; set; }
    }


}

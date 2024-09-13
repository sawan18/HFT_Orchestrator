using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TdAmeritrade.Processors
{
    public class StockProcessorBase
    {
        private TdAmeritradeSettings ameritradeSettings = TdAmeritradeSettings.Instance;
        internal RestController restController = new RestController();
        public Authenticator AuthenticationProcessor {
            get { return ameritradeSettings.AuthenticationProcessor; }
        }
        public DateTime EpochToTime(long epoch)
        {
            return DateTimeOffset.FromUnixTimeMilliseconds(epoch).LocalDateTime;
        }

        public long TimeToEpoch(DateTime datetime)
        {
            return DateTimeOffset.Parse(datetime.ToString()).ToUnixTimeMilliseconds();
        }
    }
}

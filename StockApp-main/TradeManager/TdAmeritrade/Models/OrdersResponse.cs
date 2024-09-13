using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TdAmeritrade.Models
{  
    public class CancelTime
    {
        public string date { get; set; }
        public bool shortFormat { get; set; }
    }

    public class OrderLegCollectionResponse
    {
        public string orderLegType { get; set; }
        public int legId { get; set; }
        public ResponseInstrument instrument { get; set; }
        public string instruction { get; set; }
        public string positionEffect { get; set; }
        public double quantity { get; set; }
        public string quantityType { get; set; }
    }
    
    public class ReplacingOrderCollection
    {
    }

    public class ChildOrderStrategy
    {
    }


    public class ResponseInstrument
    {
        public string assetType { get; set; }
        public string cusip { get; set; }
        public string symbol { get; set; }
    }

    public class Order
    {
        public string session { get; set; }
        public string duration { get; set; }
        public string orderType { get; set; }
        public CancelTime cancelTime { get; set; }
        public string complexOrderStrategyType { get; set; }
        public double quantity { get; set; }
        public double filledQuantity { get; set; }
        public double remainingQuantity { get; set; }
        public string requestedDestination { get; set; }
        public string destinationLinkName { get; set; }
        public string releaseTime { get; set; }
        public double stopPrice { get; set; }
        public string stopPriceLinkBasis { get; set; }
        public string stopPriceLinkType { get; set; }
        public int stopPriceOffset { get; set; }
        public string stopType { get; set; }
        public string priceLinkBasis { get; set; }
        public string priceLinkType { get; set; }
        public double price { get; set; }
        public string taxLotMethod { get; set; }
        public List<OrderLegCollectionResponse> orderLegCollection { get; set; }
        public double activationPrice { get; set; }
        public string specialInstruction { get; set; }
        public string orderStrategyType { get; set; }
        public long orderId { get; set; }
        public bool cancelable { get; set; }
        public bool editable { get; set; }
        public string status { get; set; }
        public string enteredTime { get; set; }
        public string closeTime { get; set; }
        public string tag { get; set; }
        public int accountId { get; set; }
        public List<string> orderActivityCollection { get; set; }
        public List<ReplacingOrderCollection> replacingOrderCollection { get; set; }
        public List<ChildOrderStrategy> childOrderStrategies { get; set; }
        public string statusDescription { get; set; }
    }

    public class OrdersResponse
    {
        public List<Order> Orders { get; set; }
    }


}

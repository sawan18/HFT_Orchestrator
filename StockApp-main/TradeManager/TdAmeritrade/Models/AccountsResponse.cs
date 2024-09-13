using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TdAmeritrade.Models
{
    public class Position
    {
        public double shortQuantity { get; set; }
        public double averagePrice { get; set; }
        public double currentDayProfitLoss { get; set; }
        public double currentDayProfitLossPercentage { get; set; }
        public double longQuantity { get; set; }
        public double settledLongQuantity { get; set; }
        public double settledShortQuantity { get; set; }
        public ResponseInstrument instrument { get; set; }
        public double marketValue { get; set; }
    }
    
    public class ExecutionLeg
    {
        public double legId { get; set; }
        public double quantity { get; set; }
        public double mismarkedQuantity { get; set; }
        public double price { get; set; }
        public DateTime time { get; set; }
    }

    public class OrderActivityCollection
    {
        public string activityType { get; set; }
        public string executionType { get; set; }
        public double quantity { get; set; }
        public double orderRemainingQuantity { get; set; }
        public List<ExecutionLeg> executionLegs { get; set; }
    }
    
    public class OrderStrategy
    {
        public string session { get; set; }
        public string duration { get; set; }
        public string orderType { get; set; }
        public string complexOrderStrategyType { get; set; }
        public double quantity { get; set; }
        public double filledQuantity { get; set; }
        public double remainingQuantity { get; set; }
        public string requestedDestination { get; set; }
        public string destinationLinkName { get; set; }
        public List<OrderLegCollectionResponse> orderLegCollection { get; set; }
        public string orderStrategyType { get; set; }
        public object orderId { get; set; }
        public bool cancelable { get; set; }
        public bool editable { get; set; }
        public string status { get; set; }
        public DateTime enteredTime { get; set; }
        public DateTime closeTime { get; set; }
        public int accountId { get; set; }
        public List<OrderActivityCollection> orderActivityCollection { get; set; }
        public double? price { get; set; }
    }

    public class InitialBalances
    {
        public double accruedInterest { get; set; }
        public double cashAvailableForTrading { get; set; }
        public double cashAvailableForWithdrawal { get; set; }
        public double cashBalance { get; set; }
        public double bondValue { get; set; }
        public double cashReceipts { get; set; }
        public double liquidationValue { get; set; }
        public double longOptionMarketValue { get; set; }
        public double longStockValue { get; set; }
        public double moneyMarketFund { get; set; }
        public double mutualFundValue { get; set; }
        public double shortOptionMarketValue { get; set; }
        public double shortStockValue { get; set; }
        public bool isInCall { get; set; }
        public double unsettledCash { get; set; }
        public double cashDebitCallValue { get; set; }
        public double pendingDeposits { get; set; }
        public double accountValue { get; set; }
    }

    public class CurrentBalances
    {
        public double accruedInterest { get; set; }
        public double cashBalance { get; set; }
        public double cashReceipts { get; set; }
        public double longOptionMarketValue { get; set; }
        public double liquidationValue { get; set; }
        public double longMarketValue { get; set; }
        public double moneyMarketFund { get; set; }
        public double savings { get; set; }
        public double shortMarketValue { get; set; }
        public double pendingDeposits { get; set; }
        public double cashAvailableForTrading { get; set; }
        public double cashAvailableForWithdrawal { get; set; }
        public double cashCall { get; set; }
        public double longNonMarginableMarketValue { get; set; }
        public double totalCash { get; set; }
        public double shortOptionMarketValue { get; set; }
        public double mutualFundValue { get; set; }
        public double bondValue { get; set; }
        public double cashDebitCallValue { get; set; }
        public double unsettledCash { get; set; }
    }

    public class ProjectedBalances
    {
        public double cashAvailableForTrading { get; set; }
        public double cashAvailableForWithdrawal { get; set; }
    }

    public class SecuritiesAccount
    {
        public string type { get; set; }
        public string accountId { get; set; }
        public int roundTrips { get; set; }
        public bool isDayTrader { get; set; }
        public bool isClosingOnlyRestricted { get; set; }
        public List<Position> positions { get; set; }
        public List<OrderStrategy> orderStrategies { get; set; }
        public InitialBalances initialBalances { get; set; }
        public CurrentBalances currentBalances { get; set; }
        public ProjectedBalances projectedBalances { get; set; }
    }

    public class Account
    {
        public SecuritiesAccount securitiesAccount { get; set; }
    }

    public class AccountsResponse
    {
        public List<Account> accounts { get; set; }
    }


}

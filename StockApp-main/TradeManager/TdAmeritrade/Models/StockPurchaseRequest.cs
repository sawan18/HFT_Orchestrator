using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TdAmeritrade.Models
{    
    public class Instrument
    {
        public string symbol { get; set; }
        public string assetType { get; set; }
    }

    public class OrderLegCollection
    {
        public string instruction { get; set; }
        public int quantity { get; set; }
        public Instrument instrument { get; set; }
    }

    public class StockPurchaseRequest
    {
        /**
         * Buy 15 shares of XYZ at the Market good for the Day.
         * {
          "orderType": "MARKET",
          "session": "NORMAL",
          "duration": "DAY",
          "orderStrategyType": "SINGLE",
          "orderLegCollection": [
            {
              "instruction": "Buy",
              "quantity": 15,
              "instrument": {
                "symbol": "XYZ",
                "assetType": "EQUITY"
              }
            }
          ]
        }
         */
        public string orderType { get; set; }
        public string session { get; set; }
        public string duration { get; set; }
        public string orderStrategyType { get; set; }
        public List<OrderLegCollection> orderLegCollection { get; set; }
    }

    public class BuyLimitSingleOption
    {
        /**
         * Buy to open 10 contracts of the XYZ March 20, 2015 $49 CALL at a Limit of $6.45 good for the Day.
         * {
              "complexOrderStrategyType": "NONE",
              "orderType": "LIMIT",
              "session": "NORMAL",
              "price": "6.45",
              "duration": "DAY",
              "orderStrategyType": "SINGLE",
              "orderLegCollection": [
                {
                  "instruction": "BUY_TO_OPEN",
                  "quantity": 10,
                  "instrument": {
                    "symbol": "XYZ_032015C49",
                    "assetType": "OPTION"
    	            }
                }
              ]
            }
         */
        public string complexOrderStrategyType { get; set; }
        public string orderType { get; set; }
        public string session { get; set; }
        public double price { get; set; }
        public string duration { get; set; }
        public string orderStrategyType { get; set; }
        public List<OrderLegCollection> orderLegCollection { get; set; }
    }

    public class BuyLimitVerticalCallSpreadOption
    {
        /**
         * Buy to open 10 contracts of the XYZ Jan 15, 2016 $40 Call and Sell to open 10 contracts of the XYZ Jan 15, 2016 $42.5 Call for a Net Debit of $1.20 good for the Day.
         * {
              "orderType": "NET_DEBIT",
              "session": "NORMAL",
              "price": "1.20",
              "duration": "DAY",
              "orderStrategyType": "SINGLE",
              "orderLegCollection": [
                {
                  "instruction": "BUY_TO_OPEN",
                  "quantity": 10,
                  "instrument": {
                    "symbol": "XYZ_011516C40",
                    "assetType": "OPTION"
                  }
                },
                {
                  "instruction": "SELL_TO_OPEN",
                  "quantity": 10,
                  "instrument": {
                    "symbol": "XYZ_011516C42.5",
                    "assetType": "OPTION"
                  }
                }
              ]
            }
         */
        public string orderType { get; set; }
        public string session { get; set; }
        public double price { get; set; }
        public string duration { get; set; }
        public string orderStrategyType { get; set; }
        public List<OrderLegCollection> orderLegCollection { get; set; }
    }

    public class CustomOptionSpread
    {
        /**
         * Buy to open 2 contracts of the XYZ Jan 17, 2020 $43 Put and Sell to open 1 contracts of the XYZ Jan 18, 2019 $45 Put at the Market good for the Day.
         * {
             "orderStrategyType": "SINGLE",
              "orderType": "MARKET",
              "orderLegCollection": [
                {
                  "instrument": {
                    "assetType": "OPTION",
                    "symbol": "XYZ_011819P45"
                },
                  "instruction": "SELL_TO_OPEN",
                  "quantity": 1
                },
                {
                  "instrument": {
                    "assetType": "OPTION",
                    "symbol": "XYZ_011720P43"
                  },
                  "instruction": "BUY_TO_OPEN",
                  "quantity": 2
                }
              ],
              "complexOrderStrategyType": "CUSTOM",
              "duration": "DAY",
              "session": "NORMAL"
            }
         */
        public string complexOrderStrategyType { get; set; }
        public string orderType { get; set; }
        public string session { get; set; }
        public string duration { get; set; }
        public string orderStrategyType { get; set; }
        public List<OrderLegCollection> orderLegCollection { get; set; }
    }

}

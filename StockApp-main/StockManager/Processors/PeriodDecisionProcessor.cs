using StockManager.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockManager.Processors
{
    public class PeriodDecisionProcessor
    {
        private RunSettings settings = RunSettings.Instance;
        public List<string> Rules
        {
            get;
            set;
        }

        public PeriodDecisionProcessor(List<string> rules, int minRepeats = Constants.MIN_REPEATS, int maxRepeats = Constants.MAX_REPEATS)
        {
            this.Rules = rules;
            this.MinRepeats = minRepeats;
            this.MaxRepeats = maxRepeats;

        }

        public int MinRepeats { get; set; }
        public int MaxRepeats { get; set; }
        public DecisionResult GetDecision(PeriodAttributes referencePeriod, PeriodAttributes currentResult, PeriodAttributes previousResult, PeriodAttributes lastPeak, int sellRepeatLimits = 5, int buyRepeatLimits = 3, double avgChange = 0.05)
        {
            if (Rules.Contains(Constants.BESTTREND))
            {
                var result = BestTrend(referencePeriod, currentResult, previousResult);
                if (result != Decision.NoAction)
                    return new DecisionResult(Constants.BESTTREND, result, 0);
            }

            if (Rules.Contains(Constants.PRICETREND))
            {
                return PriceTrend(referencePeriod, currentResult, previousResult, lastPeak, sellRepeatLimits, buyRepeatLimits, avgChange);
            }

            if (Rules.Contains(Constants.CROSSOVERUPFIRSTBUY))
            {
                var result = CrossOverUpwardFirstBuy(referencePeriod, currentResult, previousResult);
                if (result != Decision.NoAction)
                    return new DecisionResult(Constants.CROSSOVERUPFIRSTBUY, result, 0);
            }
            
            if (Rules.Contains(Constants.CROSSOVERUP))
            {
                var result = CrossOverUpward(referencePeriod, currentResult, previousResult);
                if (result != Decision.NoAction)
                    return new DecisionResult(Constants.CROSSOVERUP, result, 0);
            }

            if (Rules.Contains(Constants.CROSSOVERDOWN))
            {
                var result = CrossOverDownward(referencePeriod, currentResult, previousResult);
                if (result != Decision.NoAction)
                    return new DecisionResult(Constants.CROSSOVERDOWN, result, 0);
            }                       

            return new DecisionResult(String.Empty, Decision.NoAction, 0);
        }

        private bool IsCrossoverUpwardsTrends(PeriodAttributes currentResult, PeriodAttributes previousResult)
        {
            if (previousResult.Histogram <= 0 && currentResult.Histogram >= 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool IsCrossoverDownwardsTrend(PeriodAttributes currentResult, PeriodAttributes previousResult)
        {
            if (previousResult.Histogram >= 0 && currentResult.Histogram <= 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private Decision CrossOverUpwardFirstBuy(PeriodAttributes referencePeriod, PeriodAttributes currentResult, PeriodAttributes previousResult)
        {
            if (referencePeriod.PeriodDecision == Decision.NoAction)
            {
                return CrossOverUpward(referencePeriod, currentResult, previousResult);
            }
            else { return Decision.NoAction; }
        }
        /// <summary>
        /// Detects when the Histogram has moved from negative to positive.
        /// </summary>
        /// <param name="currentResult"></param>
        /// <param name="previousResult"></param>
        /// <returns></returns>
        private Decision CrossOverUpward(PeriodAttributes referencePeriod, PeriodAttributes currentResult, PeriodAttributes previousResult)
        {            
            if (                
                (referencePeriod.PeriodDecision == Decision.Sell || referencePeriod.PeriodDecision == Decision.NoAction)
                && IsCrossoverUpwardsTrends(currentResult, previousResult)
                )
            {
                return Decision.Buy;

            } else
            {
                return Decision.NoAction;
            }
        }

        /// <summary>
        /// Detects when the histogram has moved from positive to negative
        /// </summary>
        /// <param name="referencePeriod"></param>
        /// <param name="currentResult"></param>
        /// <param name="previousResult"></param>
        /// <returns></returns>
        private Decision CrossOverDownward(PeriodAttributes referencePeriod, PeriodAttributes currentResult, PeriodAttributes previousResult)
        {     
            if (referencePeriod.PeriodDecision == Decision.Buy
                && IsCrossoverDownwardsTrend(currentResult, previousResult))
            {
                return Decision.Sell;
            }
            else { return Decision.NoAction; }
        }
               

        /// <summary>
        /// Best Trend Decision
        /// </summary>
        /// <param name="referencePeriod"></param>
        /// <param name="currentResult"></param>
        /// <param name="previousResult"></param>
        /// <returns></returns>
        private Decision BestTrend(PeriodAttributes referencePeriod, PeriodAttributes currentResult, PeriodAttributes previousResult)
        {
            if (
                (referencePeriod.PeriodDecision == Decision.Buy)
                && currentResult.Trend == -1
                && previousResult.Trend == 1
                )
            {
                return Decision.Sell;

            } else if (
                (referencePeriod.PeriodDecision == Decision.Sell || referencePeriod.PeriodDecision == Decision.NoAction)
                && currentResult.Trend == 1
                && (previousResult.Trend == -1 || previousResult.Trend == 0)
                )
            {
                return Decision.Buy;
            }
            else { return Decision.NoAction; }
        }

        /// <summary>
        /// Price Trend Decision
        /// </summary>
        /// <param name="referencePeriod"></param>
        /// <param name="currentResult"></param>
        /// <param name="previousResult"></param>
        /// <returns></returns>
        private DecisionResult PriceTrend(PeriodAttributes referencePeriod, PeriodAttributes currentResult, PeriodAttributes previousResult, PeriodAttributes lastPeak, int sellRepeatLimits, int buyRepeatLimits, double avgChange)
        {
            
            // If there are no actions yet, buy if there are 2 periods of downward trend
            var noactionReports = 2;          
            
            // If we continue to see a negative trend up to 2 periods, trigger a buy.
            if (
              (referencePeriod.PeriodDecision == Decision.NoAction)
              && currentResult.Trend == -1
              && currentResult.TrendRepeats >= noactionReports
              )
            {
                return new DecisionResult(Constants.PRICETREND + ":1 - Repeat:" + lastPeak.TrendRepeats, Decision.Buy, previousResult.Price);
            }

            // If we have no action and the market is trending up after a bearish trend
            if (
              (referencePeriod.PeriodDecision == Decision.NoAction)
              && currentResult.Trend == 1
              && (lastPeak.Trend == -1 || lastPeak.Trend == 0)
              )
            {
                return new DecisionResult(Constants.PRICETREND + ":2 - Repeat:" + lastPeak.TrendRepeats, Decision.Buy, previousResult.Price);
            }
            
            // If we already have bought some stock and the we see a downward trend after a bullish trend.
            if (referencePeriod.PeriodDecision == Decision.Buy
                && lastPeak.Trend == 1
                && lastPeak.TrendRepeats >= sellRepeatLimits
                && currentResult.Trend == -1 
                && (
                // We ignore this price rule in actual trades since it will be dealt with down stream
                // In simulation mode, we need to compare with the purchase price.
                settings.ProcessTrade || currentResult.Price > referencePeriod.Price
                )
                ) {

                return new DecisionResult(Constants.PRICETREND + ":1 - Repeat:" + lastPeak.TrendRepeats, Decision.Sell, previousResult.Price);
            }

            if (referencePeriod.PeriodDecision == Decision.Buy
                && currentResult.Trend == -1
                && ((referencePeriod.Price - currentResult.Price) / referencePeriod.Price > settings.StopLossPercentage)
                )
            {
                return new DecisionResult(Constants.PRICETREND + ":4 - Repeat:" + lastPeak.TrendRepeats, Decision.Sell, previousResult.Price);
            }

            // If we had a bearish trend and the trend is changing to an upward trend
            // then we should buy stocks
            if (referencePeriod.PeriodDecision == Decision.Sell
                && currentResult.Trend == 1
                && (lastPeak.Trend == -1 || lastPeak.Trend == 0)
                && lastPeak.TrendRepeats >= buyRepeatLimits
                && ((double) (currentResult.Price - previousResult.Price) / (double) previousResult.Price < 0.10)
                )
            {
                return new DecisionResult(Constants.PRICETREND + ":3 - Repeat:" + lastPeak.TrendRepeats, Decision.Buy, previousResult.Price);
            }            
            

            return new DecisionResult(Constants.PRICETREND + ":8 - Repeat:" + currentResult.TrendRepeats, Decision.NoAction, 0);
        }

        
    }
}

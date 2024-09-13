using StockManager.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockManager.Processors
{
    /// <summary>
    /// Price-trend Processor    
    /// </summary>
    public class BestTrendProcessor : StockBase
    {

        private List<StockFeed> feeds = new List<StockFeed>();
        private PeriodAttributes referencePeriod = new PeriodAttributes();
        private PeriodDecisionProcessor decisionProcessor = new PeriodDecisionProcessor(new List<string> { Constants.BESTTREND});
        
        #region constructor
        public BestTrendProcessor(double initialValue = 0, long initialVolume = 0, double transactionFee = 0)
        {            
            base.InitialVolume = initialVolume;
            base.InitialValue = initialValue;
            base.TransactionFee = transactionFee;

            SetDecisionRules();
        }
        
        #endregion
        /// <summary>
        /// Resets the feed data.
        /// </summary>
        public override void Reset()
        {
            this.feeds.Clear();
        }

        /// <summary>
        /// Seeds the data for the engine.
        /// </summary>
        /// <param name="data"></param>
        public override void SeedData(StockFeed data)
        {
            // Calculate current trend
            var currentPeriod = ComputeTrend(data);
            var previousPeriod = PeriodCalculations[PeriodCalculations.Count - 1];

            // Cache the feed
            feeds.Add(data);

            // Cache the previous calculations
            PeriodCalculations.Add(currentPeriod);

            var maxCacheSize = 14;

            // Trim cached data to reduce memory use.
            if (feeds.Count == maxCacheSize)
            {
                var adjustedFeed = new List<StockFeed>();
                var adjustedPeriodCalculations = new List<PeriodAttributes>();

                for (var i = 1; i < maxCacheSize; i++)
                {
                    adjustedFeed.Add(feeds[i]);
                    adjustedPeriodCalculations.Add(PeriodCalculations[i]);
                }

                // Reset cached feeds and calculations.
                this.feeds = adjustedFeed;
                this.PeriodCalculations = adjustedPeriodCalculations;
            }

            // Submit for trading
            if (Trader == null)
            {
                Trader = new TradingComponents.StockTrader(InitialValue, InitialVolume, TransactionFee);
                Simulator = new Simulator()
                {
                    InitialValue = InitialValue,
                    InitialVolume = InitialVolume,
                    BuyTransactionFees = TransactionFee,
                    SaleTransactionFees = TransactionFee
                };
            }

            var periodResult = Simulator.ProcessPeriod(previousPeriod.Period
                , previousPeriod.Price
                , 0
                , 0
                , 0
                , previousPeriod.PeriodDecision);

            Trader.ProcessDecision(data.Stock, previousPeriod.PeriodDecisionRule, periodResult);

        }

        #region private methods

        private PeriodAttributes GetDefaultPeriod(PeriodAttributes currentPeriod)
        {
            var previousPeriod = new PeriodAttributes();
            previousPeriod.Price = currentPeriod.Price;
            previousPeriod.Period = currentPeriod.Period.AddDays(-1);
            referencePeriod = previousPeriod;
            PeriodCalculations.Add(previousPeriod);

            return previousPeriod;
        }
        private PeriodAttributes ComputeTrend(StockFeed newFeed)
        {            
            var currentPeriod = new PeriodAttributes()
            {
                Period = newFeed.Period,
                Price = newFeed.Price
            };

            // Pull the previous entry
            var previousPeriod = PeriodCalculations.Count > 0 ? PeriodCalculations[PeriodCalculations.Count - 1] : GetDefaultPeriod(currentPeriod);

            if (currentPeriod.Price > previousPeriod.Price)
            {
                //Trending up
                currentPeriod.Trend = 1;

            } else if(previousPeriod.Price == currentPeriod.Price)
            {
                //Trend is neither up or down
                currentPeriod.Trend = 0;

            } else
            {
                // Trending down
                currentPeriod.Trend = -1;
            }

            //Calculate Trend Repeats
            if (currentPeriod.Trend == previousPeriod.Trend)
            {
                currentPeriod.TrendRepeats = previousPeriod.TrendRepeats +  1;
            }
            else
            {
                currentPeriod.TrendRepeats = 0;
            }

            //Calculate decisions           
            return CalculateDecision(currentPeriod);
        }

        /// <summary>
        /// Calculate decisions
        /// </summary>
        /// <param name="macdAttribute"></param>
        /// <returns></returns>
        private PeriodAttributes CalculateDecision(PeriodAttributes currentPeriod)
        {
            var previousPeriod = PeriodCalculations[PeriodCalculations.Count - 1];
            DecisionResult decisionResult = decisionProcessor.GetDecision(referencePeriod, currentPeriod, previousPeriod, new PeriodAttributes());

            if(decisionResult.RuleDecision != Decision.NoAction)
            {
                previousPeriod.PeriodDecisionRule = decisionResult.Rule;
                previousPeriod.PeriodDecision = decisionResult.RuleDecision;
                PeriodCalculations[PeriodCalculations.Count - 1] = previousPeriod;
                referencePeriod = previousPeriod;
            }
            
            return currentPeriod;
        }

        private void SetDecisionRules()
        {
            decisionProcessor = new PeriodDecisionProcessor(new List<string> { Constants.BESTTREND });
            PeriodCalculations = new List<PeriodAttributes>();
        }

        #endregion
    }
}

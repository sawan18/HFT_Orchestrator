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
    public class PriceTrendProcessor : StockBase
    {

        private List<StockFeed> feeds = new List<StockFeed>();
        private PeriodAttributes referencePeriod = new PeriodAttributes();
        private PeriodDecisionProcessor decisionProcessor = new PeriodDecisionProcessor(new List<string>{Constants.PRICETREND });
        private double priceAdjustment = 1.0;
        private int peakAdjustment = 0;
        private PeriodAttributes lastPeak = new PeriodAttributes();
        private List<Trend> trends = new List<Trend>();

        #region constructor
        public PriceTrendProcessor(double initialValue = 0, long initialVolume = 0, double transactionFee = 0, double adjustment = 0, int peakAdjustment = 0)
        {            
            base.InitialVolume = initialVolume;
            base.InitialValue = initialValue;
            base.TransactionFee = transactionFee;
            priceAdjustment = adjustment;
            this.peakAdjustment = peakAdjustment;
            SetDecisionRules();
        }
        
        #endregion
        /// <summary>
        /// Resets the feed data.
        /// </summary>
        public override void Reset()
        {
            this.feeds.Clear();
            this.trends.Clear();
        }

        /// <summary>
        /// Seeds the data for the engine.
        /// </summary>
        /// <param name="data"></param>
        public override void SeedData(StockFeed data)
        {
            // Calculate current truend
            var currentPeriod = ComputeTrend(data);
            
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

            var periodResult = Simulator.ProcessPeriod(data.Period
                , data.Price
                , currentPeriod.Limit
                , 0
                , 0
                , currentPeriod.PeriodDecision);

            Trader.ProcessDecision(data.Stock, currentPeriod.PeriodDecisionRule, periodResult);

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
                Price = newFeed.Price,
                Index = newFeed.Index
            };

            // Pull the previous entry
            var previousPeriod = PeriodCalculations.Count > 0 ? PeriodCalculations[PeriodCalculations.Count - 1] : GetDefaultPeriod(currentPeriod);

            // Generate trend data using a smoother
            currentPeriod = GenerateTrendData(currentPeriod, previousPeriod);

            //Calculate decisions based on the current MACD attribute.            
            return CalculateDecision(currentPeriod);
        }

        private PeriodAttributes GenerateTrendData(PeriodAttributes currentPeriod, PeriodAttributes previousPeriod)
        {
            //var minimumChange = GetAverageActualChange(currentPeriod.Price > previousPeriod.Price ? 1 : -1);

            var minimumChange = priceAdjustment;
            var ignoreTrend = false;

            if (previousPeriod.Trend == 0)
            {
                minimumChange = 0;
            }

            //We need to have a trend. So when there is no trend, don't smoothen the curve.
            if ((currentPeriod.Price - previousPeriod.Price) >= minimumChange)
            {
                //Trending up
                currentPeriod.Trend = 1;

            }
            else if (Math.Abs((previousPeriod.Price - currentPeriod.Price)) < minimumChange)
            {
                //Trend is neither up or down
                currentPeriod.Trend = previousPeriod.Trend;

                //Ignore this trend though
                ignoreTrend = true;

            }
            else
            {
                // Trending down
                currentPeriod.Trend = -1;
            }


            //Calculate Trend Repeats
            if (currentPeriod.Trend == previousPeriod.Trend)
            {
                if (!ignoreTrend)
                {
                    currentPeriod.TrendRepeats = previousPeriod.TrendRepeats + 1;
                }
            }
            else
            {
                //Save the previous period as a new trend
                trends.Add(new Trend(previousPeriod.Period, previousPeriod.Trend, previousPeriod.TrendRepeats, (currentPeriod.Price - previousPeriod.Price) / currentPeriod.Price));

                // We capture the last peak price and the trend that got us to this peak from the previous peak
                lastPeak = previousPeriod;

                currentPeriod.TrendRepeats = 1;
            }

            if ((currentPeriod.Price - referencePeriod.Price) >= priceAdjustment * 3
                   && referencePeriod.PeriodDecision == Decision.Buy)
            {
                //Increase our reference price 
                referencePeriod.HighPrice = currentPeriod.Price;
                referencePeriod.HighPriceTrend += 1;
            }

            if ((referencePeriod.Price - currentPeriod.Price) > priceAdjustment * 3
                    && referencePeriod.PeriodDecision == Decision.Sell)
            {
                //Increase our reference price 
                referencePeriod.LowPrice = currentPeriod.Price;
                referencePeriod.LowPriceTrend += 1;
            }

            if (currentPeriod.Price < previousPeriod.Price)
                currentPeriod.ActualTrend = -1;
            else if (currentPeriod.Price > previousPeriod.Price)
                currentPeriod.ActualTrend = 1;
            else
                currentPeriod.ActualTrend = 0;

            currentPeriod.ActualChange = Math.Abs(currentPeriod.Price - previousPeriod.Price);

            return currentPeriod;
        }

        /// <summary>
        /// Calculate decisions
        /// </summary>
        /// <param name="macdAttribute"></param>
        /// <returns></returns>
        private PeriodAttributes CalculateDecision(PeriodAttributes currentPeriod)
        {
            var previousPeriod = PeriodCalculations[PeriodCalculations.Count - 1];
            DecisionResult decisionResult = new DecisionResult("Start", Decision.NoAction, 0);
            var sellRepeat = GetAverageRepeat(1);
            var buyRepeat = GetAverageRepeat(-1);
            var avgChange = GetAverageChange(1);

            if (decisionResult.RuleDecision == Decision.NoAction)
            {
                if (sellRepeat == 0 || buyRepeat == 0)
                {
                    decisionResult = decisionProcessor.GetDecision(referencePeriod, currentPeriod, previousPeriod, lastPeak);
                }
                else
                {                    
                    decisionResult = decisionProcessor.GetDecision(referencePeriod, currentPeriod, previousPeriod, lastPeak, sellRepeat, buyRepeat, avgChange);
                }
                
            }

            if (decisionResult.RuleDecision != Decision.NoAction)
            {
                currentPeriod.PeriodDecisionRule = decisionResult.Rule;
                currentPeriod.PeriodDecision = decisionResult.RuleDecision;
                currentPeriod.Limit = decisionResult.Tolerance;
                referencePeriod = currentPeriod;
            }
            
            return currentPeriod;
        }
        
        private void SetDecisionRules()
        {
            decisionProcessor = new PeriodDecisionProcessor(new List<string> { Constants.PRICETREND });
            PeriodCalculations = new List<PeriodAttributes>();
        }

        private int GetAverageRepeat(int trendDirection)
        {
            try
            {
                var list = (from t in this.trends
                            where t.TrendDirection == trendDirection
                            orderby t.Period descending
                            select t
                        ).Take(5);

                var averagePeak = Convert.ToInt32(list.Average(t => t.Repeat));
                return averagePeak < this.peakAdjustment ? this.peakAdjustment : averagePeak;
            }
            catch
            {
                return 0;
            }            
        }

        private double GetAverageChange(int trendDirection)
        {
            try
            {
                var size = 9;
                if(this.trends.Count < 9)
                {
                    size = this.trends.Count;
                }

                var list = (from t in this.trends
                            where t.TrendDirection == trendDirection
                            orderby t.Period descending
                            select t
                        ).Take(size);

                return list.Average(t => t.Change);
            }
            catch
            {
                return 0;
            }
        }

        private double GetAverageActualChange(int trendDirection)
        {
            try
            {
                var size = 5;
                if (this.PeriodCalculations.Count < size)
                {
                    size = this.PeriodCalculations.Count;
                }

                var list = (from t in this.PeriodCalculations
                            where t.ActualTrend == trendDirection
                            orderby t.Period descending
                            select t
                        ).Take(size);                

                return list.Average(t => t.ActualChange);
            }
            catch
            {
                return 0;
            }
        }

        #endregion
    }

    public struct Trend
    {
        public Trend(DateTime period, int trendDirection, int repeat, double change)
        {
            this.Period = period;
            this.TrendDirection = trendDirection;
            this.Repeat = repeat;
            this.Change = change;
        }
        public DateTime Period { get; set; }
        public int TrendDirection { get; set; }
        public int Repeat { get; set; }

        public double Change { get; set; }

    }
}

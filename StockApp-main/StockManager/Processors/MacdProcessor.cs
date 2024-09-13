using StockManager.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockManager.Processors
{
    /// <summary>
    /// MACD Processor
    /// References:
    /// https://www.investopedia.com/terms/e/ema.asp
    /// https://www.investopedia.com/terms/m/macd.asp
    /// https://medium.com/@randerson112358/determine-when-to-buy-sell-stock-edeeac03f9fb
    /// https://school.stockcharts.com/doku.php?id=technical_indicators:moving_average_convergence_divergence_macd
    /// </summary>
    public class MacdProcessor : StockBase
    {

        private List<StockFeed> feeds = new List<StockFeed>();
        private PeriodAttributes referencePeriod = new PeriodAttributes();
        private PeriodDecisionProcessor decisionProcessor = new PeriodDecisionProcessor(new List<string>{Constants.CROSSOVERUP, Constants.CROSSOVERDOWN});
        private PeriodAttributes lastPeak = new PeriodAttributes();

        #region constructor
        public MacdProcessor(int emashortTerm = 12, int emaLongTerm = 26, int emaSignalTerm = 9, double smoothing = 2.0, double initialValue = 0, long initialVolume = 0, double transactionFee = 0, MacdDecisionMode decisionMode = MacdDecisionMode.CrossoversOnly)
        {
            this.EmaShortTerm = emashortTerm;
            this.EmaLongTerm = emaLongTerm;
            this.EmaSignalTerm = emaSignalTerm;
            this.Smoothing = smoothing;
            base.InitialVolume = initialVolume;
            base.InitialValue = initialValue;
            base.TransactionFee = transactionFee;
            base.DecisionMode = decisionMode;

            SetDecisionRules();
        }
        
        #endregion

        #region properties

        /// <summary>
        /// Gets and sets the EMA Shorter Period
        /// </summary>
        public int EmaShortTerm { get; set; }

        /// <summary>
        /// Gets and Sets EMA Longer period (Default 26)
        /// </summary>
        public int EmaLongTerm { get; set; }

        /// <summary>
        /// Gets and Sets EMA Signal Period calculation period (Default 9)
        /// </summary>
        public int EmaSignalTerm { get; set; }

        /// <summary>
        /// Gets and sets the Smoothing for MACD processor.
        /// </summary>
        public double Smoothing { get; set; }

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
            // Calcualte MACD
            var macdAttribute = ComputeMacd(data);

            // This is a greedy approach. We tag the high prices to help us 
            // Minimize loses.
            if (macdAttribute.Price > referencePeriod.Price                    
                    && referencePeriod.PeriodDecision == Decision.Buy)
            {
                //Increase our reference price 
                referencePeriod.Price = macdAttribute.Price;
            }

            // Cache the feed
            feeds.Add(data);

            // Cache the previous calculations
            PeriodCalculations.Add(macdAttribute);

            var maxCacheSize = EmaLongTerm + EmaSignalTerm + 2;

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
                base.PeriodCalculations = adjustedPeriodCalculations;
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
                , 0
                , macdAttribute.Macd
                , macdAttribute.Signal
                , macdAttribute.PeriodDecision);

            Trader.ProcessDecision(data.Stock, macdAttribute.PeriodDecisionRule, periodResult);
        }

        #region private methods

        private PeriodAttributes ComputeMacd(StockFeed newFeed)
        {
            //Compute the EMA values;
            var emaShort = CalculateEma(EmaShortTerm, newFeed);
            var emaLong = CalculateEma(EmaLongTerm, newFeed);
            var emaSignal = CalculateMacdSignal(EmaSignalTerm, newFeed);

            //If any of the EMAs is not calculated, MACD will not be computed.
            var macdCalculationValid = true;
            if (emaShort == -1 || emaLong == -1)
            {
                macdCalculationValid = false;
            }
            
            //Perform the MACD calculation
            var macd = macdCalculationValid ? (emaShort - emaLong) : Constants.INVALID_VALUE;
            var macdAttribute = new PeriodAttributes()
            {
                Period = newFeed.Period,
                MacdLongerPeriodLength = EmaLongTerm,
                MacdShorterPeriodLength = EmaShortTerm,
                MacdSignalPeriodLength = EmaSignalTerm,
                EmaLonger = emaLong,
                EmaShorter = emaShort,
                Signal = emaSignal,
                Macd = macd,
                Histogram = (macd != Constants.INVALID_VALUE && emaSignal != Constants.INVALID_VALUE) ? (macd - emaSignal): Constants.INVALID_VALUE,
                Price = newFeed.Price
            };

            if (!macdCalculationValid)
                return macdAttribute;

            // Pull the previous MACD calulation.
            var previousMacd = PeriodCalculations[PeriodCalculations.Count - 1];

            if (macdAttribute.Histogram > previousMacd.Histogram)
            {
                //Trending up
                macdAttribute.Trend = 1;

            } else if(previousMacd.Histogram == Constants.INVALID_VALUE || macdAttribute.Histogram == previousMacd.Histogram)
            {
                //Trend is neither up or down
                macdAttribute.Trend = 0;

            } else
            {
                // Trending down
                macdAttribute.Trend = -1;
            }

            //Calculate Trend Repeats
            if (macdAttribute.Trend == previousMacd.Trend)
            {
                macdAttribute.TrendRepeats = previousMacd.TrendRepeats +  1;
            }
            else
            {
                // We capture the last peak price and the trend that got us to this peak from the previous peak
                lastPeak = macdAttribute;
                lastPeak.Trend = previousMacd.Trend;

                macdAttribute.TrendRepeats = 1;
            }

            //Calculate decisions based on the current MACD attribute.            
            return CalculateDecision(macdAttribute);
        }

        /// <summary>
        /// Calculate decisions
        /// </summary>
        /// <param name="macdAttribute"></param>
        /// <returns></returns>
        private PeriodAttributes CalculateDecision(PeriodAttributes macdAttribute)
        {           
            
            if (macdAttribute.Macd == Constants.INVALID_VALUE)
            {
                macdAttribute.PeriodDecision = Decision.NoAction;
                return macdAttribute;
            }

            var previousMacd = PeriodCalculations[PeriodCalculations.Count - 1];
            DecisionResult decisionResult = decisionProcessor.GetDecision(referencePeriod, macdAttribute, previousMacd, lastPeak);

            if(decisionResult.RuleDecision != Decision.NoAction)
            {
                macdAttribute.PeriodDecisionRule = decisionResult.Rule;
                macdAttribute.PeriodDecision = decisionResult.RuleDecision;
                referencePeriod = macdAttribute;
            }
            
            return macdAttribute;
        }

        /// <summary>
        /// Calculate SMA for a set of prices.
        /// </summary>
        /// <param name="prices"></param>
        /// <returns>SMA</returns>
        private double CalculateSma(double[] prices)
        {
            return prices.Sum() / (double)prices.Length;
        }

        /// <summary>
        /// Calculate EMA for a period using a new feed.
        /// </summary>
        /// <param name="periodLength"></param>
        /// <param name="newFeed"></param>
        /// <returns></returns>
        private double CalculateEma(int periodLength, StockFeed newFeed)
        {
            // If we don't have enough periods, do not compute EMA
            if (feeds.Count < periodLength)
                return -1;

            // Pull items that matches the period
            var startIndex = feeds.Count - periodLength;
            var prices = new double[periodLength];
            for (var i = startIndex; i < feeds.Count; i++)
            {
                prices[i - startIndex] = feeds[i].Price;
            }

            var smoothingMeasure = (Smoothing / (1 + periodLength));
            var yesterdayEma = GetYesterdayEma(periodLength);

            if (yesterdayEma == -1)
            {
                yesterdayEma = CalculateSma(prices.ToArray());
            }
           
            return (newFeed.Price * smoothingMeasure) + (yesterdayEma * (1 - smoothingMeasure));
        }

        /// <summary>
        /// Calculate EMA for a period using a new feed.
        /// </summary>
        /// <param name="periodLength"></param>
        /// <param name="newFeed"></param>
        /// <returns></returns>
        private double CalculateMacdSignal(int periodLength, StockFeed newFeed)
        {
            // If we don't have enough periods, do not compute EMA
            if (feeds.Count < EmaLongTerm + periodLength)
                return Constants.INVALID_VALUE;

            // Pull items that matches the period
            var startIndex = PeriodCalculations.Count - periodLength;
            var previousMacds = new double[periodLength];
            for (var i = startIndex; i < PeriodCalculations.Count; i++)
            {
                previousMacds[i - startIndex] = PeriodCalculations[i].Macd;
            }

            return CalculateSma(previousMacds.ToArray());
        }

        /// <summary>
        /// Pulls yesterday EMA value.
        /// </summary>
        /// <param name="periodLength"></param>
        /// <returns></returns>
        private double GetYesterdayEma(int periodLength)
        {
            var yesterdayIndex = feeds.Count - 1;
            var yesterdayEma = 0.0;
            if (periodLength == EmaShortTerm)
            {
                yesterdayEma = PeriodCalculations[yesterdayIndex].EmaShorter;
            }

            if (periodLength == EmaLongTerm)
            {
                yesterdayEma = PeriodCalculations[yesterdayIndex].EmaLonger;
            }

            if (periodLength == EmaSignalTerm)
            {
                yesterdayEma = PeriodCalculations[yesterdayIndex].Signal;
            }

            return yesterdayEma;
        }

        private void SetDecisionRules()
        {
            if(this.DecisionMode == MacdDecisionMode.CrossoversOnly)
            {
                decisionProcessor = new PeriodDecisionProcessor(new List<string> { Constants.CROSSOVERUP, Constants.CROSSOVERDOWN });
            }
            else if (this.DecisionMode == MacdDecisionMode.Greedy)
            {
                decisionProcessor = new PeriodDecisionProcessor(
                    new List<string> {
                        Constants.CROSSOVERUPFIRSTBUY,
                        Constants.STRONGUPWARDTRENDINGSTOCK,
                        Constants.STRONGDOWNARDTRENDINGSTOCK
                     }
                    );
            }
            else
            {
                decisionProcessor = new PeriodDecisionProcessor(
                    new List<string> {
                        Constants.CROSSOVERUPFIRSTBUY,
                        Constants.STRONGTRENDPEAKING,
                        Constants.STRONGTRENDTROUGH }
                    );
            }

            PeriodCalculations = new List<PeriodAttributes>();
            
        }
        #endregion
    }
}

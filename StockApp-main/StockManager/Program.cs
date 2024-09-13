using StockManager.Model;
using StockManager.Processors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CCACDataAccessLayer;
using System.Data;
using StockManager.Rest;
using MyData.Csv;
using System.IO;
using System.Data.SqlClient;
using StockManager.Data;
using System.Timers;

namespace StockManager
{
    class Program
    {
        private static RunSettings settings = RunSettings.Instance;
        private static SqlServerCalls sqlCalls = new SqlServerCalls();
        private static ImportExportCalls importExportCalls = new ImportExportCalls();
        private static Algorithm processAlgorithm = Algorithm.Macd;
        private static DateTime startDate = DateTime.Today;
        private static DateTime endDate = DateTime.Today;
        private static string mode = "";
        private static DateTime simulationTime;
        private static Timer aTimer = new System.Timers.Timer();
        private static Dictionary<string, string> tradeSettings;
        private static bool testMode = true;
        private static string metaFolder = @"C:\Stock\App";

        private static bool DownloadHistoricalData(string symbol)
        {
            settings.Log.LogMessage("Downloading historical stock for " + symbol.ToUpper());
            startDate = DateTime.Today.Month <= 4 ? Convert.ToDateTime(String.Format("{0}-06-01", DateTime.Today.Year - 1)) : Convert.ToDateTime(String.Format("{0}-01-01", DateTime.Today.Year));
            endDate = DateTime.Now;

            YahooDataPull yahooDataPull = new YahooDataPull()
            {
                Symbol = symbol,
                Interval = YahooInterval.OneDay,
                StartPeriod = startDate,
                EndPeriod = endDate
            };
            
            //Download the CSV
            yahooDataPull.Download();

            //Import into the database
            return importExportCalls.ImportStockData(symbol);

        }

        private static bool GetDbConnection()
        {
            settings.Log.LogMessage("Initializing database connnection...");
            var dalProcessor = new DalProcessor
            {
                Server = settings.Server,
                Database = settings.Database,
                SqlUser = "",
                SqlPassword = ""
            };

            if (!dalProcessor.TestConnection())
            {
                settings.Log.LogError("Database connection initialization failed!");
                return false;
            }

            sqlCalls.Cn = dalProcessor;
            importExportCalls.Cn = dalProcessor;
            return true;
        }

        private static Algorithm GetAlgorithm(string algorithm)
        {
            if (algorithm.ToLower().StartsWith("macd"))
                return Algorithm.Macd;
            else if (algorithm.ToLower().StartsWith("price"))
                return Algorithm.PriceTrend;
            else if (algorithm.ToLower().StartsWith("best"))
                return Algorithm.BestTrend;
            else
                return Algorithm.Macd;
        }
        static void Main(string[] args)
        {          
            mode = "";

            //Process any run-time argument included.
            foreach (string arg in args)
            {
                //break up the string
                string[] tuple = arg.Split(':');
                string key = tuple[0].Trim();

                if (key.Equals("alg", StringComparison.CurrentCultureIgnoreCase))
                {
                    processAlgorithm = GetAlgorithm(tuple[1]);
                }

                if (key.Equals("mode", StringComparison.CurrentCultureIgnoreCase))
                {
                    mode = tuple[1];
                }

                if (key.Equals("server", StringComparison.CurrentCultureIgnoreCase))
                {
                    settings.Server = tuple[1];
                }

                if (key.Equals("database", StringComparison.CurrentCultureIgnoreCase))
                {
                    settings.Database = tuple[1];
                }

                if (key.Equals("meta", StringComparison.CurrentCultureIgnoreCase))
                {
                    metaFolder = "";
                    if (tuple.Length > 2)
                    {
                        for (int i = 1; i < tuple.Length; i++)
                        {
                            if (i < tuple.Length - 1)
                                metaFolder += tuple[i] + ":";
                            else
                                metaFolder += tuple[i];
                        }
                    }
                    else
                        metaFolder = tuple[1];

                    if(!String.IsNullOrEmpty(metaFolder) && Directory.Exists(metaFolder))
                    {
                        RunSettings.Instance.MetaDataFolder = metaFolder;
                    }
                }
            }

            settings.Log.WriteLogHeader("Run settings");
            settings.Log.LogMessage(String.Format("META {0}", RunSettings.Instance.MetaDataFolder));

            if (!GetDbConnection())
                return;

            if (!importExportCalls.ImportConfig())
                return;

            if (!importExportCalls.ImportSettings())
                return;


            tradeSettings = sqlCalls.GetAccountSettings();
            var heartBeat = tradeSettings.ContainsKey("HeartBeatMins") ? Convert.ToInt32(tradeSettings["HeartBeatMins"]) : 5;            
            testMode = tradeSettings.ContainsKey("TestMode") ? Convert.ToBoolean(tradeSettings["TestMode"]) : true;
            processAlgorithm = tradeSettings.ContainsKey("Algorithm") ? GetAlgorithm(tradeSettings["Algorithm"]) : Algorithm.PriceTrend;
            mode = tradeSettings.ContainsKey("RunMode") ? tradeSettings["RunMode"]: "predict_trades";
            settings.StopLossPercentage = tradeSettings.ContainsKey("StopLossPercentage") ? Convert.ToDouble(tradeSettings["StopLossPercentage"]) : 10.0;

            if(settings.StopLossPercentage > 0.0)
            {
                //Convert to a fraction
                settings.StopLossPercentage = settings.StopLossPercentage / 100.0;
            }

            settings.Log.LogMessage(String.Format("START {0}", processAlgorithm));
            settings.Log.LogMessage(String.Format("MODE {0}", mode));
            settings.Log.LogMessage(String.Format("TEST {0}", testMode ? "Yes" : "No"));
            settings.Log.LogMessage(String.Format("HEART BEAT(mins) {0}", heartBeat));
            settings.Log.LogMessage(String.Format("Server {0}", settings.Server));
            settings.Log.LogMessage(String.Format("Database {0}", settings.Database));

            settings.Log.PrintGap();


            if (mode.Equals("predict_trades", StringComparison.CurrentCultureIgnoreCase))
            {
                ProcessStockSimulation();
            } else if (mode.Equals("process_trades"))
            {
                settings.ProcessTrade = true;

                // Run the timed event                
                aTimer.Elapsed += new ElapsedEventHandler(ProcessTimeEvent);
                aTimer.Interval = 1000 * 60 * heartBeat;
                aTimer.Enabled = true;
                aTimer.Start();
                ProcessTimeEvent(null, null);
            }            

            Console.Read();
        }        

        private static bool RerunSimulation()
        {
            if ((DateTime.Now - sqlCalls.GetLastTradeDataDate()).TotalHours >= 24)
                return true;            
            else
            {
                if ((DateTime.Now - simulationTime).TotalHours < 10)
                    return false;
                else if ((DateTime.Now.Hour == 9 && DateTime.Now.Minute >= 15 && DateTime.Now.Minute <= 30) && (DateTime.Now - sqlCalls.GetLastTradeDataDate()).TotalMinutes > 30)
                    return true;
                else
                    return false;
            }
        }
            
        private static void ProcessTimeEvent(object source, ElapsedEventArgs e)
        {
            aTimer.Enabled = false;
            settings.Log.PrintGap();
            settings.Log.LogMessage("Heart beat: " + DateTime.Now.ToString());
            settings.Log.PrintGap();

            try
            {
                if (mode.Equals("predict_prices", StringComparison.CurrentCultureIgnoreCase))
                {
                    TradeProcessor tradeProcessor = new TradeProcessor(sqlCalls, false);
                    tradeProcessor.PredictStockPrices();
                }

                if (RerunSimulation())
                {
                    ProcessStockSimulation();
                }

                if (mode.Equals("process_trades", StringComparison.CurrentCultureIgnoreCase))
                {
                    TradeProcessor tradeProcessor = new TradeProcessor(sqlCalls, testMode);
                    tradeProcessor.ProcessTrades();
                }

            }
            catch (Exception ex)
            {
                settings.Log.LogError("Unknown exception: {0}", ex.Message);
                settings.Log.LogError("Error stack trace: {0}", ex.ToString());
            }
            
            aTimer.Enabled = true;
        }
        private static void ProcessStockSimulation()
        {
            simulationTime = DateTime.Now;

            var dt = sqlCalls.GetStockDefinitions();
            foreach (DataRow row in dt.Rows)
            {
                var stock = row["Symbol"].ToString();
                var name = row["Name"].ToString();
                var active = Convert.ToBoolean(row["Active"]);
                var value = row["Value"] == DBNull.Value ? 0 : Convert.ToDouble(row["Value"]);
                var volume = row["Volume"] == DBNull.Value ? 0 : Convert.ToInt64(row["Volume"]);
                var algorithm = row["Algorithm"] == DBNull.Value ? processAlgorithm : (Algorithm)Enum.Parse(typeof(Algorithm), row["Algorithm"].ToString());
                var peakAdjustment = row["PeakAdjustment"] == DBNull.Value ? 0 : Convert.ToInt32(row["PeakAdjustment"]);

                settings.Log.PrintGap();
                settings.Log.WriteLogHeader(String.Format("Processing {0} stocks", stock));

                if (!DownloadHistoricalData(stock))
                    return;

                settings.Log.LogMessage("Simulating stock trading for " + stock + " using " + algorithm.ToString());
                SimulateStockTrade(algorithm, stock, value, volume, peakAdjustment);
                settings.Log.PrintGap();
            }
        }
        private static void SimulateStockTrade(Algorithm algorithm, string symbol, double value, long volume, int peakAdjustment = 0)
        {
            var feeds = new List<StockFeed>();            
            var dt = sqlCalls.GetStockValues(symbol);

            var index = 0;
            foreach (DataRow row in dt.Rows)
            {
                index += 1;
                feeds.Add(new StockFeed(
                    index
                    , symbol
                    , Convert.ToDateTime(row["Date"])
                    , Convert.ToDouble(row["Close"])
                    , Convert.ToInt64(row["Volume"])
                    )
                );
            }

            StockBase stockMgr = new MacdProcessor(
                        initialValue: value
                        , initialVolume: volume
                        , transactionFee: 0
                        , decisionMode: MacdDecisionMode.CrossoversOnly);

            switch (algorithm)
            {                
                case Algorithm.PriceTrend:
                    var smoother = sqlCalls.GetStockSmoother(symbol, startDate, endDate);
                    stockMgr = new PriceTrendProcessor(
                        initialValue: value
                        , initialVolume: volume
                        , transactionFee: 0
                        , adjustment: smoother
                        , peakAdjustment: peakAdjustment);
                    break;
                case Algorithm.BestTrend:
                    stockMgr = new BestTrendProcessor(
                        initialValue: value
                        , initialVolume: volume
                        , transactionFee: 0);
                    break;                
            }

            ProcessResults(algorithm, symbol, value, volume, feeds, stockMgr);
        }

        private static void ProcessResults(Algorithm algorithm, string symbol, double value, long volume, List<StockFeed> feeds, StockBase stockMgr)
        {           
            try
            {
                for (var i = 0; i < feeds.Count; i++)
                {
                    var feed = feeds[i];
                    stockMgr.SeedData(feed);
                }
            }
            catch(Exception ex)
            {
                settings.Log.LogError("Uknown error processing stock " + ex.Message);
            }

            sqlCalls.CreateTradeTable();
            sqlCalls.ClearStockForSymbol(symbol);
            foreach (var trade in stockMgr.Trader.Trades)
            {
                settings.Log.LogMessage(String.Format("Period: {0}, Type: {1} ({5}), Price: {2}, Value: {3}, Volume: {4}"
                    , trade.Period.ToString("dd MMM yyyy")
                    , trade.TradeType
                    , trade.Price
                    , trade.Value
                    , trade.Volume
                    , trade.Rule
                    ));

                sqlCalls.SaveSimulatedTrade(symbol, trade);
            }

            var tradedPeriods = 0;
            var totalTradedPeriod = Convert.ToInt32((feeds[feeds.Count - 1].Period - stockMgr.Trader.Trades[0].Period).TotalDays);
            for (int i=0; i < stockMgr.Trader.Trades.Count; i += 2)
            {
                tradedPeriods += Convert.ToInt32((
                    ((i+1) >= stockMgr.Trader.Trades.Count ? DateTime.Today : stockMgr.Trader.Trades[i + 1].Period) - stockMgr.Trader.Trades[i].Period).TotalDays);
            }

            foreach (var simulationResult in stockMgr.Simulator.Results)
            {
                sqlCalls.SaveSimulationData(symbol, simulationResult);
            }

            var nonSimulationValue = (value / feeds[0].Price) * feeds[feeds.Count - 1].Price;
            var stockPerformance = (feeds[feeds.Count - 1].Price - feeds[0].Price) / feeds[0].Price;
            var algorithmPerformance = (stockMgr.Simulator.CurrentValue - stockMgr.Simulator.InitialValue) / stockMgr.Simulator.InitialValue;

            settings.Log.PrintGap();
            settings.Log.LogMessage(String.Format("{0} Summary", symbol));
            settings.Log.LogMessage(String.Format("Initial stock value: {0}", value));
            settings.Log.LogMessage(String.Format("Initial volume: {0}", stockMgr.Simulator.InitialVolume));
            settings.Log.PrintGap();

            settings.Log.LogMessage(String.Format("Current volume: {0}", stockMgr.Simulator.CurrentVolume));
            settings.Log.LogMessage(String.Format("Current value trading with {0}: {1}", algorithm, stockMgr.Simulator.CurrentValue));
            settings.Log.LogMessage(String.Format("Current value with no trading: {0}", nonSimulationValue));
            settings.Log.LogMessage(String.Format("Stock Performance: {0} %", (stockPerformance * 100.00).ToString("0#.00")));
            settings.Log.LogMessage(String.Format("Stock Algorithm Performance: {0} %", (algorithmPerformance * 100.00).ToString("0#.00")));
            var diff = (stockMgr.Simulator.CurrentValue - nonSimulationValue);

            if (diff > 0)
            {
                settings.Log.LogMessage(String.Format("{0} performance above untraded stock - Profit: {1}", algorithm, diff));
            }
            else
            {
                settings.Log.LogMessage(String.Format("{0} performance above untraded stock - Loss: {1}", algorithm, diff * -1));
            }

            settings.Log.PrintGap();
            diff = (stockMgr.Simulator.CurrentValue - value);
            if (diff > 0)
            {
                settings.Log.LogMessage(String.Format("{0} investment profit: {1}", algorithm, diff));
            }
            else
            {
                settings.Log.LogMessage(String.Format("{0} investment loss: {1}", algorithm, diff * - 1));
            }

            sqlCalls.SaveStockPerformance(symbol, algorithm.ToString(), totalTradedPeriod, tradedPeriods, stockPerformance, algorithmPerformance);
            importExportCalls.ExportSymbolSummary(symbol, processAlgorithm);
        }
    }
}

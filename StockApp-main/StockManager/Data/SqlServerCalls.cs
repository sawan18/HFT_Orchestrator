using CCACDataAccessLayer;
using StockManager.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockManager.Data
{
    public class SqlServerCalls
    {        
        public DalProcessor Cn { get; set; }

        public Dictionary<string, string> GetAccountSettings()
        {
            var sql = @"
                SELECT 
                   [Key]
                  ,[Value]
              FROM [dbo].[AccountSettings]
            ";

            var dt = Cn.ExecuteQuery(sql);

            if (dt != null && dt.Rows.Count > 0)
            {
                Dictionary<string, string> items = new Dictionary<string, string>();
                foreach (DataRow row in dt.Rows)
                {
                    items.Add(row["Key"].ToString(), row["Value"].ToString());
                }
                return items;
            }
            else
                return new Dictionary<string, string>();
        }

        public DataTable GetTradeTriggers(DateTime period)
        {
            var sql = @"
                WITH StockDefinitions 
                AS 
                (
                SELECT [Symbol]
                      ,[Name]
                      ,[Active]
                      ,[Value]
                      ,[Volume]
	                  ,[InitialTradeQuantity]
                      ,[Algorithm]
                      ,[PeakAdjustment]
                  FROM [dbo].[StockDefinitions]
                  WHERE Active='TRUE'
                ),
                LastTrades AS
                (
	                SELECT [ID]
                        ,[Symbol]
                        ,[TradeDateTime]
                        ,[TradeType]
                        ,[OrderId]
                        ,[Instruction]
                        ,[Price]
                        ,[Volume]
                        ,[Value]
                        ,[DateGenerated]
		                ,ROW_NUMBER() OVER (PARTITION BY [Symbol] ORDER BY TradeDateTime DESC) As Row_No
                    FROM [dbo].[ActualTrades]
                ),
                StockPerfomance AS
                (
                    SELECT [Symbol]
                        , [AlgorithmPerformance]
                        ,ROW_NUMBER() OVER (PARTITION BY [Symbol] ORDER BY DateGenerated DESC) As Row_No
                    FROM [dbo].[StockPerformance]
                )

                SELECT st.[ID]
                    ,st.[Symbol]
                    ,st.[Period]
                    ,st.[TradeType]
                    ,st.[Price]
                    ,st.[Limit]
                    ,st.[Value]
	                ,CASE WHEN lt.Volume > 0 THEN lt.Volume
		                WHEN sd.InitialTradeQuantity > 0 THEN sd.InitialTradeQuantity
		                ELSE st.[Volume]
	                 END As Volume
                    ,st.[RuleApplied]
                    ,st.[DateGenerated]
                    ,ROW_NUMBER() OVER (PARTITION BY st.[Symbol] ORDER BY [Period] DESC) As Row_No
                FROM [dbo].[StockTrades] st
                LEFT JOIN StockDefinitions sd ON st.Symbol = sd.Symbol
                LEFT JOIN LastTrades lt ON lt.Symbol = st.Symbol AND lt.Row_No = 1
                LEFT JOIN LastTrades ltf ON ltf.Symbol = st.Symbol AND ltf.TradeType = st.TradeType AND lt.Row_No = 1
                LEFT JOIN StockPerfomance sp ON sp.Symbol = st.Symbol AND sp.Row_No = 1
                WHERE 
                -- We pick up signals from days before that were left unfulfilled.
                ([Period] BETWEEN DATEADD(day, -4, @Period) AND @Period) 
                -- We also want to make sure that it has not be fulfilled.
                AND (ltf.ID IS NULL OR st.Period >= ltf.TradeDateTime OR DATEDIFF(day, ltf.TradeDateTime, getdate()) > 4)
                ORDER BY st.TradeType, sp.[AlgorithmPerformance] DESC
            ";

            var parameters = new List<SqlParameter>();
            parameters.Add(new SqlParameter("Period", period.ToString("yyyy-MM-dd")));
            return Cn.ExecuteQuery(sql, parameters);
        }

        public DataTable GetSavedActualTrades(DateTime period)
        {
            var sql = @"
                SELECT [ID]
                  ,[Symbol]
                  ,[TradeDateTime]
                  ,[TradeType]
                  ,[OrderId]
                  ,[Instruction]
                  ,[Price]
                  ,[Volume]
                  ,[Value]
                  ,[DateGenerated]
              FROM [dbo].[ActualTrades]
              WHERE [TradeDateTime] >= @Period AND [TradeDateTime] <= @Period2
            ";

            var parameters = new List<SqlParameter>();
            parameters.Add(new SqlParameter("Period", period.ToString("yyyy-MM-dd")));
            parameters.Add(new SqlParameter("Period2", period.AddDays(1).ToString("yyyy-MM-dd")));
            return Cn.ExecuteQuery(sql, parameters);
        }

        public DataTable GetSymbolLastBuy(string symbol, DateTime period)
        {
            var sql = @"
              SELECT Top 1 *
              FROM [dbo].[ActualTrades]
              WHERE [Symbol] = @Symbol AND TradeType=1
              ORDER BY TradeDateTime DESC
            ";

            var parameters = new List<SqlParameter>();
            parameters.Add(new SqlParameter("Symbol", symbol));

            return Cn.ExecuteQuery(sql, parameters);
        }

        public int GetSymbolSavedActualTrades(string symbol, DateTime period)
        {
            var sql = @"
              SELECT COUNT(1) As Total
              FROM [dbo].[ActualTrades]
              WHERE [Symbol] = @Symbol AND [TradeDateTime] >= @Period AND [TradeDateTime] <= @Period2
            ";

            var parameters = new List<SqlParameter>();
            parameters.Add(new SqlParameter("Symbol", symbol));
            parameters.Add(new SqlParameter("Period", period.ToString("yyyy-MM-dd")));
            parameters.Add(new SqlParameter("Period2", period.AddDays(1).ToString("yyyy-MM-dd")));
            object cnt =  Cn.ExecuteScalar(sql, parameters);

            if(cnt == DBNull.Value)
            {
                return 0;
            }
            else
            {
                return Convert.ToInt32(cnt);
            }
        }

        public void SaveActualTrade(string symbol, DateTime tradeDateStamp, Decision decision, string instruction, string orderId, double price, double volume)
        {
            var parameters = new List<SqlParameter>();
            string sql;
            if (GetSymbolSavedActualTrades(symbol, tradeDateStamp) == 0)
            {
                sql = @"
                INSERT INTO [dbo].[ActualTrades]
                       ([Symbol]
                       ,[TradeDateTime]
                       ,[OrderId]
                       ,[TradeType]
                       ,[Instruction]
                       ,[Price]
                       ,[Volume]
                       ,[Value]
                       ,[DateGenerated])
                 VALUES
                       (
                        @Symbol
                       ,@TradeDateStamp
                       ,@OrderId
                       ,@TradeType
                       ,@Instruction
                       ,@Price
                       ,@Volume
                       ,@Value
                       ,getdate()
                    )
                ";

                parameters.Add(new SqlParameter("Symbol", symbol));
                parameters.Add(new SqlParameter("TradeDateStamp", tradeDateStamp));
                parameters.Add(new SqlParameter("OrderId", orderId));
                parameters.Add(new SqlParameter("TradeType", (int)decision));
                parameters.Add(new SqlParameter("Instruction", instruction));
                parameters.Add(new SqlParameter("Price", price));
                parameters.Add(new SqlParameter("Value", price * volume));
                parameters.Add(new SqlParameter("Volume", volume));

                Cn.ExecuteQuery(sql, parameters);
            }            
        }

        public void SaveSimulatedTrade(string symbol, StockTrade trade)
        {            
            var sql = @"
                INSERT INTO [dbo].[StockTrades]
                       ([Symbol]
                       ,[Period]
                       ,[TradeType]
                       ,[Price]
                       ,[Limit]
                       ,[Value]
                       ,[Volume]
                       ,[RuleApplied]
                       ,[DateGenerated])
                 VALUES
                       (
                        @Symbol
                       ,@Period
                       ,@TradeType
                       ,@Price
                       ,@Limit
                       ,@Value
                       ,@Volume
                       ,@RuleApplied
                       ,getdate()
                )
            ";

            var parameters = new List<SqlParameter>();
            parameters.Add(new SqlParameter("Symbol", symbol));
            parameters.Add(new SqlParameter("Period", trade.Period));
            parameters.Add(new SqlParameter("TradeType", trade.TradeType));
            parameters.Add(new SqlParameter("Price", trade.Price));
            parameters.Add(new SqlParameter("Limit", trade.Limit));
            parameters.Add(new SqlParameter("Value", trade.Value));
            parameters.Add(new SqlParameter("Volume", trade.Volume));
            parameters.Add(new SqlParameter("RuleApplied", trade.Rule.ToString()));

            Cn.ExecuteQuery(sql, parameters);
        }

        public void SaveSimulationData(string symbol, SimulationResult simulationResult)
        {
            var sql = @"
                INSERT INTO [dbo].[SimulationData]
                       ([Symbol]
                       ,[Period]
                       ,[TradeType]
                       ,[Price]
                       ,[Value]
                       ,[Control]
                       ,[Volume]
                       ,[Macd]
                       ,[Signal]
                       ,[Histogram]
                       ,[DateGenerated])
                 VALUES
                       (
                        @Symbol
                       ,@Period
                       ,@TradeType
                       ,@Price
                       ,@Value
                       ,@Control
                       ,@Volume
                       ,@Macd
                       ,@Signal
                       ,@Histogram        
                       ,getdate()
                )
            ";

            var parameters = new List<SqlParameter>();
            parameters.Add(new SqlParameter("Symbol", symbol));
            parameters.Add(new SqlParameter("Period", simulationResult.Period));
            parameters.Add(new SqlParameter("TradeType", simulationResult.TradeDecision));
            parameters.Add(new SqlParameter("Price", simulationResult.Price));
            parameters.Add(new SqlParameter("Value", simulationResult.Value));
            parameters.Add(new SqlParameter("Control", simulationResult.Control));
            parameters.Add(new SqlParameter("Macd", (simulationResult.Macd != Constants.INVALID_VALUE ? simulationResult.Macd: 0)));
            parameters.Add(new SqlParameter("Signal", (simulationResult.Signal != Constants.INVALID_VALUE) ? simulationResult.Signal : 0));
            parameters.Add(new SqlParameter("Histogram", simulationResult.Signal != Constants.INVALID_VALUE ? (simulationResult.Macd - simulationResult.Signal) : 0));
            parameters.Add(new SqlParameter("Volume", simulationResult.Volume));

            Cn.ExecuteQuery(sql, parameters);
        }

        public double GetStockPerformance(string symbol)
        {
            var sql = @"
                SELECT Top 1 [StockPerformance]
                FROM [dbo].[StockPerformance]
                WHERE [Symbol] = @Symbol
            ";

            var parameters = new List<SqlParameter>();
            parameters.Add(new SqlParameter("Symbol", symbol));

            object result = Cn.ExecuteScalar(sql, parameters);

            if (result == DBNull.Value)
                return 1.0;
            else
                return Convert.ToDouble(result);
        }

        public double GetAlgorithmPerformance(string symbol)
        {
            var sql = @"
                SELECT Top 1 [AlgorithmPerformance]
                FROM [dbo].[StockPerformance]
                WHERE [Symbol] = @Symbol
                ORDER BY [DateGenerated] DESC
            ";

            var parameters = new List<SqlParameter>();
            parameters.Add(new SqlParameter("Symbol", symbol));

            object result = Cn.ExecuteScalar(sql, parameters);

            if (result == DBNull.Value)
                return 1.0;
            else
                return Convert.ToDouble(result);
        }

        public void SaveStockPerformance(string symbol, string algorithm, int totalPeriods, int tradingDays, double stockPerformance, double algorithmPerformance)
        {
            var sql = @"
                INSERT INTO [dbo].[StockPerformance]
                       ([Symbol]
                       ,[Algorithm]
                       ,[TotalPeriods]
                       ,[TradingDays]
                       ,[StockPerformance]
                       ,[AlgorithmPerformance]
                       ,[DateGenerated])
                 VALUES
                       (
                        @Symbol
                       ,@Algorithm
                       ,@TotalPeriods
                       ,@TradingDays
                       ,@StockPerformance
                       ,@AlgorithmPerformance
                       ,getdate()
                )
            ";

            var parameters = new List<SqlParameter>();
            parameters.Add(new SqlParameter("Symbol", symbol));
            parameters.Add(new SqlParameter("Algorithm", algorithm));
            parameters.Add(new SqlParameter("TotalPeriods", totalPeriods));
            parameters.Add(new SqlParameter("TradingDays", tradingDays));
            parameters.Add(new SqlParameter("StockPerformance", stockPerformance));
            parameters.Add(new SqlParameter("AlgorithmPerformance", algorithmPerformance));

            Cn.ExecuteQuery(sql, parameters);
        }

        public void CreateTradeTable()
        {
            if (!Cn.TableExist(new Table("StockPerformance")))
            {
                var sql = @"
                CREATE TABLE [dbo].[StockPerformance](
	                [ID] [int] IDENTITY(1,1) NOT NULL,
	                [Symbol] [nvarchar](100) NULL,
                    [Algorithm] [nvarchar](100) NULL,
	                [TotalPeriods] [int] NULL,
	                [TradingDays] [int] NULL,
                    [StockPerformance] [float] NULL,
                    [AlgorithmPerformance] [float] NULL,
	                [DateGenerated] [datetime] NULL,
                 CONSTRAINT [PK_StockPerformance] PRIMARY KEY CLUSTERED 
                (
	                [ID] ASC
                )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
                ) ON [PRIMARY]
                ";
                Cn.ExecuteScalar(sql);
            }

            if (!Cn.TableExist(new Table("ActualTrades")))
            {
                var sql = @"
                CREATE TABLE [dbo].[ActualTrades](
	                [ID] [int] IDENTITY(1,1) NOT NULL,
	                [Symbol] [nvarchar](100) NULL,
	                [TradeDateTime] [datetime] NULL,
	                [TradeType] [int] NULL,
                    [Instruction] varchar(100) NULL,
                    [OrderId] nvarchar(100) NULL,
	                [Price] [float] NULL,
	                [Volume] [float] NULL,
	                [Value] [float] NULL,
	                [DateGenerated] [datetime] NULL,
                 CONSTRAINT [PK_ActualTrades] PRIMARY KEY CLUSTERED 
                (
	                [ID] ASC
                )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
                ) ON [PRIMARY]
                ";
                Cn.ExecuteScalar(sql);
            }

            if (!Cn.TableExist(new Table("StockTrades")))
            {
                var sql = @"
                CREATE TABLE [dbo].[StockTrades](
	                [ID] [int] IDENTITY(1,1) NOT NULL,
	                [Symbol] [nvarchar](100) NULL,
	                [Period] [datetime] NULL,
	                [TradeType] [int] NULL,
	                [Price] [float] NULL,
                    [Limit] [float] NULL,
	                [Value] [float] NULL,
	                [Volume] [float] NULL,
	                [RuleApplied] [nvarchar](250) NULL,
	                [DateGenerated] [datetime] NULL,
                 CONSTRAINT [PK_StockTrades] PRIMARY KEY CLUSTERED 
                (
	                [ID] ASC
                )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
                ) ON [PRIMARY]
                ";
                Cn.ExecuteScalar(sql);
            }

            if (!Cn.TableExist(new Table("SimulationData")))
            {
                var sql = @"
                CREATE TABLE [dbo].[SimulationData](
	                [ID] [int] IDENTITY(1,1) NOT NULL,
	                [Symbol] [nvarchar](100) NULL,
	                [Period] [datetime] NULL,
	                [TradeType] [int] NULL,
	                [Price] [float] NULL,
	                [Value] [float] NULL,
                    [Control] [float] NULL,
	                [Volume] [float] NULL, 
                    [Macd] [float] NULL,
                    [Signal] [float] NULL,
                    [Histogram] [float] NULL,
	                [DateGenerated] [datetime] NULL,
                 CONSTRAINT [PK_SimulationData] PRIMARY KEY CLUSTERED 
                (
	                [ID] ASC
                )WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
                ) ON [PRIMARY]
                ";
                Cn.ExecuteScalar(sql);
            }
        }

        public void ClearStockForSymbol(string symbol)
        {
            Cn.ExecuteScalar(String.Format("DELETE FROM dbo.StockTrades WHERE Symbol = '{0}'", symbol));
            Cn.ExecuteScalar(String.Format("DELETE FROM dbo.SimulationData WHERE Symbol = '{0}'", symbol));
        }

        public DataTable GetStockValues (string symbol)
        {            
            var sql = String.Format(@"
                SELECT 
                   [Date]
                  ,[Open]
                  ,[High]
                  ,[Low]
                  ,[Close]
                  ,[Adj Close]
                  ,[Volume]
              FROM [dbo].[{0}]
              ORDER BY [Date]
            ", symbol);

            return Cn.ExecuteQuery(sql);
        }

        public DataTable GetStockDefinitions()
        {
            var sql = @"
                SELECT [Symbol]
                  ,[Name]
                  ,[Active]
                  ,[Value]
                  ,[Volume]
                  ,[Algorithm]
                  ,[PeakAdjustment]
              FROM [dbo].[StockDefinitions]
              WHERE [Active] = 'TRUE'
            ";
            return Cn.ExecuteQuery(sql);
        }

        public double GetStockSmoother(string symbol, DateTime startDate, DateTime endDate)
        {
            var sql = String.Format(@"
                WITH
                 StockDetails AS
                 (
                 SELECT m2.[Date]
	                  , m2.[Close]
                      ,CAST(m2.[Close] AS FLOAT) - CAST(m.[Close] AS FLOAT) As Price_Change
                      ,CAST(m2.[Volume] AS FLOAT) - CAST(m.[Volume] AS FLOAT) As Volume_Change
                  FROM [dbo].{0} m
                  JOIN [dbo].{0} m2 ON DATEADD(day, 1, m.[Date]) = m2.[Date]
                  WHERE m.[Date] BETWEEN @StartDate AND @EndDate
                  )

                  SELECT Top 1 PERCENTILE_CONT(0.25) WITHIN GROUP (ORDER BY ABS(Price_Change)) OVER (PARTITION BY 1) AS Median
                  FROM StockDetails
            ", symbol);

            List<SqlParameter> parameters = new List<SqlParameter>();
            parameters.Add(new SqlParameter("StartDate", startDate));
            parameters.Add(new SqlParameter("EndDate", endDate));

            object smoother = Cn.ExecuteScalar(sql, parameters);

            if (smoother == DBNull.Value)
                return 0;
            else
                return Convert.ToDouble(smoother);
        }

        public DateTime GetLastTradeDataDate()
        {
            var sql = @"
                SELECT MAX([DateGenerated]) AS LatestDateGenerated
                FROM [dbo].[StockTrades]
            ";

            List<SqlParameter> parameters = new List<SqlParameter>();
            object maxValue = Cn.ExecuteScalar(sql, parameters);

            if (maxValue == DBNull.Value)
                return new DateTime();
            else
                return Convert.ToDateTime(maxValue);
        }

        public DateTime GetLastTradeDate()
        {
            var sql = @"
                SELECT MAX([Period]) AS LatestDateGenerated
                FROM [dbo].[SimulationData]
            ";

            List<SqlParameter> parameters = new List<SqlParameter>();
            object maxValue = Cn.ExecuteScalar(sql, parameters);

            if (maxValue == DBNull.Value)
                return new DateTime();
            else
                return Convert.ToDateTime(maxValue);
        }
    }
}

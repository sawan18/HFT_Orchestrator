using CCACDataAccessLayer;
using MyData.Csv;
using StockManager.Model;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StockManager.Data
{
    public class ImportExportCalls
    {
        private static RunSettings settings = RunSettings.Instance;
        public DalProcessor Cn { get; set; }
        public bool ImportCsv(string csvFilePath, string tableName, List<Column> columns = null)
        {
            settings.Log.LogMessage(String.Format("Importing {0} file", csvFilePath));
            Cn.ExecuteScalar(String.Format("DROP TABLE IF EXISTS dbo.[{0}]", tableName));

            var importExportSpec = new ImportExportSpec
            {
                TableSchema = "dbo",
                TableName = tableName,
                FilePath = csvFilePath,
                Delimiter = Delimiters.csv,
                Overwrite = true,
                IgnoreFirst = true
            };

            if (columns != null)
            {
                importExportSpec.Columns = columns;
            }

            var importCsv = new ProcessImport();

            importCsv.Spec = importExportSpec;
            importCsv.DalProcessor = Cn;

            if (importCsv.DalProcessor.TestConnection() && File.Exists(csvFilePath))
            {

                Cn.ExecuteScalar(String.Format("DROP TABLE IF EXISTS dbo.[{0}]", tableName));
                var status = importCsv.Csv2MSSql();
                if (!status)
                {
                    RunSettings.Instance.Log.LogError("Import failed: " + importCsv.ImportError);
                    return false;
                }


                //Check that the file has some content.
                var countImported = (int)importCsv.DalProcessor.ExecuteScalar(String.Format("SELECT COUNT(1) FROM dbo.[{0}]", tableName), new List<SqlParameter>());
                settings.Log.LogMessage(String.Format("Imported data for {0}", countImported));
            }

            return true;
        }

        public bool ExportSymbolSummary(string symbol, Algorithm algorithm)
        {
            var sql = String.Format(
                @"
                DROP TABLE IF EXISTS dbo.[{0}_Stock_Summary];
                DECLARE @Symbol AS VARCHAR(10) = '{0}';
                With YearData
                AS
                (
                    SELECT Symbol, YEAR(Period) As YearOfTrade, COUNT(1) As TradingDays, MIN(Period) As FirstDate, MAX(Period) As LastDate
                    FROM [dbo].[SimulationData] s
                    WHERE s.Symbol = @Symbol
                    GROUP BY Symbol, YEAR(Period)
                )
                

                SELECT yd.Symbol, yd.TradingDays, yd.FirstDate, sd.Value As BeginBalance, yd.LastDate, sd2.Value As EndBalance, sd2.Value - sd.Value AS Difference
                INTO dbo.[{0}_Stock_Summary]
                FROM YearData yd
                JOIN [dbo].[SimulationData] sd ON sd.Period = yd.FirstDate AND sd.Symbol = yd.Symbol
                JOIN [dbo].[SimulationData] sd2 ON sd2.Period = yd.LastDate AND sd2.Symbol = yd.Symbol

                ", symbol);
            Cn.ExecuteNonQuery(sql);

            var exportPath = String.Format("{0}/{1}/{2}_StockSummary_{3}.csv", RunSettings.Instance.MetaDataFolder, "Results", algorithm.ToString() + "_" + symbol.ToUpper(), DateTime.Now.ToString("yyyy-MM-dd_HH_mm_ss"));

            return ExportCsv(exportPath, string.Format("{0}_Stock_Summary", symbol));
        }

        public bool ExportCsv(string csvFilePath, string tableName)
        {
            settings.Log.LogMessage(String.Format("Exporting to {0} file", csvFilePath));
            var importExportSpec = new ImportExportSpec
            {                
                TableSchema = "dbo",
                TableName = tableName,
                FilePath = csvFilePath,
                Delimiter = Delimiters.csv,
                Overwrite = true,
                IgnoreFirst = true
            };
                       
            var exportCsv = new ProcessExport();

            exportCsv.Spec = importExportSpec;
            exportCsv.DalProcessor = Cn;

            if (exportCsv.DalProcessor.TestConnection())
            {

                var status = exportCsv.MSSqlToCSV();
                if (!status)
                {
                    RunSettings.Instance.Log.LogError("Export failed: " + exportCsv.ExportError);
                    return false;
                }
                RunSettings.Instance.Log.LogMessage("Export complete");
            }

            return true;
        }
        public bool ImportStockData(string symbol)
        {
            var importPath = String.Format("{0}\\{1}\\{2}.csv", RunSettings.Instance.MetaDataFolder, "Data", symbol);
            settings.Log.LogMessage("Importing config - " + importPath);

            return ImportCsv(importPath, symbol);
        }
        public bool ImportConfig()
        {
            var importPath = String.Format("{0}\\{1}\\Stocks.csv", RunSettings.Instance.MetaDataFolder, "Config");
            settings.Log.LogMessage("Importing stock config = " + importPath);
            var columns = new List<Column>();
            columns.Add(new Column("Symbol", SqlServerTypes.nvarchar));
            columns.Add(new Column("Name", SqlServerTypes.nvarchar));
            columns.Add(new Column("Active", SqlServerTypes.nvarchar));
            columns.Add(new Column("Value", SqlServerTypes.@float));
            columns.Add(new Column("Volume", SqlServerTypes.bigint));
            columns.Add(new Column("Algorithm", SqlServerTypes.varchar));
            columns.Add(new Column("InitialTradeQuantity", SqlServerTypes.bigint));
            columns.Add(new Column("PeakAdjustment", SqlServerTypes.bigint));

            return ImportCsv(importPath, "StockDefinitions", columns);
        }

        public bool ImportSettings()
        {
            var importPath = String.Format("{0}\\{1}\\Settings.csv", RunSettings.Instance.MetaDataFolder, "Config");
            settings.Log.LogMessage("Importing stock settings = " + importPath);
            var columns = new List<Column>();
            columns.Add(new Column("Key", SqlServerTypes.nvarchar));
            columns.Add(new Column("Value", SqlServerTypes.nvarchar));

            return ImportCsv(importPath, "AccountSettings", columns);
        }

    }
}

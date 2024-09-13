using CCACDataAccessLayer;
using MyData.Csv;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HsoImportExportTest
{
    [TestClass]
    public class ImportExportTest
    {
        private string exportPath = @"C:\etl\dev-stage\csv\export\test.csv";
        private string exportSpecPath = @"C:\etl\dev-stage\csv\export\test_spec.xml";

        private string importPath = @"C:\etl\dev-stage\csv\import\test.csv";
        private string importSpecPath = @"C:\etl\dev-stage\csv\import\test_spec.xml";
        private ImportExportSpec GetGenericImportSpec(string tablename, string path, Delimiters delimiter, bool overwrite)
        {
            var importExportSpec = new ImportExportSpec
            {
                TableName = tablename,
                TableSchema = "dbo",
                FilePath = path,
                Delimiter = delimiter,
                Overwrite = overwrite
            };
            return importExportSpec;
        }

        private ImportExportSpec GetGenericExportSpec(string tablename, string path, Delimiters delimiter, List<string> nullStrings)
        {
            var importExportSpec = new ImportExportSpec
            {
                TableName = tablename,
                TableSchema = "dbo",
                FilePath = path,
                Delimiter = delimiter,
                NullString = nullStrings
            };
            return importExportSpec;
        }

        private DalProcessor GetDbConnection()
        {
            var dalProcessor = new DalProcessor();

            dalProcessor.Server = "DESKTOP-T45AO6O";
            dalProcessor.Database = "GrassHopper";
            dalProcessor.SqlUser = "";
            dalProcessor.SqlPassword = "";

            return dalProcessor;
        }

        [TestMethod]
        public void TestExportSuccess()
        {
            var exportSpec = GetGenericExportSpec("SurveyorProfile", exportPath, Delimiters.csv, new List<String> { "", "-" });
            exportSpec.Shuffle = false;
            var exportCsv = new ProcessExport();
            
            exportCsv.Spec = exportSpec;
            exportCsv.DalProcessor = GetDbConnection();

            if (exportCsv.DalProcessor.TestConnection())
            {
                var status = exportCsv.MSSqlToCSV();
                if (!status)
                {
                    Console.WriteLine(exportCsv.ExportError);
                }

                Assert.AreEqual(true, status);

                //Check that the file has some content.
                var fileInfo = new FileInfo(exportPath);
                Assert.AreNotEqual(0, fileInfo.Length);


                //Capture the meta
                exportCsv.CaptureTableFields();
                String xml = exportCsv.Spec.ToString();
                File.WriteAllText(exportSpecPath, xml);

                fileInfo = new FileInfo(exportSpecPath);
                Assert.AreNotEqual(0, fileInfo.Length);
            }            
        }

        [TestMethod]
        public void TestImportSuccess()
        {

            var importSpecSaved = new ImportExportSpec();

            try
            {
                ImportExportSpec.LoadFromXml(ref importSpecSaved, File.ReadAllText(importSpecPath));
            }
            catch
            {
                importSpecSaved = new ImportExportSpec();
            }
            

            importSpecSaved.TableName = "SurveyorProfile_imported";
            importSpecSaved.FilePath = importPath;
            importSpecSaved.Overwrite = true;
            importSpecSaved.EncodingType = EncodingTypes.UTF8;
            var importCsv = new ProcessImport();

            importCsv.Spec = importSpecSaved;
            importCsv.DalProcessor = GetDbConnection();

            if (importCsv.DalProcessor.TestConnection() && File.Exists(importPath))
            {
                var status = importCsv.Csv2MSSql();
                if (!status)
                {
                    Console.WriteLine(importCsv.ImportError);
                }

                Assert.AreEqual(true, status);

                //Check that the file has some content.
                var countImported = (int) importCsv.DalProcessor.ExecuteScalar("SELECT COUNT(1) FROM dbo.SurveyorProfile_imported", new List<SqlParameter>());
                var countExisting = (int)importCsv.DalProcessor.ExecuteScalar("SELECT COUNT(1) FROM dbo.SurveyorProfile", new List<SqlParameter>());

                Assert.AreNotEqual(0, countImported);
                Assert.AreEqual(countImported, countExisting);
            }
        }
    }
}

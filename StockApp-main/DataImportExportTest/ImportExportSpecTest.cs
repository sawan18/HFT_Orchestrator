using System;
using System.Collections.Generic;
using MyData.Csv;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HsoImportExportTest
{
    [TestClass]
    public class ImportExportSpecTest
    {
        private ImportExportSpec GetGenericImportSpec(string tablename, string path, Delimiters delimiter, bool overwrite)
        {
            var importExportSpec = new ImportExportSpec
            {
                TableName = tablename,
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
                FilePath = path,
                Delimiter = delimiter,
                NullString = nullStrings
            };
            return importExportSpec;
        }

        [TestMethod]
        public void TestImportSpecXmlSerializeSuccess()
        {
            var importExportSpec = GetGenericImportSpec("testTable", @"c:\Test\FilePath", Delimiters.csv, true);
            var xml = importExportSpec.ToString();
            Assert.AreNotEqual(String.Empty, xml);
        }

        [TestMethod]
        public void TestExportSpecXmlSerializeSuccess()
        {
            var importExportSpec = GetGenericExportSpec("testTable", @"c:\Test\FilePath", Delimiters.csv, new List<String> { "", "-" });
            var xml = importExportSpec.ToString();
            Assert.AreNotEqual(String.Empty, xml);
        }

        [TestMethod]
        public void TestImportSpecXmlDeserializeSuccess()
        {
            var importExportSpec = GetGenericImportSpec("testTable", @"c:\Test\FilePath", Delimiters.csv, true);
            var xml = importExportSpec.ToString();

            var deserializedSpec = new ImportExportSpec();
            ImportExportSpec.LoadFromXml(ref deserializedSpec, xml);

            Assert.AreEqual(deserializedSpec.FilePath, importExportSpec.FilePath);
            Assert.AreEqual(deserializedSpec.TableName, importExportSpec.TableName);
            Assert.AreEqual(deserializedSpec.Delimiter, importExportSpec.Delimiter);
            Assert.AreEqual(deserializedSpec.Overwrite, importExportSpec.Overwrite);
        }

        [TestMethod]
        public void TestExportSpecXmlDeserializeSuccess()
        {
            var importExportSpec = GetGenericExportSpec("testTable", @"c:\Test\FilePath", Delimiters.csv, new List<String> { "", "-" });
            var xml = importExportSpec.ToString();

            var deserializedSpec = new ImportExportSpec();
            ImportExportSpec.LoadFromXml(ref deserializedSpec, xml);

            Assert.AreEqual(deserializedSpec.FilePath, importExportSpec.FilePath);
            Assert.AreEqual(deserializedSpec.TableName, importExportSpec.TableName);
            Assert.AreEqual(deserializedSpec.Delimiter, importExportSpec.Delimiter);
            Assert.AreEqual(deserializedSpec.NullString.Count, importExportSpec.NullString.Count);
        }
    }
}

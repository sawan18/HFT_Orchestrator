using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Xml;
using System.IO;

namespace MyData.Csv
{
    [DataContract]
    public class ImportExportSpec
    {
        private string filePath = "";
        [DataMember]
        public string FilePath
        {
            set { filePath = value; }
            get { return filePath; }
        }

        private string tableName = "";
        [DataMember]
        public string TableName
        {
            set { tableName = value; }
            get
            {

                if (tableName == "" && filePath != "")
                {
                    try
                    {
                        FileInfo f = new FileInfo(filePath);
                        tableName = f.Name;

                        if (tableName.ToLower().Contains(".csv"))
                        {
                            int i = tableName.ToLower().IndexOf(".csv");
                            tableName = tableName.Substring(0, i);
                        }

                        if (tableName.ToLower().Contains(".txt"))
                        {
                            int i = tableName.ToLower().IndexOf(".txt");
                            tableName = tableName.Substring(0, i);
                        }

                        f = null;
                    }
                    catch
                    {
                        tableName = "";
                    }

                }

                return tableName;

            }
        }

        private string schema = "dbo";
        [DataMember]
        public string TableSchema
        {
            get { return schema; }
            set { schema = value; }
        }

        private List<Column> columns = new List<Column>();
        [DataMember]
        public List<Column> Columns
        {
            set { columns = value; }
            get { return columns; }
        }

        private bool ignoreFirst = true;
        [DataMember]
        public bool IgnoreFirst
        {
            set { ignoreFirst = value; }
            get { return ignoreFirst; }
        }

        private bool overwrite = false;
        [DataMember]
        public bool Overwrite
        {
            set { overwrite = value; }
            get { return overwrite; }
        }

        private EncodingTypes encodingType = EncodingTypes.Default;
        /// <summary>
        /// Set Encoding types for the CSV/TAB delimited file
        /// </summary>
        [DataMember]
        public EncodingTypes EncodingType
        {
            set
            {
                try
                {
                    encodingType = (EncodingTypes)value;
                }
                catch
                {
                    encodingType = EncodingTypes.Default;
                }

            }

            get
            {
                return encodingType;
            }

        }

        private Delimiters documentDelimiter = Delimiters.csv;
        /// <summary>
        /// Set/Get Delimiter for the document
        /// </summary>
        [DataMember]
        public Delimiters Delimiter
        {
            set
            {
                try
                {
                    documentDelimiter = (Delimiters)value;
                }
                catch
                {
                    documentDelimiter = Delimiters.csv;
                }
            }

            get
            {
                return documentDelimiter;
            }
        }

        private List<string> nullStrings = new List<string>();
        /// <summary>
        /// Add string as the list of string to be imported as nulls
        /// Defaulted to empty strings
        /// </summary>
        /// <param name="nullString"></param>
        [DataMember]
        public List<string> NullString
        {
            set
            {
                nullStrings = value;
            }

            get { return nullStrings; }

        }

        private bool includeColumnHeaders = true;
        [DataMember]
        public bool IncludeColumnHeaders
        {
            set { includeColumnHeaders = value; }
            get { return includeColumnHeaders; }
        }

        private bool shuffle = false;
        [DataMember]
        public bool Shuffle
        {
            set { shuffle = value; }
            get { return shuffle; }
        }

        //There are serialization and deserialization
        public static void LoadFromXml(ref ImportExportSpec obj, string xml)
        {
            DataContractSerializer dcs = new DataContractSerializer(typeof(ImportExportSpec));
            StringBuilder sb = new StringBuilder();

            using (XmlTextReader reader = new XmlTextReader(new StringReader(xml)))
            {
                obj = (ImportExportSpec)dcs.ReadObject(reader, true);
            }
        }

        public override string ToString()
        {
            DataContractSerializer dcs = new DataContractSerializer(typeof(ImportExportSpec));
            StringBuilder sb = new StringBuilder();

            using (XmlWriter writer = XmlWriter.Create(sb))
            {
                dcs.WriteObject(writer, this);
            }

            return sb.ToString();
        }

        public char GetDelimiter()
        {
            switch (this.Delimiter)
            {
                case Delimiters.csv:
                    return ',';
                case Delimiters.tab:
                    return '\t';
                case Delimiters.colon:
                    return ':';
                case Delimiters.semiColon:
                    return ';';
                case Delimiters.verticalBar:
                    return '|';
                default:
                    return ',';
            }
        }
    }
}
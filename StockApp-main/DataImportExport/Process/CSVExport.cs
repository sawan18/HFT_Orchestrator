using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.Common;
using System.IO;
using CCACDataAccessLayer;
using MyData.Data;
using System.ComponentModel;
using System.Data.SqlClient;

namespace MyData.Csv
{
    public class ProcessExport
    {
        #region private

        private string error = "";
        private DalProcessor dalProcessor;

        #endregion

        #region properties

        private bool showLogProgress = true;

        /// <summary>
        /// Gets and sets flag for showing the log progress.
        /// </summary>
        public bool ShowLogProgress
        {
            get { return showLogProgress; }
            set { showLogProgress = value; }
        }

        /// <summary>
        /// Set the database executor
        /// </summary>
        public DalProcessor DalProcessor
        {
            set
            {
                dalProcessor = value.GetCopy();
            }
            get { return dalProcessor; }
        }

        public bool IncludeColumnHeaders
        {
            set { spec.IncludeColumnHeaders = value; }
            get { return spec.IncludeColumnHeaders; }
        }

        public bool Shuffle
        {
            set { spec.Shuffle = value; }
            get { return spec.Shuffle; }
        }
        /// <summary>
        /// Set/Get Delimiter for the document
        /// </summary>
        public object Delimiter
        {
            set
            {
                try
                {
                    spec.Delimiter = (Delimiters)value;
                }
                catch
                {
                    spec.Delimiter = Delimiters.csv;
                }
            }

            get
            {
                return spec.Delimiter;
            }
        }



        /// <summary>
        /// String for representing NULL characters
        /// </summary>
        /// <param name="nullString"></param>
        public string RepresentNullAs
        {
            set
            {
                if (!spec.NullString.Contains(value))
                    spec.NullString.Add(value);
            }
            get { return spec.NullString[0]; }
        }

        private ImportExportSpec spec = new ImportExportSpec();

        /// <summary>
        /// Complete Import Specification
        /// </summary>
        public ImportExportSpec Spec
        {
            set { spec = value; }
            get { return spec; }
        }

        /// <summary>
        /// Import table to export 
        /// </summary>
        public string Table
        {
            set
            {
                spec.TableName = value.Trim();
            }
            get
            {
                return spec.TableName;
            }
        }

        public string Path
        {
            set { spec.FilePath = value; }
            get { return spec.FilePath; }
        }

        private string exportSpec = "";
        /// <summary>
        /// GET/SET Import Specification XML string
        /// </summary>
        public string ExportSpecXML
        {
            set
            {
                exportSpec = value;

                //load the specification
                try
                {
                    ImportExportSpec.LoadFromXml(ref spec, value);
                }
                catch { }

            }
            get
            {
                return spec.ToString();
            }
        }

        /// <summary>
        /// Add columns
        /// </summary>
        /// <param name="name">name of the column</param>
        /// <param name="type">data type</param>
        /// <param name="format">date format only</param>
        /// <param name="selected">Mark the column as selected</param>
        public void AddColumn(String name)
        {

            Column col = new Column(name);
            spec.Columns.Add(col);
        }

        /// <summary>
        /// Add column and alias 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="alias"></param>
        public void AddColumnAlias(string name, string alias)
        {
            bool match = false;

            foreach (Column col in spec.Columns)
            {
                if (col.Name.Trim().ToLower() == name.Trim().ToLower())
                {
                    col.Alias = alias;
                    match = true;
                    break;
                }
            }

            if (!match)
            {
                Column col = new Column(name);
                col.Alias = alias;

                spec.Columns.Add(col);
            }
        }

        /// <summary>
        /// Error messages
        /// </summary>
        public string ExportError
        {
            get
            {
                return this.error;
            }
        }       

        private long totalRows = -1;
        public long TotalRows
        {
            get
            {
                return totalRows;
            }
        }

        private long rowsImported = 0;
        public long RowsImported
        {
            set { rowsImported = value; }
            get { return rowsImported; }
        }

        private int progress = 0;
        public int Progress
        {
            set { progress = value; }
            get { return progress; }
        }


        private List<object> importLog = new List<object>();
        public List<object> ImportLog
        {
            set { importLog = value; }
            get { return importLog; }
        }

        private int MaxUploadSize
        {
            set;
            get;
        }

        #endregion

        #region public modules
        public ProcessExport() { }

        /// <summary>
        /// Initialize import internal variables
        /// </summary>
        public void InitializeExport()
        {
            spec = new ImportExportSpec();
            error = "";
            spec.Shuffle = true;
            spec.IncludeColumnHeaders = true;
            spec.Delimiter = Delimiters.csv;
            spec.NullString.Add("");
        }

        public bool MSSqlToCSV(string path)
        {
            return MSSqlToCSV(this.Table, "dbo", path);
        }

        public bool MSSqlToCSV(string tablename, string schema, string path)
        {
            return MSSqlToCSV(tablename, schema, path, spec.IncludeColumnHeaders, spec.Shuffle);
        }

        public bool MSSqlToCSV(string tablename, string schema, string path, bool includeHeaders, bool shuffleResult)
        {
            if (path != "")
            {
                try
                {                      
                    return WriteToCSV(spec.TableName, spec.TableSchema, spec.FilePath, includeHeaders, shuffleResult);
                }
                catch (Exception ex)
                {
                    this.error = "Unknown error occurred during export: " + ex.Message;
                    return false;
                }

            }
            else
            {
                this.error = "Invalid path";
                return false;
            }
        }

        /// <summary>
        /// Run export from a saved process
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public bool MSSqlToCSV()
        {           
            return MSSqlToCSV(spec.TableName, spec.TableSchema, spec.FilePath, spec.IncludeColumnHeaders, spec.Shuffle);            
        }

        /// <summary>
        /// Capture Table fields for the specifications
        /// </summary>
        public void CaptureTableFields()
        {
            var fields = DalProcessor.GetTableColumns(new Table(spec.TableName, spec.TableSchema));
            spec.Columns = new List<Column>();
            foreach(var field in fields)
            {
                var column = new Column();
                column.Name = field.Name;
                column.DataType = field.SqlServerType;
                column.Size = field.Size;
                column.Precision = field.Precision;
                column.Scale = field.Scale;
                spec.Columns.Add(column);
            }
        }
        #endregion

        #region private modules
        /// <summary>
        /// Write CSV to MSSQL
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="path"></param>
        /// <param name="addColumnNames"></param>
        /// <returns></returns>
        private bool WriteQueryToCSV(string sql, string path, bool addColumnNames)
        {
            this.error = null;
            char charDelimiter = spec.GetDelimiter();
            long rowCounter = 0;
            Int64 rowIndex = 1;
            int noOfBatches = 1;
            int maxRowsPerBatchSize = 20000;

            //write the file to csv
            using (SqlConnection con = new SqlConnection(dalProcessor.ConnectionString))
            {
                try
                {
                    con.Open();

                    using (SqlCommand cmd = con.CreateCommand())
                    {
                        //Initialize the command object 
                        //Set it to have a long timeout
                        cmd.Connection = con;
                        cmd.CommandText = sql;
                        cmd.CommandTimeout = 0;

                        try
                        {
                            using (IDataReader reader = cmd.ExecuteReader())
                            {
                                int length = -1, i;
                                length = reader.FieldCount;
                                using (TextWriter tw = new StreamWriter(path, false))
                                {
                                    if (addColumnNames)
                                    {
                                        if (spec.Columns.Count == 0)
                                        {
                                            for (i = 0; i < length; i++)
                                            {
                                                tw.Write(String.Format(i > 0 ? charDelimiter + "{0}" : "{0}", reader.GetName(i)));
                                            }
                                        }
                                        else
                                        {
                                            for (i = 0; i < length; i++)
                                            {
                                                tw.Write(String.Format(i > 0 ? charDelimiter + "{0}" : "{0}", spec.Columns[i].Alias != "" ? spec.Columns[i].Alias : spec.Columns[i].Name));
                                            }
                                        }

                                        tw.Write("\r\n");
                                    }

                                    while (reader.Read())
                                    {
                                        for (i = 0; i < length; i++)
                                        {
                                            /* TODO: May want to 'sanitize' (e.g. replace/remove [,"';] etc.) reader[i] */
                                            Type type = reader.GetFieldType(i);

                                            if (type == typeof(double) || type == typeof(float))
                                            {

                                                try
                                                {
                                                    tw.Write(
                                                    String.Format(i > 0 ? "{1}{0:###########.##########}" : "{0:###########.##########}"
                                                    , reader.IsDBNull(i) ? "" : reader.GetDouble(i) == 0.0 ? "0.0" : reader[i], charDelimiter));
                                                }
                                                catch
                                                {
                                                    string valueText = reader[i].ToString();
                                                    double valueDouble = 0.0;

                                                    double.TryParse(valueText, out valueDouble);

                                                    tw.Write(
                                                    String.Format(i > 0 ? "{1}{0:###########.##########}" : "{0:###########.##########}"
                                                    , reader.IsDBNull(i) ? "" : valueDouble == 0.0 ? "0.0" : valueText, charDelimiter));
                                                }
                                            }
                                            else
                                            {
                                                StringBuilder builder = new StringBuilder();
                                                string value = "";
                                                string nullString = "";

                                                if (spec.NullString != null && spec.NullString.Count > 0)
                                                {
                                                    nullString = spec.NullString[0];
                                                }

                                                value = (reader.IsDBNull(i) || reader[i].ToString().Trim() == "") ? nullString : reader[i].ToString();

                                                if (i > 0)
                                                    builder.Append(charDelimiter);

                                                if (value.IndexOfAny(new char[] {
                                                            '"'
                                                            , ','
                                                            , ';'
                                                            , ':'
                                                            , '|'
                                                            ,'\''
                                                            , '\t'
                                                            , '\n'
                                                            , '\r'
                                                            }) != -1)
                                                {
                                                    // Special handling for values that contain comma or quote
                                                    // Enclose in quotes and double up any double quotes
                                                    builder.AppendFormat("\"{0}\"", value.Replace("\"", "\"\""));
                                                }
                                                else builder.Append(value);

                                                tw.Write(builder.ToString());

                                            }
                                        }
                                        tw.Write("\r\n");

                                        //Increase the row counter
                                        rowCounter += 1;
                                        rowIndex += 1;

                                        if (rowCounter == maxRowsPerBatchSize)
                                        {
                                            this.rowsImported = rowIndex - 1;



                                            rowCounter = 0; //Reset counter
                                            noOfBatches += 1; //Increase the number of batches exported
                                                                                      
                                        }
                                    }

                                    this.rowsImported = rowIndex - 1;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            this.error = "An unknown error occurred exporting data : " + ex.Message;

                            return false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    this.error = "Error initializing connection : " + ex.Message;
                    

                    return false;
                }
            }

            return true;
        }


        /// <summary>
        /// Write to CSV from MSSQL
        /// </summary>
        /// <param name="table"></param>
        /// <param name="path"></param>
        /// <param name="addColumnNames"></param>
        /// <param name="shuffle"></param>
        /// <returns></returns>
        private bool WriteToCSV(string table, string schema, string path, bool addColumnNames, bool shuffle)
        {            
            string[] columns;
            string sql = "";

            //Determine the number of rows to export
            sql = String.Format(
                    @"
                        SELECT COUNT(*) AS TotalRows 
                        FROM {0}.{1}
                    "
                , Utilities.Escape(spec.TableSchema)
                , Utilities.Escape(spec.TableName));

            try
            {
                totalRows = Convert.ToInt32(dalProcessor.ExecuteScalar(sql));
            }
            catch (Exception ex)
            {
                this.error = "Export failed: " + ex.Message;

                return false;
            }


            if (spec.Columns.Count > 0)
            {
                columns = new string[spec.Columns.Count];

                for (int i = 0; i < spec.Columns.Count; i++)
                {
                    columns[i] = Utilities.Escape(spec.Columns[i].Name);
                }

                sql = String.Format("SELECT {0} FROM {1}.{2} {3}"
                   , String.Join(",", columns)
                   , Utilities.Escape(spec.TableSchema)
                   , Utilities.Escape(spec.TableName)
                   , shuffle ? " order by NewID()" : "");
            }
            else
            {
                sql = String.Format("SELECT * FROM {0}.{1} {2}"
                    , Utilities.Escape(spec.TableSchema)
                   , Utilities.Escape(spec.TableName)
                   , shuffle ? " order by NewID()" : "");
            }

            return WriteQueryToCSV(sql, path, addColumnNames);
        }        
        
        #endregion
    }
}
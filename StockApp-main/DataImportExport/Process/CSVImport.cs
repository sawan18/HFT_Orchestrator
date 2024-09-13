/**
 * Database operations. CSV Import
 * Built to support all the features of CSV Import developed by Sadrul Habib
 * 
 * Provides a very flexible CSV import functionality. It uses the fast
    LumenWorks.Framework.IO.CSV.CsvReader to Parse the CSV/TAB delimited file
 *  
 * Main module for import is Csv2MSSql
 *      Provide the path to the CSV and the columns will automatically be
 *      detected and imported into the database using the proper data types.
 *      
 * Example
 *      import.InitializeImport();
        status = import.Csv2MSSql(@"C:\Codes\verybig_longitudinal.csv");
        
 *      Always check for warnings afterwards even when there are no errors
 *      
 *      Optionally you can add column attributes. Whatever is missing (datetype, format) is detected automatically
 *              
 * Example 
 *              import.InitializeImport();

                import.AddColumn("id1");
                import.AddColumn("mydob", "date");
                import.AddColumn("mydod", "date");
                import.AddColumn("id4");
                import.AddColumn("id5");

                status = import.Csv2MSSql(@"C:\Codes\generalize3.csv");
 
 * This also support for CSV comments, tab delimited files, various encoding formats, and column headers
 * You can also import and export settings as XML
 * 
 Properties

CSVImport Settings
 *  DestinationTable    - The target destination table, the CSV filename is used if this is not provided
 *  IgnoreFirst         - When set to TRUE, execution will ignore the first non-commented row and this as headers
 *  Overwrite           - When set to TRUE, execution will overwrite the destination dataset/table or return an error
 *  IgnoreWarning       - When set to TRUE, execution will ignore all validation warnings
 *  EncodingTypes       - SET/GETS Encoding type for the CSV file. Use the enum EncodingTypes in .NET or numbers 0-6
 *                        such that (Default=0, ASCII=1, BigEndianUnicode=2, Unicode=3, UTF32=4, UTF7=5,UTF8=6 )
 *  DocumentDelimiter   - SET/GETS the delimiter. Default is Delimiters.CSV or 0. Tab is rep as Delimiters.TAB or 1
 *  AddNullString       - Add a string representation for null in your document. Default is ''
 *  ImportSpec          - The object for the import specifiication
 *  ImportSpecXML       - XML Version of the specification above
 *  Error               - Import Execution Errors
 *  Warning             - Import Execution Warning

Authentication password
 * Public functions
 *  InitializeImport    - Reset internal variables
 *  AddColumn           - Add each column name, and optionally data type and date format
 *  GetColumnHeaders    - Get the list of column headers read from a CSV file
 *  PredictCSVColumnsAndTypes - Predict the column names and types of a CSV document
 *  Csv2MSSql           - Main Import module (Please specific database connection settings before making this call)
 
Database Settings
 *  ConnectionString    - SET/GET full connection for the database. With this set, you have to ensure that the right provide 
 *                          setting is in use
 *  Provider            - SET/GET the provider: 0=OLEDB, 1=SQL, 2=ODBC
 *  ServerName          - The name of the SQL Server installation BEZE_MOBILE\SQLServer
 *  UseWindowsAuthentication-If SET connection will use windows authentication
 *  DatabaseName        - The name of the database to default to
 *  Username            - SQL Authentication username
 *  Password            - SQL Authentication password

 * Finally, this can be used on its own without setting the database properties.
 * However, the database settings are set, you are able to process the CSV file 
 * and push data to the database table specified.
 * 
 * @author: Ben Eze (Dec. 2019)
 * 
 * */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using System.Data.OleDb;
using System.Collections;
using System.IO;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Data.SqlClient;
using CCACDataAccessLayer;
using System.Web.Script.Serialization;
using MyData.Data;
using System.ComponentModel;

namespace MyData.Csv
{
    /// <summary>
    /// Processes a CSV or TAB delimited file. 
    /// Optionally set the database settings to upload file to a remote database
    /// </summary>
    public class ProcessImport
    {

        #region private
        private const int constMaxUpload = 20000;
        private string error = "";
        private string warning = "";
        private string importPath = "";
        private String[] formats;
        private long maxUploadSize = constMaxUpload;
        private DalProcessor dalProcessor = new DalProcessor();
        private JavaScriptSerializer serializer =
                            new System.Web.Script.Serialization.JavaScriptSerializer();
        
        private List<Column> selectedColumns = new List<Column>();
        private List<Type> columnTypes = new List<Type>();
        
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
        /// Set the database processor
        /// </summary>
        public DalProcessor DalProcessor
        {
            set
            {
                dalProcessor = value.GetCopy();
            }
            get { return dalProcessor; }
        }

        /// <summary>
        /// Ignore the first line after CSV comments as headers
        /// </summary>
        public bool IgnoreFirst
        {
            set
            {
                spec.IgnoreFirst = value;
            }

            get
            {
                return spec.IgnoreFirst;
            }
        }


        /// <summary>
        /// Set to overwrite an existing table or return an error
        /// </summary>
        public bool Overwrite
        {
            set
            {
                spec.Overwrite = value;
            }

            get
            {
                return spec.Overwrite;
            }
        }

        private bool ignoreWarnings = true;

        /// <summary>
        /// Set to Ignore warning messages during validation
        /// </summary>
        public bool IgnoreWarnings
        {
            set
            {
                this.ignoreWarnings = value;
            }

            get
            {
                return this.ignoreWarnings;
            }
        }


        /// <summary>
        /// Set Encoding types for the CSV/TAB delimited file
        /// </summary>
        public object EncodingType
        {
            set
            {
                spec.EncodingType = (EncodingTypes)value;

            }

            get
            {
                return spec.EncodingType;
            }

        }


        /// <summary>
        /// Set/Get Delimiter for the document
        /// </summary>
        public object Delimiter
        {
            set
            {
                spec.Delimiter = (Delimiters)value;
            }

            get
            {
                return spec.Delimiter;
            }
        }


        /// <summary>
        /// Add string as the list of string to be imported as nulls
        /// Defaulted to empty strings
        /// </summary>
        /// <param name="nullString"></param>
        public void AddNullString(string nullString)
        {
            if (!spec.NullString.Contains(nullString))
                spec.NullString.Add(nullString);
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

        public string Path
        {
            set { spec.FilePath = value; importPath = value; }
            get { return spec.FilePath; }
        }

        /// <summary>
        /// Import destination table 
        /// </summary>
        public string DestinationTable
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


        private string importSpec = "";
        /// <summary>
        /// GET/SET Import Specification XML string
        /// </summary>
        public string ImportSpecXML
        {
            set
            {
                importSpec = value;

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
        /// GET Import Specification in JSON format
        /// </summary>
        public string ImportSpecJSON
        {
            get
            {
                return serializer.Serialize(spec);
            }

            set
            {
                try
                {
                    spec = serializer.Deserialize<ImportExportSpec>(value);
                }
                catch { };
            }
        }

        /// <summary>
        /// Error messages
        /// </summary>
        public string ImportError
        {
            get
            {
                return this.error;
            }
        }

        /// <summary>
        /// Validation warning
        /// </summary>
        public string Warning
        {
            get
            {
                return this.warning;
            }
        }

        private List<object[]> previewData = new List<object[]>();
        public List<object[]> PreviewData
        {
            get
            {
                return previewData;
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

        public BackgroundWorker Worker
        {
            set;
            get;
        }

        private int MaxUploadSize
        {
            set;
            get;
        }

        #endregion

        #region public

        public ProcessImport()
        {
            spec.NullString.Add("");  //Empty space is added by default          
        }

        /// <summary>
        /// Initialize import internal variables
        /// </summary>
        public void InitializeImport()
        {
            spec = new ImportExportSpec();
            formats = null;

            this.importSpec = "";
            this.error = "";
            this.warning = "";
            this.importPath = "";
            this.previewData = new List<object[]>();
            this.totalRows = 0;
            this.progress = 0;
            this.importLog.Clear();
        }

        /// <summary>
        /// Add columns
        /// </summary>
        /// <param name="name">name of the column</param>
        public void AddColumn(string name)
        {
            this.AddColumn(name, "");
        }

        /// <summary>
        /// Add columns
        /// </summary>
        /// <param name="name">name of the column</param>
        /// <param name="type">data type</param>
        public void AddColumn(String name, String type)
        {
            this.AddColumn(name, type, "");
        }

        /// <summary>
        /// Add columns
        /// </summary>
        /// <param name="name">name of the column</param>
        /// <param name="type">data type</param>
        /// <param name="format">date format only</param>
        public void AddColumn(string name, string type, String format)
        {
            AddColumn(name, type, format, true);
        }

        /// <summary>
        /// Add columns
        /// </summary>
        /// <param name="name">name of the column</param>
        /// <param name="type">data type</param>
        /// <param name="format">date format only</param>
        /// <param name="selected">Mark the column as selected</param>
        public void AddColumn(String name, String type, String format, bool selected)
        {            
            Column col = new Column(name, ImportUtilities.GetSqlDataType(type), format);

            if (String.IsNullOrEmpty(type))
            {
                col.DetectType = true;
            }

            col.Selected = selected;

            spec.Columns.Add(col);
        }

        /// <summary>
        /// Add A Column with Format Code
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <param name="formatCode"></param>
        /// <param name="selected"></param>
        public void AddColumnWithFormatCode(string name, string type, int formatCode, bool selected)
        {
            if (formatCode == -1)
            {
                this.AddColumn(name, type, "", selected);
            }
            else
            {
                string[] formats = (from n in this.GetListOfFomats()
                                    where n.FormatCode == formatCode
                                    select n.Format).ToArray();

                if (formats.Length > 0)
                {
                    this.AddColumn(name, type, formats[0], selected);
                }
                else
                {
                    this.AddColumn(name, type, "", selected);
                }
            }
        }

        public string GetColumnHeadersJSON()
        {
            //return Utilities.ConvertStringArrayToJason(GetColumnHeaders());

            return serializer.Serialize(GetColumnHeaders());
        }

        public string GetColumnHeadersJSON(string path)
        {
            //return Utilities.ConvertStringArrayToJason(GetColumnHeaders(path));


            return serializer.Serialize(GetColumnHeaders(path));
        }

        /// <summary>
        /// Return the list of column header names
        /// </summary>
        /// <returns></returns>
        public string[] GetColumnHeaders()
        {
            if (importPath == "")
            {
                this.error = "Please specify an import path";
                return null;
            }

            return GetColumnHeaders(importPath);
        }

        /// <summary>
        /// Return the list of column header names
        /// </summary>
        /// <param name="filepath">path to the csv file</param>
        /// <returns>string array</returns>
        public string[] GetColumnHeaders(string filepath)
        {
            //CSV reader                
            using (CsvReader reader = GetReader(filepath))
            {
                bool status = false;

                if (reader == null)
                {
                    String[] x = { "false" };
                    return x;
                }

                String[] columnNames;

                if (spec.IgnoreFirst)
                {
                    try
                    {
                        columnNames = ((string[])reader.GetFieldHeaders());

                        reader.Dispose();
                        return columnNames;
                    }
                    catch
                    {
                        spec.IgnoreFirst = false;
                    }
                }

                //Create default headers if there are no headers
                if (!spec.IgnoreFirst)
                {
                    try
                    {
                        status = reader.ReadNextRecord();
                    }
                    catch (Exception ex)
                    {
                        error = "Error reading file :" + ex.Message;
                    }

                    if (!status)
                    {
                        this.error = "Unable to read the file ";
                        reader.Dispose();
                        return null;
                    }

                    try
                    {
                        int index = 0;

                        do
                        {
                            string dummy = reader[index];
                            spec.Columns.Add(new Column(String.Format("Col{0}", (index + 1))));
                            index += 1;
                        } while (index > 0);


                    }
                    catch { }

                    columnNames = new string[spec.Columns.Count];

                    for (int i = 0; i < columnNames.Length; i++)
                    {
                        columnNames[i] = spec.Columns[i].Name;
                    }

                    return columnNames;
                }

                return null;
            }

        }


        /// <summary>
        /// Predict the column names and attributes of each column
        /// Uses the import file path
        /// </summary>
        /// <returns></returns>
        public bool PredictCSVColumnsAndTypes()
        {
            if (importPath == "")
            {
                this.error = "Please specify an import path";
                return false;
            }

            return PredictCSVColumnsAndTypes(importPath);
        }


        /// <summary>
        /// Predict the column names and attributes of each column
        /// Uses the import file path
        /// </summary>
        /// <param name="filepath">File Path</param>
        /// <returns></returns>
        public bool PredictCSVColumnsAndTypes(string filepath)
        {
            bool detectTypes = false;
            bool status = false;

            //get total rows

            FileInfo f = new FileInfo(filepath);
            if (f.Length < 100000000)
                totalRows = File.ReadAllLines(filepath).Count();


            //Change the import reporting frequency
            if (totalRows > 0 && totalRows <= constMaxUpload)
                maxUploadSize = totalRows / 10;
            else if (totalRows > 0 && totalRows <= constMaxUpload)
                maxUploadSize = constMaxUpload / 2;
            else
                maxUploadSize = constMaxUpload;



            using (CsvReader reader = GetReader(filepath))
            {
                spec.FilePath = filepath;

                if (reader == null)
                    return false;

                if (spec.Columns.Count == 0)
                {
                    //get the column headers
                    String[] columnNames;

                    if (spec.IgnoreFirst)
                    {
                        try
                        {
                            columnNames = ((string[])reader.GetFieldHeaders());

                            if (columnNames.Length > 0)
                            {
                                foreach (string header in columnNames)
                                {
                                    spec.Columns.Add(new Column(header));
                                }
                            }

                            totalRows -= 1;
                        }
                        catch
                        {
                            spec.IgnoreFirst = false;
                        }
                    }


                    //name the columns, col 1 - n
                    if (!spec.IgnoreFirst)
                    {
                        try
                        {
                            status = reader.ReadNextRecord();
                        }
                        catch (Exception ex)
                        {
                            error = "Error reading file :" + ex.Message;
                        }

                        if (!status)
                        {
                            this.error = "Unable to read the file ";
                            return false;
                        }

                        try
                        {
                            int index = 0;

                            do
                            {
                                string dummy = reader[index];
                                spec.Columns.Add(new Column(String.Format("Col{0}", (index + 1))));
                                index += 1;
                            } while (index > 0);

                        }
                        catch { }
                    }
                }
                else
                {
                    if (spec.IgnoreFirst)
                    {
                        try
                        {
                            reader.GetFieldHeaders();

                        }
                        catch
                        {
                            spec.IgnoreFirst = false;
                        }
                    }

                }



                foreach (Column col in spec.Columns)
                {
                    if (col.Selected)
                    {
                        if (col.DetectType)
                        {
                            detectTypes = true;
                            break;
                        }
                    }

                }

                //Kill the reader and restart things                    
                reader.Dispose();
            }

            using (CsvReader reader = GetReader(filepath))
            {
                if (spec.IgnoreFirst)
                {
                    try
                    {
                        reader.GetFieldHeaders();

                    }
                    catch
                    {
                        spec.IgnoreFirst = false;
                    }
                }

                if (detectTypes)
                {
                    //loop through the values and detect types for those without datatypes
                    for (int i = 0; i < 500; i++)
                    {
                        try
                        {
                            status = reader.ReadNextRecord();
                        }
                        catch (Exception ex)
                        {
                            error = "Error reading file :" + ex.Message;
                        }

                        if (!status)
                            break;

                        //initialize data import
                        object[] row = new object[spec.Columns.Count];

                        for (int j = 0; j < spec.Columns.Count; j++)
                        {
                            if (spec.Columns[j].Selected && spec.Columns[j].DetectType)
                            {
                                string val = reader[j];
                                row[j] = val;

                                string format = "";
                                //Run predictions on only non empty strings
                                if (val.Trim() != "" && !spec.NullString.Contains(val))
                                {
                                    string datatype = PredictDataType(val, ref format);

                                    if (spec.Columns[j].PredictedValues == null)
                                    {
                                        spec.Columns[j].PredictedValues = new List<PredictValue>();
                                    }
                                    else
                                    {
                                        List<PredictValue> matches = (from n in spec.Columns[j].PredictedValues
                                                                      where n.DataType == datatype && n.Format == format
                                                                      select n).ToList();
                                        if (matches.Count == 0)
                                        {
                                            spec.Columns[j].PredictedValues.Add(new PredictValue(datatype, format, 1));
                                        }
                                        else
                                        {
                                            matches[0].Occurrence += 1;
                                        }
                                    }

                                }
                            }
                        }

                        //Add to the list of objects
                        previewData.Add(row);
                    }

                    //close the reader
                    reader.Dispose();

                    //Now sort each of these predicted values by the descending order of the occurrence
                    foreach (Column col in spec.Columns)
                    {
                        if (col.PredictedValues == null)
                            col.PredictedValues = new List<PredictValue>();

                        if (col.Selected && col.DetectType)
                        {
                            //Sort by the highest precendence                    
                            List<PredictValue> sortedValues
                               = (from n in col.PredictedValues
                                  where n.Occurrence > 0
                                  orderby n.Occurrence descending
                                  select n
                                   ).ToList();

                            if (sortedValues.Count > 0)
                            {

                                if (sortedValues.Count > 1
                                    && (sortedValues[0].DataType == "int" || sortedValues[0].DataType == "bigInt") //Always choose real over int or big int
                                    && (sortedValues[1].DataType == "real"))
                                {
                                    col.DataType = SqlServerTypes.@float;
                                }
                                else
                                {
                                    //set the first to be the predicted column
                                    col.DataType = ImportUtilities.GetSqlDataType(sortedValues[0].DataType);
                                    col.DateFormat = sortedValues[0].Format;
                                }
                            }
                            else
                            {
                                col.DataType = SqlServerTypes.varchar;
                                col.DateFormat = "";
                            }
                        }
                    }
                }

                return true;
            }


        }

        public bool Csv2MSSql()
        {     
            return Csv2MSSql(spec.FilePath);
        }

        /// <summary>
        /// Main module for CSV import
        /// </summary>
        /// <param name="path">file path</param>
        /// <returns></returns>
        public bool Csv2MSSql(string path)
        {
            return Csv2MSSql(path, "");
        }

        /// <summary>
        /// Main module for CSV import
        /// </summary>
        /// <param name="path">file path</param>
        /// <param name="table">destination table</param>
        /// <param name="schema">destination table schema</param>
        /// <returns></returns>
        public bool Csv2MSSql(String path, String table, string schema="dbo")
        {
            spec.FilePath = path;

            if (table != "")
                spec.TableName = table;

            if (!spec.Overwrite)
            {
                //Check if this table exists and return an error
                string sql = String.Format("SELECT COUNT(*) As NoRows FROM {0}"
                    , Utilities.Escape(spec.TableName));

                int no = (int)dalProcessor.ExecuteScalar(sql);

                if (dalProcessor.Error == "")
                {
                    this.error = "Destination table already exist. Change the table name or choose to overwrite its contents";
                    return false;
                }
            }

            table = Utilities.UnEscape(spec.TableName);

            string tempTable = table + "_tmp";

            try
            {
                //Drop temp if it already exist
                dalProcessor.DropTable(new Table(tempTable, schema));
            }
            catch (Exception ex)
            {
                this.error = "Error initializing table " + ex.Message;
                return false;
            }

            bool importStatus = false;
            try
            {
                //Perform the actual import of data from the CSV file
                importStatus = ProcessCSVImport(path, tempTable, schema);
            }
            catch (Exception ex)
            {
                importStatus = false;
                this.error = "Unknown error occurred importing data: " + ex.Message;

                this.LogData(95, this.error);
                this.CompleteLogging();
            }

            //Check for the status
            if (!importStatus)
            {
                try
                {
                    //drop temp table if it exists                       
                    dalProcessor.DropTable(new Table(tempTable, schema));
                    return false;
                }
                catch (Exception ex)
                {
                    this.error += "Error dropping import temp table  :" + ex.Message; //add this error to other errors

                    return false;
                }
            }
            else
            {                
                try
                {
                    var destinationTable = new Table(this.DestinationTable, schema);
                    //drop the original table
                    dalProcessor.DropTable(destinationTable);

                    //rename the temp to the original
                    dalProcessor.RenameTable(new Table(tempTable, schema), spec.TableName);

                }
                catch (Exception ex)
                {
                    //is there anything that should be done?
                    this.error += "Error creating table trigger:" + ex.Message; //add this error to other errors

                    this.LogData(95, this.error);
                    this.CompleteLogging();

                    return false;
                }
            }

            this.LogData(99, "Cleaning up", "Completed");
            this.CompleteLogging();

            return importStatus;
        }        
        
        #endregion

        #region private modules

        /// <summary>
        /// Get the date format for the current user
        /// </summary>
        /// <returns></returns>
        private string GetUserDateFormat()
        {
            CultureInfo ci = CultureInfo.CurrentCulture;
            return ci.DateTimeFormat.ShortDatePattern;
        }

        /// <summary>
        /// Get the current time format for the current user
        /// </summary>
        /// <returns></returns>
        private string GetUserTimeFormat()
        {
            CultureInfo ci = CultureInfo.CurrentCulture;
            return ci.DateTimeFormat.ShortTimePattern;
        }

        /// <summary>
        /// Test if a string is an integer number: Positive or Negative
        /// </summary>
        /// <param name="strNumber"></param>
        /// <returns></returns>
        private bool IsInteger(String strNumber)
        {
            Regex objNotIntPattern = new Regex("[^0-9-]");
            Regex objIntPattern = new Regex("^-[0-9]+$|^[0-9]+$");

            return !objNotIntPattern.IsMatch(strNumber) && objIntPattern.IsMatch(strNumber);
        }

        /// <summary>
        /// test if the string is a number
        /// </summary>
        /// <param name="strNumber"></param>
        /// <returns></returns>
        private bool IsNumber(String strNumber)
        {
            Regex objNotNumberPattern = new Regex("[^0-9.-]");
            Regex objTwoDotPattern = new Regex("[0-9]*[.][0-9]*[.][0-9]*");
            Regex objTwoMinusPattern = new Regex("[0-9]*[-][0-9]*[-][0-9]*");
            String strValidRealPattern = "^([-]|[.]|[-.]|[0-9])[0-9]*[.]*[0-9]+$";
            String strValidIntegerPattern = "^([-]|[0-9])[0-9]*$";
            Regex objNumberPattern = new Regex("(" + strValidRealPattern + ")|(" + strValidIntegerPattern + ")");

            return !objNotNumberPattern.IsMatch(strNumber) &&
            !objTwoDotPattern.IsMatch(strNumber) &&
            !objTwoMinusPattern.IsMatch(strNumber) &&
            objNumberPattern.IsMatch(strNumber);
        }

        /// <summary>
        /// Predict the data type for a text field. If it is date, return the date format
        /// as well.
        /// </summary>
        /// <param name="Text">Input text</param>
        /// <param name="format">Return data format</param>
        /// <returns>Data type</returns>
        private string PredictDataType(string Text, ref string format)
        {
            int testInteger = 0;
            double testDouble = 0;

            //Start with the numbers               
            if (IsInteger(Text))
            {
                if (int.TryParse(Text, out testInteger) && Text.Length != 8) //In case this is a special date format YYYYMMDD
                    return "int";

                long testLong = 0;
                if (long.TryParse(Text, out testLong) && Text.Length != 8) //In case this is a special date format YYYYMMDD
                    return "bigint";
            }

            //Try for real numbers                
            if (IsNumber(Text) && Text.Length != 8)
            {
                if (double.TryParse(Text, out testDouble))
                    return "real";
            }

            //try both int and real for cases we didn't cover
            if (int.TryParse(Text, out testInteger) && Text.Length != 8)
                return "int";

            if (double.TryParse(Text, out testDouble) && Text.Length != 8)
                return "real";

            //Check if it is a date/time string
            if (GetStringDateFormat(Text, ref format))
                return "date";

            //Ensure that integers and long of length 8 are not misclassified
            if (IsInteger(Text))
            {
                if (int.TryParse(Text, out testInteger) && Text.Length == 8) //In case this is a special date format YYYYMMDD
                    return "int";

                long testLong = 0;
                if (long.TryParse(Text, out testLong) && Text.Length == 8) //In case this is a special date format YYYYMMDD
                    return "bigint";
            }

            //Try for real numbers                
            if (IsNumber(Text) && Text.Length == 8)
            {
                if (double.TryParse(Text, out testDouble))
                    return "real";
            }

            //try both int and real for cases we didn't cover
            if (int.TryParse(Text, out testInteger) && Text.Length == 8)
                return "int";

            if (double.TryParse(Text, out testDouble) && Text.Length == 8)
                return "real";

            return "string";
        }

        /// <summary>
        /// Get the data format for a string with a date value
        /// </summary>
        /// <param name="DateText"></param>
        /// <param name="DateFormat"></param>
        /// <returns></returns>
        private bool GetStringDateFormat(string DateText, ref string DateFormat)
        {
            if (formats == null || formats.Length == 0)
                formats = GetListOfFormats();

            DateTime TestDate;

            //test if the data matches the current user format
            if (DateTime.TryParseExact(DateText, GetUserDateFormat()
                , CultureInfo.CurrentCulture
                , DateTimeStyles.None
                , out TestDate))
            {
                DateFormat = GetUserDateFormat();
                return true;
            }
            else
            {
                //try all the formats in the list  - all for "new CultureInfo("en-US")"
                foreach (string s in formats)
                {
                    if (DateTime.TryParseExact(DateText, s
                        , CultureInfo.CurrentCulture
                        , DateTimeStyles.None
                        , out TestDate))
                    {

                        DateFormat = s;
                        return true;
                    }
                }

                //we are still here - see if the system can find this automatically
                if (DateTime.TryParse(DateText, out TestDate))
                {

                    DateFormat = "";
                    return true;
                }
                else
                    return false;
            }

        }
        
        private DateFormats[] GetListOfFomats()
        {
            DateFormats[] formats ={
                        new DateFormats(-1, "Do not know")
                    ,   new DateFormats(103, "DD/MM/YEAR")
                    ,   new DateFormats(104, "DD.MM.YEAR")
                    ,   new DateFormats(105, "DD-MM-YEAR")
                    ,   new DateFormats(106, "DD MMM YEAR")
                    ,   new DateFormats(110, "MM-DD-YEAR")
                    ,   new DateFormats(101, "MM/DD/YEAR")
                    ,   new DateFormats(107, "MMM DD,YEAR")
                    ,   new DateFormats(102, "YEAR.MM.DD")
                    ,   new DateFormats(111, "YEAR/MM/DD")
                    ,   new DateFormats(222, "YEAR-MM-DD")
                    ,   new DateFormats(112, "YEARMMDD")
                    ,   new DateFormats(301, "YEAR MM DD") //non-standard
                    ,   new DateFormats(302, "DD-MMM-YEAR") //non-standard
                };

            return formats;
        }


        private string[] GetListOfFormats()
        {
            DateFormats[] formats = this.GetListOfFomats();
            List<string> lstFormats = new List<string>();

            for (int i = 1; i < formats.Length; i++)
            {
                lstFormats.Add(formats[i].Format.Replace("YEAR", "YYYY").ToLower());
                lstFormats.Add(formats[i].Format.Replace("YEAR", "YY").ToLower());
            }

            string[] arrFormats = new string[lstFormats.Count];

            //I do this because .ToArray() is only supported in .net 4.0
            for (int i = 0; i < lstFormats.Count; i++)
            {
                arrFormats[i] = lstFormats[i].ToLower().Replace("mmm", "MMM").Replace("mm", "MM");
            }

            return arrFormats;
        }

        private string[] GetActualFormats(string format)
        {
            string[] actualFormats;

            if (format.ToLower().Contains("year"))
            {
                actualFormats = new string[2];

                actualFormats[0] = format.Replace("year", "yyyy");
                actualFormats[0] = actualFormats[0].Replace("YEAR", "YYYY");
                actualFormats[0] = actualFormats[0].ToLower().Replace("mmm", "MMM").Replace("mm", "MM");

                actualFormats[1] = format.Replace("year", "yy");
                actualFormats[1] = actualFormats[1].Replace("YEAR", "YY");
                actualFormats[1] = actualFormats[1].ToLower().Replace("mmm", "MMM").Replace("mm", "MM");
            }
            else
            {
                actualFormats = new string[1];
                actualFormats[0] = format.ToLower().Replace("mmm", "MMM").Replace("mm", "MM");
            }

            return actualFormats;
        }        

        private CsvReader GetReader(string filepath)
        {
            CsvReader reader = new CsvReader();

            bool status = GetReader(filepath, ref reader);

            if (status)
                return reader;
            else
                return null;
        }

        private bool GetReader(string filepath, ref CsvReader reader)
        {
            //Open the reader for reading
            try
            {
                if (spec.IgnoreFirst)
                {
                    reader = new CsvReader(new StreamReader(filepath
                        , GetEncoding(spec.EncodingType))
                        , true
                        , spec.GetDelimiter());
                }
                else
                {
                    reader = new CsvReader(new StreamReader(filepath
                        , GetEncoding(spec.EncodingType))
                        , false
                        , spec.GetDelimiter());
                }

            }
            catch (Exception ex)
            {
                this.error = "Error opening CSV file: " + ex.Message;
                return false;
            }

            reader.MissingFieldAction = MissingFieldAction.ReplaceByEmpty;
            reader.DefaultParseErrorAction = ParseErrorAction.AdvanceToNextLine;

            return true;
        }


        private System.Text.Encoding GetEncoding(EncodingTypes type)
        {
            switch (type)
            {
                case EncodingTypes.Default:
                    return System.Text.Encoding.Default;
                case EncodingTypes.ASCII:
                    return System.Text.Encoding.ASCII;
                case EncodingTypes.BigEndianUnicode:
                    return System.Text.Encoding.BigEndianUnicode;
                case EncodingTypes.Unicode:
                    return System.Text.Encoding.Unicode;
                case EncodingTypes.UTF32:
                    return System.Text.Encoding.UTF32;
                case EncodingTypes.UTF7:
                    return System.Text.Encoding.UTF7;
                case EncodingTypes.UTF8:
                    return System.Text.Encoding.UTF8;
                default:
                    return System.Text.Encoding.Default;
            }

        }

        private bool ProcessCSVImport(string filepath, string destinationTable, string schema)
        {
           
            //Perform initial validation of the file
            var errorState = this.ValidateCSVImport(filepath);

            if (errorState == ErrorReturnTypes.Warning && !ignoreWarnings)
            {
                error = warning;
                return false;
            }
            else if (errorState == ErrorReturnTypes.Error)
            {
                return false;
            }

            bool status = false;

            this.LogData(5, "Loading source csv file for import");

            using (CsvReader reader = GetReader(filepath))
            {
                if (reader == null)
                    return false;

                Int64 innerIndex = 0;
                Int64 rowIndex = 0;
                Int64 rowErrorCnt = 0;
                Int64 rowWarningCnt = 0;
                                
                this.LogData(7, "Creating import table");

                //set the reader's properties, read beyond the first line
                if (spec.IgnoreFirst)
                {
                    try
                    {
                        reader.GetFieldHeaders();
                        rowIndex += 1;
                    }
                    catch { }
                }


                //Prepare the upload table
                DataSet dset = new DataSet();
                DataTable dtable = dset.Tables.Add(Utilities.UnEscape(destinationTable));

                //No of Columns we are processing
                int ncols = spec.Columns.Count;
                selectedColumns = new List<Column>();

                for (int i = 0; i < ncols; i++)
                {

                    if (spec.Columns[i].Selected)
                    {
                        DataColumn col = new DataColumn();
                        col.ColumnName = spec.Columns[i].Name;
                        col.DataType = ImportUtilities.GetSystemType(spec.Columns[i].DataType);

                        //clear existing errors
                        spec.Columns[i].ClearErrors();
                        dtable.Columns.Add(col);
                        selectedColumns.Add(spec.Columns[i]);
                    }
                }

                //Drop old table
                dalProcessor.DropTable(new Table(destinationTable));

                //Create the new table
                status = ImportUtilities.CreateTable(dalProcessor, selectedColumns, destinationTable, schema);

                if (!status)
                {
                    this.error = "Failed to create destination table: " + dalProcessor.Error;
                    return false;
                }

                SqlBulkCopy bcopy;

                try
                {
                    bcopy = new SqlBulkCopy(dalProcessor.ConnectionString);
                    bcopy.DestinationTableName = Utilities.Escape(destinationTable);
                    bcopy.BulkCopyTimeout = 0;
                }
                catch (Exception ex)
                {
                    this.error = "Bulk copy failed to load: " + ex.Message;
                    return false;
                }

                //read the first record
                bool ReadStatus = reader.ReadNextRecord();

                if (!ReadStatus)
                {
                    this.error = "The import table is empty";
                    return false;
                }

                this.LogData(10, "Data import starting...");

                this.GetColumnTypes();

                try
                {
                    do
                    {

                        innerIndex += 1;
                        rowIndex += 1;

                        //read the row
                        List<object> processedRow = new List<object>();
                        string[] currentRow = new string[ncols];

                        try
                        {
                            for (int j = 0; j < ncols; j++)
                            {
                                currentRow[j] = reader[j];
                            }
                        }
                        catch
                        {
                            this.error = String.Format("The numbers of columns mismatch at row {0}", rowIndex);
                            return false;
                        }


                        //Process this and look for errors
                        errorState = ProcessCSVRow(currentRow, (rowIndex), ref processedRow, false);

                        if (errorState == ErrorReturnTypes.Error)
                            rowErrorCnt += 1;
                        else if (errorState == ErrorReturnTypes.Warning)
                            rowWarningCnt += 1;

                        //Process row
                        DataRow drow = dtable.NewRow();

                        //set the row column values

                        for (int k = 0; k < processedRow.Count; k++)
                        {
                            drow[k] = processedRow[k];
                        }

                        //Add row to the table
                        dtable.Rows.Add(drow);


                        if (innerIndex == maxUploadSize)
                        {
                            innerIndex = 0;

                            //uplolad file
                            bcopy.WriteToServer(dtable);

                            //Prepare the upload table                                   
                            dtable.Rows.Clear();

                            this.rowsImported = spec.IgnoreFirst ? rowIndex - 1 : rowIndex;

                            if (totalRows > 0)
                            {
                                this.progress = Convert.ToInt32((double)(spec.IgnoreFirst ? rowIndex - 1 : rowIndex) / (double)totalRows * 85.0) + 10;
                                this.LogData(this.progress, String.Format("Imported {0}/{1} rows", spec.IgnoreFirst ? rowIndex - 1 : rowIndex, totalRows - 1));
                            }
                            else
                            {
                                if (this.progress < 95)
                                    this.progress += 1;

                                this.LogData(this.progress, String.Format("Imported {0} rows", spec.IgnoreFirst ? rowIndex - 1 : rowIndex));
                            }
                        }

                    } while (reader.ReadNextRecord());

                    //Write what is left
                    if (dtable.Rows.Count > 0)
                    {
                        rowsImported += innerIndex;
                        bcopy.WriteToServer(dtable);

                        if (totalRows > 0)
                        {
                            this.progress = Convert.ToInt32((double)(rowIndex) / (double)totalRows * 85.0) + 10;
                            this.LogData(this.progress, String.Format("Imported {0}/{1} rows", spec.IgnoreFirst ? rowIndex - 1 : rowIndex, totalRows - 1));
                        }
                        else
                        {
                            if (this.progress < 95)
                                this.progress += 1;

                            this.LogData(this.progress, String.Format("Imported {0} rows", spec.IgnoreFirst ? rowIndex - 1 : rowIndex));
                        }
                    }

                 
                    this.LogData(96, "Data uploaded completed");

                    //Pull the errors
                    if (rowErrorCnt > 0)
                    {
                        //There were import error
                        //Process the column errors
                        for (int j = 0; j < ncols; j++)
                        {

                            if (spec.Columns[j].GetImportError() != "")
                            {
                                this.error = Utilities.Catenate(this.error
                                    , spec.Columns[j].GetImportError(), ".\r\n");
                            }
                        }
                    }

                    if (rowWarningCnt > 0)
                    {
                        //There were import error
                        //Process the column errors
                        for (int j = 0; j < ncols; j++)
                        {

                            if (spec.Columns[j].GetImportWarning() != "")
                            {
                                this.error = Utilities.Catenate(this.warning
                                    , spec.Columns[j].GetImportWarning(), ".\r\n");
                            }
                        }
                    }

                }
                catch (Exception ex)
                {
                    this.error = "Unknown error occurred during import : " + ex.Message;
                    return false;
                }

                if (rowErrorCnt == 0)
                    return true;
                else
                {
                    if ((double)rowErrorCnt / (double)(rowIndex) > 0.8)
                    {
                        return false;
                    }
                    else
                    {
                        //Make the errors warnings
                        this.warning = this.error;
                        this.error = "";
                        return true;
                    }

                }
            }

        }

        /// <summary>
        /// Get the list of data types for the columns
        /// </summary>
        private void GetColumnTypes()
        {
            int ncols = spec.Columns.Count;
            columnTypes = new List<Type>();

            for (int i = 0; i < ncols; i++)
            {
                columnTypes.Add(ImportUtilities.GetSystemType(spec.Columns[i].DataType));
            }
        }

        /// <summary>
        /// Cleans up a row before it is uploaded.
        /// </summary>
        /// <param name="Data"></param>
        /// <param name="rowIndex"></param>
        /// <param name="returnRow"></param>
        /// <param name="considerNullStringValueAsNull"></param>
        /// <returns></returns>
        private ErrorReturnTypes ProcessCSVRow(string[] Data, Int64 rowIndex, ref List<object> returnRow, bool considerNullStringValueAsNull)
        {
            ErrorReturnTypes errorState = ErrorReturnTypes.None;

            int ncols = spec.Columns.Count;


            for (int i = 0; i < ncols; i++)
            {
                //ensure that the types defined works for these records    
                DataColumn c = new DataColumn();

                if (!spec.Columns[i].Selected)
                    continue;

                string CurrentValue = Data[i];
                //string CurrentDataType = GetSystemTypeName(spec.Columns[i].DataType);

                c.DataType = columnTypes[i];

                if (considerNullStringValueAsNull && CurrentValue.ToLower().Trim() == "null")
                    CurrentValue = String.Empty;


                if (spec.NullString.Contains(CurrentValue))
                {
                    c.DefaultValue = DBNull.Value;
                    //spec.Columns[i].AddImportError(ErrorTypes.BlankColumnError
                    //                , "", rowIndex);

                    //if (errorState != ErrorReturnTypes.Error) //Error supersedes warnings
                    //    errorState = ErrorReturnTypes.Warning;
                }

                if (!spec.NullString.Contains(CurrentValue))
                {

                    bool typeCheck = true;

                    switch (c.DataType.ToString())
                    {
                        case "System.DateTime":
                            DateTime TestDate;

                            if (spec.Columns[i].DateFormat == "")
                            {
                                if (!DateTime.TryParse(CurrentValue, out TestDate))
                                {
                                    typeCheck = false;
                                }

                                //Check all the cultures and year formats
                                if (!typeCheck && !DateTime.TryParseExact(CurrentValue
                                        , GetListOfFormats()
                                        , CultureInfo.CurrentCulture
                                        , DateTimeStyles.None
                                        , out TestDate))
                                {
                                    typeCheck = false;
                                }
                                else
                                {
                                    typeCheck = true;
                                }
                            }
                            else
                            {
                                //Check all the cultures and year formats
                                if (!DateTime.TryParseExact(CurrentValue
                                        , GetActualFormats(spec.Columns[i].DateFormat)
                                        , CultureInfo.CurrentCulture
                                        , DateTimeStyles.None
                                        , out TestDate))
                                {
                                    typeCheck = false;
                                }
                            }

                            //if there is a format and it didn't work - unlikely but could happen
                            if (!typeCheck)
                            {
                                if (DateTime.TryParse(CurrentValue, out TestDate))
                                {
                                    typeCheck = true;
                                    spec.Columns[i].DateFormat = ""; //remove this format
                                }
                            }

                            if (!typeCheck)
                                spec.Columns[i].AddImportError(ErrorTypes.TypeConversionError
                                    , "Invalid date format", rowIndex);
                            else
                                c.DefaultValue = TestDate;

                            break;
                        case "System.Int64":
                            Int64 _testInt64 = 0;

                            if (!String.IsNullOrEmpty(CurrentValue) && !Int64.TryParse(CurrentValue, out _testInt64))
                            {
                                typeCheck = false;
                                spec.Columns[i].AddImportError(ErrorTypes.TypeConversionError
                                    , "Invalid long integer", rowIndex);
                            }
                            else
                            {
                                c.DefaultValue = _testInt64;
                            }

                            break;
                        case "System.Int32":
                            Int32 _testInt32 = 0;

                            if (!String.IsNullOrEmpty(CurrentValue) && !Int32.TryParse(CurrentValue, out _testInt32))
                            {
                                typeCheck = false;
                                spec.Columns[i].AddImportError(ErrorTypes.TypeConversionError
                                    , "Invalid integer", rowIndex);
                            }
                            else
                            {
                                c.DefaultValue = _testInt32;
                            }

                            break;
                        case "System.Int16":
                            Int16 _testInt16 = 0;

                            if (!String.IsNullOrEmpty(CurrentValue) && !Int16.TryParse(CurrentValue, out _testInt16))
                            {
                                typeCheck = false;
                                spec.Columns[i].AddImportError(ErrorTypes.TypeConversionError
                                    , "Invalid small integer", rowIndex);
                            }
                            else
                            {
                                c.DefaultValue = _testInt16;
                            }

                            break;
                        case "System.Decimal":
                            Decimal _testDecimal = 0;

                            if (!String.IsNullOrEmpty(CurrentValue) && !Decimal.TryParse(CurrentValue, out _testDecimal))
                            {
                                typeCheck = false;
                                spec.Columns[i].AddImportError(ErrorTypes.TypeConversionError
                                    , "Invalid decimal", rowIndex);
                            }
                            else
                            {
                                c.DefaultValue = _testDecimal;
                            }

                            break;
                        case "System.Double":
                            Double _testDouble = 0;

                            if (!String.IsNullOrEmpty(CurrentValue) && !Double.TryParse(CurrentValue, out _testDouble))
                            {
                                typeCheck = false;
                                spec.Columns[i].AddImportError(ErrorTypes.TypeConversionError
                                    , "Invalid real number", rowIndex);
                            }
                            else
                            {
                                c.DefaultValue = _testDouble;
                            }

                            break;
                        case "System.String":

                            //do nothing :)
                            c.DefaultValue = CurrentValue;
                            break;
                    }

                    if (!typeCheck) errorState = ErrorReturnTypes.Error;
                }

                //Add the column to the row
                returnRow.Add(c.DefaultValue);
            }

            return errorState;
        }

        private ErrorReturnTypes ValidateCSVImport(string filepath)
        {
            bool status = true;

            //Predict the CSV columns datatype for the columns with no datatypes
            status = PredictCSVColumnsAndTypes(filepath);

            if (!status)
            {
                return ErrorReturnTypes.Error;
            }

            using (CsvReader reader = GetReader(filepath))
            {
                if (reader == null)
                    return ErrorReturnTypes.Error; ;


                //set the reader's properties, read beyond the first line
                if (spec.IgnoreFirst)
                {
                    try
                    {
                        reader.GetFieldHeaders();
                    }
                    catch { }
                }


                //No of Columns we are processing
                int ncols = spec.Columns.Count;
                Int64 rowIndex = 1;
                ErrorReturnTypes errorState = ErrorReturnTypes.None;

                this.GetColumnTypes();

                //Run through the first 10 rows to be sure the datatypes and formats are correct
                for (int i = 0; i < 10; i++)
                {
                    bool readStatus = reader.ReadNextRecord();

                    if (!readStatus)
                        break;

                    bool columnCountMatch = true;

                    if (i == 0)
                    {
                        //Ensure that the columns counts are the same
                        try
                        {
                            for (int j = 0; j < ncols; j++)
                            {
                                string dummy = reader[j];
                            }
                        }
                        catch { columnCountMatch = false; }
                    }

                    if (!columnCountMatch)
                    {
                        this.error = "Incorrect number of columns";
                        return ErrorReturnTypes.Error;
                    }

                    List<object> processedRow = new List<object>();
                    string[] currentRow = new string[ncols];

                    for (int j = 0; j < ncols; j++)
                    {
                        currentRow[j] = reader[j];
                    }

                    //Process this and look for errors
                    errorState = ProcessCSVRow(currentRow
                        , (i + rowIndex)
                        , ref processedRow, true);

                    //Some errors or warning
                    if (errorState != ErrorReturnTypes.None)
                    {
                        //Process the column errors
                        for (int j = 0; j < ncols; j++)
                        {

                            if (spec.Columns[j].GetImportError() != "")
                            {
                                this.warning = Utilities.Catenate(this.warning
                                    , spec.Columns[j].GetImportError()
                                    , ".\r\n"
                                    );

                            }

                            if (spec.Columns[j].GetImportWarning() != "")
                            {
                                this.warning = Utilities.Catenate(this.warning
                                    , spec.Columns[j].GetImportWarning()
                                    , "\r\n"
                                    );

                            }
                        }

                        errorState = ErrorReturnTypes.Warning;

                        return errorState;
                    }
                }


                //return state
                reader.Dispose();
                return ErrorReturnTypes.None;
            }

        }

        /// <summary>
        /// Log import operations
        /// </summary>
        /// <param name="progress"></param>
        /// <param name="operation"></param>
        /// <param name="data"></param>
        private void LogData(int progress, string operation, string data)
        {
            this.progress = progress;
            this.importLog.Add(String.Format("{0}: {1}", DateTime.Now.ToString("HH:mm:ss"), data));
        }

        private void LogData(int progress, string data)
        {
            LogData(progress, "", data);
        }

        /// <summary>
        /// Complete task logging
        /// </summary>
        private void CompleteLogging()
        {
            LogData(100, "Done");
        }


        #endregion

    }

}
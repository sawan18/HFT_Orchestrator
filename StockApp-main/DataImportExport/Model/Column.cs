using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Xml;
using System.IO;
using System.Data;
using CCACDataAccessLayer;

namespace MyData.Csv
{
    [DataContract]
    public class Column
    {
        private SqlServerTypes columnDataType = SqlServerTypes.varchar;
        private string columnName = "";
        private string columnDateFormat = "";
        private bool selected = true;
        private string columnAlias = "";

        private List<PredictValue> lstPredictedTypes = new List<PredictValue>();
        private List<ImportError> lstErrors = new List<ImportError>();
        public Column()
        {

        }

        public Column(string name)
        {
            columnName = name;
        }

        public Column(string name, SqlServerTypes datatype)
        {
            columnName = name;
            columnDataType = datatype;
        }

        public Column(string name, SqlServerTypes datatype, string dataformat)
        {
            columnName = name;
            columnDataType = datatype;
            columnDateFormat = dataformat;
        }

        [DataMember]
        public SqlServerTypes DataType
        {
            set { columnDataType = value; }
            get { return columnDataType; }
        }

        private int size = 4000;
        [DataMember]
        public int Size
        {
            get { return size; }
            set { size = value; }
        }

        private int precision = 10;
        [DataMember]
        public int Precision
        {
            get { return precision; }
            set { precision = value; }
        }

        private int scale = 5;
        [DataMember]
        public int Scale
        {
            get { return scale; }
            set { scale = value; }
        }

        [DataMember]
        public string Name
        {
            set { columnName = value; }
            get { return columnName; }
        }

        [DataMember]
        public string Alias
        {
            set { columnAlias = value; }
            get { return columnAlias; }
        }

        [DataMember]
        public string DateFormat
        {
            set { columnDateFormat = value; }
            get { return columnDateFormat; }
        }

        [DataMember]
        public bool Selected
        {
            set { selected = value; }
            get { return selected; }
        }

        
        private bool detectType = false;
        [DataMember]
        public bool DetectType
        {
            get { return detectType; }
            set { detectType = value; }
        }

        public List<PredictValue> PredictedValues
        {
            set { lstPredictedTypes = value; }
            get { return lstPredictedTypes; }
        }

        public void ClearErrors()
        {
            if (lstErrors == null)
                lstErrors = new List<ImportError>();
            else
                lstErrors.Clear();
        }

        public void AddImportError(ErrorTypes type, string error, long row)
        {
            if (lstErrors != null)
            {
                if (lstErrors.Count < 10)
                {
                    List<ImportError> matches
                           = (from n in lstErrors
                              where n.Error == error && n.ErrorType == type
                              select n
                               ).ToList();

                    if (matches.Count > 0)
                    {
                        matches[0].ApplicableRows.Add(row);
                    }
                    else
                    {
                        lstErrors.Add(new ImportError(type, error, row));
                    }
                }
            }
            else
            {
                lstErrors = new List<ImportError>();
            }

        }


        public string GetImportError()
        {
            return ProcessError(ErrorTypes.TypeConversionError);
        }

        public string GetImportWarning()
        {
            return ProcessError(ErrorTypes.BlankColumnError);
        }

        private string ProcessError(ErrorTypes type)
        {
            string errorMessage = "";
            string errorTemplate = "";
            
            if (lstErrors != null)
            {
                foreach (ImportError error in lstErrors)
                {
                    string title = "";
                    string rowsAffected = "";

                    if (error.ErrorType == ErrorTypes.TypeConversionError && error.ErrorType == type)
                    {
                        title = "Type conversion";
                    }
                    else if (error.ErrorType == ErrorTypes.BlankColumnError && error.ErrorType == type)
                    {
                        title = "Blank column";
                    }

                    if (title != "")
                    {
                        string customError = "";
                        if (error.Error != "")
                        {
                            customError = String.Format("({0})", error.Error);
                        }

                        if (error.ApplicableRows.Count > 10)
                        {
                            for (int i = 0; i < 10; i++)
                            {
                                rowsAffected = Utilities.Catenate(rowsAffected
                                    , error.ApplicableRows[i].ToString()
                                    , ","
                                    );

                            }


                            errorTemplate = String.Format("{0} has too many {1} error(s){3} . Your datatype for this column is probably incorrect. Some of the rows affected are {2}"
                                , Name, title, rowsAffected, customError);
                        }
                        else
                        {
                            for (int i = 0; i < error.ApplicableRows.Count; i++)
                            {
                                rowsAffected = Utilities.Catenate(rowsAffected
                                    , error.ApplicableRows[i].ToString()
                                    , ","
                                    );

                            }

                            errorTemplate = String.Format("{0} has a {1} error{3}  at rows {2}"
                                , Name, title, rowsAffected, customError);
                        }

                        errorMessage = Utilities.Catenate(errorMessage, errorTemplate, "\r\n");
                    }

                }
            }


            return errorMessage;
        }

        /// <summary>
        /// Gets the cast text for the field
        /// </summary>
        /// <returns></returns>
        public string GetFieldCast()
        {
            switch (this.DataType)
            {
                //Exact Numerics with precision
                case SqlServerTypes.@decimal:
                case SqlServerTypes.numeric:
                    return String.Format("{0}({1},{2})", Utilities.Escape(DataType.ToString()), this.Precision, this.Scale);

                //Exact Numerics
                case SqlServerTypes.@int:
                case SqlServerTypes.bigint:
                case SqlServerTypes.money:
                case SqlServerTypes.bit:
                case SqlServerTypes.smallint:
                case SqlServerTypes.smallmoney:
                case SqlServerTypes.tinyint:

                //Approximate Numerics
                case SqlServerTypes.@float:
                case SqlServerTypes.real:

                //Date and Time
                case SqlServerTypes.date:
                case SqlServerTypes.datetimeoffset:
                case SqlServerTypes.datetime2:
                case SqlServerTypes.datetime:
                case SqlServerTypes.smalldatetime:
                case SqlServerTypes.time:

                case SqlServerTypes.text:
                case SqlServerTypes.ntext:

                //Binary Strings
                case SqlServerTypes.binary:
                case SqlServerTypes.varbinary:
                case SqlServerTypes.image:

                    return String.Format("{0}", DataType.ToString());

                //Character Strings
                case SqlServerTypes.@char:
                case SqlServerTypes.varchar:

                //Unicode Character Strings
                case SqlServerTypes.nchar:
                case SqlServerTypes.nvarchar:
                    return String.Format("{0}({1})", DataType.ToString(), (this.Size == -1) ? "max" : this.Size.ToString());

                case SqlServerTypes.uniqueidentifier:
                case SqlServerTypes.xml:
                    return String.Format("{0}", DataType.ToString());

                default:
                    return "varchar(4000)";

            }
        }
    }

}
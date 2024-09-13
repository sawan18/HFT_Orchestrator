using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyData.Csv
{
    class ImportError
    {
        private ErrorTypes errorType = ErrorTypes.TypeConversionError;
        private string error = "";
        private List<long> applicableRows = new List<long>();

        public ImportError() { }

        public ImportError(ErrorTypes type, string errorMsg, long currentRow)
        {
            errorType = type;
            error = errorMsg;
            applicableRows.Add(currentRow);
        }
        public ErrorTypes ErrorType
        {
            set { errorType = value; }
            get { return errorType; }
        }

        public string Error
        {
            set { error = value; }
            get { return error; }
        }

        public List<long> ApplicableRows
        {
            set { applicableRows = value; }
            get { return applicableRows; }
        }
    }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyData.Csv
{
    public class PredictValue
    {
        private string dataType = "";
        private string format = "";
        private int occurrence = 0;

        public PredictValue()
        {

        }

        public PredictValue(string _datatype, string _format, int _occurrence)
        {
            dataType = _datatype;
            format = _format;
            occurrence = _occurrence;

        }
        public string DataType
        {
            set { dataType = value; }
            get { return dataType; }
        }

        public string Format
        {
            set { format = value; }
            get { return format; }
        }
        public int Occurrence
        {
            set { occurrence = value; }
            get { return occurrence; }
        }
    }
}
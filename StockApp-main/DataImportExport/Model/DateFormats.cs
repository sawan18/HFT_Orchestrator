using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MyData.Csv
{
    public class DateFormats
    {
        private int formatCode = -1;
        private string format = "";

        public DateFormats() { }

        public DateFormats(int code, string formatString)
        {
            formatCode = code;
            format = formatString;
        }
        public int FormatCode
        {
            set { formatCode = value; }
            get { return formatCode; }
        }

        public string Format
        {
            set { format = value; }
            get { return format; }

        }
    }
}
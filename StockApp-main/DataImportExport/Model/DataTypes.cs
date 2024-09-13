using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;

namespace MyData.Csv
{

    [DataContract]
    public enum Delimiters
    {
        [EnumMember]
        csv = 0,
        [EnumMember]
        tab = 1,
        [EnumMember]
        semiColon = 2,
        [EnumMember]
        colon = 3,
        [EnumMember]
        verticalBar = 4
    }

    [DataContract]
    public enum ErrorTypes
    {
        [EnumMember]
        TypeConversionError = 0,
        [EnumMember]
        BlankColumnError = 1
    }

    [DataContract]
    public enum ErrorReturnTypes
    {
        [EnumMember]
        None = 0,
        [EnumMember]
        Warning = 1,
        [EnumMember]
        Error = 2
    }

    [DataContract]
    public enum EncodingTypes
    {
        [EnumMember]
        Default = 0,
        [EnumMember]
        ASCII = 1,
        [EnumMember]
        BigEndianUnicode = 2,
        [EnumMember]
        Unicode = 3,
        [EnumMember]
        UTF32 = 4,
        [EnumMember]
        UTF7 = 5,
        [EnumMember]
        UTF8 = 6
    }
}
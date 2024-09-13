using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.OleDb;
using System.Data;
using MyData.Csv;
using CCACDataAccessLayer;

namespace MyData.Data
{
    public static class ImportUtilities
    {

        #region public
        
        public static bool CreateTable(DalProcessor dalProcessor, ImportExportSpec spec, string tablename, string schema)
        {
            Column[] cols = new Column[spec.Columns.Count];

            for (int i = 0; i < spec.Columns.Count; i++)
            {
                cols[i] = spec.Columns[i];
            }

            return CreateTable(dalProcessor, cols, tablename, schema);
        }

        public static bool CreateTable(DalProcessor dalProcessor, List<Column> columns, string tablename, string schema)
        {
            Column[] cols = new Column[columns.Count];
            for (int i = 0; i < columns.Count; i++)
            {
                cols[i] = columns[i];
            }

            return CreateTable(dalProcessor, cols, tablename, schema);
        }

        public static bool CreateTable(DalProcessor dalProcessor, Column[] columns, string tablename, string schema)
        {
            return CreateTableExplicit(dalProcessor, columns, tablename, schema);
        }

        public static SqlServerTypes GetSqlDataType(string type)
        {
            switch (type)
            {
                case "System.String":
                case "varchar":
                case "nvarchar":
                case "string":
                    return SqlServerTypes.varchar;


                case "System.Int16":
                case "smallint":
                case "small":
                case "short":
                    return SqlServerTypes.smallint;

                case "int":
                case "integer":
                case "System.Int32":
                    return SqlServerTypes.@int;


                case "System.Int64":
                case "long":
                case "bigInt":
                case "big":
                    return SqlServerTypes.bigint;

                case "System.Double":
                case "double":
                case "single":
                case "numeric":
                case "real":
                case "float":
                case "decimal":
                    return SqlServerTypes.@float;

                case "System.DateTime":
                case "datetime":
                case "time":
                case "date":
                    return SqlServerTypes.datetime;
                                   
                default:
                    SqlServerTypes convertedType;
                    if(Enum.TryParse<SqlServerTypes>(type, out convertedType))
                    {
                        return convertedType;
                    }
                    else
                    {
                        return SqlServerTypes.varchar;
                    }
            }

        }

        public static Type GetSystemType(SqlServerTypes type)
        {
            switch (type)
            {
                case SqlServerTypes.varchar:                
                case SqlServerTypes.nvarchar:
                case SqlServerTypes.text:
                    return typeof(string);


                case SqlServerTypes.smallint:
                    return typeof(Int16);

                case SqlServerTypes.@int:
                    return typeof(int);

                case SqlServerTypes.bigint:
                    return typeof(long);                

                case SqlServerTypes.@float:
                    return typeof(double);

                case SqlServerTypes.datetime:
                    return typeof(DateTime);

                default:
                    return typeof(string);
            }

        }
        #endregion

        #region private modules
        /// <summary>
        /// Creates table using explicit create table definition
        /// </summary>       
        private static bool CreateTableExplicit(DalProcessor dalTarget, Column[] columns, string tablename, string tableSchema="dbo")
        {           
            string createTableTemplate = @"
                CREATE TABLE {2}.{0} (
                              {1}
                ) ON [PRIMARY]
            ";
            
            string fieldListing = "";

            foreach (Column field in columns)
            {
                fieldListing = Utilities.Catenate(fieldListing
                            , String.Format("{0} {1} NULL", Utilities.Escape(field.Name), field.GetFieldCast())
                            , ",");
            }

            string sql = String.Format(createTableTemplate
                , Utilities.Escape(tablename)
                , fieldListing
                , tableSchema
                );

            dalTarget.DropTable(new Table(tablename, tableSchema));
            dalTarget.ExecuteScalar(sql);

            if (dalTarget.Error != "")
            {
                throw new Exception(String.Format("Unknown error creating table {0}.{1}: {2}", tableSchema, tablename, dalTarget.Error));
            }


            return true;
        }
        
        #endregion

    }
}
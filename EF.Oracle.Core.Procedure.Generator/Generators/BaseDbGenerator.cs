using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using EF.Oracle.Core.Procedure.Generator.Model;
using EF.Oracle.Core.Procedure.Generator.Utils;
using Oracle.ManagedDataAccess.Client;

namespace EF.Oracle.Core.Procedure.Generator.Generators
{
    internal class BaseDbGenerator
    {
        public static List<DatabseObjectException> ExceptionList { get; set; }

        public BaseDbGenerator()
        {
            ExceptionList = new List<DatabseObjectException>();
        }


        protected string SP_GetType(string type, string paramName, bool isNullable)
        {
            type = type.ToUpper().Trim();


            var suffix = isNullable ? "?" : string.Empty;

            if (type == "NUMBER(1,0)")
                return $"Boolean{suffix}";
            if (type == "NUMBER(5,0)")
                return $"Int16{suffix}";
            if (type == "NUMBER(10,0)")
                return $"Int32{suffix}";
            if (type == "NUMBER(19,0)")
                return $"Int64{suffix}";
            if (type.Contains("NUMBER"))
                return $"Decimal{suffix}";
            if (type.Contains("DATE"))
                return $"DateTime{suffix}";
            if (type.Contains("NVARCHAR"))
                return "String";
            if (type.Contains("VARCHAR"))
                return "String";
            if (type.Contains("CLOB"))
                return "String";
            if (type == "BLOB")
                return "byte[]";
            if (type.Contains("TIMESTAMP"))
                return $"DateTime{suffix}";
            if (type.Contains("CHAR"))
                return "String";
            if (type == "FLOAT")
                return $"Decimal{suffix}";


            return $"{paramName}";
        }

        protected OracleDbType SP_GetDbType(string type)
        {
            type = type.ToUpper().Trim();

            if (type == "NUMBER(10,0)")
                return OracleDbType.Int32;
            if (type == "NUMBER(19,0)")
                return OracleDbType.Int64;
            if (type.Contains("NUMBER"))
                return OracleDbType.Decimal;
            if (type == "NUMBER(5,0)")
                return OracleDbType.Int16;

            if (type.Contains("NVARCHAR"))
                return OracleDbType.NVarchar2;
            if (type.Contains("VARCHAR"))
                return OracleDbType.Varchar2;
            if (type.Contains("CHAR"))
                return OracleDbType.Char;
            if (type.Contains("TIMESTAMP"))
                return OracleDbType.TimeStamp;
            if (type.Contains("DATE"))
                return OracleDbType.Date;
            if (type == "BLOB")
                return OracleDbType.Blob;
            if (type == "CLOB")
                return OracleDbType.Clob;
            if (type == "NUMBER(1,0)")
                return OracleDbType.Boolean;

            return OracleDbType.Array;

        }

        protected void WriteException(string physicalFolder,string fileName)
        {
            var sb = new StringBuilder();

            try
            {
                foreach (var (exception, index) in ExceptionList.WithIndex())
                {
                    sb.AppendLine(
                        $"{DateTime.Now:yyyy-MM-dd HH':'mm':'ss} - EXCEPTION {index + 1} / {ExceptionList.Count}: {exception.DbObject} - {exception.Message}");
                }

                File.WriteAllText(Path.Combine(physicalFolder, $"{fileName}.txt"), sb.ToString());
            }
            catch (Exception e)
            {
                Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH':'mm':'ss} ERROR!!! --> {e.Message}");
            }
        }


    }
}
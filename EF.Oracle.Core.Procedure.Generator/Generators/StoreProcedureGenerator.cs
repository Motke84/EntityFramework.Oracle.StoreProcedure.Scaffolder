using EF.Oracle.Core.Procedure.Generator.Model;
using EF.Oracle.Core.Procedure.Generator.T4;
using Oracle.ManagedDataAccess.Client;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;

namespace EF.Oracle.Core.Procedure.Generator.Generators
{
    internal class StoreProcedureGenerator : BaseDbGenerator
    {
        
        public static List<StoreProcedure> SpList { get; set; }

        public StoreProcedureGenerator()
        {
            SpList = new List<StoreProcedure>();
        }

        public void StoreProcedureToCoreScan(Program.Options options)
        {
            Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH':'mm':'ss} STEP 1 - GET STORED PROCEDURE LIST");

            var storeProcedureList = GetStoreProcedureList(options.Connection, options.Schema);

            Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH':'mm':'ss} STEP 2 - PROCESS STORED PROCEDURE");
            Console.WriteLine(
                $"{DateTime.Now:yyyy-MM-dd HH':'mm':'ss} STEP 2 - Total Stored Procedure: {storeProcedureList.Rows.Count}");

            var counter = 1;

            foreach (DataRow dataRow in storeProcedureList.Rows)
            {

                var schema = dataRow["OWNER"].ToString();
                var spName = dataRow["OBJECT_NAME"].ToString();

                var dtSpParam = GetStoreProcedureParam(schema, spName, options.Connection);
                var isHasReturnValue = IsStoreProcedureHasReturnValue(schema, spName, options.Connection);



                Console.WriteLine(
                    $"{DateTime.Now:yyyy-MM-dd HH':'mm':'ss} STEP 2 - {counter} / {storeProcedureList.Rows.Count} ==> \"{dataRow["OWNER"]}.{dataRow["OBJECT_NAME"]}\"");

                var pList = (from DataRow par in dtSpParam.Rows
                             select new DatabaseParam
                             {
                                 Param = par["ARGUMENT_NAME"].ToString()?.Replace("@", ""),
                                 Type = SP_GetType(par["DATA_TYPE"].ToString(),par["ARGUMENT_NAME"].ToString(), true),
                                 Length = par["DATA_LENGTH"].GetType().Name == "DBNull" ? null : par["DATA_LENGTH"].ToString(),
                                 Precision = par["DATA_PRECISION"].GetType().Name == "DBNull"
                                     ? null
                                     : par["DATA_PRECISION"].ToString(),
                                 Order = par["POSITION"].GetType().Name == "DBNull" ? null : par["POSITION"].ToString(),
                                 Output = par["IN_OUT"].ToString(),
                                 IsNullable = true,
                                 DbType = SP_GetDbType(par["DATA_TYPE"].ToString()).ToString(),
                             }).ToList();


                var procedure = new StoreProcedure
                {
                    Name = spName,
                    Schema = schema,
                    HasReturnValue = isHasReturnValue,
                    Params = pList,
                };

                SpList.Add(procedure);
                counter++;

                //   break;
            }

            Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH':'mm':'ss} FINISH");

            var procedureGeneratorT4 =
                new StoreProcedureGeneratorT4(SpList, options.Namespace, options.PhysicalFolder, options.Context);

            File.WriteAllText(Path.Combine(options.PhysicalFolder, $"StoreProcedures{options.FileName}"),
                procedureGeneratorT4.TransformText());

            if (ExceptionList.Count <= 0)
                return;

            Console.WriteLine(
                $"{DateTime.Now:yyyy-MM-dd HH':'mm':'ss} EXCEPTIONS FOUND! Please check log.txt in '{options.PhysicalFolder}'");
            WriteException(options.PhysicalFolder, "StoreProcedureLog");
        }


        private static DataTable GetStoreProcedureList(string connectionString, string schema)
        {
            DataTable dtResult = new DataTable();
            try
            {
                using OracleConnection connection = new OracleConnection(connectionString);
                string sql = $@"SELECT ALL_OBJECTS.* 
                                FROM ALL_OBJECTS 
                                WHERE ALL_OBJECTS.OBJECT_TYPE IN ('PROCEDURE') and 
                                ALL_OBJECTS.OWNER = '{schema}'";

                OracleDataAdapter adapter = new OracleDataAdapter();
                adapter.SelectCommand = new OracleCommand(sql, connection);
                adapter.Fill(dtResult);

                return dtResult;
            }
            catch (Exception e)
            {

                ExceptionList.Add(new DatabseObjectException()
                { Method = "GetStoreProcedureList", Message = e.Message });

                return dtResult;
            }
        }

        private DataTable GetStoreProcedureParam(string schema, string sp, string connectionString)
        {
            var dtResult = new DataTable();

            try
            {
                using var connection = new OracleConnection(connectionString);

                var sql = $@"
                    SELECT 
                    SYS.ALL_ARGUMENTS.ARGUMENT_NAME,
                    SYS.ALL_ARGUMENTS.CHAR_LENGTH,
                    SYS.ALL_ARGUMENTS.DATA_TYPE,
                    SYS.ALL_ARGUMENTS.DATA_LENGTH,
                    SYS.ALL_ARGUMENTS.IN_OUT,
                    SYS.ALL_ARGUMENTS.DATA_PRECISION,
                    SYS.ALL_ARGUMENTS.POSITION               
                        FROM SYS.ALL_ARGUMENTS  
                    where object_name  = '{sp}'
                    order by SYS.ALL_ARGUMENTS.POSITION";

                var adapter = new OracleDataAdapter();
                adapter.SelectCommand = new OracleCommand(sql, connection);
                adapter.Fill(dtResult);

                return dtResult;
            }
            catch (Exception e)
            {

                ExceptionList.Add(new DatabseObjectException
                {
                    Method = "GetStoreProcedureParam",
                    FullName = $"{schema}.{sp}",
                    Schema = schema,
                    DbObject = sp,
                    Message = e.Message
                });

                return dtResult;
            }
        }

        private bool IsStoreProcedureHasReturnValue(string schema, string sp, string connectionString)
        {
            var dtResult = new DataTable();

            try
            {
                using var connection = new OracleConnection(connectionString);

                var sql = $@"
                    SELECT 
                     count(1) RET_VALS
                        FROM SYS.ALL_SOURCE   
                    where name  = '{sp}' and
                    text like '%RETURN_RESULT%' ";

                var adapter = new OracleDataAdapter();
                adapter.SelectCommand = new OracleCommand(sql, connection);
                adapter.Fill(dtResult);

                return Convert.ToInt32(dtResult.Rows[0]["RET_VALS"]) > 0;
            }
            catch (Exception e)
            {
                ExceptionList.Add(new DatabseObjectException
                {
                    Method = "IsStoreProcedureHasReturnValue",
                    FullName = $"{schema}.{sp}",
                    Schema = schema,
                    DbObject = sp,
                    Message = e.Message
                });

                return false;
            }
        }
    }
}

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
    internal class UddtGenerator : BaseDbGenerator
    {
        public static List<Uddt> Uddts { get; set; }

        public UddtGenerator()
        {
            Uddts = new List<Uddt>();
        }

        public void UddtsToCoreScan(Program.Options options)
        {
            Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH':'mm':'ss} STEP 1 - GET UDDTs LIST");

            var uddtsList = GetUddtsList(options.Connection, options.Schema);

            Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH':'mm':'ss} STEP 2 - PROCESS UDDTs");
            Console.WriteLine(
                $"{DateTime.Now:yyyy-MM-dd HH':'mm':'ss} STEP 2 - Total UDDTs: {uddtsList.Rows.Count}");

            var counter = 1;

            foreach (DataRow dataRow in uddtsList.Rows)
            {

                var schema = dataRow["OWNER"].ToString();
                var udttName = dataRow["OBJECT_NAME"].ToString();
                var isAnArray = int.Parse(dataRow["IS_ARRAY"].ToString()) == 1;

                var dtSpParam = GetUddtParam(schema, udttName, options.Connection);

                Console.WriteLine(
                    $"{DateTime.Now:yyyy-MM-dd HH':'mm':'ss} STEP 2 - {counter} / {uddtsList.Rows.Count} ==> \"{schema}.{udttName}\"");


                var pList = (from DataRow par in dtSpParam.Rows
                             select new DatabaseParam
                             {
                                 Param = par["PARAM_NAME"].ToString()?.Replace("@", ""),
                                 Type = SP_GetType(par["PARAM_TYPE"].ToString(),par["PARAM_NAME"].ToString(), true),
                                 Length = par["PARAM_LENGTH"].GetType().Name == "DBNull" ? null : par["PARAM_LENGTH"].ToString(),
                                 Precision = par["PARAM_PRECISION"].GetType().Name == "DBNull"
                                     ? null
                                     : par["PARAM_PRECISION"].ToString(),
                                 Order = par["PARAM_NO"].GetType().Name == "DBNull" ? null : par["PARAM_NO"].ToString(),
                                 IsNullable = true,
                                 DbType = SP_GetDbType(par["PARAM_TYPE"].ToString()).ToString(),
                             }).ToList();


                var uddt = new Uddt
                {
                    Name = udttName,
                    Schema = schema,
                    Params = pList,
                    IsAnArray = isAnArray
                };

                Uddts.Add(uddt);
                counter++;
            }

            Console.WriteLine($"{DateTime.Now:yyyy-MM-dd HH':'mm':'ss} FINISH");

            var procedureGeneratorT4 =
                new UddtGeneratorT4(Uddts, options.Namespace, options.PhysicalFolder, options.Context);

            File.WriteAllText(Path.Combine(options.PhysicalFolder, $"Uddts{options.FileName}"),
                procedureGeneratorT4.TransformText());

            if (ExceptionList.Count <= 0)
                return;

            Console.WriteLine(
                $"{DateTime.Now:yyyy-MM-dd HH':'mm':'ss} EXCEPTIONS FOUND! Please check log.txt in '{options.PhysicalFolder}'");
            WriteException(options.PhysicalFolder, "UddtLog");
        }


        private static DataTable GetUddtsList(string connectionString, string schema)
        {
            DataTable dtResult = new DataTable();
            try
            {
                using OracleConnection connection = new OracleConnection(connectionString);
                string sql = $@"SELECT 
                                distinct ALL_OBJECTS.OBJECT_NAME,
                                ALL_OBJECTS.OWNER,
                                CASE (SELECT COUNT (1)
                                       FROM SYS.ALL_COLL_TYPES 
                                      WHERE ALL_OBJECTS.OBJECT_NAME = TYPE_NAME)
                                   WHEN 0 THEN 0
                                   ELSE 1 END    IS_ARRAY
                                FROM ALL_OBJECTS 
                         WHERE OBJECT_TYPE IN ('TYPE') AND OWNER = '{schema}'";

                OracleDataAdapter adapter = new OracleDataAdapter();
                adapter.SelectCommand = new OracleCommand(sql, connection);
                adapter.Fill(dtResult);

                return dtResult;
            }
            catch (Exception e)
            {
                ExceptionList.Add(new DatabseObjectException()
                { Method = "GetUddtsList", Message = e.Message });

                return dtResult;
            }
        }

        private DataTable GetUddtParam(string schema, string uddtName, string connectionString)
        {
            var dtResult = new DataTable();

            try
            {
                using var connection = new OracleConnection(connectionString);

                var sql = $@"
                       SELECT *
                        FROM (SELECT ATTRS.TYPE_NAME          TYPE_NAME,
                                     ATTRS.ATTR_NAME          PARAM_NAME,
                                     ATTRS.ATTR_TYPE_NAME     PARAM_TYPE,
                                     ATTRS.LENGTH             PARAM_LENGTH,
                                     ATTRS.PRECISION          PARAM_PRECISION,
                                     ATTRS.SCALE              PARAM_SCALE,
                                     ATTRS.ATTR_NO            PARAM_NO,
                                     ATTRS.OWNER,
                                     0                        IS_ARRAY
                                FROM SYS.ALL_TYPE_ATTRS ATTRS
                              UNION
                              SELECT ALL_COLL_TYPES.TYPE_NAME          TYPE_NAME,
                                     ALL_COLL_TYPES.ELEM_TYPE_NAME     PARAM_NAME,
                                     ALL_COLL_TYPES.COLL_TYPE          PARAM_TYPE,
                                     0                                 PARAM_LENGTH,
                                     0                                 PARAM_PRECISION,
                                     0                                 PARAM_SCALE,
                                     1                                 PARAM_NO,
                                     ALL_COLL_TYPES.OWNER,
                                     1                                 IS_ARRAY
                                FROM SYS.ALL_COLL_TYPES ALL_COLL_TYPES
                               WHERE SYS.ALL_COLL_TYPES.COLL_TYPE = 'TABLE')
                       WHERE OWNER = '{schema}' AND TYPE_NAME = '{uddtName}'
                    ORDER BY TYPE_NAME, PARAM_NO";

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
                    FullName = $"{schema}.{uddtName}",
                    Schema = schema,
                    DbObject = uddtName,
                    Message = e.Message
                });

                return dtResult;
            }
        }

       
    }
}

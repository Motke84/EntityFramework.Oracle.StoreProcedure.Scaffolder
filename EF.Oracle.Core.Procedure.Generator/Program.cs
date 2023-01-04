using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommandLine;
using EF.Oracle.Core.Procedure.Generator.Generators;
using EF.Oracle.Core.Procedure.Generator.Model;
using EF.Oracle.Core.Procedure.Generator.T4;
using Figgle;
using Oracle.ManagedDataAccess.Client;

namespace EF.Oracle.Core.Procedure.Generator
{
    static class Program
    {
        internal class Options
        {
            [Option('c', "connection", Required = true, HelpText = "ORACLE Connection String.")]
            public string Connection { get; set; }

            [Option('s', "schema", Required = true, HelpText = "ORACLE Schema to filter Stored Procedure")]
            public string Schema { get; set; }

            [Option('n', "namespace", Required = true, HelpText = "Namespace of the original context file")]
            public string Namespace { get; set; }

            [Option('x', "context", Required = true, HelpText = "Oracle Entity Framework dbContext from where the new files will extend")]
            public string Context { get; set; }

            [Option('S', "sfolder", Required = true, HelpText = "Relative Solution folder where the output file will be added")]
            public string SolutionFolder { get; set; }

            [Option('P', "pfolder", Required = true, HelpText = @"Physical destination folder where the output file will be written")]
            public string PhysicalFolder { get; set; }

            [Option('F', "fileName", Required = true, HelpText = @"Output file name")]
            public string FileName { get; set; }

            [Option('M', "mode", Required = false, HelpText = @"can be StoreProcedures, UDDT or ALL the default ALL", Default = Mode.ALL)]
            public Mode Mode { get; set; }
        }


        [Flags]
        internal enum Mode
        {
            None = 0,
            StoreProcedures = 1,
            UDDT = 2,
            ALL = StoreProcedures | UDDT,
        }


        public static async Task Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.White;

            Console.WriteLine(
                FiggleFonts.Starwars.Render("SP To EF Oracle"));

            args ??= new string[] { };


            await Task.Factory.StartNew(() =>
                Parser.Default.ParseArguments<Options>(args)
                    .WithParsed(opt =>
                    {
                        if (opt.Mode.HasFlag(Mode.UDDT))
                        {
                            var uddtGenerator = new UddtGenerator();
                            uddtGenerator.UddtsToCoreScan(opt);
                        }
                        if (opt.Mode.HasFlag(Mode.StoreProcedures))
                        {
                            var procedureGenerator = new StoreProcedureGenerator();
                            procedureGenerator.StoreProcedureToCoreScan(opt);
                        }

                    }));
        }
    }
}

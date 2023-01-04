using System.Collections.Generic;
using EF.Oracle.Core.Procedure.Generator.Model;

namespace EF.Oracle.Core.Procedure.Generator.T4
{
    partial class StoreProcedureGeneratorT4
    {
        private List<StoreProcedure> SpList { get; set; }

        private string Namespace { get; set; }
        private string SolutionDestinationFolder { get; set; }
        private string DestinationDbContext { get; set; }
        private string ProgramName => System.AppDomain.CurrentDomain.FriendlyName;


        private string SourceDbContext { get; set; }
        public StoreProcedureGeneratorT4(List<StoreProcedure> _spList,
            string _namespace, 
            string _solutionDestinationFolder,            
            string _sourceDbContext) {
        
            this.SpList = _spList;
            this.Namespace = _namespace;
            this.SolutionDestinationFolder = _solutionDestinationFolder;
            

            this.SourceDbContext = _sourceDbContext;
        }
    }

    
}

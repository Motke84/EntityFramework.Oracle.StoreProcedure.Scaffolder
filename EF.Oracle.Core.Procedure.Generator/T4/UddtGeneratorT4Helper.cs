using System.Collections.Generic;
using EF.Oracle.Core.Procedure.Generator.Model;

namespace EF.Oracle.Core.Procedure.Generator.T4
{
    partial class UddtGeneratorT4
    {
        private List<Uddt> Uddts { get; set; }

        private string Namespace { get; set; }
        private string SolutionDestinationFolder { get; set; }
        private string DestinationDbContext { get; set; }
        private string ProgramName => System.AppDomain.CurrentDomain.FriendlyName;


        private string SourceDbContext { get; set; }
        public UddtGeneratorT4(List<Uddt> uddts,
            string _namespace, 
            string _solutionDestinationFolder,            
            string _sourceDbContext) {
        
            this.Uddts = uddts;
            this.Namespace = _namespace;
            this.SolutionDestinationFolder = _solutionDestinationFolder;
            

            this.SourceDbContext = _sourceDbContext;
        }
    }

    
}

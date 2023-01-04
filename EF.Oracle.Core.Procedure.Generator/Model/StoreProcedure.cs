using System.Collections.Generic;

namespace EF.Oracle.Core.Procedure.Generator.Model
{
    public class StoreProcedure
    {
        public StoreProcedure()
        {
            Params = new List<DatabaseParam>();
        }

        public List<DatabaseParam> Params { get; set; }

        public string Name { get; set; }
        public string Schema { get; set; }
        public bool HasReturnValue { get; set; }

        public string GetMethodDefinition() =>
            !HasReturnValue ? $@"public async Task<int> {Name}_Async" : $@"public async Task<List<T>> {Name}_Async<T>";

    }

}

using System.Collections.Generic;

namespace EF.Oracle.Core.Procedure.Generator.Model
{
    public class Uddt
    {
        public Uddt()
        {
            Params = new List<DatabaseParam>();
        }

        public List<DatabaseParam> Params { get; set; }

        public string Name { get; set; }
        public string Schema { get; set; }

        public bool IsAnArray { get; set; }
    }

}

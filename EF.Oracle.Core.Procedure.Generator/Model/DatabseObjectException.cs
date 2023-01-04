namespace EF.Oracle.Core.Procedure.Generator.Model
{
    public class DatabseObjectException
    {
        public string Method { get; set; }
        public string FullName { get; set; }
        public string Schema { get; set; }
        public string DbObject { get; set; }
        public string Message { get; set; }
    }
}

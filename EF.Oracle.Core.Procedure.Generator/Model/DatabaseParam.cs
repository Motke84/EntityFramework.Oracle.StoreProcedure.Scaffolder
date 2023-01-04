namespace EF.Oracle.Core.Procedure.Generator.Model
{
    public class DatabaseParam
    {
        public string Param { get; set; }
        public string Type { get; set; }
        public string Length { get; set; }
        public string Precision { get; set; }

        public string Order { get; set; }
        public bool IsOutput => Output == "Output";

        public string Output { get; set; }
        public bool IsNullable { get; set; }
        public string DbType { get; set; }
    }
}

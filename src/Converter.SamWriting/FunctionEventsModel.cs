using DotLiquid;

namespace Converter.SamWriting
{
    public class FunctionEventsModel : Drop
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public YamlValue Properties { get; set; }
    }
}

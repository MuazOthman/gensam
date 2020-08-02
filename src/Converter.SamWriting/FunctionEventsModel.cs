using DotLiquid;
using System.Collections.Generic;

namespace Converter.SamWriting
{
    public class FunctionEventsModel : Drop
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
    }
}

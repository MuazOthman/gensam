using DotLiquid;
using System.Collections.Generic;

namespace Converter.SamWriting
{
    public class FunctionModel : Drop
    {
        public string Name { get; set; }
        public string CodeUri { get; set; }
        public string Handler { get; set; }
        public Dictionary<string, string> EnvironmentVariables { get; } = new Dictionary<string, string>();
        public YamlValue Policies { get; } = new YamlValue(new string[] { });
        public List<FunctionEventsModel> Events { get; } = new List<FunctionEventsModel>();
    }
}

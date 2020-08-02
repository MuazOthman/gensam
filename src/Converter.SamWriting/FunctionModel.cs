using DotLiquid;
using System.Collections.Generic;

namespace Converter.SamWriting
{
    public class FunctionModel : Drop
    {
        public string Name { get; set; }
        public Dictionary<string, string> EnvironmentVariables { get; } = new Dictionary<string, string>();
        public Dictionary<string, string> Policies { get; } = new Dictionary<string, string>();
        public List<FunctionEventsModel> Events { get; } = new List<FunctionEventsModel>();
    }
}

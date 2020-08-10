using DotLiquid;
using System.Collections.Generic;

namespace Converter.SamWriting
{
    public class FunctionModel : Drop
    {
        private readonly Dictionary<string, string> environmentVariables = new Dictionary<string, string>();

        public string Name { get; set; }
        public string CodeUri { get; set; }
        public string Handler { get; set; }
        public IReadOnlyDictionary<string, string> EnvironmentVariables => environmentVariables; 
        public YamlValue Policies { get; } = new YamlValue(new string[] { });
        public List<FunctionEventsModel> Events { get; } = new List<FunctionEventsModel>();

        public void AddEnvironmentVariable(string key, string value)
        {
            if (!environmentVariables.ContainsKey(key))
                environmentVariables.Add(key, value);
        }
    }
}

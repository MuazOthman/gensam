using DotLiquid;
using System.Collections.Generic;

namespace Converter.SamWriting
{
    public class FunctionModel : Drop
    {
        private readonly Dictionary<string, string> environmentVariables = new Dictionary<string, string>();
        private readonly Dictionary<string, FunctionEventsModel> events = new Dictionary<string, FunctionEventsModel>();

        public string Name { get; set; }
        public string CodeUri { get; set; }
        public string Handler { get; set; }
        public IReadOnlyDictionary<string, string> EnvironmentVariables => environmentVariables; 
        public IReadOnlyDictionary<string, FunctionEventsModel> Events => events; 
        public YamlValue Policies { get; } = new YamlValue(new string[] { });

        public void AddEnvironmentVariable(string key, string value)
        {
            if (!environmentVariables.ContainsKey(key))
                environmentVariables.Add(key, value);
        }

        public void AddEvent(string key, FunctionEventsModel value)
        {
            if (!events.ContainsKey(key))
                events.Add(key, value);
        }
    }
}

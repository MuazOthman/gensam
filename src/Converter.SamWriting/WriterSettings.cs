using DotLiquid;
using System.Collections.Generic;

namespace Converter.SamWriting
{
    public class WriterSettings
    {
        public FunctionSettings Function { get; set; }
    }
    public class FunctionSettings
    {
        public string CodeUriTemplate { get; set; }
        public string HandlerTemplate { get; set; }
        public FunctionGlobals Globals { get; set; }
    }

    public class FunctionGlobals : Drop
    {
        public string Runtime { get; set; }
        public string Timeout { get; set; }
        public Dictionary<string, string> EnvironmentVariables { get; set; }
    }
}

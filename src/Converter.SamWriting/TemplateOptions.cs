using System;

namespace Converter.SamWriting
{
    public class TemplateOptions
    {
        public Func<string, string> GetFunctionCodeUri { get; set; } = (name) => $"./Lamb{name}/";
        public Func<string, string> GetFunctionHandler { get; set; } = (name) => $"Lamb{name}::Lamb{name}.{name}::Invoke";
    }
}

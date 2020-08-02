using DotLiquid;
using System.Collections.Generic;

namespace Converter.SamWriting
{
    public class TemplateModel : Drop
    {
        public TemplateOptions Options { get; set; }
        public bool IsCorsEnabled { get; internal set; }
        public bool HasRestApi { get; internal set; }
        public List<TableModel> Tables { get; internal set; }
        public List<string> Topics { get; internal set; }
        public List<FunctionModel> Functions { get; internal set; }
    }
}

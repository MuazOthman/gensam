using DotLiquid;
using System.Collections.Generic;

namespace Converter.SamWriting
{
    public class TemplateModel : Drop
    {
        public bool IsCorsEnabled { get; internal set; }
        public bool HasRestApi { get; internal set; }
        public List<TableModel> Tables { get; internal set; }
        public List<string> Topics { get; internal set; }
        public List<string> Queues { get; internal set; }
        public Dictionary<string, List<string>> QueuePoliciesPerTopic { get; internal set; }
        public List<BucketModel> Buckets { get; internal set; }
        public List<FunctionModel> Functions { get; internal set; }
        public List<QueueTopicSubscription> QueueTopicSubscriptions { get; internal set; }
    }

    public class QueueTopicSubscription : Drop
    {
        public string Topic { get; set; }
        public string Queue { get; set; }
        public YamlValue FilterPolicy { get; set; }
    }
}

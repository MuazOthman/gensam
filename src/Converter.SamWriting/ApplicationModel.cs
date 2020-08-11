using DotLiquid;
using System.Collections.Generic;

namespace Converter.SamWriting
{
    public class ApplicationModel : Drop
    {
        public List<string> IncludedFiles { get; internal set; }
        private readonly Dictionary<string, string> parameters = new Dictionary<string, string>();
        public IReadOnlyDictionary<string, string> Parameters => parameters; 
        public FunctionGlobals FunctionGlobals { get; internal set; }
        public bool IsCorsEnabled { get; internal set; }
        public bool HasRestApi { get; internal set; }
        public List<TableModel> Tables { get; internal set; }
        public List<TopicModel> Topics { get; internal set; }
        public List<QueueModel> Queues { get; internal set; }
        public List<QueuePoliciesForTopic> QueuePoliciesPerTopic { get; internal set; }
        public List<BucketModel> Buckets { get; internal set; }
        public List<FunctionModel> Functions { get; internal set; }
        public List<QueueTopicSubscription> QueueTopicSubscriptions { get; internal set; }

        public void AddParameter(string key, string value)
        {
            if (!parameters.ContainsKey(key))
                parameters.Add(key, value);
        }
    }
}

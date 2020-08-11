using DotLiquid;
using System.Collections.Generic;

namespace Converter.SamWriting
{
    public class QueuePoliciesForTopic : Drop
    {
        public string TopicName { get; set; }
        public List<string> QueueNames { get; set; }
    }
}

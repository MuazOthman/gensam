using DotLiquid;

namespace Converter.SamWriting
{
    public class QueueTopicSubscription : Drop
    {
        public string Topic { get; set; }
        public string Queue { get; set; }
        public YamlValue FilterPolicy { get; set; }
    }
}

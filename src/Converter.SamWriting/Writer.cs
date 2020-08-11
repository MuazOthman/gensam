using Converter.Model;
using DotLiquid;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Converter.SamWriting
{
    public class Writer
    {
        private readonly Application application;
        public WriterSettings WriterSettings { get; set; }
        public List<string> IncludedFiles { get; set; }

        private readonly ApplicationModel applicationModel;

        public Writer(Application application)
        {
            this.application = application;
            if (!application.IsCompiled)
                application.Compile();
            applicationModel = new ApplicationModel();
            PrepareApplicationModel();
        }

        public void Write(string filePath)
        {
            Template.NamingConvention = new DotLiquid.NamingConventions.CSharpNamingConvention();
            var fs = new TemplateFileSystem();
            fs["FunctionCodeUri"] = WriterSettings?.Function?.CodeUriTemplate;
            fs["FunctionHandler"] = WriterSettings?.Function?.HandlerTemplate;
            Template.FileSystem = fs;
            Template t = Template.Parse(fs.ReadTemplateFile("application"));

            applicationModel.FunctionGlobals = WriterSettings?.Function?.Globals;
            applicationModel.IncludedFiles = IncludedFiles;

            foreach (var f in applicationModel.Functions)
            {
                foreach (var e in f.Events)
                {
                    if (e.Value.Properties != null)
                        e.Value.Properties.Prefix = "            ";
                }
                if (f.Policies != null)
                    f.Policies.Prefix = "            ";
            }

            foreach (var sub in applicationModel.QueueTopicSubscriptions)
            {
                if (sub.FilterPolicy != null)
                    sub.FilterPolicy.Prefix = "        ";
            }

            var s = t.Render(Hash.FromAnonymousObject(new { model = applicationModel }));
            File.WriteAllText(filePath, s);
        }

        private void PrepareApplicationModel()
        {
            applicationModel.HasRestApi = application.Components
                .Any(c => c.Type == ComponentType.RestEndpoint);

            applicationModel.IsCorsEnabled = application.Components
                .Where(c => c.Type == ComponentType.RestEndpoint)
                .Any(c => c.InboundConnections.Any());

            applicationModel.Functions = application.Components
                .Where(c => c.Type == ComponentType.Function)
                .Select(c => BuildFunction(c))
                .ToList();

            applicationModel.Tables = application.Components
                .Where(c => c.Type == ComponentType.Table)
                .Select(c => new TableModel
                {
                    Name = c.Name,
                    IsStreamEnabled = c.OutboundConnections.Any()
                })
                .ToList();

            applicationModel.Topics = application.Components
                .Where(c => c.Type == ComponentType.Topic)
                .Select(c => new TopicModel { Name = c.Name })
                .ToList();

            applicationModel.QueuePoliciesPerTopic = application.Components
                .Where(c => c.Type == ComponentType.Topic)
                .SelectMany(c => c.OutboundConnections.Where(conn => conn.Target.Type == ComponentType.Queue))
                .GroupBy(conn => conn.Source.Name)
                .Select(g => new QueuePoliciesForTopic
                {
                    TopicName = g.Key,
                    QueueNames = g.Select(conn => conn.Target.Name).ToList()
                })
                .ToList();

            applicationModel.QueueTopicSubscriptions = application.Components
                .Where(c => c.Type == ComponentType.Topic)
                .SelectMany(c => c.OutboundConnections.Where(conn => conn.Target.Type == ComponentType.Queue))
                .GroupBy(conn => new { Topic = conn.Source.Name, Queue = conn.Target.Name })
                .Select(g =>
                    new QueueTopicSubscription
                    {
                        Topic = g.Key.Topic,
                        Queue = g.Key.Queue,
                        FilterPolicy =
                            MergeSnsFilters(
                                g.Select(conn => ParseSnsMessageAttributes(conn.Label))
                                .ToList()
                            )
                            .ToDictionary(p => p.Key, p => new YamlValue(p.Value))
                    }
                )
                .ToList();

            applicationModel.Queues = application.Components
                .Where(c => c.Type == ComponentType.Queue)
                .Select(c => new QueueModel { Name = c.Name })
                .ToList();

            applicationModel.Buckets = application.Components
                .Where(c => c.Type == ComponentType.Bucket)
                .Select(c => new BucketModel
                {
                    Name = c.Name,
                    IsCorsEnabled = c.InboundConnections.Any(conn2 => conn2.Source.Type == ComponentType.Browser)
                })
                .ToList();

            var lambdaSnsSubs = application.Components
                .Where(c => c.Type == ComponentType.Topic)
                .SelectMany(c => c.OutboundConnections.Where(conn => conn.Target.Type == ComponentType.Function))
                .GroupBy(conn => new { Topic = conn.Source.Name, Function = conn.Target.Name })
                .Select(g =>
                    new
                    {
                        g.Key.Topic,
                        g.Key.Function,
                        FilterPolicy =
                            MergeSnsFilters(
                                g.Select(conn => ParseSnsMessageAttributes(conn.Label))
                                .ToList()
                            )
                            .ToDictionary(p => p.Key, p => new YamlValue(p.Value))
                    }
                )
                .ToList();

            foreach (var item in lambdaSnsSubs)
            {
                var func = applicationModel.Functions.SingleOrDefault(f => f.Name == item.Function);
                if (func == null)
                    continue;

                var @event = new FunctionEventsModel
                {
                    Type = "SNS",
                    Properties = new Dictionary<string, YamlValue>
                    {
                        { "Topic", $"!Ref {item.Topic}"},
                    }
                };
                if (item.FilterPolicy.Count > 0)
                    @event.Properties.Add("FilterPolicy", item.FilterPolicy);

                func.AddEvent(
                    Regex.Replace(item.Topic, "[^a-zA-Z0-9]", "") + "SNS",
                    @event
                );
            }
        }

        private FunctionModel BuildFunction(Component component)
        {
            var result = new FunctionModel
            {
                Name = component.Name
            };
            foreach (var conn in component.OutboundConnections)
            {
                switch (conn.Target.Type)
                {
                    case ComponentType.Browser:
                        // not supported
                        break;
                    case ComponentType.Bucket:
                        result.AddEnvironmentVariable($"{conn.Target.Name}BucketName", $"!Ref {conn.Target.Name}");
                        result.Policies.Add(
                            new Dictionary<string, string> {
                                { "S3CrudPolicy", $"BucketName: !Ref {conn.Target.Name}" }
                            }
                        );
                        break;
                    case ComponentType.EventBus:
                        result.AddEnvironmentVariable($"{conn.Target.Name}BusName", $"!Ref {conn.Target.Name}");
                        result.Policies.Add(
                            new Dictionary<string, string> {
                                { "EventBridgePutEventsPolicy", $"EventBusName: !Ref {conn.Target.Name}" }
                            }
                        );
                        break;
                    case ComponentType.Function:
                        // not supported
                        break;
                    case ComponentType.Queue:
                        result.AddEnvironmentVariable($"{conn.Target.Name}QueueUrl", $"!Ref {conn.Target.Name}");
                        result.Policies.Add(
                            new Dictionary<string, string> {
                                { "SQSSendMessagePolicy", $"QueueName: !GetAtt {conn.Target.Name}.QueueName" }
                            }
                        );
                        break;
                    case ComponentType.RestEndpoint:
                        // not supported
                        break;
                    case ComponentType.Schedule:
                        // not supported
                        break;
                    case ComponentType.Table:
                        result.AddEnvironmentVariable($"{conn.Target.Name}TableName", $"!Ref {conn.Target.Name}");
                        result.Policies.Add(
                            new Dictionary<string, string> {
                                { "DynamoDBCrudPolicy", $"TableName: !Ref {conn.Target.Name}" }
                            }
                        );
                        break;
                    case ComponentType.Topic:
                        result.AddEnvironmentVariable($"{conn.Target.Name}TopicArn", $"!Ref {conn.Target.Name}");
                        result.Policies.Add(
                            new Dictionary<string, string> {
                                { "SNSPublishMessagePolicy", $"TopicName: !GetAtt {conn.Target.Name}.TopicName" }
                            }
                        );
                        break;
                }
            }
            foreach (var conn in component.InboundConnections)
            {
                switch (conn.Source.Type)
                {
                    case ComponentType.Browser:
                        // not supported
                        break;
                    case ComponentType.Bucket:
                        var eventNames =
                            string.IsNullOrEmpty(conn.Label)
                                ? new[] { "\"s3:ObjectCreated:*\"" }
                                : conn.Label.Split('\n', System.StringSplitOptions.RemoveEmptyEntries);
                        result.AddEvent(
                            Regex.Replace(conn.Source.Name, "[^a-zA-Z0-9]", "") + "Bucket",
                            new FunctionEventsModel
                            {
                                Type = "S3",
                                Properties = new Dictionary<string, YamlValue>
                                {
                                    { "Bucket", $"!Ref {conn.Source.Name}" },
                                    { "Events", eventNames}
                                }
                            }
                        );
                        break;
                    case ComponentType.EventBus:
                        result.AddEvent(
                            Regex.Replace(conn.Source.Name, "[^a-zA-Z0-9]", "Rule"),
                            new FunctionEventsModel
                            {
                                Type = "EventBridgeRule",
                                Properties = new Dictionary<string, YamlValue>
                                {
                                    { "EventBusName", $"!Ref {conn.Source.Name}" },
                                    { "Pattern", ParseSnsMessageAttributes(conn.Label)}
                                }
                            }
                        );
                        break;
                    case ComponentType.Function:
                        // not supported
                        break;
                    case ComponentType.Queue:
                        result.AddEvent(
                            Regex.Replace(conn.Source.Name, "[^a-zA-Z0-9]", "") + "Queue",
                            new FunctionEventsModel
                            {
                                Type = "SQS",
                                Properties = new Dictionary<string, YamlValue>
                                {
                                    { "Queue", $"!GetAtt {conn.Source.Name}.Arn" },
                                    { "BatchSize", "10"}
                                }
                            }
                        );
                        break;
                    case ComponentType.RestEndpoint:
                        result.AddEvent(
                            Regex.Replace(conn.Source.Name, "[^a-zA-Z0-9]", "") + "Api",
                            new FunctionEventsModel
                            {
                                Type = "Api",
                                Properties = new Dictionary<string, YamlValue>
                                {
                                    { "Path", conn.Source.Properties["Endpoint"]},
                                    { "Method", conn.Source.Properties["HttpMethod"]}
                                }
                            }
                        );
                        if (conn.Source.InboundConnections.Any(conn2 => conn2.Source.Type == ComponentType.Browser))
                        {
                            result.AddEnvironmentVariable("AllowedDomains", "!Ref AllowedDomains");
                        }
                        break;
                    case ComponentType.Schedule:
                        result.AddEvent(
                            Regex.Replace(conn.Source.Name, "[^a-zA-Z0-9]", "") + "Timer",
                            new FunctionEventsModel
                            {
                                Type = "Schedule",
                                Properties = new Dictionary<string, YamlValue>
                                {
                                    { "Schedule", $"!Ref {conn.Source.Name}Expression"}
                                }
                            }
                        );
                        applicationModel.AddParameter($"{conn.Source.Name}Expression", "rate(1 day)");
                        break;
                    case ComponentType.Table:
                        result.AddEvent(
                            Regex.Replace(conn.Source.Name, "[^a-zA-Z0-9]", "") + "Stream",
                            new FunctionEventsModel
                            {
                                Type = "DynamoDB",
                                Properties = new Dictionary<string, YamlValue>
                                {
                                    { "Stream", $"!GetAtt {conn.Source.Name}.StreamArn"},
                                    { "BatchSize", "10"},
                                    { "StartingPosition", "TRIM_HORIZON"},
                                    { "BisectBatchOnFunctionError", "true"}
                                }
                            }
                        );
                        break;
                    case ComponentType.Topic:
                        // handled separately to allow for merging filters
                        break;
                }
            }
            return result;
        }

        private Dictionary<string, string> ParseSnsMessageAttributes(string s)
        {
            var result = new Dictionary<string, string>();
            foreach (var p in s.Split(new[] { '\n', '\r', ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = p.Split('=', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2)
                {
                    result.Add(parts[0].Trim(), parts[1].Replace("\"", "").Trim());
                }
            }
            return result;
        }

        private Dictionary<string, List<string>> MergeSnsFilters(List<Dictionary<string, string>> filters)
        {
            var result = new Dictionary<string, List<string>>();
            foreach (var f in filters)
            {
                foreach (var k in f.Keys)
                {
                    if (!result.ContainsKey(k))
                        result.Add(k, new List<string> { f[k] });
                    else
                    {
                        if (!result[k].Contains(f[k]))
                            result[k].Add(f[k]);
                    }
                }
            }
            return result;
        }
    }
}

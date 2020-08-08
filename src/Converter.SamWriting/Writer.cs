using Converter.Model;
using DotLiquid;
using DotLiquid.FileSystems;
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

        private readonly TemplateModel templateModel;

        public Writer(Application application)
        {
            this.application = application;
            if (!application.IsCompiled)
                application.Compile();
            templateModel = new TemplateModel();
            PrepareTemplate();
        }

        public void Write(string filePath)
        {
            Template.NamingConvention = new DotLiquid.NamingConventions.CSharpNamingConvention();
            var fs = new TemplateFileSystem();
            fs["FunctionCodeUri"] = WriterSettings?.Function?.CodeUriTemplate;
            fs["FunctionHandler"] = WriterSettings?.Function?.HandlerTemplate;
            Template.FileSystem = fs;
            Template t = Template.Parse(fs.ReadTemplateFile("template"));

            templateModel.FunctionGlobals = WriterSettings?.Function?.Globals;
            templateModel.IncludedFiles = IncludedFiles;

            foreach (var f in templateModel.Functions)
            {
                foreach (var e in f.Events)
                {
                    if (e.Properties != null)
                        e.Properties.Prefix = "            ";
                }
                if (f.Policies != null)
                    f.Policies.Prefix = "            ";
            }

            foreach (var sub in templateModel.QueueTopicSubscriptions)
            {
                if (sub.FilterPolicy != null)
                    sub.FilterPolicy.Prefix = "        ";
            }

            var s = t.Render(Hash.FromAnonymousObject(new { model = templateModel }));
            File.WriteAllText(filePath, s);
        }

        private void PrepareTemplate()
        {
            templateModel.HasRestApi = application.Components
                .Any(c => c.Type == ComponentType.RestEndpoint);

            templateModel.IsCorsEnabled = application.Components
                .Where(c => c.Type == ComponentType.RestEndpoint)
                .Any(c => c.InboundConnections.Any());

            templateModel.Tables = application.Components
                .Where(c => c.Type == ComponentType.Table)
                .Select(c => new TableModel
                {
                    Name = c.Name,
                    IsStreamEnabled = c.OutboundConnections.Any()
                })
                .ToList();

            templateModel.Functions = application.Components
                .Where(c => c.Type == ComponentType.Function)
                .Select(c => BuildFunction(c))
                .ToList();

            templateModel.Topics = application.Components
                .Where(c => c.Type == ComponentType.Topic)
                .Select(c => c.Name)
                .ToList();

            templateModel.QueuePoliciesPerTopic = application.Components
                .Where(c => c.Type == ComponentType.Topic)
                .SelectMany(c => c.OutboundConnections.Where(conn => conn.Target.Type == ComponentType.Queue))
                .GroupBy(conn => conn.Source.Name)
                .ToDictionary(g => g.Key, g => g.Select(conn => conn.Target.Name).ToList());

            templateModel.QueueTopicSubscriptions = application.Components
                .Where(c => c.Type == ComponentType.Topic)
                .SelectMany(c => c.OutboundConnections.Where(conn => conn.Target.Type == ComponentType.Queue))
                .Select(conn => new QueueTopicSubscription
                {
                    Topic = conn.Source.Name,
                    Queue = conn.Target.Name,
                    FilterPolicy = ParseSnsMessageAttributes(conn.Label)
                }
                )
                .ToList();

            templateModel.Queues = application.Components
                .Where(c => c.Type == ComponentType.Queue)
                .Select(c => c.Name)
                .ToList();

            templateModel.Buckets = application.Components
                .Where(c => c.Type == ComponentType.Bucket)
                .Select(c => new BucketModel
                {
                    Name = c.Name,
                    IsCorsEnabled = c.InboundConnections.Any(conn2 => conn2.Source.Type == ComponentType.Browser)
                })
                .ToList();
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
                        if (!result.EnvironmentVariables.ContainsKey($"{conn.Target.Name}BucketName"))
                            result.EnvironmentVariables.Add($"{conn.Target.Name}BucketName", $"!Ref {conn.Target.Name}");
                        result.Policies.Add(
                            new Dictionary<string, string> {
                                { "S3CrudPolicy", $"BucketName: !Ref {conn.Target.Name}" }
                            }
                        );
                        break;
                    case ComponentType.EventBus:
                        if (!result.EnvironmentVariables.ContainsKey($"{conn.Target.Name}BusName"))
                            result.EnvironmentVariables.Add($"{conn.Target.Name}BusName", $"!Ref {conn.Target.Name}");
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
                        if (!result.EnvironmentVariables.ContainsKey($"{conn.Target.Name}QueueUrl"))
                            result.EnvironmentVariables.Add($"{conn.Target.Name}QueueUrl", $"!Ref {conn.Target.Name}");
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
                        if (!result.EnvironmentVariables.ContainsKey($"{conn.Target.Name}TableName"))
                            result.EnvironmentVariables.Add($"{conn.Target.Name}TableName", $"!Ref {conn.Target.Name}");
                        result.Policies.Add(
                            new Dictionary<string, string> {
                                { "DynamoDBCrudPolicy", $"TableName: !Ref {conn.Target.Name}" }
                            }
                        );
                        break;
                    case ComponentType.Topic:
                        if (!result.EnvironmentVariables.ContainsKey($"{conn.Target.Name}TopicArn"))
                            result.EnvironmentVariables.Add($"{conn.Target.Name}TopicArn", $"!Ref {conn.Target.Name}");
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
                        result.Events.Add(new FunctionEventsModel
                        {
                            Name = Regex.Replace(conn.Source.Name, "[^a-zA-Z0-9]", "") + "Bucket",
                            Type = "S3",
                            Properties = new Dictionary<string, YamlValue>
                            {
                                { "Bucket", $"!Ref {conn.Source.Name}" },
                                { "Events", eventNames}
                            }
                        });
                        break;
                    case ComponentType.EventBus:
                        result.Events.Add(new FunctionEventsModel
                        {
                            Name = Regex.Replace(conn.Source.Name, "[^a-zA-Z0-9]", "Rule"),
                            Type = "EventBridgeRule",
                            Properties = new Dictionary<string, YamlValue>
                            {
                                { "EventBusName", $"!Ref {conn.Source.Name}" },
                                { "Pattern", ParseSnsMessageAttributes(conn.Label)}
                            }
                        });
                        break;
                    case ComponentType.Function:
                        // not supported
                        break;
                    case ComponentType.Queue:
                        result.Events.Add(new FunctionEventsModel
                        {
                            Name = Regex.Replace(conn.Source.Name, "[^a-zA-Z0-9]", "") + "Queue",
                            Type = "SQS",
                            Properties = new Dictionary<string, YamlValue>
                            {
                                { "Queue", $"!GetAtt {conn.Source.Name}.Arn" },
                                { "BatchSize", "10"}
                            }
                        });
                        break;
                    case ComponentType.RestEndpoint:
                        result.Events.Add(new FunctionEventsModel
                        {
                            Name = Regex.Replace(conn.Source.Name, "[^a-zA-Z0-9]", "") + "Api",
                            Type = "Api",
                            Properties = new Dictionary<string, YamlValue>
                            {
                                { "Path", conn.Source.Properties["Endpoint"]},
                                { "Method", conn.Source.Properties["HttpMethod"]}
                            }
                        });
                        if (conn.Source.InboundConnections.Any(conn2 => conn2.Source.Type == ComponentType.Browser))
                        {
                            result.EnvironmentVariables.Add("AllowedDomains", "!Ref AllowedDomains");
                        }
                        break;
                    case ComponentType.Schedule:
                        // TODO: add event to function
                        // TODO: add parameter to stack for schedule expression
                        // TODO: add environment variable function
                        break;
                    case ComponentType.Table:
                        result.Events.Add(new FunctionEventsModel
                        {
                            Name = Regex.Replace(conn.Source.Name, "[^a-zA-Z0-9]", "") + "Stream",
                            Type = "DynamoDB",
                            Properties = new Dictionary<string, YamlValue>
                            {
                                { "Stream", $"!GetAtt {conn.Source.Name}.StreamArn"},
                                { "BatchSize", "10"},
                                { "StartingPosition", "TRIM_HORIZON"},
                                { "BisectBatchOnFunctionError", "true"}
                            }
                        });
                        break;
                    case ComponentType.Topic:
                        result.Events.Add(new FunctionEventsModel
                        {
                            Name = Regex.Replace(conn.Source.Name, "[^a-zA-Z0-9]", "") + "SNS",
                            Type = "SNS",
                            Properties = new Dictionary<string, YamlValue>
                            {
                                { "Topic", $"!Ref {conn.Source.Name}"},
                                { "FilterPolicy", ParseSnsMessageAttributes(conn.Label) }
                            }
                        });
                        break;
                }
            }
            return result;
        }

        private YamlValue ParseSnsMessageAttributes(string s)
        {
            var result = new Dictionary<string, YamlValue>();
            foreach (var p in s.Split(new [] { '\n','\r',',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = p.Split('=', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2)
                {
                    result.Add(parts[0].Trim(), new YamlValue(new[] { parts[1].Replace("\"", "").Trim() }));
                }
            }
            return result;
        }
    }
}

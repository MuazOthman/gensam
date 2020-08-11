using Converter.Model;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml.Linq;

namespace Converter.DiagramFileReading
{
    public class Reader
    {
        private readonly Application application;

        public Reader(Application application)
        {
            this.application = application;
        }

        public void ReadFile(string filePath)
        {
            var fileId = Guid.NewGuid().ToString();
            var diagram = XElement.Load(filePath) //mxfile
                .Elements("diagram").FirstOrDefault();
            var root = GetDiagramRoot(diagram);
            List<XElement> vertices =
                (from el in root.Elements("mxCell")
                 where (string)el.Attribute("vertex") == "1"
                 select el).ToList();

            List<XElement> edges =
                (from el in root.Elements("mxCell")
                 where (string)el.Attribute("edge") == "1"
                 select el).ToList();

            foreach (XElement el in vertices)
            {
                var c = ReadComponent(el);
                if (c != null)
                {
                    application.RegisterComponent(fileId + el.Attribute("id").Value, c);
                }
            }
            foreach (XElement edge in edges)
            {
                var labelElement =
                (from el in root.Elements("mxCell")
                 where
                    (string)el.Attribute("vertex") == "1"
                    && (string)el.Attribute("parent") == (string)edge.Attribute("id")
                    && ((string)el.Attribute("style")).Contains("edgeLabel")
                 select el).SingleOrDefault();
                var label = "";
                if (labelElement != null)
                {
                    var html = new HtmlDocument();
                    html.LoadHtml(labelElement.Attribute("value").Value);
                    label = WebUtility.HtmlDecode(html.DocumentNode.InnerText) ?? "";
                }
                application.RegisterConnection(
                    fileId + edge.Attribute("source")?.Value,
                    fileId + edge.Attribute("target")?.Value,
                    label
                );
            }
        }

        Component ReadComponent(XElement element)
        {
            if (element.Attribute("vertex")?.Value != "1")
                return null;
            var html = new HtmlDocument();
            html.LoadHtml(element.Attribute("value").Value);
            var name = WebUtility.HtmlDecode(html.DocumentNode.InnerText);
            if (element.Attribute("style").Value.Contains("shape=mxgraph.aws4.lambda_function"))
                return new Component(name, ComponentType.Function);
            if (element.Attribute("style").Value.Contains("shape=mxgraph.aws4.client"))
                return new Component("_browser", ComponentType.Browser);
            if (element.Attribute("style").Value.Contains("shape=mxgraph.aws4.endpoint"))
                return new Component(name, ComponentType.RestEndpoint);
            if (element.Attribute("style").Value.Contains("shape=mxgraph.aws4.table"))
                return new Component(name, ComponentType.Table);
            if (element.Attribute("style").Value.Contains("shape=mxgraph.aws4.topic"))
                return new Component(name, ComponentType.Topic);
            if (element.Attribute("style").Value.Contains("shape=mxgraph.aws4.queue"))
                return new Component(name, ComponentType.Queue);
            if (element.Attribute("style").Value.Contains("shape=mxgraph.aws4.bucket"))
                return new Component(name, ComponentType.Bucket);
            if (element.Attribute("style").Value.Contains("shape=mxgraph.aws4.event_time_based"))
                return new Component(name, ComponentType.Schedule);
            return null;
        }

        private XElement GetDiagramRoot(XElement diagram)
        {
            XElement mxGraphModel;
            if (diagram.FirstNode.NodeType == System.Xml.XmlNodeType.Text)
            {
                var deflated = Deflate(diagram.Value);
                var urlDecoded = WebUtility.UrlDecode(deflated);
                mxGraphModel = XElement.Parse(urlDecoded);
            }
            else
            {
                mxGraphModel = diagram.Elements("mxGraphModel").FirstOrDefault();
            }
            return mxGraphModel.Elements("root").FirstOrDefault();
        }

        public static string Deflate(string s)
        {
            var bytes = Convert.FromBase64String(s);
            using (var msi = new MemoryStream(bytes))
            using (var mso = new MemoryStream())
            {
                using (var gs = new DeflateStream(msi, CompressionMode.Decompress))
                {
                    CopyTo(gs, mso);
                }

                return Encoding.UTF8.GetString(mso.ToArray());
            }
        }

        public static void CopyTo(Stream src, Stream dest)
        {
            byte[] bytes = new byte[4096];

            int cnt;

            while ((cnt = src.Read(bytes, 0, bytes.Length)) != 0)
            {
                dest.Write(bytes, 0, cnt);
            }
        }
    }

}

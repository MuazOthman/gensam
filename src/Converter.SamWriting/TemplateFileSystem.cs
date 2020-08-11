using DotLiquid;
using DotLiquid.Exceptions;
using DotLiquid.FileSystems;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Converter.SamWriting
{
    /// <summary>
    /// Adapted from https://github.com/dotliquid/dotliquid/blob/e3032ff36bf0d64aca14851cdc6cf487224f8d25/src/DotLiquid/FileSystems/EmbeddedFileSystem.cs
    /// </summary>
    class TemplateFileSystem : IFileSystem
    {
        private static readonly string root = "Converter.SamWriting";
        private readonly Assembly assembly;
        private readonly Dictionary<string, string> inlineTemplates = new Dictionary<string, string>();

        public string this[string index]
        {
            get => inlineTemplates.ContainsKey(index) ? inlineTemplates[index] : null;
            set
            {
                if (inlineTemplates.ContainsKey(index))
                    inlineTemplates[index] = value;
                else
                    inlineTemplates.Add(index, value);
            }
        }

        public string FullPath(string templatePath)
        {
            if (templatePath == null || !Regex.IsMatch(templatePath, @"^[^.\/][a-zA-Z0-9_\/]+$"))
                throw new FileSystemException(
                    "Error - Illegal template name '{0}'", templatePath);

            var basePath = templatePath.Contains("/")
                ? Path.Combine(root, Path.GetDirectoryName(templatePath))
                : root;

            var fileName = string.Format("liquid/{0}.liquid", Path.GetFileName(templatePath));

            var fullPath = Regex.Replace(Path.Combine(basePath, fileName), @"\\|/", ".");

            return fullPath;
        }

        public TemplateFileSystem()
        {
            assembly = Assembly.GetExecutingAssembly();
        }
        public string ReadTemplateFile(Context context, string templateName)
        {
            var templatePath = (string)context[templateName];
            return ReadTemplateFile(templatePath);
        }

        public string ReadTemplateFile(string templatePath)
        {
            if (inlineTemplates.ContainsKey(templatePath))
                return inlineTemplates[templatePath];

            var fullPath = FullPath(templatePath);

            using Stream stream = assembly.GetManifestResourceStream(fullPath);
            if (stream == null)
                throw new FileSystemException("Error - No such template '{0}'", fullPath);
            var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}

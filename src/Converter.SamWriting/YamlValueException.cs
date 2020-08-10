using System;
using System.Runtime.Serialization;

namespace Converter.SamWriting
{
    [Serializable]
    public class YamlValueException : Exception
    {
        public YamlValueException()
        {
        }

        public YamlValueException(string message) : base(message)
        {
        }

        public YamlValueException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected YamlValueException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}

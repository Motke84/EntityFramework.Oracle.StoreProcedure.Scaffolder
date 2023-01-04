using System;
using System.Runtime.Serialization;

namespace EF.Oracle.Core.Procedure.Generator.Model
{
    [Serializable]
    internal class UnknownDbTypeException : Exception
    {
        public UnknownDbTypeException()
        {
        }

        public UnknownDbTypeException(string message) : base(message)
        {
        }

        public UnknownDbTypeException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected UnknownDbTypeException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}
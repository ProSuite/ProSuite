using System;
using System.Runtime.Serialization;

namespace ProSuite.Commons.Exceptions
{
	[Serializable]
	public class InvalidDataException : Exception
	{
		public InvalidDataException() { }

		public InvalidDataException(string message)
			: base(message) { }

		public InvalidDataException(string message, Exception e)
			: base(message, e) { }

		protected InvalidDataException(SerializationInfo info,
		                               StreamingContext context)
			: base(info, context) { }
	}
}
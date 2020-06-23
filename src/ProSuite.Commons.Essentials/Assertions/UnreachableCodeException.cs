using System;
using System.Runtime.Serialization;

namespace ProSuite.Commons.Essentials.Assertions
{
	[Serializable]
	public class UnreachableCodeException : Exception
	{
		public UnreachableCodeException() { }

		public UnreachableCodeException(string message)
			: base(message) { }

		public UnreachableCodeException(string message, Exception e)
			: base(message, e) { }

		protected UnreachableCodeException(SerializationInfo info,
		                                   StreamingContext context)
			: base(info, context) { }
	}
}

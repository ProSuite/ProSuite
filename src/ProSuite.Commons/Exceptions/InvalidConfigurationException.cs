using System;
using System.Runtime.Serialization;

namespace ProSuite.Commons.Exceptions
{
	[Serializable]
	public class InvalidConfigurationException : Exception
	{
		public InvalidConfigurationException() { }

		public InvalidConfigurationException(string message)
			: base(message) { }

		public InvalidConfigurationException(string message, Exception e)
			: base(message, e) { }

		protected InvalidConfigurationException(SerializationInfo info,
		                                        StreamingContext context)
			: base(info, context) { }
	}
}
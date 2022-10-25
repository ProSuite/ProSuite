using System;
using System.Runtime.Serialization;

namespace ProSuite.Processing
{
	[Serializable]
	public class CartoConfigException : Exception
	{
		public CartoConfigException() { }

		public CartoConfigException(string message)
			: base(message) { }

		public CartoConfigException(string message, Exception e)
			: base(message, e) { }

		protected CartoConfigException(SerializationInfo info,
		                               StreamingContext context)
			: base(info, context) { }
	}
}

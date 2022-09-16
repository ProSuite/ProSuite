using System;
using System.Runtime.Serialization;

namespace ProSuite.Commons.Xml
{
	[Serializable]
	public class XmlDeserializationException : Exception
	{
		public XmlDeserializationException() { }

		public XmlDeserializationException(string message)
			: base(message) { }

		public XmlDeserializationException(string message, Exception e)
			: base(message, e) { }

		protected XmlDeserializationException(SerializationInfo info,
		                                      StreamingContext context)
			: base(info, context) { }
	}
}

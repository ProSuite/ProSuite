using System;
using System.Runtime.Serialization;
using ProSuite.Commons.Exceptions;

namespace ProSuite.Commons.IoC
{
	public class ComponentNotFoundException : InvalidConfigurationException
	{
		public ComponentNotFoundException() { }

		public ComponentNotFoundException(string message) : base(message) { }

		public ComponentNotFoundException(string message, Exception e) : base(message, e) { }

		protected ComponentNotFoundException(SerializationInfo info, StreamingContext context) :
			base(info, context) { }
	}
}

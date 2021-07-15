using System;
using System.Runtime.Serialization;

namespace ProSuite.DomainModel.Core.DataModel
{
	public abstract class ModelElementAccessException : Exception
	{
		protected ModelElementAccessException() { }

		protected ModelElementAccessException(string message)
			: base(message) { }

		protected ModelElementAccessException(string message, Exception e)
			: base(message, e) { }

		protected ModelElementAccessException(SerializationInfo info,
		                                      StreamingContext context)
			: base(info, context) { }
	}
}

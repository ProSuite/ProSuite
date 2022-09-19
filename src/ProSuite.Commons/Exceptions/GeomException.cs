using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Exceptions
{
	[Serializable]
	public class GeomException : Exception
	{
		public GeomException() { }

		public GeomException(string message)
			: base(message) { }

		public GeomException(string message, Exception e)
			: base(message, e) { }

		protected GeomException(SerializationInfo info,
		                        StreamingContext context)
			: base(info, context) { }

		[CanBeNull]
		public IList<string> ErrorGeometries { get; set; }
	}
}

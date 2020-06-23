using System;
using System.Runtime.Serialization;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Essentials.Assertions
{
	[Serializable]
	public class AssertionException : Exception
	{
		public AssertionException() { }

		public AssertionException([NotNull] string message)
			: base(message) { }

		public AssertionException([NotNull] string message, [CanBeNull] Exception e)
			: base(message, e) { }

		protected AssertionException([NotNull] SerializationInfo info, StreamingContext context)
			: base(info, context) { }
	}
}

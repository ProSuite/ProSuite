using System;
using System.Runtime.Serialization;

namespace ProSuite.Processing.Evaluation
{
	[Serializable]
	public class EvaluationException : Exception
	{
		public EvaluationException() { }

		public EvaluationException(string message)
			: base(message) { }

		public EvaluationException(string message, Exception e)
			: base(message, e) { }

		protected EvaluationException(SerializationInfo info,
		                              StreamingContext context)
			: base(info, context) { }
	}
}

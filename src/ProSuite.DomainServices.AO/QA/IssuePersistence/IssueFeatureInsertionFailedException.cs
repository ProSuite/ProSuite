using System;
using System.Runtime.Serialization;

namespace ProSuite.DomainServices.AO.QA.IssuePersistence
{
	public class IssueFeatureInsertionFailedException : Exception
	{
		public IssueFeatureInsertionFailedException() { }

		public IssueFeatureInsertionFailedException(string message)
			: base(message) { }

		public IssueFeatureInsertionFailedException(string message, Exception e)
			: base(message, e) { }

		protected IssueFeatureInsertionFailedException(SerializationInfo info,
		                                               StreamingContext context)
			: base(info, context) { }
	}
}

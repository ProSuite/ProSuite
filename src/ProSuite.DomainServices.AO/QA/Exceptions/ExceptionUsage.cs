using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.Exceptions
{
	public class ExceptionUsage
	{
		public ExceptionUsage([NotNull] ExceptionObject exceptionObject)
		{
			ExceptionObject = exceptionObject;
		}

		[NotNull]
		public ExceptionObject ExceptionObject { get; }

		public void AddUsage()
		{
			UsageCount++;
		}

		public int UsageCount { get; private set; }
	}
}

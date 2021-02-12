using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainServices.AO.QA.Issues;

namespace ProSuite.DomainServices.AO.QA.Exceptions
{
	public abstract class ExceptionDataset : IExceptionDataset
	{
		protected ExceptionDataset([NotNull] IObjectClass objectClass,
		                           [NotNull] IIssueTableFields fields,
		                           int exceptionCount)
		{
			Assert.ArgumentNotNull(objectClass, nameof(objectClass));
			Assert.ArgumentNotNull(fields, nameof(fields));

			ObjectClass = objectClass;
			Fields = fields;
			ExceptionCount = exceptionCount;
		}

		public IObjectClass ObjectClass { get; }

		public int ExceptionCount { get; }

		public IIssueTableFields Fields { get; }
	}
}

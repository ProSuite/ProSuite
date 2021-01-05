using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainServices.AO.QA.Issues;

namespace ProSuite.DomainServices.AO.QA.Exceptions
{
	public abstract class ExceptionDataset : IExceptionDataset
	{
		[CLSCompliant(false)]
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

		[CLSCompliant(false)]
		public IObjectClass ObjectClass { get; }

		public int ExceptionCount { get; }

		[CLSCompliant(false)]
		public IIssueTableFields Fields { get; }
	}
}

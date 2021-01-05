using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainServices.AO.QA.Issues;

namespace ProSuite.DomainServices.AO.QA.Exceptions
{
	public class ExceptionFeatureClass : ExceptionDataset, IExceptionFeatureClass
	{
		[CLSCompliant(false)]
		public ExceptionFeatureClass([NotNull] IFeatureClass featureClass,
		                             [NotNull] IIssueTableFields fields,
		                             int exceptionCount)
			: base(featureClass, fields, exceptionCount)
		{
			FeatureClass = featureClass;
		}

		[CLSCompliant(false)]
		public IFeatureClass FeatureClass { get; }
	}
}

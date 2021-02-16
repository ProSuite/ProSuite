using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainServices.AO.QA.Issues;

namespace ProSuite.DomainServices.AO.QA.Exceptions
{
	public class ExceptionFeatureClass : ExceptionDataset, IExceptionFeatureClass
	{
		public ExceptionFeatureClass([NotNull] IFeatureClass featureClass,
		                             [NotNull] IIssueTableFields fields,
		                             int exceptionCount)
			: base(featureClass, fields, exceptionCount)
		{
			FeatureClass = featureClass;
		}

		public IFeatureClass FeatureClass { get; }
	}
}

using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.Exceptions
{
	public interface IExceptionFeatureClass : IExceptionDataset
	{
		[NotNull]
		IFeatureClass FeatureClass { get; }
	}
}

using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.Exceptions
{
	[CLSCompliant(false)]
	public interface IExceptionFeatureClass : IExceptionDataset
	{
		[NotNull]
		IFeatureClass FeatureClass { get; }
	}
}

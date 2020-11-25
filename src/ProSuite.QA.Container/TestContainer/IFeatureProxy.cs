using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.TestContainer
{
	[CLSCompliant(false)]
	public interface IFeatureProxy
	{
		[NotNull]
		IFeature Inner { get; }
	}
}

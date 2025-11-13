using System;
using System.Threading.Tasks;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Env;

public interface IArcGISProEnvironment
{
	Task ZoomToAsync([NotNull] MapView mapView,
	                 [NotNull] Envelope extent,
	                 double minScaleDenominator = 0,
	                 double expansionFactor = 1.0,
	                 TimeSpan? duration = null);

	Task PanToAsync([NotNull] MapView mapView,
	                [NotNull] Envelope extent,
	                double minScaleDenominator = 0,
	                double expansionFactor = 1.0,
	                TimeSpan? duration = null);
}

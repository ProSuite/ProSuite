using System.Collections.Generic;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geometry.ChangeAlong
{
	public interface ISubcurveCalculator
	{
		/// <summary>
		/// Prepares the subcurve calculator for an upcoming calculation:
		/// - Sets the clip extent, minimum tolerance setting
		/// - Prepares the subcurve filter to be applied to the calculated curves.
		/// </summary>
		/// <param name="sourceFeatures"></param>
		/// <param name="targetFeatures"></param>
		/// <param name="processingExtent"></param>
		/// <param name="filterOptions"></param>
		void Prepare([NotNull] IEnumerable<IFeature> sourceFeatures,
		             IList<IFeature> targetFeatures,
		             [CanBeNull] IEnvelope processingExtent,
		             [NotNull] ReshapeCurveFilterOptions filterOptions);

		/// <summary>
		/// Calculates subcurves and adds them to the provided result list.
		/// </summary>
		/// <param name="sourceGeometry">The original source geometry</param>
		/// <param name="targetPolyline">The (potentially clipped) target converted to a polyline</param>
		/// <param name="resultList"></param>
		/// <param name="trackCancel"></param>
		/// <returns></returns>
		ReshapeAlongCurveUsability CalculateSubcurves(
			[NotNull] IGeometry sourceGeometry,
			[NotNull] IPolyline targetPolyline,
			[NotNull] IList<CutSubcurve> resultList,
			[CanBeNull] ITrackCancel trackCancel);

		/// <summary>
		/// Whether the minimum tolerance should be applied to ensure even very small differences yield a subcurve.
		/// </summary>
		bool UseMinimumTolerance { get; set; }

		/// <summary>
		/// The new custom tolerance that is more flexible than the minimum vs standard tolerance
		/// setting (which could be replaced in the future).
		/// </summary>
		double? CustomTolerance { get; set; }

		/// <summary>
		/// The subcurve filter that determines the IsFiltered property on the resulting subcurves.
		/// The prepared filter should be set before calculating the subcurves.
		/// </summary>
		SubcurveFilter SubcurveFilter { get; set; }

		/// <summary>
		/// The extent that can be used to clip input geometries for better performance.
		/// </summary>
		IEnvelope ClipExtent { get; set; }

		/// <summary>
		/// Whether the provided geometry type can be used as source geometry.
		/// </summary>
		/// <param name="geometryType"></param>
		/// <returns></returns>
		bool CanUseSourceGeometryType(esriGeometryType geometryType);
	}
}

using System.Collections.Generic;
using System.Threading;
using ArcGIS.Core.Data;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;

namespace ProSuite.Commons.AGP.Core.GeometryProcessing.ChangeAlong;

/// <summary>
/// Abstraction for the reshaping and cutting of features along existing features.
/// </summary>
public interface IChangeAlongService
{
	ChangeAlongCurves CalculateReshapeLines(
		[NotNull] IList<Feature> sourceFeatures,
		[NotNull] IList<Feature> targetFeatures,
		TargetBufferOptions targetBufferOptions,
		ReshapeCurveFilterOptions curveFilterOptions,
		double? customTolerance,
		CancellationToken cancellationToken);

	ChangeAlongCurves CalculateCutLines([NotNull] IList<Feature> sourceFeatures,
	                                    [NotNull] IList<Feature> targetFeatures,
	                                    TargetBufferOptions targetBufferOptions,
	                                    IBoundedXY clipExtent,
	                                    double? customTolerance,
	                                    ZValueSource zValueSource,
	                                    CancellationToken cancellationToken);

	List<ResultFeature> ApplyReshapeLines(
		[NotNull] IList<Feature> sourceFeatures,
		[NotNull] IList<Feature> targetFeatures,
		[NotNull] IList<CutSubcurve> selectedReshapeLines,
		[NotNull] TargetBufferOptions targetBufferOptions,
		[NotNull] ReshapeCurveFilterOptions curveFilterOptions,
		double? customTolerance,
		bool insertVerticesInTarget,
		CancellationToken cancellationToken,
		out ChangeAlongCurves newChangeAlongCurves);

	List<ResultFeature> ApplyCutLines([NotNull] IList<Feature> sourceFeatures,
	                                  [NotNull] IList<Feature> targetFeatures,
	                                  [NotNull] IList<CutSubcurve> selectedReshapeLines,
	                                  TargetBufferOptions targetBufferOptions,
	                                  IBoundedXY clipExtent,
	                                  double? customTolerance,
	                                  ZValueSource zValueSource,
	                                  bool insertVerticesInTarget,
	                                  CancellationToken cancellationToken,
	                                  out ChangeAlongCurves newChangeAlongCurves);
}

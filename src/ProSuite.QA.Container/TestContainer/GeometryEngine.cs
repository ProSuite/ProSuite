using System;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Com;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.TestContainer
{
	internal sealed class GeometryEngine : IGeometryEngine
	{
		private readonly IEnvelope _extentTemplateSource = new EnvelopeClass();
		private readonly IEnvelope _extentTemplateTarget = new EnvelopeClass();

		private IGeometry _sourceGeometry;
		private IGeometry _targetGeometry;

		private const string _relationInteriorIntersects = "RELATE (G1, G2, 'T********')";

		#region IGeometryEngine Members

		public bool AssumeEnvelopeIntersects { get; set; } = true;

		public void SetSourceGeometry(IGeometry sourceGeometry)
		{
			Assert.ArgumentNotNull(sourceGeometry, nameof(sourceGeometry));

			_sourceGeometry = sourceGeometry;

			GeometryUtils.AllowIndexing(_sourceGeometry);
		}

		public void SetTargetGeometry(IGeometry targetGeometry)
		{
			_targetGeometry = targetGeometry;

			GeometryUtils.AllowIndexing(_targetGeometry);
		}

		public bool EvaluateRelation(IFeatureClassFilter spatialFilter)
		{
			Assert.ArgumentNotNull(spatialFilter, nameof(spatialFilter));
			Assert.NotNull(_sourceGeometry, nameof(_sourceGeometry));
			Assert.NotNull(_targetGeometry, nameof(_targetGeometry));

			esriSpatialRelEnum relation = spatialFilter.SpatialRelationship;

			switch (relation)
			{
				case esriSpatialRelEnum.esriSpatialRelIntersects:
					return EvaluateIntersects(_sourceGeometry, _targetGeometry);

				case esriSpatialRelEnum.esriSpatialRelTouches:
					return EvaluateTouches(_sourceGeometry, _targetGeometry);

				case esriSpatialRelEnum.esriSpatialRelOverlaps:
					return EvaluateOverlaps(_sourceGeometry, _targetGeometry);

				case esriSpatialRelEnum.esriSpatialRelCrosses:
					return EvaluateCrosses(_sourceGeometry, _targetGeometry);

				case esriSpatialRelEnum.esriSpatialRelWithin:
					return EvaluateWithin(_sourceGeometry, _targetGeometry);

				case esriSpatialRelEnum.esriSpatialRelContains:
					return EvaluateContains(_sourceGeometry, _targetGeometry);

				case esriSpatialRelEnum.esriSpatialRelRelation:
					return EvaluateRelation(_sourceGeometry, _targetGeometry,
					                        spatialFilter.SpatialRelDescription);

				case esriSpatialRelEnum.esriSpatialRelEnvelopeIntersects:
					return EvaluateEnvelopeIntersects(_sourceGeometry, _targetGeometry);

				case esriSpatialRelEnum.esriSpatialRelUndefined:
				case esriSpatialRelEnum.esriSpatialRelIndexIntersects:
					throw new ArgumentException(
						string.Format("Filter uses unsupported spatial relation: {0}", relation),
						nameof(spatialFilter));

				default:
					throw new ArgumentException(
						string.Format("Filter uses unknown spatial relation: {0}", relation),
						nameof(spatialFilter));
			}
		}

		#endregion

		private bool EvaluateEnvelopeIntersects([NotNull] IGeometry source,
		                                        [NotNull] IGeometry target)
		{
			if (AssumeEnvelopeIntersects)
			{
				return true;
			}

			if (source == target)
			{
				return true;
			}

			source.QueryEnvelope(_extentTemplateSource);
			target.QueryEnvelope(_extentTemplateTarget);

			return EvaluateIntersects(_extentTemplateSource, _extentTemplateTarget);
		}

		private bool EvaluateRelation([NotNull] IGeometry source,
		                              [NotNull] IGeometry target,
		                              [NotNull] string relationDescription)
		{
			if (source == target)
			{
				return false;
			}

			var sourceRelOp = (IRelationalOperator) source;

			if (sourceRelOp.Disjoint(target))
			{
				return false;
			}

			// TODO consolidate optimization of touches with QaMustIntersectMatrixOther
			if (IsInteriorIntersectionRelation(relationDescription))
			{
				// use simpler and more robust implementation for 'interior intersects': 
				// - not disjoint (already checked) and not touching
				// 9IM matrix for interior intersects can cause exceptions (0x80040239)
				// in IRelationalOperator.Relation in special situations

				return ! Touches(source, target);
			}

			// NOTE: 
			// - ****T**** is not fulfilled when a line connects to the end point of a closed polyline
			//   (maybe the closed polyline is not considered to have a boundary?)
			return sourceRelOp.Relation(target, relationDescription);
		}

		private static bool IsInteriorIntersectionRelation(
			[NotNull] string relationDescription)
		{
			return string.Equals(relationDescription.Trim(),
			                     _relationInteriorIntersects,
			                     StringComparison.OrdinalIgnoreCase);
		}

		private bool EvaluateTouches([NotNull] IGeometry source,
		                             [NotNull] IGeometry target)
		{
			if (source == target)
			{
				return false;
			}

			return ! ((IRelationalOperator) source).Disjoint(target) &&
			       Touches(source, target);
		}

		private static bool EvaluateOverlaps([NotNull] IGeometry source,
		                                     [NotNull] IGeometry target)
		{
			if (source == target)
			{
				return false;
			}

			var sourceRelOp = (IRelationalOperator) source;

			return ! sourceRelOp.Disjoint(target) && sourceRelOp.Overlaps(target);
		}

		private static bool EvaluateContains([NotNull] IGeometry source,
		                                     [NotNull] IGeometry target)
		{
			if (source == target)
			{
				return false;
			}

			var sourceRelOp = (IRelationalOperator) source;

			return ! sourceRelOp.Disjoint(target) && sourceRelOp.Contains(target);
		}

		private static bool EvaluateWithin([NotNull] IGeometry source,
		                                   [NotNull] IGeometry target)
		{
			if (source == target)
			{
				return false;
			}

			// return EvaluateContains(target, source);
			var sourceRelOp = (IRelationalOperator) source;

			return ! sourceRelOp.Disjoint(target) && sourceRelOp.Within(target);
		}

		private static bool EvaluateIntersects([NotNull] IGeometry source,
		                                       [NotNull] IGeometry target)
		{
			if (source == target)
			{
				return true;
			}

			return ! ((IRelationalOperator) source).Disjoint(target);
		}

		private static bool EvaluateCrosses([NotNull] IGeometry source,
		                                    [NotNull] IGeometry target)
		{
			if (source == target)
			{
				return false;
				// return GeometryUtils.IsSelfIntersecting(source);
			}

			var sourceRelOp = (IRelationalOperator) source;

			// Crosses is extremely expensive, check for Disjoint first 
			return ! sourceRelOp.Disjoint(target) && sourceRelOp.Crosses(target);
		}

		private bool Touches([NotNull] IGeometry source, [NotNull] IGeometry target)
		{
			IGeometry clippedSource = null;
			IGeometry clippedTarget = null;

			try
			{
				if (UseEnvelopeIntersection(source, target))
				{
					IEnvelope envelopeIntersection = GetEnvelopeIntersection(source, target);

					if (envelopeIntersection.IsEmpty)
					{
						return false;
					}

					clippedSource = GetClipped(source, envelopeIntersection);
					if (clippedSource != null)
					{
						source = clippedSource;
					}

					clippedTarget = GetClipped(target, envelopeIntersection);
					if (clippedTarget != null)
					{
						target = clippedTarget;
					}
				}

				return GeometryUtils.Touches(source, target);
			}
			finally
			{
				if (clippedSource != null)
				{
					ComUtils.ReleaseComObject(clippedSource);
				}

				if (clippedTarget != null)
				{
					ComUtils.ReleaseComObject(clippedTarget);
				}
			}
		}

		private static bool UseEnvelopeIntersection([NotNull] IGeometry source,
		                                            [NotNull] IGeometry target,
		                                            int minimumTotalPointCount = 100)
		{
			// use if source and target are both either polygons or polylines, and if total point count exceeds limit

			if (! (source is IPolycurve && target is IPolycurve))
			{
				return false;
			}

			int totalPointCount = GeometryUtils.GetPointCount(source) +
			                      GeometryUtils.GetPointCount(target);

			return totalPointCount >= minimumTotalPointCount;
		}

		[NotNull]
		private IEnvelope GetEnvelopeIntersection([NotNull] IGeometry source,
		                                          [NotNull] IGeometry target)
		{
			double xyToleranceSource =
				((ISpatialReferenceTolerance) source.SpatialReference).XYTolerance;
			double xyToleranceTarget =
				((ISpatialReferenceTolerance) target.SpatialReference).XYTolerance;

			const int toleranceFactor = 100;

			source.QueryEnvelope(_extentTemplateSource);
			_extentTemplateSource.Expand(xyToleranceSource * toleranceFactor,
			                             xyToleranceSource * toleranceFactor,
			                             asRatio: false);

			target.QueryEnvelope(_extentTemplateTarget);
			_extentTemplateTarget.Expand(xyToleranceTarget * toleranceFactor,
			                             xyToleranceTarget * toleranceFactor,
			                             asRatio: false);

			// use the envelope with the smaller XY tolerance as left value
			IEnvelope leftValue;
			IEnvelope rightValue;
			if (xyToleranceSource <= xyToleranceTarget)
			{
				leftValue = _extentTemplateSource;
				rightValue = _extentTemplateTarget;
			}
			else
			{
				leftValue = _extentTemplateTarget;
				rightValue = _extentTemplateSource;
			}

			leftValue.Intersect(rightValue);

			return leftValue;
		}

		[CanBeNull]
		private static IGeometry GetClipped([NotNull] IGeometry input,
		                                    [NotNull] IEnvelope envelope)
		{
			var polygon = input as IPolygon;
			if (polygon != null)
			{
				return GeometryUtils.GetClippedPolygon(polygon, envelope);
			}

			var polyline = input as IPolyline;
			if (polyline != null)
			{
				return GeometryUtils.GetClippedPolyline(polyline, envelope);
			}

			return null;
		}
	}
}

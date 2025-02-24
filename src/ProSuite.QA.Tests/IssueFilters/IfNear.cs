using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.AO.Geometry.Proxy;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Geom;
using ProSuite.QA.Core.TestCategories;
using ProSuite.QA.Container;
using ProSuite.QA.Tests.Coincidence;
using ProSuite.QA.Tests.Documentation;
using Pnt = ProSuite.Commons.Geom.Pnt;

namespace ProSuite.QA.Tests.IssueFilters
{
	[UsedImplicitly]
	public class IfNear : IssueFilter
	{
		private readonly double _near;

		private readonly double _tolerance = 0;
		private bool _is3D;

		private IList<QueryFilterHelper> _filterHelpers;
		private IList<IFeatureClassFilter> _spatialFilters;

		[DocIf(nameof(DocIfStrings.IfNear_0))]
		public IfNear(
			[DocIf(nameof(DocIfStrings.IfNear_featureClass))]
			IReadOnlyFeatureClass featureClass,
			[DocIf(nameof(DocIfStrings.IfNear_near))]
			double near)
			: base(new[] { featureClass })
		{
			_near = near;
		}

		[InternallyUsedTest]
		public IfNear([NotNull] IfNearDefinition definition)
			: this((IReadOnlyFeatureClass) definition.FeatureClass, definition.Near) { }

		public override bool Check(QaErrorEventArgs error)
		{
			IGeometry errorGeometry = error.QaError.Geometry;

			if (errorGeometry == null || errorGeometry.IsEmpty)
			{
				return false;
			}

			EnsureFilters();

			IReadOnlyTable table = InvolvedTables[0];
			IFeatureClassFilter filter = _spatialFilters[0];
			QueryFilterHelper helper = _filterHelpers[0];

			IEnvelope searchBox = GeometryFactory.Clone(errorGeometry.Envelope);
			searchBox.Expand(_near, _near, false);
			filter.FilterGeometry = searchBox;

			bool releaseOnDispose = false;
			Dictionary<SegmentHull, List<double[]>> uncompleteSegs = null;
			Pnt errorPnt = null;
			IBox nearPntBox = null;
			List<Tuple<Pnt, IBox>> errorPnts = null;
			if (errorGeometry is ISegmentCollection curve)
			{
				IIndexedSegments errorSegs = new SegmentSearcher(curve, releaseOnDispose);
				uncompleteSegs = new Dictionary<SegmentHull, List<double[]>>();
				foreach (var errorSeg in errorSegs.GetSegments())
				{
					SegmentHull hull = errorSeg.CreateHull(_near);
					uncompleteSegs.Add(hull, new List<double[]>());
				}
			}
			else if (errorGeometry is IPoint p)
			{
				errorPnt = ProxyUtils.CreatePoint3D(p);
				nearPntBox = GeomUtils.GetExpanded(errorPnt, _near);
			}
			else if (errorGeometry is IMultipoint multipoint)
			{
				errorPnts = new List<Tuple<Pnt, IBox>>();
				foreach (IPoint point in GeometryUtils.GetPoints(multipoint))
				{
					Pnt singlePnt = ProxyUtils.CreatePoint3D(point);
					IBox singlePntBox = GeomUtils.GetExpanded(singlePnt, _near);

					errorPnts.Add(new Tuple<Pnt, IBox>(singlePnt, singlePntBox));
				}
			}
			else
			{
				throw new NotImplementedException(
					$"Unhandled geometry type {errorGeometry.GeometryType}");
			}

			foreach (var row in Search(table, filter, helper))
			{
				var feat = (IReadOnlyFeature) row;

				IIndexedSegments geom =
					IndexedSegmentUtils.GetIndexedGeometry(feat, releaseOnDispose);

				if (uncompleteSegs != null)
				{
					HandleUncompleteSegs(geom, uncompleteSegs);
					if (uncompleteSegs.Count == 0)
					{
						return true;
					}
				}
				else if (errorPnt != null)
				{
					if (HandlePoint(geom, errorPnt, nearPntBox))
					{
						return true;
					}
				}
				else
				{
					Assert.NotNull(errorPnts);

					if (HandlePoints(geom, errorPnts))
					{
						return true;
					}
				}
			}

			return false;
		}

		private bool HandlePoint(IIndexedSegments geom, Pnt p, IBox nearBox)
		{
			foreach (SegmentProxy neighborSegment in geom.GetSegments(nearBox))
			{
				neighborSegment.QueryOffset(p, out double offset, out _);
				if (Math.Abs(offset) < _near)
				{
					return true;
				}
			}

			return false;
		}

		private bool HandlePoints(IIndexedSegments geom, List<Tuple<Pnt, IBox>> errorPnts)
		{
			foreach ((Pnt p, IBox box) in errorPnts)
			{
				if (! HandlePoint(geom, p, box))
				{
					return false;
				}
			}

			return true;
		}

		private void HandleUncompleteSegs(IIndexedSegments geom,
		                                  Dictionary<SegmentHull, List<double[]>> uncompletedHulls)
		{
			List<SegmentHull> completeds = new List<SegmentHull>();

			foreach (var pair in uncompletedHulls)
			{
				SegmentHull hull = pair.Key;
				List<double[]> covered = pair.Value;

				IBox nearBox = GeomUtils.GetExpanded(hull.Segment.Extent, _near);

				foreach (var neighborSegment in geom.GetSegments(nearBox))
				{
					IBox neighborBox = neighborSegment.Extent;

					if (! nearBox.Intersects(neighborBox))
					{
						continue;
					}

					var neighborHull = neighborSegment.CreateHull(0);

					SegmentPair segmentPair = SegmentPair.Create(hull, neighborHull, _is3D);
					segmentPair.CutCurveHull(_tolerance,
					                         out IList<double[]> limits, out _, out _,
					                         out bool coincident);

					if (coincident)
					{
						completeds.Add(hull);
						break;
					}

					if (limits.Count > 0)
					{
						covered.AddRange(limits);
						if (IsComplete(covered))
						{
							completeds.Add(hull);
							break;
						}
					}
				}
			}

			foreach (var completed in completeds)
			{
				uncompletedHulls.Remove(completed);
			}
		}

		private void EnsureFilters()
		{
			if (_spatialFilters == null)
			{
				CopyFilters(out _spatialFilters, out _filterHelpers);
				_spatialFilters[0].SpatialRelationship =
					esriSpatialRelEnum.esriSpatialRelEnvelopeIntersects;
				_filterHelpers[0].FullGeometrySearch = true;
			}
		}

		private bool IsComplete(List<double[]> covered)
		{
			covered.Sort((x, y) => x[0].CompareTo(y[0]));

			double x0 = 0;
			foreach (double[] limits in covered)
			{
				if (limits[0] > x0)
				{
					return false;
				}

				x0 = Math.Max(x0, limits[1]);
			}

			if (x0 < 1)
			{
				return false;
			}

			return true;
		}
	}
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AO.Geometry.LinearNetwork
{
	/// <summary>
	/// Base class providing feature access for 2D-linear network feature finders.
	/// </summary>
	public abstract class LinearNetworkFeatureFinderBase : ILinearNetworkFeatureFinder
	{
		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		#region Implementation of ILinearNetworkFeatureFinder

		public double SearchTolerance { get; set; }

		public IList<IFeature> TargetFeatureCandidates { get; protected set; }

		public void InvalidateTargetFeatureCache()
		{
			TargetFeatureCandidates = null;
		}

		public IList<IFeature> FindEdgeFeaturesAt(IPoint point,
		                                          Predicate<IFeature> predicate = null)
		{
			return FindPolylinesAt(point, predicate);
		}

		public IList<IFeature> FindJunctionFeaturesAt(IPoint point)
		{
			var geometryTypes = new[] {esriGeometryType.esriGeometryPoint};

			return FindFeaturesAt(point, geometryTypes);
		}

		public IList<IFeature> GetConnectedEdgeFeatures(IFeature toEdgeFeature,
		                                                IPolyline edgeGeometry,
		                                                LineEnd atEnd)
		{
			Assert.ArgumentNotNull(toEdgeFeature, nameof(toEdgeFeature));

			IPolyline polyline = edgeGeometry ?? (IPolyline) toEdgeFeature.Shape;

			var result = new List<IFeature>();

			IPoint fromPoint = polyline.FromPoint;
			IPoint toPoint = polyline.ToPoint;

			if (atEnd == LineEnd.From || atEnd == LineEnd.Both)
			{
				result.AddRange(FindPolylinesAt(
					                fromPoint, IsConnected(toEdgeFeature, fromPoint)));
			}

			if (atEnd == LineEnd.To || atEnd == LineEnd.Both)
			{
				result.AddRange(FindPolylinesAt(
					                toPoint, IsConnected(toEdgeFeature, toPoint)));
			}

			return result;
		}

		public void CacheTargetFeatureCandidates(IGeometry searchGeometry)
		{
			if (searchGeometry.IsEmpty)
			{
				return;
			}

			double xyTolerance = GeometryUtils.GetXyTolerance(searchGeometry);

			double expansion = SearchTolerance > xyTolerance
				                   ? SearchTolerance
				                   : xyTolerance;

			// Searching by envelope does not use the tolerance!
			IEnvelope searchEnvelope = GeometryUtils.GetExpandedEnvelope(
				searchGeometry, expansion);

			Stopwatch watch = _msg.DebugStartTiming();

			var geometryTypes = new[]
			                    {
				                    esriGeometryType.esriGeometryPolyline,
				                    esriGeometryType.esriGeometryPoint
			                    };

			TargetFeatureCandidates = ReadFeatures(searchEnvelope, geometryTypes);

			_msg.DebugStopTiming(watch, "Cached network features.");
		}

		#endregion

		protected abstract IList<IFeature> ReadFeaturesCore(
			[NotNull] IGeometry searchGeometry,
			[NotNull] ICollection<esriGeometryType> geometryTypes);

		protected static List<IFeature> SearchFeatureClasses(
			[NotNull] IEnumerable<LinearNetworkClassDef> linearNetworkClasses,
			[NotNull] IGeometry searchGeometry,
			[NotNull] ICollection<esriGeometryType> geometryTypes,
			[CanBeNull] IWorkspace alternateVersion = null)
		{
			var foundFeatures = new List<IFeature>();

			foreach (LinearNetworkClassDef networkClassDef in linearNetworkClasses)
			{
				IFeatureClass featureClass = networkClassDef.FeatureClass;

				if (alternateVersion != null &&
				    ! WorkspaceUtils.IsSameVersion(alternateVersion,
				                                   DatasetUtils.GetWorkspace(featureClass)))
				{
					featureClass =
						DatasetUtils.OpenFeatureClass(alternateVersion,
						                              DatasetUtils.GetName(featureClass));
				}

				if (! geometryTypes.Contains(featureClass.ShapeType))
				{
					continue;
				}

				IQueryFilter filter =
					GdbQueryUtils.CreateSpatialFilter(featureClass, searchGeometry);

				filter.WhereClause = networkClassDef.WhereClause;

				foundFeatures.AddRange(GdbQueryUtils.GetFeatures(featureClass, filter, false));
			}

			return foundFeatures;
		}

		private IList<IFeature> ReadFeatures(
			[NotNull] IGeometry searchGeometry,
			[NotNull] ICollection<esriGeometryType> geometryTypes)
		{
			if (SearchTolerance > GeometryUtils.GetXyTolerance(searchGeometry))
			{
				IEnvelope searchEnvelope = searchGeometry.Envelope;

				searchEnvelope.Expand(SearchTolerance, SearchTolerance, false);

				searchGeometry = searchEnvelope;
			}

			return ReadFeaturesCore(searchGeometry, geometryTypes);
		}

		private IEnumerable<IFeature> GetFeaturesFromCache(IList<IFeature> cache,
		                                                   IPoint searchPoint,
		                                                   IList<esriGeometryType> geometryTypes)
		{
			if (SearchTolerance > GeometryUtils.GetXyTolerance(searchPoint))
			{
				searchPoint = GeometryFactory.Clone(searchPoint);
				GeometryUtils.SetXyTolerance(searchPoint, SearchTolerance);
			}

			IEnumerable<IFeature> targetFeatures = cache.Where(
				f =>
				{
					IGeometry shape = f.Shape;

					bool canBeUsed = geometryTypes.Contains(shape.GeometryType) &&
					                 GeometryUtils.Intersects(searchPoint, shape);

					Marshal.ReleaseComObject(shape);

					return canBeUsed;
				});

			return targetFeatures;
		}

		[NotNull]
		private static Predicate<IFeature> IsConnected([NotNull] IFeature toEdgeFeature,
		                                               [NotNull] IPoint atPoint)
		{
			return feature =>
				GeometryUtils.Touches(feature.Shape, atPoint) &&
				! GdbObjectUtils.IsSameObject(feature, toEdgeFeature,
				                              ObjectClassEquality.SameTableSameVersion);
		}

		private IList<IFeature> FindPolylinesAt(
			[NotNull] IPoint searchPoint,
			[CanBeNull] Predicate<IFeature> predicate)
		{
			var geometryTypes = new[] {esriGeometryType.esriGeometryPolyline};

			return FindFeaturesAt(searchPoint,
			                      geometryTypes, predicate);
		}

		private IList<IFeature> FindFeaturesAt(
			[NotNull] IPoint searchPoint,
			[NotNull] IList<esriGeometryType> geometryTypes,
			[CanBeNull] Predicate<IFeature> predicate = null)
		{
			IEnumerable<IFeature> targetFeatures;

			if (TargetFeatureCandidates != null)
			{
				targetFeatures =
					GetFeaturesFromCache(TargetFeatureCandidates, searchPoint, geometryTypes);
			}
			else
			{
				targetFeatures = ReadFeatures(searchPoint, geometryTypes);
			}

			IList<IFeature> result = new List<IFeature>(
				predicate == null
					? targetFeatures
					: targetFeatures.Where(targetFeature => predicate(targetFeature)));

			return result;
		}
	}
}

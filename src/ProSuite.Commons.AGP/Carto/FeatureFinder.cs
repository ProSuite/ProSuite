using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Carto
{
	public class FeatureFinder
	{
		private readonly MapView _mapView;

		public FeatureFinder(
			MapView mapView,
			TargetFeatureSelection? featureSelectionType = null)
		{
			_mapView = mapView;
			FeatureSelectionType = featureSelectionType;
		}

		public SpatialRelationship SpatialRelationship { get; set; } =
			SpatialRelationship.Intersects;

		/// <summary>
		/// The selection type that determines which layers are searched.
		/// </summary>
		[CanBeNull]
		public TargetFeatureSelection? FeatureSelectionType { get; set; }

		/// <summary>
		/// The selected features, relevant only for
		/// <see cref="FeatureSelectionType"/> with value <see cref="TargetFeatureSelection.SameClass"></see>
		/// </summary>
		public ICollection<Feature> SelectedFeatures { get; set; }

		public bool DelayFeatureFetching { get; set; }

		public IEnumerable<FeatureClassSelection> FindFeaturesByLayer(
			[NotNull] Geometry searchGeometry,
			[CanBeNull] Predicate<FeatureLayer> layerPredicate = null,
			[CanBeNull] Predicate<Feature> featurePredicate = null,
			CancelableProgressor cancelableProgressor = null)
		{
			Predicate<FeatureLayer> predicate = fl => IsLayerApplicable(fl, layerPredicate);

			IEnumerable<FeatureLayer> featureLayers = MapUtils.GetLayers(predicate, _mapView);

			return FindFeaturesByLayer(featureLayers, searchGeometry, featurePredicate,
			                           cancelableProgressor);
		}

		public IEnumerable<FeatureClassSelection> FindFeaturesByLayer(
			IEnumerable<FeatureLayer> featureLayers,
			Geometry searchGeometry,
			Predicate<Feature> featurePredicate,
			CancelableProgressor cancelableProgressor)
		{
			foreach (FeatureLayer featureLayer in featureLayers)
			{
				if (cancelableProgressor != null
				    && cancelableProgressor.CancellationToken.IsCancellationRequested)
				{
					yield break;
				}

				if (featurePredicate == null)
				{
					featurePredicate = f => true;
				}

				QueryFilter filter =
					GdbQueryUtils.CreateSpatialFilter(searchGeometry, SpatialRelationship);

				FeatureClass featureClass = featureLayer.GetFeatureClass();

				if (DelayFeatureFetching)
				{
					filter.SubFields = featureClass.GetDefinition().GetObjectIDField();

					List<long> objectIds =
						LayerUtils.SearchObjectIds(featureLayer, filter, featurePredicate).ToList();

					if (objectIds.Count > 0)
					{
						yield return new FeatureClassSelection(
							featureClass, objectIds, featureLayer);
					}
				}
				else
				{
					List<Feature> features =
						LayerUtils.SearchRows(featureLayer, filter, featurePredicate).ToList();

					if (features.Count > 0)
					{
						yield return
							new FeatureClassSelection(featureClass, features, featureLayer);
					}
				}
			}
		}

		public IEnumerable<FeatureClassSelection> FindFeaturesByFeatureClass(
			[NotNull] Geometry searchGeometry,
			[CanBeNull] Predicate<FeatureLayer> layerPredicate = null,
			[CanBeNull] Predicate<Feature> featurePredicate = null,
			CancelableProgressor cancelableProgressor = null)
		{
			Predicate<FeatureLayer> predicate = fl => IsLayerApplicable(fl, layerPredicate);

			IEnumerable<FeatureLayer> featureLayers = MapUtils.GetLayers(predicate, _mapView);

			return FindFeaturesByFeatureClass(featureLayers, searchGeometry, featurePredicate,
			                                  cancelableProgressor);
		}

		/// <summary>
		/// Finds the features in the map by the specified criteria, grouped by feature class
		/// </summary>
		/// <param name="searchGeometry">The search geometry</param>
		/// <param name="featureLayers">An extra layer predicate that allows for a more
		/// fine-granular determination of the layers to be searched.</param>
		/// <param name="featurePredicate">An extra feature predicate that allows to determine
		/// criteria on the feature level.</param>
		/// <param name="cancelableProgressor"></param>
		/// <returns></returns>
		public IEnumerable<FeatureClassSelection> FindFeaturesByFeatureClass(
			IEnumerable<FeatureLayer> featureLayers,
			Geometry searchGeometry,
			Predicate<Feature> featurePredicate,
			CancelableProgressor cancelableProgressor)
		{
			IEnumerable<IGrouping<IntPtr, FeatureLayer>> layersGroupedByClass =
				featureLayers.GroupBy(fl => fl.GetFeatureClass().Handle);

			foreach (var layersInClass in layersGroupedByClass)
			{
				// One query per distinct definition query, then make OIDs distinct

				FeatureClass featureClass = null;
				FeatureLayer featureLayer = null;
				List<Feature> features = new List<Feature>();
				foreach (IGrouping<string, FeatureLayer> layers in layersInClass.GroupBy(
					         fl => fl.DefinitionQuery))
				{
					if (cancelableProgressor != null
					    && cancelableProgressor.CancellationToken.IsCancellationRequested)
					{
						yield break;
					}

					featureLayer = layers.First();
					featureClass = featureLayer.GetFeatureClass();

					QueryFilter filter =
						GdbQueryUtils.CreateSpatialFilter(searchGeometry, SpatialRelationship);
					filter.WhereClause = layers.Key;

					IEnumerable<Feature> foundFeatures = GdbQueryUtils
					                                     .GetFeatures(featureClass, filter, false)
					                                     .Where(f => featurePredicate == null ||
						                                            featurePredicate(f));
					features.AddRange(foundFeatures);
				}

				if (featureClass != null && features.Count > 0)
				{
					yield return new FeatureClassSelection(
						featureClass, features.DistinctBy(f => f.GetObjectID()).ToList(),
						featureLayer);
				}
			}
		}

		/// <summary>
		/// Finds the distinct visible features in the map that intersect the selected
		/// features and that fulfill the target-selection-type criteria.
		/// </summary>
		/// <param name="intersectingSelectedFeatures">The selected features to use in the search for
		/// other visible features intersecting any of the selected features. When using target selection
		/// type SameClass these features are used to determine whether a potential target feature comes
		/// from the same class as one of them.</param>
		/// <param name="layerPredicate">An additional layer predicate to be tested.</param>
		/// <param name="extent">The area of interest to which the search can be limited</param>
		/// <param name="cancelableProgressor">The progress/cancel tracker.</param>
		/// <remarks>The <see cref="FeatureSelectionType"/> most not be Undefined and must not be
		/// SelectedFeatures.</remarks>
		/// <returns>The found features in the same spatial reference as the provided selected features</returns>
		[NotNull]
		public IEnumerable<FeatureClassSelection> FindIntersectingFeaturesByFeatureClass(
			[NotNull] Dictionary<MapMember, List<long>> intersectingSelectedFeatures,
			[CanBeNull] Predicate<FeatureLayer> layerPredicate = null,
			[CanBeNull] Envelope extent = null,
			[CanBeNull] CancelableProgressor cancelableProgressor = null)
		{
			Assert.ArgumentCondition(
				FeatureSelectionType != TargetFeatureSelection.SelectedFeatures &&
				FeatureSelectionType != TargetFeatureSelection.Undefined,
				"Unsupported target selection type");

			SelectedFeatures = MapUtils.GetFeatures(intersectingSelectedFeatures).ToList();

			var searchGeometry = GetSearchGeometry(SelectedFeatures, extent);

			if (searchGeometry == null)
			{
				yield break;
			}

			foreach (var classSelection in FindFeaturesByFeatureClass(
				         searchGeometry, layerPredicate, null, cancelableProgressor))
			{
				yield return classSelection;
			}
		}

		private bool IsLayerApplicable(
			[CanBeNull] FeatureLayer featureLayer,
			[CanBeNull] Predicate<FeatureLayer> layerPredicate)
		{
			return IsLayerApplicable(featureLayer, layerPredicate, FeatureSelectionType,
			                         SelectedFeatures);
		}

		private static bool IsLayerApplicable(
			[CanBeNull] FeatureLayer featureLayer,
			[CanBeNull] Predicate<FeatureLayer> layerPredicate,
			TargetFeatureSelection? targetSelectionType,
			[CanBeNull] ICollection<Feature> selectedFeatures)
		{
			if (featureLayer?.GetFeatureClass() == null)
			{
				return false;
			}

			if (layerPredicate != null && ! layerPredicate(featureLayer))
			{
				return false;
			}

			if (targetSelectionType == null)
			{
				return true;
			}

			if ((targetSelectionType == TargetFeatureSelection.VisibleEditableFeatures ||
			     targetSelectionType == TargetFeatureSelection.VisibleSelectableEditableFeatures) &&
			    ! featureLayer.IsEditable)
			{
				return false;
			}

			if ((targetSelectionType == TargetFeatureSelection.VisibleSelectableFeatures ||
			     targetSelectionType == TargetFeatureSelection.VisibleSelectableEditableFeatures) &&
			    ! featureLayer.IsSelectable)
			{
				return false;
			}

			if (! featureLayer.IsVisible)
			{
				return false;
			}

			if (targetSelectionType == TargetFeatureSelection.SameClass &&
			    ! Assert.NotNull(selectedFeatures).Any(
				    f => DatasetUtils.IsSameClass(f.GetTable(), featureLayer.GetTable())))
			{
				return false;
			}

			return true;
		}

		[CanBeNull]
		private static Geometry GetSearchGeometry(
			[NotNull] ICollection<Feature> intersectingFeatures,
			[CanBeNull] Envelope clipExtent)
		{
			var intersectingGeometries =
				GetSearchGeometries(intersectingFeatures, clipExtent);

			Geometry result = null;

			if (intersectingGeometries.Count != 0)
			{
				var sr = intersectingGeometries[0].SpatialReference;
				result = GeometryBagBuilder.CreateGeometryBag(intersectingGeometries, sr);
				//result = GeometryEngine.Instance.Union(intersectingGeometries);
			}

			return result;
		}

		/// <summary>
		///     Returns the list of geometries that can be used as spatial filter. Multipatches
		///     are translated into polygons, polycurves are clipped.
		/// </summary>
		/// <param name="features">The features.</param>
		/// <param name="clipExtent">The clip extent.</param>
		/// <returns></returns>
		[NotNull]
		private static IList<Geometry> GetSearchGeometries(
			[NotNull] ICollection<Feature> features,
			[CanBeNull] Envelope clipExtent)
		{
			var result = new List<Geometry>(features.Count);

			foreach (var geometry in GdbObjectUtils.GetGeometries(features))
			{
				if (clipExtent != null)
				{
					clipExtent =
						GeometryUtils.EnsureSpatialReference(clipExtent, geometry.SpatialReference);

					if (GeometryUtils.Disjoint(geometry, clipExtent))
					{
						continue;
					}
				}

				var multiPatch = geometry as Multipatch;

				// multipatches are not supported by ISpatialFilter (and neither are bags containing them)
				var polycurve = multiPatch != null
					                ? PolygonBuilder.CreatePolygon(
						                multiPatch
							                .Extent) // GeometryFactory.CreatePolygon(multiPatch)
					                : geometry as Multipart;

				if (polycurve != null)
				{
					// clipping is an optimization to pull less features from the db
					result.Add(clipExtent == null
						           ? polycurve
						           : GetClippedGeometry(polycurve, clipExtent));
				}
				else
				{
					// don't clip points etc.
					result.Add(geometry);
				}
			}

			return result;
		}

		[NotNull]
		private static Geometry GetClippedGeometry(
			[NotNull] Multipart polycurve,
			[NotNull] Envelope clipExtent)
		{
			Geometry clippedGeometry;

			if (GeometryUtils.Contains(clipExtent, polycurve))
			{
				return GeometryFactory.Clone(polycurve);
			}

			if (polycurve.GeometryType == GeometryType.Polygon)
			{
				clippedGeometry =
					GeometryUtils.GetClippedPolygon((Polygon) polycurve, clipExtent);
			}
			else
			{
				clippedGeometry = GeometryUtils.GetClippedPolyline((Polyline) polycurve,
					clipExtent);
			}

			return clippedGeometry;
		}
	}
}
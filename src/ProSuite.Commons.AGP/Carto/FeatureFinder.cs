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
using ProSuite.Commons.AGP.Selection;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Carto
{
	// todo daro refactor, maybe FeatureSelectionBase is unnecessary
	/// <summary>
	/// Provides functionality to find features in the map. The features' shapes are returned in the
	/// map spatial reference.
	/// </summary>
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
		/// <see cref="FeatureSelectionType" /> with value <see cref="TargetFeatureSelection.SameClass"></see>
		/// </summary>
		public ICollection<Feature> SelectedFeatures { get; set; }

		// todo daro rethink usage
		public bool DelayFeatureFetching { get; set; }

		public IEnumerable<FeatureSelectionBase> FindFeaturesByLayer(
			[NotNull] Geometry searchGeometry,
			[CanBeNull] Predicate<BasicFeatureLayer> layerPredicate = null,
			[CanBeNull] Predicate<Feature> featurePredicate = null,
			CancelableProgressor cancelableProgressor = null)
		{
			// todo daro refactor
			Predicate<BasicFeatureLayer> predicate = fl => IsLayerApplicable(fl, layerPredicate);

			IEnumerable<BasicFeatureLayer>
				featureLayers = MapUtils.GetFeatureLayers(predicate, _mapView);

			return FindFeaturesByLayer(featureLayers, searchGeometry, featurePredicate,
			                           cancelableProgressor);
		}

		public IEnumerable<FeatureSelectionBase> FindFeaturesByLayer(
			IEnumerable<BasicFeatureLayer> layers,
			Geometry searchGeometry,
			Predicate<Feature> featurePredicate,
			CancelableProgressor cancelableProgressor)
		{
			SpatialReference outputSpatialReference = _mapView.Map.SpatialReference;

			foreach (BasicFeatureLayer basicFeatureLayer in layers)
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

				FeatureClass featureClass = basicFeatureLayer.GetFeatureClass();

				if (DelayFeatureFetching)
				{
					filter.SubFields = featureClass.GetDefinition().GetObjectIDField();

					List<long> objectIds =
						LayerUtils.SearchObjectIds(basicFeatureLayer, filter, featurePredicate)
						          .ToList();

					if (objectIds.Count > 0)
					{
						yield return new OidSelection(objectIds, basicFeatureLayer, outputSpatialReference);
					}
				}
				else
				{
					filter.OutputSpatialReference = outputSpatialReference;

					List<Feature> features =
						LayerUtils.SearchRows(basicFeatureLayer, filter, featurePredicate).ToList();

					if (features.Count > 0)
					{
						yield return new FeatureSelection(features, basicFeatureLayer);
					}
				}
			}
		}

		// todo daro rename to FindFeaturesByTable?
		public IEnumerable<FeatureSelectionBase> FindFeaturesByFeatureClass(
			[NotNull] Geometry searchGeometry,
			[CanBeNull] Predicate<BasicFeatureLayer> layerPredicate = null,
			[CanBeNull] Predicate<Feature> featurePredicate = null,
			CancelableProgressor cancelableProgressor = null)
		{
			Predicate<BasicFeatureLayer> predicate = fl => IsLayerApplicable(fl, layerPredicate);

			IEnumerable<BasicFeatureLayer>
				featureLayers = MapUtils.GetFeatureLayers(predicate, _mapView);

			return FindFeaturesByFeatureClass(featureLayers, searchGeometry, featurePredicate,
			                                  cancelableProgressor);
		}

		/// <summary>
		/// Finds the features in the map by the specified criteria, grouped by feature class
		/// </summary>
		/// <param name="searchGeometry">The search geometry</param>
		/// <param name="featureLayers">
		/// An extra layer predicate that allows for a more
		/// fine-granular determination of the layers to be searched.
		/// </param>
		/// <param name="featurePredicate">
		/// An extra feature predicate that allows to determine
		/// criteria on the feature level.
		/// </param>
		/// <param name="cancelableProgressor"></param>
		/// <returns></returns>
		public IEnumerable<FeatureSelectionBase> FindFeaturesByFeatureClass(
			IEnumerable<BasicFeatureLayer> featureLayers,
			Geometry searchGeometry,
			Predicate<Feature> featurePredicate,
			CancelableProgressor cancelableProgressor)
		{
			IEnumerable<IGrouping<IntPtr, BasicFeatureLayer>> layersGroupedByClass =
				featureLayers.GroupBy(fl => fl.GetTable().Handle);

			SpatialReference outputSpatialReference = _mapView.Map.SpatialReference;

			foreach (IGrouping<IntPtr, BasicFeatureLayer> layersInClass in layersGroupedByClass)
			{
				// One query per distinct definition query, then make OIDs distinct

				FeatureClass featureClass = null;
				BasicFeatureLayer basicFeatureLayer = null;
				var features = new List<Feature>();
				foreach (IGrouping<string, BasicFeatureLayer> layers in layersInClass.GroupBy(
					         fl => fl.DefinitionQuery))
				{
					if (cancelableProgressor != null
					    && cancelableProgressor.CancellationToken.IsCancellationRequested)
					{
						yield break;
					}

					basicFeatureLayer = layers.First();
					featureClass = basicFeatureLayer.GetFeatureClass();

					QueryFilter filter =
						GdbQueryUtils.CreateSpatialFilter(searchGeometry, SpatialRelationship);
					filter.WhereClause = layers.Key;

					filter.OutputSpatialReference = outputSpatialReference;

					IEnumerable<Feature> foundFeatures = GdbQueryUtils
					                                     .GetFeatures(featureClass, filter, false)
					                                     .Where(f => featurePredicate == null ||
						                                            featurePredicate(f));
					features.AddRange(foundFeatures);
				}

				if (featureClass != null && features.Count > 0)
				{
					yield return new FeatureSelection(features.DistinctBy(f => f.GetObjectID()).ToList(),
					                                  basicFeatureLayer);
				}
			}
		}

		/// <summary>
		/// Finds the distinct visible features in the map that intersect the selected
		/// features and that fulfill the target-selection-type criteria.
		/// </summary>
		/// <param name="intersectingSelectedFeatures">
		/// The selected features to use in the search for
		/// other visible features intersecting any of the selected features. When using target selection
		/// type SameClass these features are used to determine whether a potential target feature comes
		/// from the same class as one of them.
		/// </param>
		/// <param name="layerPredicate">An additional layer predicate to be tested.</param>
		/// <param name="extent">The area of interest to which the search can be limited</param>
		/// <param name="cancelableProgressor">The progress/cancel tracker.</param>
		/// <remarks>
		/// The <see cref="FeatureSelectionType" /> most not be Undefined and must not be
		/// SelectedFeatures.
		/// </remarks>
		/// <returns>The found features in the same spatial reference as the provided selected features</returns>
		[NotNull]
		public IEnumerable<FeatureSelectionBase> FindIntersectingFeaturesByFeatureClass(
			[NotNull] Dictionary<MapMember, List<long>> intersectingSelectedFeatures,
			[CanBeNull] Predicate<BasicFeatureLayer> layerPredicate = null,
			[CanBeNull] Envelope extent = null,
			[CanBeNull] CancelableProgressor cancelableProgressor = null)
		{
			Assert.ArgumentCondition(
				FeatureSelectionType != TargetFeatureSelection.SelectedFeatures &&
				FeatureSelectionType != TargetFeatureSelection.Undefined,
				"Unsupported target selection type");

			SpatialReference spatialReference = _mapView.Map.SpatialReference;
			SelectedFeatures = MapUtils.GetFeatures(intersectingSelectedFeatures, spatialReference)
			                           .ToList();

			Geometry searchGeometry = GetSearchGeometry(SelectedFeatures, extent);

			if (searchGeometry == null)
			{
				yield break;
			}

			foreach (FeatureSelectionBase classSelection in FindFeaturesByFeatureClass(
				         searchGeometry, layerPredicate, null, cancelableProgressor))
			{
				yield return classSelection;
			}
		}

		private bool IsLayerApplicable(
			[CanBeNull] BasicFeatureLayer layer,
			[CanBeNull] Predicate<BasicFeatureLayer> layerPredicate)
		{
			return IsLayerApplicable(layer, layerPredicate, FeatureSelectionType,
			                         SelectedFeatures);
		}

		private static bool IsLayerApplicable(
			[CanBeNull] BasicFeatureLayer basicFeatureLayer,
			[CanBeNull] Predicate<BasicFeatureLayer> layerPredicate,
			TargetFeatureSelection? targetSelectionType,
			[CanBeNull] ICollection<Feature> selectedFeatures)
		{
			if (! basicFeatureLayer.IsVisible())
			{
				return false;
			}

			if (layerPredicate != null && ! layerPredicate(basicFeatureLayer))
			{
				return false;
			}

			if (targetSelectionType == null)
			{
				return true;
			}

			if (basicFeatureLayer == null)
			{
				return false;
			}

			if ((targetSelectionType == TargetFeatureSelection.VisibleEditableFeatures ||
			     targetSelectionType == TargetFeatureSelection.VisibleSelectableEditableFeatures) &&
			    ! basicFeatureLayer.IsEditable)
			{
				return false;
			}

			if ((targetSelectionType == TargetFeatureSelection.VisibleSelectableFeatures ||
			     targetSelectionType ==
			     TargetFeatureSelection.VisibleSelectableEditableFeatures) &&
			    ! basicFeatureLayer.IsSelectable)
			{
				return false;
			}

			if (targetSelectionType == TargetFeatureSelection.SameClass &&
			    ! Assert.NotNull(selectedFeatures).Any(
				    f => DatasetUtils.IsSameClass(f.GetTable(), basicFeatureLayer.GetTable())))
			{
				return false;
			}

			if (basicFeatureLayer is FeatureLayer fl)
			{
				if (fl.GetFeatureClass() == null)
				{
					return false;
				}
			}

			// AnnotationLayer has it's own GetFeatureClass() method. There is no base
			// method on BasicFeatureLayer.
			if (basicFeatureLayer is AnnotationLayer annoLayer)
			{
				if (annoLayer.GetFeatureClass() == null)
				{
					return false;
				}
			}

			return true;
		}

		[CanBeNull]
		private static Geometry GetSearchGeometry(
			[NotNull] ICollection<Feature> intersectingFeatures,
			[CanBeNull] Envelope clipExtent)
		{
			IList<Geometry> intersectingGeometries =
				GetSearchGeometries(intersectingFeatures, clipExtent);

			Geometry result = null;

			if (intersectingGeometries.Count != 0)
			{
				SpatialReference sr = intersectingGeometries[0].SpatialReference;
				result = GeometryBagBuilder.CreateGeometryBag(intersectingGeometries, sr);
				//result = GeometryEngine.Instance.Union(intersectingGeometries);
			}

			return result;
		}

		/// <summary>
		/// Returns the list of geometries that can be used as spatial filter. Multipatches
		/// are translated into polygons, polycurves are clipped.
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

			foreach (Geometry geometry in GdbObjectUtils.GetGeometries(features))
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
				Multipart polycurve = multiPatch != null
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

using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Gdb;
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
	}
}

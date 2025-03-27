using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.Picker;
using ProSuite.Commons.AGP.Selection;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Editing.Test.Picker
{
	public class StandardPickerPrecedenceMock : IPickerPrecedence
	{
		private static readonly int _maxItems = 25;
		private static MapPoint _selectionCentroid;

		private Geometry _selectionGeometry;

		public Geometry SelectionGeometry
		{
			get => _selectionGeometry;
			set
			{
				_selectionGeometry = value;
				_selectionCentroid = GeometryUtils.Centroid(value);
			}
		}

		protected List<Key> PressedKeys { get; }

		public IPickableItem PickBest(IEnumerable<IPickableItem> items)
		{
			throw new NotImplementedException();
		}

		public int SelectionTolerance { get; set; }
		public bool IsSingleClick { get; }
		public bool AggregateItems { get; }
		public Point PickerLocation { get; set; }

		public SpatialRelationship SpatialRelationship { get; }
		public SelectionCombinationMethod SelectionCombinationMethod { get; }
		public bool NoMultiselection { get; set; }

		public StandardPickerPrecedenceMock(List<Key> pressedKeys)
		{
			PressedKeys = pressedKeys;
		}

		public PickerMode GetPickerMode(List<int> orderedSelection)
		{
			PickerMode result = PickerMode.PickBest;

			if (NoMultiselection)
			{
				result = PickerMode.ShowPicker;
			}

			if (PressedKeys.Contains(Key.LeftCtrl) || PressedKeys.Contains(Key.RightCtrl))
			{
				result = PickerMode.ShowPicker;
			}

			if (orderedSelection.Count > 1)
			{
				result = PickerMode.ShowPicker;
			}

			if (!IsSingleClick)
			{
				result = PickerMode.PickAll;
			}

			if (PressedKeys.Contains(Key.LeftAlt) || PressedKeys.Contains(Key.LeftAlt))
			{
				result = PickerMode.PickAll;
			}

			return result;
		}

		public PickerMode GetPickerMode(ICollection<FeatureSelectionBase> candidates)
		{
			PickerMode result = PickerMode.PickBest;

			if (NoMultiselection)
			{
				result = PickerMode.ShowPicker;
			}

			if (PressedKeys.Contains(Key.LeftCtrl) || PressedKeys.Contains(Key.RightCtrl))
			{
				result = PickerMode.ShowPicker;
			}

			if (CountLowestShapeDimension(candidates) > 1)
			{
				result = PickerMode.ShowPicker;
			}

			if (! IsSingleClick)
			{
				result = PickerMode.PickAll;
			}

			if (PressedKeys.Contains(Key.LeftAlt) || PressedKeys.Contains(Key.LeftAlt))
			{
				result = PickerMode.PickAll;
			}

			return result;
		}

		protected static int CountLowestShapeDimension(
			IEnumerable<FeatureSelectionBase> layerSelection)
		{
			var count = 0;

			int? lastShapeDimension = null;

			foreach (FeatureSelectionBase selection in layerSelection)
			{
				if (lastShapeDimension == null)
				{
					lastShapeDimension = selection.ShapeDimension;

					count += selection.GetCount();

					continue;
				}

				if (lastShapeDimension < selection.ShapeDimension)
				{
					continue;
				}

				count += selection.GetCount();
			}

			return count;
		}

		public void EnsureGeometryNonEmpty()
		{
			throw new NotImplementedException();
		}

		public Geometry GetSelectionGeometry()
		{
			return SelectionGeometry;
		}

		public IPickableItemsFactory CreateItemsFactory()
		{
			throw new NotImplementedException();
		}

		public IPickableItemsFactory CreateItemsFactory<T>() where T : IPickableItem
		{
			throw new NotImplementedException();
		}

		public IEnumerable<IPickableItem> Order(IEnumerable<IPickableItem> items)
		{
			return items.Take(_maxItems)
			            .Select(item => SetScoreConsideringDistances(item, _selectionCentroid))
			            .OrderBy(item => item, new PickableItemComparer());
		}

		[CanBeNull]
		public T PickBest<T>(IEnumerable<IPickableItem> items) where T : class, IPickableItem
		{
			return Order(items).FirstOrDefault() as T;
		}

		private static IPickableItem SetScoreConsideringDistances(
			IPickableItem item,
			Geometry selectionGeometry)
		{
			double score = 0.0;
			Geometry geometry = item.Geometry;

			if (geometry == null)
			{
				return item;
			}

			switch (geometry.GeometryType)
			{
				case GeometryType.Point:
					score = GeometryUtils.Engine
					                     .NearestPoint(selectionGeometry, (MapPoint) item.Geometry)
					                     .Distance;
					break;
				case GeometryType.Polyline:
					score = SumDistancesStartEndPoint(selectionGeometry, (Multipart) item.Geometry);
					break;
				case GeometryType.Polygon:
					// negative
					score = ((Polygon) geometry).Area;
					break;
				case GeometryType.Unknown:
				case GeometryType.Envelope:
				case GeometryType.Multipoint:
				case GeometryType.Multipatch:
				case GeometryType.GeometryBag:
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
			
			SetScore(item, score);

			return item;
		}

		private static double SumDistancesStartEndPoint(Geometry referenceGeometry,
		                                                Multipart multipart)
		{
			MapPoint startPoint = GeometryUtils.GetStartPoint(multipart);
			MapPoint endPoint = GeometryUtils.GetEndPoint(multipart);

			double distanceFromStartPoint =
				GeometryUtils.Engine.NearestPoint(referenceGeometry, startPoint).Distance;

			double distanceFromEndPoint =
				GeometryUtils.Engine.NearestPoint(referenceGeometry, endPoint).Distance;

			return distanceFromStartPoint + distanceFromEndPoint;
		}

		private static void SetScore(IPickableItem item, double score)
		{
			item.Score = Math.Round(score, 2);
		}

		public void Dispose() { }
	}
}

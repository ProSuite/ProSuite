using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.Selection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Editing.Picker
{
	public class SelectionToolPickerPrecedence : PickerPrecedenceBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();
		private static readonly int _maxItems = 25;

		private MapPoint _selectionCentroid;

		[UsedImplicitly]
		public SelectionToolPickerPrecedence(Geometry selectionGeometry,
		                                     int selectionTolerance,
		                                     Point pickerLocation) :
			base(selectionGeometry, selectionTolerance, pickerLocation) { }

		public override PickerMode GetPickerMode(IEnumerable<FeatureSelectionBase> orderedSelection,
		                                         bool areaSelect = false)
		{
			if (PressedKeys.Contains(Key.LeftAlt) || PressedKeys.Contains(Key.RightAlt))
			{
				return PickerMode.PickAll;
			}

			if (PressedKeys.Contains(Key.LeftCtrl) || PressedKeys.Contains(Key.RightCtrl))
			{
				return PickerMode.ShowPicker;
			}

			areaSelect = ! IsSingleClick;
			if (areaSelect)
			{
				return PickerMode.PickAll;
			}

			return PickerMode.PickBest;
		}

		public override IEnumerable<IPickableItem> Order(IEnumerable<IPickableItem> items)
		{
			return items.Take(_maxItems)
			            .Select(item => SetScoreConsideringDistances(item, SelectionCentroid))
			            .OfType<IPickableFeatureItem>()
			            .Select(item => SetScoreConsideringDrawingOutline(item, SelectionCentroid))
			            .OrderBy(item => item, new PickableItemComparer());
		}

		public override T PickBest<T>(IEnumerable<IPickableItem> items)
		{
			return Order(items).FirstOrDefault() as T;
		}

		[NotNull]
		private MapPoint SelectionCentroid =>
			_selectionCentroid ??= GeometryUtils.Centroid(SelectionGeometry);

		private static IPickableItem SetScoreConsideringDistances(
			IPickableItem item,
			Geometry selectionGeometry)
		{
			try
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
						                     .NearestPoint(selectionGeometry, (MapPoint) geometry)
						                     .Distance;
						break;
					case GeometryType.Polyline:
						score = SumDistancesStartEndPoint(selectionGeometry, (Multipart) geometry);
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
			}
			catch (Exception e)
			{
				_msg.Debug($"{nameof(SetScoreConsideringDistances)}", e);
			}

			return item;
		}

		private static IPickableFeatureItem SetScoreConsideringDrawingOutline(
			IPickableFeatureItem item,
			Geometry selectionGeometry)
		{
			try
			{
				Geometry itemGeometry = item.Geometry;

				if (itemGeometry == null)
				{
					return item;
				}

				Geometry geometry = UseDrawingOutline(itemGeometry)
					                    ? GetDrawingOutline(item.Layer, item.Oid)
					                    : itemGeometry;

				if (GeometryUtils.Disjoint(geometry, selectionGeometry))
				{
					SetScore(item, item.Score * item.Score);

					return item;
				}
			}
			catch (Exception e)
			{
				_msg.Debug($"{nameof(SetScoreConsideringDrawingOutline)}", e);
			}

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

		private static bool UseDrawingOutline(Geometry geometry)
		{
			switch (geometry.GeometryType)
			{
				case GeometryType.Point:
				case GeometryType.Multipoint:
				case GeometryType.Polyline:
					return true;
				case GeometryType.Polygon:
				case GeometryType.Multipatch:
				case GeometryType.Envelope:
				case GeometryType.Unknown:
				case GeometryType.GeometryBag:
					return false;
				default:
					throw new ArgumentOutOfRangeException(nameof(geometry), geometry,
					                                      $@"Unexpected geometry type: {geometry}");
			}
		}

		private static Geometry GetDrawingOutline(BasicFeatureLayer layer, long oid)
		{
			//todo: use UJR's ProLayerProxy?
			Geometry drawingOutline =
				MapView.Active.NotNullCallback(
					mv => layer.GetDrawingOutline(oid, mv, DrawingOutlineType.Exact));

			Assert.NotNull(drawingOutline);
			Assert.False(drawingOutline.IsEmpty, "outline is empty");
			Assert.False(drawingOutline.GeometryType == GeometryType.Point, "outline is a point");
			Assert.False(drawingOutline.GeometryType == GeometryType.Polyline,
			             "outline is a polyline");

			return drawingOutline;
		}

		private static void SetScore(IPickableItem item, double score)
		{
			item.Score = Math.Round(score, 2);
		}
	}
}

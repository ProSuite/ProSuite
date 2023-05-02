using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.Keyboard;

namespace ProSuite.AGP.Editing.Picker
{
	public class StandardPickerPrecedence : IPickerPrecedence
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

		public PickerMode GetPickerMode()
		{
			if (KeyboardUtils.IsModifierPressed(Keys.Alt))
			{
				return PickerMode.PickAll;
			}

			if (KeyboardUtils.IsModifierPressed(Keys.Control))
			{
				return PickerMode.ShowPicker;
			}

			return PickerMode.PickBest;
		}

		public IEnumerable<IPickableItem> Order(IEnumerable<IPickableItem> items)
		{
			return items.Take(_maxItems)
			            .Select(item => SetScoreCosideringDistances(item, _selectionCentroid))
			            .OfType<IPickableFeatureItem>()
			            .Select(item => SetScoreConsideringDrawingOutline(item, _selectionCentroid))
			            .OrderBy(item => item, new PickableItemComparer());
		}

		// todo daro move to subclass?
		[CanBeNull]
		public T PickBest<T>(IEnumerable<IPickableItem> items) where T : class, IPickableItem
		{
			return Order(items).FirstOrDefault() as T;
		}

		private static IPickableItem SetScoreCosideringDistances(
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

		private static IPickableFeatureItem SetScoreConsideringDrawingOutline(
			IPickableFeatureItem item,
			Geometry selectionGeometry)
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
			Geometry drawingOutline =
				MapView.Active.NotNullCallback(
					mv => layer.QueryDrawingOutline(oid, mv, DrawingOutlineType.Exact));

			Assert.NotNull(drawingOutline);
			Assert.False(drawingOutline.IsEmpty, "outline is empty");
			Assert.False(drawingOutline.GeometryType == GeometryType.Point, "outline is a point");
			Assert.False(drawingOutline.GeometryType == GeometryType.Polyline, "outline is a polyline");

			return drawingOutline;
		}

		private static void SetScore(IPickableItem item, double score)
		{
			item.Score = Math.Round(score, 2);
		}
	}
}

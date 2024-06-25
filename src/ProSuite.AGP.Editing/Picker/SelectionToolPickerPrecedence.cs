using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.AGP.Carto;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.Selection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI.Input;

namespace ProSuite.AGP.Editing.Picker
{
	public class SelectionToolPickerPrecedence : IPickerPrecedence
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();
		private static readonly int _maxItems = 25;
		private MapPoint _selectionCentroid;
		private Geometry _selectionGeometry;

		[UsedImplicitly]
		public SelectionToolPickerPrecedence() { }

		[UsedImplicitly]
		public SelectionToolPickerPrecedence(Geometry selectionGeometry, int selectionTolerance)
		{
			_selectionGeometry = selectionGeometry;
			SelectionTolerance = selectionTolerance;
		}

		[NotNull]
		public Geometry SelectionGeometry
		{
			get => Assert.NotNull(_selectionGeometry);
			set => _selectionGeometry = value;
		}

		public int SelectionTolerance { get; }
		public bool IsSingleClick { get; }

		public PickerMode GetPickerMode(IEnumerable<FeatureSelectionBase> orderedSelection,
		                                bool areaSelect = false)
		{
			if (KeyboardUtils.IsModifierDown(Key.LeftAlt, exclusive: true) ||
			    KeyboardUtils.IsModifierDown(Key.RightAlt, exclusive: true))
			{
				return PickerMode.PickAll;
			}

			if (KeyboardUtils.IsModifierDown(Key.LeftCtrl, exclusive: true) ||
			    KeyboardUtils.IsModifierDown(Key.RightCtrl, exclusive: true))
			{
				return PickerMode.ShowPicker;
			}

			return PickerMode.PickBest;
		}

		public void EnsureGeometryNonEmpty()
		{
			throw new NotImplementedException();
		}

		public IEnumerable<IPickableItem> Order(IEnumerable<IPickableItem> items)
		{
			return items.Take(_maxItems)
			            .Select(item => SetScoreConsideringDistances(item, SelectionCentroid))
			            .OfType<IPickableFeatureItem>()
			            .Select(item => SetScoreConsideringDrawingOutline(item, SelectionCentroid))
			            .OrderBy(item => item, new PickableItemComparer());
		}

		// todo daro move to subclass?
		[CanBeNull]
		public T PickBest<T>(IEnumerable<IPickableItem> items) where T : class, IPickableItem
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
						                     .NearestPoint(selectionGeometry,
						                                   (MapPoint) item.Geometry)
						                     .Distance;
						break;
					case GeometryType.Polyline:
						score = SumDistancesStartEndPoint(selectionGeometry,
						                                  (Multipart) item.Geometry);
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

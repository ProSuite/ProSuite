using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.Editing.Picker
{
	public class StandardPickerPrecedence : IPickerPrecedence
	{
		private static readonly int _maxItems = 50;

		private Geometry _selectionGeometry;
		[CanBeNull] private IPickableItem _previousBestPick;

		public StandardPickerPrecedence() { }

		public StandardPickerPrecedence(Geometry selectionGeometry)
		{
			_selectionGeometry = selectionGeometry;
		}

		public Geometry SelectionGeometry
		{
			get => _selectionGeometry;
			set => _selectionGeometry = value;
		}

		public IEnumerable<IPickableItem> Order(IEnumerable<IPickableItem> items)
		{
			return items.Take(_maxItems)
			            .Select(item => SetScore(item, SelectionGeometry))
			            .OrderBy(item => item, new PickableItemComparer());
		}

		[CanBeNull]
		public IPickableItem PickBest(IEnumerable<IPickableItem> items)
		{
			return Order(items).FirstOrDefault();
		}

		public T PickBest<T>(IEnumerable<IPickableItem> items) where T : class, IPickableItem
		{
			return Order(items).FirstOrDefault() as T;
		}

		private static IPickableItem SetScore(IPickableItem item, Geometry referenceGeometry)
		{
			double? score = 0.0;
			Geometry geometry = item.Geometry;

			if (geometry == null)
			{
				return item;
			}

			switch (geometry.GeometryType)
			{
				case GeometryType.Point:
					score = GeometryUtils.Engine.NearestPoint(referenceGeometry, (MapPoint) item.Geometry).Distance;
					break;
				case GeometryType.Polyline:
					score = SumDistancesStartEndPoint(referenceGeometry, (Multipart) item.Geometry);
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

			Assert.NotNull(score);
			item.Score = Math.Round(score.Value, 2);

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
	}
}

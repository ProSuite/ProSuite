using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Geometry;
using ProSuite.AGP.Editing.Picker;
using ProSuite.Commons.AGP.Core.Spatial;
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

		public PickerMode GetPickerMode(int candidateCount, bool areaSelect = false)
		{
			return PickerMode.PickBest;
		}

		public IEnumerable<IPickableItem> Order(IEnumerable<IPickableItem> items)
		{
			return items.Take(_maxItems)
			            .Select(item => SetScoreCosideringDistances(item, _selectionCentroid))
			            .OrderBy(item => item, new PickableItemComparer());
		}

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
	}
}
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
			return items.Select(item => SetScore(item, SelectionGeometry))
			            .OrderBy(item => item, new PickableItemComparer());
		}

		public IPickableItem PickBest(IEnumerable<IPickableItem> items)
		{
			List<IPickableItem> itemsList = items.ToList();

			IPickableItem bestPick = itemsList.FirstOrDefault();

			// todo daro remove assertion
			Assert.NotNull(bestPick);

			IPickableItem result;

			if (itemsList.Count == 0)
			{
				result = bestPick;
			}
			else if (_previousBestPick != null && string.Equals(_previousBestPick.ItemText, bestPick.ItemText))
			{
				IPickableItem secondBestPick = itemsList[itemsList.Count - (itemsList.Count - 1)];
				result = secondBestPick;
			}
			else
			{
				result = bestPick;
			}
			
			_previousBestPick = result;
			return result;
		}

		private static IPickableItem SetScore(IPickableItem item, Geometry referenceGeometry)
		{
			Geometry geometry = item.Geometry;

			// todo to factory
			if (geometry is Polyline multipart)
			{
				item.Score = (int) SumDistancesStartEndPoint(referenceGeometry, multipart);
			}
			else if (geometry is Polygon polygon)
			{
				// negativ
				item.Score = (int) (0 - polygon.Area);
			}

			return item;
		}

		private static double SumDistancesStartEndPoint(Geometry referenceGeometry,
		                                                Multipart multipart)
		{
			double distanceToStartPoint =
				GetDistanceToPoint(referenceGeometry, GeometryUtils.GetStartPoint(multipart));

			double distanceToEndPoint =
				GetDistanceToPoint(referenceGeometry, GeometryUtils.GetEndPoint(multipart));

			return distanceToStartPoint + distanceToEndPoint;
		}

		private static double GetDistanceToPoint(Geometry referenceGeometry, MapPoint mapPoint)
		{
			return GeometryEngine.Instance.Distance(referenceGeometry, mapPoint);
		}
	}
}

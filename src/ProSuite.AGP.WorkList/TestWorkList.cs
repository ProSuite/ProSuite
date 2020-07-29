using System.Collections.Generic;
using ArcGIS.Core.Geometry;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.AGP.WorkList.Domain;

namespace ProSuite.AGP.WorkList
{
	/// <summary>
	/// A Work List for testing: the 26 Swiss canton capitals in WGS84.
	/// </summary>
	public class TestWorkList : Domain.WorkList
	{
		#region Factory

		public static Domain.WorkList Create(string name = null)
		{
			var items = CreateWorkItems();
			return new TestWorkList(name ?? "Test Items", items);
		}

		private static IEnumerable<WorkItem> CreateWorkItems()
		{
			yield return new TestItem(1, 8.54226, 47.37174, "Zürich");
			yield return new TestItem(2, 7.44743, 46.94798, "Bern");
			yield return new TestItem(3, 8.30585, 47.05207, "Luzern");
			yield return new TestItem(4, 8.6439, 46.88177, "Altdorf");
			yield return new TestItem(5, 8.65365, 47.02101, "Schwyz");
			yield return new TestItem(6, 8.24568, 46.89601, "Sarnen");
			yield return new TestItem(7, 8.36622, 46.95696, "Stans");
			yield return new TestItem(8, 9.06728, 47.04088, "Glarus");
			yield return new TestItem(9, 8.51549, 47.16617, "Zug");
			yield return new TestItem(10, 7.16259, 46.80624, "Fribourg");
			yield return new TestItem(11, 7.53858, 47.20808, "Solothurn");
			yield return new TestItem(12, 7.58769, 47.55814, "Basel");
			yield return new TestItem(13, 7.735, 47.484, "Liestal");
			yield return new TestItem(14, 8.63386, 47.69653, "Schaffhausen");
			yield return new TestItem(15, 9.27991, 47.38577, "Herisau");
			yield return new TestItem(16, 9.40876, 47.33077, "Appenzell");
			yield return new TestItem(17, 9.37749, 47.42356, "St. Gallen");
			yield return new TestItem(18, 9.53222, 46.84829, "Chur");
			yield return new TestItem(19, 8.04434, 47.39285, "Aarau");
			yield return new TestItem(20, 8.89795, 47.55606, "Frauenfeld");
			yield return new TestItem(21, 9.02342, 46.19181, "Bellinzona");
			yield return new TestItem(22, 6.63448, 46.51942, "Lausanne");
			yield return new TestItem(23, 7.36083, 46.23316, "Sion");
			yield return new TestItem(24, 6.92932, 46.99001, "Neuchâtel");
			yield return new TestItem(25, 6.149985, 46.200013, "Genève");
			yield return new TestItem(26, 7.34367, 47.3652, "Delémont");
		}

		private static Envelope CreateExtent(double x, double y)
		{
			var sref = SpatialReferenceBuilder.CreateSpatialReference(4326);

			const double dx = 0.005;
			const double dy = 0.005;
			var min = new Coordinate2D(x - dx, y - dy);
			var max = new Coordinate2D(x + dx, y + dy);
			return EnvelopeBuilder.CreateEnvelope(min, max, sref);
		}

		private static MapPoint CreatePoint(double x, double y)
		{
			var sref = SpatialReferenceBuilder.CreateSpatialReference(4326);

			return MapPointBuilder.CreateMapPoint(x, y, sref);
		}

		#endregion

		private TestWorkList(string name, IEnumerable<IWorkItem> items) : base(name)
		{
			SetItems(items);

			GeometryType = GetGeometryTypeFromItems(items);
			Extent = GetExtentFromItems(items);
		}

		#region Nested type: TestItem

		private class TestItem : WorkItem
		{
			public TestItem(int oid, double x, double y, string name)
			{
				OID = oid;
				Description = name ?? string.Empty;
				Status = WorkItemStatus.Todo;
				Visited = WorkItemVisited.NotVisited;
				Shape = CreatePoint(x, y);
				Extent = CreateExtent(x, y);
			}

			public override int OID { get; }
			public override string Description { get; }
			public override WorkItemStatus Status { get; protected set; }
			public override WorkItemVisited Visited { get; protected set; }
			public override Geometry Shape { get; }
			public override Envelope Extent { get; }

			public override void SetDone(bool done = true)
			{
				Status = done ? WorkItemStatus.Done : WorkItemStatus.Todo;
			}

			public override void SetVisited(bool visited = true)
			{
				Visited = visited ? WorkItemVisited.Visited : WorkItemVisited.NotVisited;
			}
		}

		#endregion
	}
}

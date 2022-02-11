using System;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.TestContainer
{
	public class TestContainerException : Exception
	{
		private readonly IEnvelope _box;
		private readonly IReadOnlyRow _row;
		private readonly ITest _test;

		#region Constructors

		public TestContainerException([NotNull] ITest test,
		                              [CanBeNull] Exception innerException)
			: base(GetMessage(test), innerException)
		{
			_test = test;
		}

		public TestContainerException([NotNull] ITest test,
		                              [NotNull] IReadOnlyRow row,
		                              [CanBeNull] Exception innerException)
			: base(GetMessage(test, row), innerException)
		{
			_test = test;
			_row = row;
		}

		public TestContainerException([NotNull] ITest test,
		                              [CanBeNull] IEnvelope box,
		                              [CanBeNull] Exception innerException)
			: base(GetMessage(test, box), innerException)
		{
			_test = test;
			_box = box;
		}

		#endregion

		public ITest Test
		{
			get { return _test; }
		}

		public IReadOnlyRow Row
		{
			get { return _row; }
		}

		public IEnvelope Box
		{
			get { return _box; }
		}

		private static string GetMessage([NotNull] ITest test)
		{
			return string.Format("Error while processing test {0}", test);
		}

		private static string GetMessage([NotNull] ITest test, [NotNull] IReadOnlyRow row)
		{
			Assert.ArgumentNotNull(test, nameof(test));
			Assert.ArgumentNotNull(row, nameof(row));

			IReadOnlyTable table = row.Table;

			if (table is IReadOnlyDataset dataset)
			{
				return table.HasOID
					       ? string.Format(
						       "Error in test {0} while processing row of table {1}, OID = {2}",
						       test, dataset.Name, row.OID)
					       : string.Format(
						       "Error in test {0} while processing row of table {1} (no OID)",
						       test, dataset.Name);
			}

			if (row is TerrainRow)
			{
				var terrainRow = (TerrainRow) row;
				IEnvelope extent = terrainRow.Extent;

				string msg =
					string.Format(
						"Error in test {0} while processing row of terrain {1}",
						test, terrainRow.DatasetName);

				if (extent != null && ! extent.IsEmpty)
				{
					double x0;
					double x1;
					double y0;
					double y1;
					extent.QueryCoords(out x0, out y0, out x1, out y1);
					msg += string.Format(", extent {0}, {1}, {2}, {3}",
					                     x0, y0, x1, y1);
				}

				return msg;
			}

			return string.Format("Error in test {0} while processing (unknown) row", test);
		}

		private static string GetMessage([NotNull] ITest test,
		                                 [CanBeNull] IEnvelope box)
		{
			return box != null && box.IsEmpty == false
				       ? string.Format(
					       "Error in test {0} while completing extent {1}, {2}, {3}, {4}",
					       test, box.XMin, box.YMin, box.XMax, box.YMax)
				       : string.Format(
					       "Error in test {0} while completing (unknown) extent", test);
		}
	}
}

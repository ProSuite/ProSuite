using System;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container.TestContainer
{
	public class TestContainerException : Exception
	{
		private readonly IEnvelope _box;
		private readonly IRow _row;
		private readonly ITest _test;

		#region Constructors

		[CLSCompliant(false)]
		public TestContainerException([NotNull] ITest test,
		                              [CanBeNull] Exception innerException)
			: base(GetMessage(test), innerException)
		{
			_test = test;
		}

		[CLSCompliant(false)]
		public TestContainerException([NotNull] ITest test,
		                              [NotNull] IRow row,
		                              [CanBeNull] Exception innerException)
			: base(GetMessage(test, row), innerException)
		{
			_test = test;
			_row = row;
		}

		[CLSCompliant(false)]
		public TestContainerException([NotNull] ITest test,
		                              [CanBeNull] IEnvelope box,
		                              [CanBeNull] Exception innerException)
			: base(GetMessage(test, box), innerException)
		{
			_test = test;
			_box = box;
		}

		#endregion

		[CLSCompliant(false)]
		public ITest Test
		{
			get { return _test; }
		}

		[CLSCompliant(false)]
		public IRow Row
		{
			get { return _row; }
		}

		[CLSCompliant(false)]
		public IEnvelope Box
		{
			get { return _box; }
		}

		private static string GetMessage([NotNull] ITest test)
		{
			return string.Format("Error while processing test {0}", test);
		}

		private static string GetMessage([NotNull] ITest test, [NotNull] IRow row)
		{
			Assert.ArgumentNotNull(test, "test");
			Assert.ArgumentNotNull(row, "row");

			ITable table = row.Table;

			if (table is IDataset)
			{
				return table.HasOID
					       ? string.Format(
						       "Error in test {0} while processing row of table {1}, OID = {2}",
						       test, ((IDataset) table).Name, row.OID)
					       : string.Format(
						       "Error in test {0} while processing row of table {1} (no OID)",
						       test, ((IDataset) table).Name);
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
using System;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container
{
	internal class RowReference : IDataReference
	{
		private Guid? _recycleUnique;

		public RowReference([NotNull] IRow row, bool recycled)
		{
			Row = row;
			Recycled = recycled;
		}

		public IEnvelope Extent => ((IFeature) Row).Extent;

		[NotNull]
		public IRow Row { get; }

		public bool Recycled { get; }

		private Guid? RecycleUnique
		{
			get
			{
				return ! Recycled
					       ? null
					       : (_recycleUnique ?? (_recycleUnique = Guid.NewGuid()));
			}
		}

		public string DatasetName => ((IDataset) Row.Table).Name;

		public string GetDescription()
		{
			return Row.HasOID
				       ? string.Format("{0} OID {1}", DatasetName, Row.OID)
				       : string.Format("{0} no OID", DatasetName);
		}

		public string GetLongDescription()
		{
			return GdbObjectUtils.ToString(Row);
		}

		public int Execute(ContainerTest containerTest, int occurance, out bool applicable)
		{
			IRow row = Row;
			int involvedTableIndex = containerTest.GetTableIndex(row.Table, occurance);

			if (containerTest.GetQueriedOnly(involvedTableIndex))
			{
				applicable = false;
				return 0;
			}

			if (! containerTest.CheckConstraint(row, involvedTableIndex))
			{
				applicable = false;
				return 0;
			}

			// constraint is fulfilled
			if (containerTest.IsOutsideAreaOfInterest(row))
			{
				applicable = false;
				return 0;
			}

			foreach (var preProcessor in containerTest.GetPreProcessors(involvedTableIndex))
			{
				if (! preProcessor.VerifyExecute(row))
				{
					applicable = false;
					return 0;
				}
			}

			// the test is applicable for the row, run it
			applicable = true;
			return containerTest.Execute(row, involvedTableIndex, RecycleUnique);
		}
	}
}

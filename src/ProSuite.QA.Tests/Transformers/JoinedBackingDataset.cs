using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests.Transformers
{
	/// <summary>
	/// Wrapper class around the standard joined dataset to provide additional functionality
	/// required by a join transformer, such as the additional base-rows field.
	/// </summary>
	public class JoinedBackingDataset : TransformedBackingData
	{
		private readonly JoinedDataset _joinDatasetImpl;

		private readonly GdbTable _joinedSchema;

		public JoinedBackingDataset([NotNull] AssociationDescription associationDescription,
		                            [NotNull] IReadOnlyTable geometryTable,
		                            [NotNull] IReadOnlyTable otherTable,
		                            [NotNull] GdbTable joinedSchema)
			: base(GetInvolvedTables(geometryTable, otherTable, associationDescription).ToList())
		{
			_joinDatasetImpl =
				new JoinedDataset(associationDescription, geometryTable, otherTable, joinedSchema)
				{
					IncludeMtoNAssociationRows = true
				};

			_joinedSchema = joinedSchema;
		}

		private static IEnumerable<IReadOnlyTable> GetInvolvedTables(
			IReadOnlyTable geometryEnd,
			IReadOnlyTable otherEnd,
			AssociationDescription associationDescription)
		{
			yield return geometryEnd;
			yield return otherEnd;

			if (associationDescription is ManyToManyAssociationDescription manyToMany)
			{
				yield return manyToMany.AssociationTable;
			}
		}

		public void SetupRowFactory()
		{
			var joinedRowFactory = new JoinedRowFactoryWithBaseRow(_joinedSchema,
				                       _joinDatasetImpl.GeometryEndClass,
				                       _joinDatasetImpl.OtherEndClass)
			                       {
				                       AssociationTable = _joinDatasetImpl.AssociationTable
			                       };

			joinedRowFactory.GeometryEndCopyMatrix =
				TableFieldsBySource[_joinDatasetImpl.GeometryEndClass].FieldIndexMapping;
			joinedRowFactory.OtherEndCopyMatrix =
				TableFieldsBySource[_joinDatasetImpl.OtherEndClass].FieldIndexMapping;

			if (_joinDatasetImpl.AssociationTable != null)
			{
				joinedRowFactory.AssociationTableCopyMatrix =
					TableFieldsBySource[_joinDatasetImpl.AssociationTable].FieldIndexMapping;
			}

			_joinDatasetImpl.JoinedRowFactory = joinedRowFactory;
		}

		public Dictionary<IReadOnlyTable, TransformedTableFields> TableFieldsBySource { get; }
			= new Dictionary<IReadOnlyTable, TransformedTableFields>();

		#region Overrides of BackingDataset

		public override IEnvelope Extent => _joinDatasetImpl.Extent;

		public override VirtualRow GetRow(int id)
		{
			return _joinDatasetImpl.GetRow(id);
		}

		public override int GetRowCount(IQueryFilter queryFilter)
		{
			return _joinDatasetImpl.GetRowCount(queryFilter);
		}

		public override IEnumerable<VirtualRow> Search(IQueryFilter filter, bool recycling)
		{
			return _joinDatasetImpl.Search(filter, recycling);
		}

		#endregion

		public JoinType JoinType
		{
			get => _joinDatasetImpl.JoinType;
			set => _joinDatasetImpl.JoinType = value;
		}

		public IReadOnlyTable ObjectIdSourceTable => _joinDatasetImpl.ObjectIdSourceTable;
	}
}

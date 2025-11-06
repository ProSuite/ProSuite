using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;

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

		private readonly IList<ContainerSearchingDataset> _involvedSearchDatasets =
			new List<ContainerSearchingDataset>(3);

		public JoinedBackingDataset([NotNull] AssociationDescription associationDescription,
		                            [NotNull] IReadOnlyTable geometryTable,
		                            [NotNull] IReadOnlyTable otherTable,
		                            [NotNull] GdbTable joinedSchema)
			: base(GetInvolvedTables(geometryTable, otherTable, associationDescription).ToList())
		{
			// Wrap the input tables to allow searching features in the container and support
			// constraints provided by the parent transformer.
			// They must be added in the same order as the InvolvedTables list!
			geometryTable = WrapTable(geometryTable);
			otherTable = WrapTable(otherTable);

			if (associationDescription is ManyToManyAssociationDescription manyToMany)
			{
				manyToMany.AssociationTable = WrapTable(manyToMany.AssociationTable);
			}

			_joinDatasetImpl =
				new JoinedDataset(associationDescription, geometryTable, otherTable, joinedSchema)
				{
					IncludeMtoNAssociationRows = true
				};

			_joinedSchema = joinedSchema;
		}

		private IReadOnlyTable WrapTable(IReadOnlyTable inputTable)
		{
			GdbTable wrappedTable;
			if (inputTable is IReadOnlyFeatureClass featureClass)
			{
				wrappedTable =
					new WrappedFeatureClass(featureClass,
					                        t => new ContainerSearchingDataset(inputTable, t));
			}
			else
			{
				wrappedTable =
					new WrappedTable(inputTable, t => new ContainerSearchingDataset(inputTable, t));
			}

			ContainerSearchingDataset backingDataset =
				(ContainerSearchingDataset) wrappedTable.BackingDataset;

			_involvedSearchDatasets.Add(backingDataset);

			return wrappedTable;
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

		public JoinType JoinType
		{
			get => _joinDatasetImpl.JoinType;
			set => _joinDatasetImpl.JoinType = value;
		}

		public IReadOnlyTable ObjectIdSourceTable => _joinDatasetImpl.ObjectIdSourceTable;

		#region Overrides of BackingDataset

		public override IEnvelope Extent => _joinDatasetImpl.Extent;

		public IReadOnlyTable LeftTable => _joinDatasetImpl.GeometryEndClass;

		public IReadOnlyTable RightTable => _joinDatasetImpl.OtherEndClass;

		public IReadOnlyTable AssociationTable => _joinDatasetImpl.AssociationTable;

		public override VirtualRow GetRow(long id)
		{
			AssignFiltersAndContainer();

			return _joinDatasetImpl.GetRow(id);
		}

		public override long GetRowCount(ITableFilter queryFilter)
		{
			AssignFiltersAndContainer();

			return _joinDatasetImpl.GetRowCount(queryFilter);
		}

		public override IEnumerable<VirtualRow> Search(ITableFilter filter, bool recycling)
		{
			AssignFiltersAndContainer();

			return _joinDatasetImpl.Search(filter, recycling);
		}

		private void AssignFiltersAndContainer()
		{
			// 1. LeftTable
			// 2. RightTable
			// 2. AssociationTable (if m:n)
			for (var i = 0; i < _involvedSearchDatasets.Count; i++)
			{
				ContainerSearchingDataset containerSearchingDataset = _involvedSearchDatasets[i];

				containerSearchingDataset.FilterHelper = QueryHelpers[i];

				if (containerSearchingDataset.HasGeometry)
				{
					containerSearchingDataset.DataContainer = DataSearchContainer;
				}
			}

			if (_involvedSearchDatasets.Count > 0)
			{
				// Currently the filter helper is not set up to honor the (spatial) filter's where
				// clause -> make sure the filtering happens in the client.
				_joinDatasetImpl.AssumeLeftTableCached = _involvedSearchDatasets[0].HasGeometry;
			}
		}

		#endregion
	}
}

using System;
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

		private readonly IList<ContainerSearchingDataset> _involvedSearchDatasets =
			new List<ContainerSearchingDataset>(3);

		public JoinedBackingDataset([NotNull] AssociationDescription associationDescription,
		                            [NotNull] IReadOnlyTable geometryTable,
		                            [NotNull] IReadOnlyTable otherTable,
		                            [NotNull] GdbTable joinedSchema)
			: base(GetInvolvedTables(geometryTable, otherTable, associationDescription).ToList())
		{
			// Wrap the input tables to allow searching features in the container:
			// Must be in the same order as the InvolvedTables list!
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
			GdbTable wrappedTable = CreateContainerSearchingClass(inputTable);

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

		public override VirtualRow GetRow(int id)
		{
			AssignFiltersAndContainer();

			return _joinDatasetImpl.GetRow(id);
		}

		public override int GetRowCount(IQueryFilter queryFilter)
		{
			AssignFiltersAndContainer();

			return _joinDatasetImpl.GetRowCount(queryFilter);
		}

		public override IEnumerable<VirtualRow> Search(IQueryFilter filter, bool recycling)
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

		private static GdbTable CreateContainerSearchingClass(
			[NotNull] IReadOnlyTable baseClass)
		{
			// NOTE: Exact same objectClassId is required for correct equality (dictionary usage)
			int classId = -1;
			string aliasName = null;
			if (baseClass is IObjectClass objectClass)
			{
				classId = objectClass.ObjectClassID;
				aliasName = objectClass.AliasName;
			}
			else if (baseClass is ReadOnlyTable roTable)
			{
				// Consider adding this to IReadOnly interface
				classId = ((IObjectClass) roTable.BaseTable).ObjectClassID;
				aliasName = ((IObjectClass) roTable.BaseTable).AliasName;
			}

			IWorkspace workspace = baseClass.Workspace;

			Func<GdbTable, BackingDataset> datasetFactory =
				t => new ContainerSearchingDataset(baseClass, t);

			GdbTable result;
			if (baseClass is IReadOnlyFeatureClass baseFeatureClass)
			{
				result = new GdbFeatureClass(
					classId, baseClass.Name, baseFeatureClass.ShapeType, aliasName,
					datasetFactory, workspace);
			}
			else
			{
				result = new GdbTable(classId, baseClass.Name, aliasName, datasetFactory,
				                      workspace);
			}

			for (int i = 0; i < baseClass.Fields.FieldCount; i++)
			{
				IField field = baseClass.Fields.Field[i];
				result.AddField(field);
			}

			return result;
		}
	}
}

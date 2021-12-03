using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;

namespace ProSuite.QA.Tests.Transformers
{
	[UsedImplicitly]
	public class TrTableAppend : InvolvesTablesBase, ITableTransformer<ITable>
	{
		private AppendedTable _transformedTable;

		public TrTableAppend([NotNull] IList<ITable> tables)
			: base(tables) { }

		object ITableTransformer.GetTransformed() => GetTransformed();

		public ITable GetTransformed() =>
			_transformedTable ?? (_transformedTable = InitTransformedTable());

		private AppendedTable InitTransformedTable()
		{
			CopyFilters(out _, out IList<QueryFilterHelper> helpers);
			AppendedTable transformedTable = AppendedTable.Create(InvolvedTables, helpers);
			return transformedTable;
		}

		private class AppendedTable : VirtualTable, ITransformedValue
		{
			private IWorkspace _workspace;

			public static AppendedTable Create(
				IList<ITable> tables, IList<QueryFilterHelper> helpers,
				string name = "appended")
			{
				if (tables.All(x => x is IFeatureClass))
				{
					return new AppendedFeatureClass(
						tables.Cast<IFeatureClass>().ToList(), helpers, name);
				}

				return new AppendedTable(tables, helpers, name);
			}

			private bool _hasOid;
			private List<Dictionary<int, int>> _fieldDicts;
			private readonly IList<QueryFilterHelper> _filterHelpers;

			protected AppendedTable(
				[NotNull] IList<ITable> tables, [NotNull] IList<QueryFilterHelper> filterHelpers,
				string name = "appended")
				: base(name)
			{
				InvolvedTables = tables;
				_filterHelpers = filterHelpers;

				Init();
			}

			private void Init()
			{
				_hasOid = InvolvedTables.All(x => x.HasOID);
				if (_hasOid)
				{
					VirtualAddField(FieldUtils.CreateOIDField(oidFieldName: VirtualOIDFieldName));
				}

				List<Dictionary<int, int>> fieldDicts = new List<Dictionary<int, int>>();
				foreach (ITable involvedTable in InvolvedTables)
				{
					Dictionary<int, int> fieldDict = new Dictionary<int, int>();

					IFields fields = involvedTable.Fields;
					for (int iField = 0; iField < fields.FieldCount; iField++)
					{
						IField field = fields.Field[iField];
						string fieldName = field.Name;

						int iExisting = VirtualFields.FindField(fieldName);
						if (iExisting < 0)
						{
							iExisting = VirtualAddField(field);
						}
						else
						{
							// TODO: Verify Field props ?
						}

						fieldDict.Add(iExisting, iField);
					}

					fieldDicts.Add(fieldDict);
				}

				_fieldDicts = fieldDicts;

				BaseRowFieldIndex =
					VirtualAddField(FieldUtils.CreateBlobField(InvolvedRowUtils.BaseRowField));
			}

			public IList<ITable> InvolvedTables { get; }
			public ISearchable DataContainer { get; set; }
			public int BaseRowFieldIndex { get; private set; }

			protected override IWorkspace VirtualWorkspace =>
				_workspace ?? (_workspace = new SimpleWorkspace()); // TODO 

			public int GetSourceFieldIndex(int tableIndex, int fieldIndex)
			{
				Dictionary<int, int> fieldDict = _fieldDicts[tableIndex];
				if (! fieldDict.TryGetValue(fieldIndex, out int sourceFieldIndex))
				{
					sourceFieldIndex = -1;
				}

				return sourceFieldIndex;
			}

			protected override bool VirtualHasOID => _hasOid;
			protected override string VirtualOIDFieldName => "AppendedOID";

			protected override int VirtualRowCount(IQueryFilter QueryFilter)
			{
				int nRows = InvolvedTables.Sum(involved => involved.RowCount(QueryFilter));
				return nRows;
			}

			protected override IEnumerable<IRow> VirtualEnumRows(
				IQueryFilter queryFilter, bool recycling)
			{
				for (int iTable = 0; iTable < InvolvedTables.Count; iTable++)
				{
					ITable table = InvolvedTables[iTable];
					if (DataContainer == null)
					{
						foreach (IRow row in new EnumCursor(table, queryFilter, recycling))
						{
							yield return AppendedRow.Create(this, iTable, row);
						}
					}
					else
					{
						foreach (IRow row in DataContainer.Search(table, queryFilter,
							_filterHelpers[iTable]))
						{
							yield return AppendedRow.Create(this, iTable, row);
						}
					}
				}
			}
		}

		private class AppendedFeatureClass : AppendedTable, IFeatureClass, IGeoDataset
		{
			private IEnvelope _extent;

			public AppendedFeatureClass(
				[NotNull] IList<IFeatureClass> featureClasses,
				[NotNull] IList<QueryFilterHelper> filterHelpers,
				string name = "appended")
				: base(CastToTables(featureClasses), filterHelpers, name) { }

			protected override ISpatialReference VirtualSpatialReference =>
				InvolvedTables.Select(x => ((IGeoDataset) x).SpatialReference).FirstOrDefault();

			protected override IEnvelope VirtualExtent => _extent ?? (_extent = GetExtent());

			private IEnvelope GetExtent()
			{
				IEnvelope fullExtent = null;
				foreach (ITable involved in InvolvedTables)
				{
					IEnvelope extent = ((IGeoDataset) involved).Extent;
					if (fullExtent == null)
					{
						fullExtent = GeometryFactory.Clone(extent);
					}
					else
					{
						fullExtent.Union(extent);
					}
				}

				return fullExtent;
			}
		}

		private class AppendedRow : VirtualRow
		{
			public static AppendedRow Create(AppendedTable table, int sourceTableIndex, IRow sourceRow)
			{
				if (table is AppendedFeatureClass fc)
				{
					return new AppendedFeature(fc, sourceTableIndex, (IFeature)sourceRow);
				}

				if (sourceRow is IFeature f)
				{
					return new AppendedFeature(table, sourceTableIndex, f);
				}

				return new AppendedRow(table, sourceTableIndex, sourceRow);
			}
			protected AppendedRow(AppendedTable table, int sourceTableIndex, IRow sourceRow)
			{
				Table = table;
				SourceTableIndex = sourceTableIndex;
				SourceRow = sourceRow;
			}

			public new AppendedTable Table { get; set; } // needed for recycling
			protected override ITable VirtualTable => Table;
			public int SourceTableIndex { get; set; } // needed for recycling
			public IRow SourceRow { get; set; } // needed for recycling

			protected override int VirtualOID =>
				Table.InvolvedTables.Count * SourceRow.OID + SourceTableIndex;

			protected override object get_VirtualValue(int index)
			{
				if (index == 0)
				{
					return VirtualOID;
				}

				if (index == Table.BaseRowFieldIndex)
				{
					return new List<IRow> {SourceRow};
				}

				int sourceFieldIndex = Table.GetSourceFieldIndex(SourceTableIndex, index);
				if (sourceFieldIndex < 0)
				{
					return DBNull.Value;
				}

				return SourceRow.Value[sourceFieldIndex];
			}
		}

		private class AppendedFeature : AppendedRow, IFeature
		{
			private readonly IFeature _sourceFeature;

			public AppendedFeature(AppendedTable table, int sourceTableIndex,
			                       IFeature sourceFeature)
				: base(table, sourceTableIndex, sourceFeature)
			{
				_sourceFeature = sourceFeature;
			}

			protected override IGeometry VirtualShape
			{
				get => _sourceFeature.Shape;
			}
		}
	}

	public class SimpleWorkspace : VirtualWorkspace
	{
		protected override esriWorkspaceType VirtualWorkspaceType =>
			esriWorkspaceType.esriFileSystemWorkspace;
	}
}

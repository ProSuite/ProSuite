using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;
using ProSuite.QA.Core.TestCategories;

namespace ProSuite.QA.Tests.Transformers
{
	[UsedImplicitly]
	[TableTransformer]
	public class TrTableAppend : InvolvesTablesBase, ITableTransformer<IReadOnlyTable>
	{
		private AppendedTable _transformedTable;
		private string _transformerName;

		public TrTableAppend([NotNull] IList<IReadOnlyTable> tables)
			: base(tables) { }

		object ITableTransformer.GetTransformed() => GetTransformed();

		public IReadOnlyTable GetTransformed() =>
			_transformedTable ?? (_transformedTable = InitTransformedTable());

		string ITableTransformer.TransformerName
		{
			get => _transformerName;
			set => _transformerName = value;
		}

		private AppendedTable InitTransformedTable()
		{
			CopyFilters(out _, out IList<QueryFilterHelper> helpers);
			AppendedTable transformedTable =
				AppendedTable.Create(InvolvedTables, helpers, _transformerName);
			return transformedTable;
		}
		
		private class AppendedTable : VirtualTable, IDataContainerAware, ITransformedTable
		{
			private IWorkspace _workspace;

			public static AppendedTable Create(
				IList<IReadOnlyTable> tables, IList<QueryFilterHelper> helpers,
				string name = null)
			{
				name = name ?? "appended";
				if (tables.All(x => x is IReadOnlyFeatureClass))
				{
					return new AppendedFeatureClass(
						tables.Cast<IReadOnlyFeatureClass>().ToList(), helpers, name);
				}

				return new AppendedTable(tables, helpers, name);
			}

			private bool _hasOid;
			private List<Dictionary<int, int>> _fieldDicts;
			private readonly IList<QueryFilterHelper> _filterHelpers;

			protected AppendedTable(
				[NotNull] IList<IReadOnlyTable> tables,
				[NotNull] IList<QueryFilterHelper> filterHelpers,
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
					AddField(FieldUtils.CreateOIDField(oidFieldName: OIDFieldName));
				}

				List<Dictionary<int, int>> fieldDicts = new List<Dictionary<int, int>>();
				foreach (IReadOnlyTable involvedTable in InvolvedTables)
				{
					Dictionary<int, int> fieldDict = new Dictionary<int, int>();

					IFields fields = involvedTable.Fields;
					for (int iField = 0; iField < fields.FieldCount; iField++)
					{
						IField field = fields.Field[iField];
						string fieldName = field.Name;

						int iExisting = Fields.FindField(fieldName);
						if (iExisting < 0)
						{
							iExisting = AddFieldT(field);
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
					AddFieldT(FieldUtils.CreateBlobField(InvolvedRowUtils.BaseRowField));
			}

			public IList<IReadOnlyTable> InvolvedTables { get; }
			public IDataContainer DataContainer { get; set; }

			bool ITransformedTable.NoCaching => true;
			bool ITransformedTable.IgnoreOverlappingCachedRows => true;

			void ITransformedTable.SetKnownTransformedRows(IEnumerable<IReadOnlyRow> knownRows) { }

			public int BaseRowFieldIndex { get; private set; }

			public override IWorkspace Workspace =>
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

			public override bool HasOID => _hasOid;
			public override string OIDFieldName => "AppendedOID";

			public override long RowCount(ITableFilter queryFilter)
			{
				long nRows = InvolvedTables.Sum(involved => involved.RowCount(queryFilter));
				return nRows;
			}

			protected override long TableRowCount(IQueryFilter queryFilter)
			{
				ITableFilter tableFilter = GdbQueryUtils.ToTableFilter(queryFilter);

				long nRows = InvolvedTables.Sum(involved => involved.RowCount(tableFilter));

				return base.TableRowCount(queryFilter);
			}

			public override IEnumerable<IReadOnlyRow> EnumReadOnlyRows(
				ITableFilter queryFilter, bool recycling)
			{
				for (int iTable = 0; iTable < InvolvedTables.Count; iTable++)
				{
					IReadOnlyTable table = InvolvedTables[iTable];
					if (DataContainer == null)
					{
						foreach (IReadOnlyRow row in table.EnumRows(queryFilter, recycling))
						{
							yield return AppendedRow.Create(this, iTable, row);
						}
					}
					else
					{
						foreach (IReadOnlyRow row in DataContainer.Search(table, queryFilter,
							         _filterHelpers[iTable]))
						{
							yield return AppendedRow.Create(this, iTable, row);
						}
					}
				}
			}
		}

		private class AppendedFeatureClass : AppendedTable, IReadOnlyFeatureClass
		{
			private IEnvelope _extent;

			public AppendedFeatureClass(
				[NotNull] IList<IReadOnlyFeatureClass> featureClasses,
				[NotNull] IList<QueryFilterHelper> filterHelpers,
				string name = "appended")
				: base(CastToTables(featureClasses), filterHelpers, name) { }

			public override ISpatialReference SpatialReference =>
				InvolvedTables.Select(x => (x as IReadOnlyFeatureClass)?.SpatialReference)
				              .FirstOrDefault();

			public override IEnvelope Extent => _extent ?? (_extent = GetExtent());

			private IEnvelope GetExtent()
			{
				IEnvelope fullExtent = null;
				foreach (IReadOnlyTable involved in InvolvedTables)
				{
					IEnvelope extent = ((IReadOnlyGeoDataset) involved).Extent;
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
			public static AppendedRow Create(AppendedTable table, int sourceTableIndex,
			                                 IReadOnlyRow sourceRow)
			{
				if (table is AppendedFeatureClass fc)
				{
					return new AppendedFeature(fc, sourceTableIndex, (IReadOnlyFeature) sourceRow);
				}

				if (sourceRow is IReadOnlyFeature f)
				{
					return new AppendedFeature(table, sourceTableIndex, f);
				}

				return new AppendedRow(table, sourceTableIndex, sourceRow);
			}

			protected AppendedRow(AppendedTable table, int sourceTableIndex, IReadOnlyRow sourceRow)
			{
				TableT = table;
				SourceTableIndex = sourceTableIndex;
				SourceRow = sourceRow;
			}

			public AppendedTable TableT { get; set; } // needed for recycling
			public override VirtualTable Table => TableT;
			public int SourceTableIndex { get; set; } // needed for recycling
			public IReadOnlyRow SourceRow { get; set; } // needed for recycling

			public override bool HasOID => SourceRow.HasOID;

			public override long OID =>
				TableT.InvolvedTables.Count * SourceRow.OID + SourceTableIndex;

			public override object get_Value(int index)
			{
				if (index == 0)
				{
					return OID;
				}

				if (index == TableT.BaseRowFieldIndex)
				{
					return new List<IReadOnlyRow> { SourceRow };
				}

				int sourceFieldIndex = TableT.GetSourceFieldIndex(SourceTableIndex, index);
				if (sourceFieldIndex < 0)
				{
					return DBNull.Value;
				}

				object value = SourceRow.get_Value(sourceFieldIndex);
				if (value is IGeometry geom)
				{
					value = GetGeometry(geom);
				}

				return value;
			}

			private IGeometry GetGeometry(IGeometry sourceGeom)
			{
				if (! (Table is IReadOnlyFeatureClass fc))
				{
					return sourceGeom;
				}

				IGeometry geom = sourceGeom;
				bool isCloned = false;
				geom = EnsureZAware(geom, ref isCloned, fc);
				geom = EnsureMAware(geom, ref isCloned, fc);
				esriGeometryType targetType = fc.ShapeType;
				if (geom.GeometryType != targetType)
				{
					if (targetType == esriGeometryType.esriGeometryMultiPatch)
					{
						geom = GeometryFactory.CreateMultiPatch(geom, 1);
					}
					else
					{
						throw new NotImplementedException(
							$"TODO: implement conversion from {geom.GeometryType} to {targetType}");
					}
				}

				return geom;
			}

			private static IGeometry EnsureZAware(IGeometry g, ref bool isCloned,
			                                      IReadOnlyFeatureClass target)
			{
				IField shp = target.Fields.Field[target.FindField(target.ShapeFieldName)];
				IGeometryDef def = shp.GeometryDef;
				if (def.HasZ != ((IZAware) g).ZAware)
				{
					g = isCloned ? g : GeometryFactory.Clone(g);
					isCloned = true;
					if (def.HasZ)
					{
						GeometryUtils.MakeZAware(g);
					}
					else
					{
						GeometryUtils.MakeNonZAware(g);
					}
				}

				return g;
			}

			private static IGeometry EnsureMAware(IGeometry geom, ref bool isCloned,
			                                      IReadOnlyFeatureClass target)
			{
				IField shp = target.Fields.Field[target.FindField(target.ShapeFieldName)];
				IGeometryDef def = shp.GeometryDef;
				if (def.HasM != ((IMAware) geom).MAware)
				{
					geom = isCloned ? geom : GeometryFactory.Clone(geom);
					isCloned = true;
					GeometryUtils.MakeMAware(geom, aware: def.HasM);
				}

				return geom;
			}
		}

		private class AppendedFeature : AppendedRow, IReadOnlyFeature
		{
			private readonly IReadOnlyFeature _sourceFeature;

			public AppendedFeature(AppendedTable table, int sourceTableIndex,
			                       IReadOnlyFeature sourceFeature)
				: base(table, sourceTableIndex, sourceFeature)
			{
				_sourceFeature = sourceFeature;
			}

			public override IGeometry Shape => _sourceFeature.Shape;
			public IReadOnlyFeatureClass FeatureClass => (IReadOnlyFeatureClass) ReadOnlyTable;
		}

		public class SimpleWorkspace : VirtualWorkspace
		{
			public override esriWorkspaceType WorkspaceType =>
				esriWorkspaceType.esriFileSystemWorkspace;
		}
	}
}

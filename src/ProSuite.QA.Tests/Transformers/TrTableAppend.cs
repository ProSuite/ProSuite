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
		private string _transformerName;

		public TrTableAppend([NotNull] IList<ITable> tables)
			: base(tables) { }

		object ITableTransformer.GetTransformed() => GetTransformed();

		public ITable GetTransformed() =>
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

		private class AppendedTable : VirtualTable, ITransformedValue, ITransformedTable
		{
			private IWorkspace _workspace;

			public static AppendedTable Create(
				IList<ITable> tables, IList<QueryFilterHelper> helpers,
				string name = null)
			{
				name = name ?? "appended";
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
					AddField(FieldUtils.CreateOIDField(oidFieldName: OIDFieldName));
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

			public IList<ITable> InvolvedTables { get; }
			public ISearchable DataContainer { get; set; }

			bool ITransformedTable.NoCaching => true;

			void ITransformedTable.SetKnownTransformedRows(IEnumerable<IRow> knownRows) { }

			public int BaseRowFieldIndex { get; private set; }

			public override IWorkspace Workspace =>
				_workspace ?? (_workspace = new SimpleWorkspace()); // TODO 

			public int GetSourceFieldIndex(int tableIndex, int fieldIndex)
			{
				Dictionary<int, int> fieldDict = _fieldDicts[tableIndex];
				if (!fieldDict.TryGetValue(fieldIndex, out int sourceFieldIndex))
				{
					sourceFieldIndex = -1;
				}

				return sourceFieldIndex;
			}

			public override bool HasOID => _hasOid;
			public override string OIDFieldName => "AppendedOID";

			public override int RowCount(IQueryFilter QueryFilter)
			{
				int nRows = InvolvedTables.Sum(involved => involved.RowCount(QueryFilter));
				return nRows;
			}

			public override IEnumerable<IRow> EnumRows(
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

			public override ISpatialReference SpatialReference =>
				InvolvedTables.Select(x => ((IGeoDataset)x).SpatialReference).FirstOrDefault();

			public override IEnvelope Extent => _extent ?? (_extent = GetExtent());

			private IEnvelope GetExtent()
			{
				IEnvelope fullExtent = null;
				foreach (ITable involved in InvolvedTables)
				{
					IEnvelope extent = ((IGeoDataset)involved).Extent;
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
																			 IRow sourceRow)
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
				TableT = table;
				SourceTableIndex = sourceTableIndex;
				SourceRow = sourceRow;
			}

			public AppendedTable TableT { get; set; } // needed for recycling
			public override ITable Table => TableT;
			public int SourceTableIndex { get; set; } // needed for recycling
			public IRow SourceRow { get; set; } // needed for recycling

			public override int OID =>
				TableT.InvolvedTables.Count * SourceRow.OID + SourceTableIndex;

			public override object get_Value(int index)
			{
				if (index == 0)
				{
					return OID;
				}

				if (index == TableT.BaseRowFieldIndex)
				{
					return new List<IRow> { SourceRow };
				}

				int sourceFieldIndex = TableT.GetSourceFieldIndex(SourceTableIndex, index);
				if (sourceFieldIndex < 0)
				{
					return DBNull.Value;
				}

				object value = SourceRow.Value[sourceFieldIndex];
				if (value is IGeometry geom)
				{
					value = GetGeometry(geom);
				}

				return value;
			}

			private IGeometry GetGeometry(IGeometry sourceGeom)
			{
				if (!(Table is IFeatureClass fc))
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
																						IFeatureClass target)
			{
				IField shp = target.Fields.Field[target.FindField(target.ShapeFieldName)];
				IGeometryDef def = shp.GeometryDef;
				if (def.HasZ != ((IZAware)g).ZAware)
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
																						IFeatureClass target)
			{
				IField shp = target.Fields.Field[target.FindField(target.ShapeFieldName)];
				IGeometryDef def = shp.GeometryDef;
				if (def.HasM != ((IMAware)geom).MAware)
				{
					geom = isCloned ? geom : GeometryFactory.Clone(geom);
					isCloned = true;
					GeometryUtils.MakeMAware(geom, aware: def.HasM);
				}

				return geom;
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

			public override IGeometry Shape => _sourceFeature.Shape;
		}

		public class SimpleWorkspace : VirtualWorkspace
		{
			public override esriWorkspaceType WorkspaceType =>
				esriWorkspaceType.esriFileSystemWorkspace;
		}
	}
}
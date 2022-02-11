using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container
{
	[Obsolete("remove")]
	public class IRow
	{ }
	[Obsolete("remove")]
	public class IFeature
	{ }

	[Obsolete("remove")]
	public class ITable
	{ }

	[Obsolete("remove")]
	public class IFeatureClass
	{ }
	[Obsolete("remove")]
	public class IGeoDataset
	{ }

	[Obsolete("remove")]
	public class IDataset
	{ }

	public class ReadOnlyTableFactory : ReadOnlyFeatureClass
	{
		protected static readonly Dictionary<ESRI.ArcGIS.Geodatabase.ITable, ReadOnlyTable> Cache = new Dictionary<ESRI.ArcGIS.Geodatabase.ITable, ReadOnlyTable>();

		public static ReadOnlyFeatureClass Create(
			[NotNull] ESRI.ArcGIS.Geodatabase.IFeatureClass featureClass)
		{
			return (ReadOnlyFeatureClass)Create((ESRI.ArcGIS.Geodatabase.ITable) featureClass);
		}
		public static ReadOnlyTable Create([NotNull] ESRI.ArcGIS.Geodatabase.ITable table)
		{
			if (!Cache.TryGetValue(table, out ReadOnlyTable existing))
			{
				if (table is ESRI.ArcGIS.Geodatabase.IFeatureClass fc)
				{ existing = CreateReadOnlyFeatureClass(fc); }
				else
				{ existing = CreateReadOnlyTable(table); }

				Cache.Add(table, existing);
			}
			return existing;
		}
		public static void ClearCache()
		{
			Cache.Clear();
		}

		private ReadOnlyTableFactory() : base(null)
		{ }
	}
	public class ReadOnlyFeatureClass : ReadOnlyTable, IReadOnlyFeatureClass
	{
		protected static ReadOnlyFeatureClass CreateReadOnlyFeatureClass(ESRI.ArcGIS.Geodatabase.IFeatureClass fc)
		{ return new ReadOnlyFeatureClass(fc); }
		protected ReadOnlyFeatureClass(ESRI.ArcGIS.Geodatabase.IFeatureClass featureClass)
			: base((ESRI.ArcGIS.Geodatabase.ITable)featureClass)
		{ }

		public string ShapeFieldName => FeatureClass.ShapeFieldName;
		public IField AreaField => Commons.AO.Geodatabase.DatasetUtils.GetAreaField(FeatureClass);
		public IField LengthField => Commons.AO.Geodatabase.DatasetUtils.GetLengthField(FeatureClass);
		public IEnvelope Extent => ((ESRI.ArcGIS.Geodatabase.IGeoDataset)FeatureClass).Extent;
		public ISpatialReference SpatialReference => ((ESRI.ArcGIS.Geodatabase.IGeoDataset)FeatureClass).SpatialReference;
		public esriGeometryType ShapeType => FeatureClass.ShapeType;
		protected ESRI.ArcGIS.Geodatabase.IFeatureClass FeatureClass => (ESRI.ArcGIS.Geodatabase.IFeatureClass)Table;
		protected override ReadOnlyRow CreateRow(ESRI.ArcGIS.Geodatabase.IRow row)
		{
			return new ReadOnlyFeature(this, (ESRI.ArcGIS.Geodatabase.IFeature)row);
		}
	}
	public class ReadOnlyTable : IReadOnlyTable
	{
		public static IEnumerable<IReadOnlyRow> EnumRows(IEnumerable<ESRI.ArcGIS.Geodatabase.IRow> rows)
		{
			ESRI.ArcGIS.Geodatabase.ITable current = null;
			ReadOnlyTable table = null;
			foreach (var row in rows)
			{
				ESRI.ArcGIS.Geodatabase.ITable t = row.Table;
				if (t != current)
				{
					table = CreateReadOnlyTable(row.Table);
					current = t;
				}
				yield return table.CreateRow(row);
			}
		}

		protected static ReadOnlyTable CreateReadOnlyTable(ESRI.ArcGIS.Geodatabase.ITable table)
		{ return new ReadOnlyTable(table); }

		private readonly ESRI.ArcGIS.Geodatabase.ITable _table;
		protected ReadOnlyTable(ESRI.ArcGIS.Geodatabase.ITable table)
		{
			_table = table;
		}
		protected ESRI.ArcGIS.Geodatabase.ITable Table => _table;
		ESRI.ArcGIS.esriSystem.IName IReadOnlyDataset.FullName => ((ESRI.ArcGIS.Geodatabase.IDataset)_table).FullName;
		IWorkspace IReadOnlyDataset.Workspace => ((ESRI.ArcGIS.Geodatabase.IDataset)_table).Workspace;
		public string Name => Commons.AO.Geodatabase.DatasetUtils.GetName(_table);
		public IFields Fields => _table.Fields;
		public int FindField(string name) => _table.FindField(name);
		public bool HasOID => _table.HasOID;
		public string OIDFieldName => _table.OIDFieldName;
		public IReadOnlyRow GetRow(int oid) => CreateRow(_table.GetRow(oid));
		public int RowCount(IQueryFilter filter) => _table.RowCount(filter);

		protected virtual ReadOnlyRow CreateRow(ESRI.ArcGIS.Geodatabase.IRow row)
		{
			return new ReadOnlyRow(this, row);
		}

		public IEnumerable<IReadOnlyRow> EnumRows(IQueryFilter filter, bool recycle)
		{
			foreach (var row in new Commons.AO.EnumCursor(_table, filter, recycle))
			{
				yield return CreateRow(row);
			}
		}
	}
	public class ReadOnlyRow : IReadOnlyRow
	{
		public ReadOnlyRow(ReadOnlyTable table, ESRI.ArcGIS.Geodatabase.IRow row)
		{
			Table = table;
			Row = row;
		}

		protected ESRI.ArcGIS.Geodatabase.IRow Row { get; }
		public bool HasOID => Row.HasOID;
		public int OID => Row.OID;
		public object get_Value(int field) => Row.Value[field];
		IReadOnlyTable IReadOnlyRow.Table => Table;
		public ReadOnlyTable Table { get; }
	}

	public class ReadOnlyFeature : ReadOnlyRow, IReadOnlyFeature
	{
		public ReadOnlyFeature(ReadOnlyFeatureClass featureClass, ESRI.ArcGIS.Geodatabase.IFeature feature)
			: base(featureClass, feature)
		{ }
		protected ESRI.ArcGIS.Geodatabase.IFeature Feature => (ESRI.ArcGIS.Geodatabase.IFeature)Row;
		public IEnvelope Extent => Feature.Extent;
		public IGeometry Shape => Feature.Shape;
		public IGeometry ShapeCopy => Feature.ShapeCopy;
		public ReadOnlyFeatureClass FeatureClass => (ReadOnlyFeatureClass)Table;
		public esriFeatureType FeatureType => Feature.FeatureType;
	}
	public interface IInvolvesTables
	{
		[NotNull]
		IList<IReadOnlyTable> InvolvedTables { get; }

		/// <summary>
		/// limits the data to execute corresponding to condition
		/// </summary>
		/// <param name="tableIndex"></param>
		/// <param name="condition"></param>
		void SetConstraint(int tableIndex, [CanBeNull] string condition);

		void SetSqlCaseSensitivity(int tableIndex, bool useCaseSensitiveQaSql);
	}

	public interface ITest : IInvolvesTables
	{
		/// <summary>
		/// thrown before a test on a row is performed
		/// </summary>
		event EventHandler<RowEventArgs> TestingRow;

		/// <summary>
		/// thrown if test detects a mistake
		/// </summary>
		event EventHandler<QaErrorEventArgs> QaError;

		/// <summary>
		/// Executes test over entire table
		/// </summary>
		/// <returns></returns>
		int Execute();

		/// <summary>
		/// executes test for objects within or cutting boundingBox
		/// </summary>
		/// <param name="boundingBox"></param>
		/// <returns></returns>
		int Execute([NotNull] IEnvelope boundingBox);

		/// <summary>
		/// executes test for objects within or cutting area
		/// </summary>
		/// <param name="area"></param>
		/// <returns></returns>
		int Execute([NotNull] IPolygon area);

		/// <summary>
		/// executes test for objects within selection
		/// </summary>
		/// <param name="selection"></param>
		/// <returns></returns>
		int Execute([NotNull] IEnumerable<IReadOnlyRow> selection);

		/// <summary>
		/// executes test for specified row
		/// </summary>
		/// <param name="row"></param>
		/// <returns></returns>
		int Execute([NotNull] IReadOnlyRow row);

		/// <summary>
		/// limits the execute area to area
		/// </summary>
		/// <param name="area"></param>
		void SetAreaOfInterest([CanBeNull] IPolygon area);
	}

	public interface IFilterTest
	{
		[CanBeNull]
		IReadOnlyList<IIssueFilter> IssueFilters { get; }

		[CanBeNull]
		IReadOnlyList<IRowFilter> GetRowFilters(int tableIndex);
	}

	public interface IFilterEditTest : IFilterTest
	{
		void SetIssueFilters([CanBeNull] string expression, IList<IIssueFilter> issueFilters);

		void SetRowFilters(int tableIndex, [CanBeNull] string expression,
											 [CanBeNull] IReadOnlyList<IRowFilter> rowFilters);
	}

	public interface ITableTransformer : IInvolvesTables
	{
		object GetTransformed();

		string TransformerName { get; set; }
	}

	public interface ITableTransformer<out T> : ITableTransformer
	{
		new T GetTransformed();
	}

	public interface IHasSearchDistance
	{
		double SearchDistance { get; }
	}

	public interface ITransformedValue
	{
		[NotNull]
		IList<IReadOnlyTable> InvolvedTables { get; }

		ISearchable DataContainer { get; set; }
	}

	public interface ITransformedTable
	{
		void SetKnownTransformedRows([CanBeNull] IEnumerable<IReadOnlyRow> knownRows);

		bool NoCaching { get; }
	}

	public interface INamedFilter : IInvolvesTables
	{
		string Name { get; set; }
	}

	public interface IRowFilter : INamedFilter
	{
		bool VerifyExecute(IReadOnlyRow row);
	}

	public interface IIssueFilter : INamedFilter
	{
		bool Check(QaErrorEventArgs args);
	}
}

using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase
{
	/// <summary>
	/// A Wrapper class around IRow, where object values can be accessed by special ways.
	/// It is mainly intended for read only operations on rows
	/// When setting values, the underlying row is captured
	/// </summary>
	public class RowProxy : IRow
	{
		[CanBeNull] private IRow _row;
		private readonly long _oid;

		/// <summary>
		/// Initializes a new instance of the <see cref="RowProxy"/> class.
		/// </summary>
		/// <param name="table">The table.</param>
		/// <param name="oid">The oid.</param>
		public RowProxy([NotNull] ITable table, long oid)
		{
			Assert.ArgumentNotNull(table, nameof(table));

			Table = table;
			_oid = oid;
		}

		public void Delete()
		{
			GetObject().Delete();
		}

		public IFields Fields => Table.Fields;

		public bool HasOID => Table.HasOID;

#if Server11 || ARCGIS_12_0_OR_GREATER
		public long OID => _oid;
#else
		public int OID => (int) _oid;
#endif

		public void Store()
		{
			GetObject().Store();
		}

		[NotNull]
		public ITable Table { get; }

		public object get_Value(int fieldIndex)
		{
			return _row == null
				       ? GetValueCore(fieldIndex)
				       : _row.Value[fieldIndex];
		}

		public void set_Value(int fieldIndex, object value)
		{
			GetObject().set_Value(fieldIndex, value);
		}

		private IRow GetObject()
		{
			return _row ?? (_row = GetRow());
		}

		private IRow GetRow()
		{
#if Server11
			long oid = _oid;
#else
			int oid = (int) _oid;
#endif

			return Table.GetRow(oid);
		}

		protected virtual object GetValueCore(int fieldIndex)
		{
			return GetObject().Value[fieldIndex];
		}
	}
}

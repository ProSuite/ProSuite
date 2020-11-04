using System;
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
	[CLSCompliant(false)]
	public class RowProxy : IRow
	{
		[CanBeNull] private IRow _row;
		private readonly int _oid;

		/// <summary>
		/// Initializes a new instance of the <see cref="RowProxy"/> class.
		/// </summary>
		/// <param name="table">The table.</param>
		/// <param name="oid">The oid.</param>
		public RowProxy([NotNull] ITable table, int oid)
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

		public int OID => _oid;

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
			return _row ?? (_row = Table.GetRow(_oid));
		}

		protected virtual object GetValueCore(int fieldIndex)
		{
			return GetObject().Value[fieldIndex];
		}
	}
}

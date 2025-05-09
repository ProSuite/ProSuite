using System;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;

namespace ProSuite.Commons.AO.Geodatabase.GdbSchema
{
	/// <summary>
	/// Gdb IObject implementation that can be instantiated in memory that typically represents
	/// an existing feature on the client. Its parent can be a real object class or a fake
	/// <see cref="GdbTable"/> also provided through the wire.
	/// </summary>
	public class GdbRow : VirtualRow, IRowSubtypes
	{
		[NotNull] private readonly GdbTable _gdbTable;

		protected IValueList ValueSet { get; }

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="GdbRow"/> class.
		/// </summary>
		/// <param name="oid">The object ID of the row. It will be added to the row values using
		/// set_Value, except if it is a negative number, in which case the
		/// <see cref="GdbRow.HasOID"/>HasOID property will return false.</param>
		/// <param name="gdbTable">The table which determines the row's schema.</param>
		/// <param name="valueList">The optional value list implementation that shall be used to
		/// store and retrieve the row values.</param>
		public GdbRow(long oid, [NotNull] GdbTable gdbTable,
		              [CanBeNull] IValueList valueList = null)
		{
			_oid = oid;
			_gdbTable = gdbTable;

			ValueSet = valueList ?? new PropertySetValueList();

			// NOTE: In AO, if a a query filter excludes the OBJECTID field, the row has no OID
			// regardless of the table having one. 
			if (oid >= 0)
			{
				if (_gdbTable.OidFieldIndex >= 0)
				{
					set_Value(_gdbTable.OidFieldIndex, oid);
				}
			}
		}

		#endregion

		public bool StoreCalled { get; private set; }

		public bool DeleteCalled { get; private set; }

		/// <summary>
		/// Sets a new OID but does not change the value list. It is the caller's
		/// responsibility to ensure the correctness of the values.
		/// </summary>
		/// <param name="newOid"></param>
		public void Recycle(int newOid)
		{
			_oid = newOid;

			RecycleCore();
		}

		public bool Equals(IObject other)
		{
			if (ReferenceEquals(null, other))
				return false;
			if (ReferenceEquals(this, other))
				return true;

			return OID == other.OID && _gdbTable.Equals(other.Table);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
				return false;
			if (ReferenceEquals(this, obj))
				return true;
			if (obj.GetType() != GetType())
				return false;

			return Equals((GdbRow) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return (OID.GetHashCode() * 397) ^ _gdbTable.GetHashCode();
			}
		}

		#region IObject implementation

		public override void Store()
		{
			StoreCalled = true;

			StoreCore();
		}

		protected virtual void StoreCore() { }

		public override void Delete()
		{
			DeleteCalled = true;
		}

		private long _oid;

		public override bool HasOID => _gdbTable.HasOID && _oid != -1;

		public override long OID => _oid < 0
			                            ? throw new InvalidOperationException("Row has no OID")
			                            : _oid;

		public override VirtualTable Table => _gdbTable;
		//		public GdbTable Table => _gdbTable;

		public override object get_Value(int index)
		{
			var result = ValueSet.GetValue(index);

			var uidResult = result as IUID;

			if (uidResult != null)
				// This is how ArcObjects behaves:
				return uidResult.Value as string;

			return result;
		}

		public sealed override void set_Value(int index, object value)
		{
			if (value == null)
			{
				value = DBNull.Value;
			}

			ValueSet.SetValue(index, value);
		}

		#endregion

		#region IRowSubtypes implementation

		public void InitDefaultValues() { }

		public int SubtypeCode { get; set; }

		#endregion

		protected virtual void RecycleCore() { }
	}
}

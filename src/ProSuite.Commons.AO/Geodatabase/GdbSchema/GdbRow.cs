using System;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

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

		public GdbRow(int oid, [NotNull] GdbTable gdbTable,
		              [CanBeNull] IValueList valueList = null)
		{
			_oid = oid;
			_gdbTable = gdbTable;

			ValueSet = valueList ?? new PropertySetValueList();

			var oidFieldIndex = _gdbTable.FindField(_gdbTable.OIDFieldName);

			if (oidFieldIndex >= 0)
			{
				set_Value(oidFieldIndex, oid);
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
				return (OID * 397) ^ _gdbTable.GetHashCode();
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

		private int _oid;
		public override int OID => _oid;

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

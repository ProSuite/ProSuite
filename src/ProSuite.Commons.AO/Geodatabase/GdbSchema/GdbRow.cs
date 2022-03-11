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
	public class GdbRow : VirtualRow, IRowSubtypes // IObject, IEquatable<IObject>, IReadOnlyRow
	{
		[NotNull] private readonly GdbTable _gdbTable;

		// Using property set to support the same COM-release calls (e.g. on GdbFeature.Shape) as
		// on a real feature. Otherwise InvalidComObjectException can happen when accessing the same
		// object again (e.g. by calling GdbFeature.Shape again)
		protected IPropertySet ValueSet { get; } = new PropertySet();

		#region Constructors

		public GdbRow(int oid, [NotNull] GdbTable gdbTable)
		{
			OID = oid;
			_gdbTable = gdbTable;

			var oidFieldIndex = _gdbTable.FindField(_gdbTable.OIDFieldName);

			if (oidFieldIndex >= 0)
			{
				set_Value(oidFieldIndex, oid);
			}
		}

		#endregion

		public bool StoreCalled { get; private set; }

		public bool DeleteCalled { get; private set; }

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

		public int OID { get; }

		public GdbTable Table => _gdbTable;

		public override object get_Value(int index)
		{
			var name = Convert.ToString(index);

			var result = PropertySetUtils.HasProperty(ValueSet, name)
				             ? ValueSet.GetProperty(name)
				             : null;

			var uidResult = result as IUID;

			if (uidResult != null)
				// This is how ArcObjects behaves:
				return uidResult.Value as string;

			return result;
		}

		public override void set_Value(int index, object value)
		{
			if (value == null)
			{
				value = DBNull.Value;
			}

			ValueSet.SetProperty(Convert.ToString(index), value);
		}

		#endregion

		#region IRowSubtypes implementation

		public void InitDefaultValues() { }

		public int SubtypeCode { get; set; }

		#endregion
	}
}

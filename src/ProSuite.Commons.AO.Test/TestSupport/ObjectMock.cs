using System;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Test.TestSupport
{
	public class ObjectMock : IObject, IRowSubtypes
	{
		[NotNull] private readonly ObjectClassMock _objectClassMock;

		// Using property set to support the same COM-release calls (e.g. on FeatureMock.Shape) as
		// on a real feature. Otherwise InvalidComObjectException can happen when accessing the same
		// object again (e.g. by calling FeatureMock.Shape again)
		protected readonly IPropertySet _valueSet = new PropertySet();

		#region Constructors

		internal ObjectMock(int oid, [NotNull] ObjectClassMock objectClassMock)
		{
			OID = oid;
			_objectClassMock = objectClassMock;

			int oidFieldIndex = _objectClassMock.FindField(_objectClassMock.OIDFieldName);

			if (oidFieldIndex >= 0)
			{
				set_Value(oidFieldIndex, oid);
			}
		}

		#endregion

		public void AddFields(params IField[] fields)
		{
			_objectClassMock.AddFields(fields);
		}

		public bool StoreCalled { get; private set; }

		public bool DeleteCalled { get; private set; }

		#region IObject implementation

		public void Store()
		{
			StoreCalled = true;

			StoreCore();
		}

		protected virtual void StoreCore() { }

		public void Delete()
		{
			DeleteCalled = true;
		}

		public IFields Fields => _objectClassMock.Fields;

		public bool HasOID => _objectClassMock.HasOID;

		public int OID { get; }

		public ITable Table => _objectClassMock;

		public IObjectClass Class => _objectClassMock;

		public object get_Value(int index)
		{
			string name = Convert.ToString(index);

			object result = PropertySetUtils.HasProperty(_valueSet, name)
				                ? _valueSet.GetProperty(name)
				                : null;

			var uidResult = result as IUID;

			if (uidResult != null)
			{
				// This is how ArcObjects behaves:
				return uidResult.Value as string;
			}

			return result;
		}

		public void set_Value(int index, object value)
		{
			_valueSet.SetProperty(Convert.ToString(index), value);
		}

		#endregion

		#region IRowSubtypes implementation

		public void InitDefaultValues() { }

		public int SubtypeCode { get; set; }

		#endregion

		public bool Equals(IObject other)
		{
			if (ReferenceEquals(null, other))
				return false;
			if (ReferenceEquals(this, other))
				return true;

			return OID == other.OID && _objectClassMock.Equals(other.Table);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
				return false;
			if (ReferenceEquals(this, obj))
				return true;
			if (obj.GetType() != GetType())
				return false;

			return Equals((ObjectMock) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return (OID * 397) ^ _objectClassMock.GetHashCode();
			}
		}
	}
}

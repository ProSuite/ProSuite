namespace ESRI.ArcGIS.Geodatabase
{
	public interface IObject : IRow
	{
		IObjectClass Class { get; }
	}

	public interface IRow : IRowBuffer
	{
		bool HasOID { get; }

		long OID { get; }

		ITable Table { get; }

		void Store();

		void Delete();
	}

	public interface IRowBuffer
	{
		object get_Value(int index);

		void set_Value(int index, object value);

		IFields Fields { get; }
	}
}
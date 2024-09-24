namespace ProSuite.GIS.Geodatabase.API
{
	public interface IClass : IDataset
	{
		int FindField(string name);

		IFields Fields { get; }

		//IIndexes Indexes { get; }

		void AddField(IField field);

		void DeleteField(IField field);

		//void AddIndex(IIndex Index);

		//void DeleteIndex(IIndex Index);

		bool HasOID { get; }

		string OIDFieldName { get; }

		//UID CLSID { get; }

		//UID EXTCLSID { get; }

		//object Extension { get; }

		//IPropertySet ExtensionProperties { get; }
	}
}

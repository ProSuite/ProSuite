using ESRI.ArcGIS.Geodatabase;

namespace ProSuite.Commons.AO.Geodatabase
{
	public class ReadOnlyJoinedFeatureClass : ReadOnlyFeatureClass
	{
		protected static ReadOnlyJoinedFeatureClass CreateReadOnlyJoinedFeatureClass(IFeatureClass fc)
		{
			return new ReadOnlyJoinedFeatureClass(fc);
		}

		protected ReadOnlyJoinedFeatureClass(IFeatureClass joinedTable) :
			base(joinedTable)
		{ }

		public override int FindField(string name)
		{
			return ReadOnlyJoinedTable.FindField(BaseTable, name);
		}
	}
}

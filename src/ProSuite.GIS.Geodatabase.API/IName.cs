namespace ESRI.ArcGIS.Geodatabase
{
	public interface IName
	{
		string NameString { set; get; }

		object Open();
	}
}

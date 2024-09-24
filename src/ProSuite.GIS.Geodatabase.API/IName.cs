namespace ProSuite.GIS.Geodatabase.API
{
	public interface IName
	{
		string NameString { set; get; }

		object Open();
	}
}

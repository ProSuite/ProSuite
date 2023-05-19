namespace ProSuite.Commons.Db
{
	/// <summary>
	/// Encapsulates the Esri field types into independent enum but with corresponding values.
	/// </summary>
	public enum FieldType
	{
		ShortInteger = 0, // SmallInteger
		LongInteger = 1, // Integer
		Float = 2, // Single
		Double = 3, // Double
		Text = 4, // String
		Date = 5, //Date
		ObjectID = 6, // OID
		Geometry = 7, // Geometry
		Blob = 8, // Blob
		Raster = 9, // Raster
		Guid = 10, // GUID
		GlobalID = 11, // GlobalID
		Xml = 12 // XML
	}
}

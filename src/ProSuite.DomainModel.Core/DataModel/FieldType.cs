using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.DataModel
{
	/// <summary>
	/// Encapsulates the ArcObjects field types into independent enum but with corresponding
	/// values.
	/// </summary>
	public enum FieldType
	{
		[UsedImplicitly] ShortInteger = 0, // esriFieldTypeSmallInteger
		[UsedImplicitly] LongInteger = 1, // esriFieldTypeInteger
		[UsedImplicitly] Float = 2, // esriFieldTypeSingle
		[UsedImplicitly] Double = 3, // esriFieldTypeDouble
		[UsedImplicitly] Text = 4, // esriFieldTypeString
		[UsedImplicitly] Date = 5, //esriFieldTypeDate
		[UsedImplicitly] ObjectID = 6, // esriFieldTypeOID
		[UsedImplicitly] Geometry = 7, // esriFieldTypeGeometry
		[UsedImplicitly] Blob = 8, // esriFieldTypeBlob
		[UsedImplicitly] Raster = 9, // esriFieldTypeRaster
		[UsedImplicitly] Guid = 10, // esriFieldTypeGUID
		[UsedImplicitly] GlobalID = 11, // esriFieldTypeGlobalID
		[UsedImplicitly] Xml = 12 // esriFieldTypeXML
	}
}

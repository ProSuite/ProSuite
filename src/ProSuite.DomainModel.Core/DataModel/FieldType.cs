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
		[UsedImplicitly] Double = 3,
		[UsedImplicitly] Text = 4,
		[UsedImplicitly] Date = 5,
		[UsedImplicitly] ObjectID = 6,
		[UsedImplicitly] Geometry = 7,
		[UsedImplicitly] Blob = 8,
		[UsedImplicitly] Raster = 9,
		[UsedImplicitly] Guid = 10,
		[UsedImplicitly] GlobalID = 11,
		[UsedImplicitly] Xml = 12
	}
}
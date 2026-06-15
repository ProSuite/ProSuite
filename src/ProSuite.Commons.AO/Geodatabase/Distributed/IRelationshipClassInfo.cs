using ESRI.ArcGIS.Geodatabase;

namespace ProSuite.Commons.AO.Geodatabase.Distributed
{
	public interface IRelationshipClassInfo
	{
		IRelationshipClass RelationshipClass { get; }

		RelationshipExtractDirection RelExtractDirection { get; set; }

		bool Excluded { get; set; }
	}
}

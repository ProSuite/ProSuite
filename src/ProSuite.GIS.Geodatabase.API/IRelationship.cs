namespace ESRI.ArcGIS.Geodatabase
{
	public interface IRelationship
	{
		IRelationshipClass RelationshipClass { get; }

		IObject OriginObject { get; }

		IObject DestinationObject { get; }
	}
}

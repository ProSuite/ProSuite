namespace ProSuite.GIS.Geodatabase.API
{
	public interface IRelationship
	{
		IRelationshipClass RelationshipClass { get; }

		IObject OriginObject { get; }

		IObject DestinationObject { get; }
	}
}

using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase
{
	public static class AssociationDescriptionUtils
	{
		[NotNull]
		public static AssociationDescription CreateAssociationDescription(
			[NotNull] IRelationshipClass relationshipClass)
		{
			// TODO test
			if (relationshipClass.Cardinality ==
			    esriRelCardinality.esriRelCardinalityManyToMany)
			{
				return new ManyToManyAssociationDescription(
					(ITable) relationshipClass.DestinationClass,
					relationshipClass.DestinationPrimaryKey,
					(ITable) relationshipClass.OriginClass,
					relationshipClass.OriginPrimaryKey,
					(ITable) relationshipClass,
					relationshipClass.DestinationForeignKey,
					relationshipClass.OriginForeignKey);
			}

			return new ForeignKeyAssociationDescription(
				(ITable) relationshipClass.DestinationClass,
				relationshipClass.OriginForeignKey,
				(ITable) relationshipClass.OriginClass,
				relationshipClass.OriginPrimaryKey);
		}
	}
}

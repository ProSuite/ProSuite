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
			if (relationshipClass.Cardinality ==
			    esriRelCardinality.esriRelCardinalityManyToMany)
			{
				return new ManyToManyAssociationDescription(
					ReadOnlyTableFactory.Create((ITable) relationshipClass.DestinationClass),
					relationshipClass.DestinationPrimaryKey,
					ReadOnlyTableFactory.Create((ITable) relationshipClass.OriginClass),
					relationshipClass.OriginPrimaryKey,
					ReadOnlyTableFactory.Create((ITable) relationshipClass),
					relationshipClass.DestinationForeignKey,
					relationshipClass.OriginForeignKey);
			}

			return new ForeignKeyAssociationDescription(
				       ReadOnlyTableFactory.Create((ITable) relationshipClass.DestinationClass),
				       relationshipClass.OriginForeignKey,
				       ReadOnlyTableFactory.Create((ITable) relationshipClass.OriginClass),
				       relationshipClass.OriginPrimaryKey)
			       {
				       HasOneToOneCardinality =
					       relationshipClass.Cardinality ==
					       esriRelCardinality.esriRelCardinalityOneToOne
			       };
		}
	}
}

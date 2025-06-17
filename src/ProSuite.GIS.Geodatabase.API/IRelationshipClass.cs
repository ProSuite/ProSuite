using System.Collections.Generic;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.GIS.Geodatabase.API
{
	public interface IRelationshipClass : IDataset
	{
		string OriginPrimaryKey { get; }

		string DestinationPrimaryKey { get; }

		string OriginForeignKey { get; }

		string DestinationForeignKey { get; }

		long RelationshipClassID { get; }

		[NotNull]
		IObjectClass OriginClass { get; }

		[NotNull]
		IObjectClass DestinationClass { get; }

		string ForwardPathLabel { get; }

		string BackwardPathLabel { get; }

		esriRelCardinality Cardinality { get; }

		bool IsAttributed { get; }

		bool IsComposite { get; }

		[NotNull]
		IRelationship CreateRelationship([NotNull] IObject originObject,
		                                 [NotNull] IObject destinationObject);

		[CanBeNull]
		IRelationship GetRelationship([NotNull] IObject originObject,
		                              [NotNull] IObject destinationObject);

		void DeleteRelationship([NotNull] IObject originObject,
		                        [NotNull] IObject destinationObject);

		IEnumerable<IObject> GetObjectsRelatedToObject([NotNull] IObject anObject);

		void DeleteRelationshipsForObject([NotNull] IObject anObject);

		IEnumerable<IObject> GetObjectsRelatedToObjectSet([NotNull] IList<IObject> objectList);

		IEnumerable<KeyValuePair<T, IObject>> GetObjectsMatchingObjectSet<T>(
			[NotNull] IEnumerable<T> sourceObjects) where T : IObject;

		void DeleteRelationshipsForObjectSet(ISet anObjectSet);
	}

	public enum esriRelRole
	{
		esriRelRoleAny = 1,
		esriRelRoleOrigin = 2,
		esriRelRoleDestination = 3,
	}
}

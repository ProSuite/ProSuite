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

		IObjectClass OriginClass { get; }

		IObjectClass DestinationClass { get; }

		//IFeatureDataset FeatureDataset { get; }

		string ForwardPathLabel { get; }

		string BackwardPathLabel { get; }

		esriRelCardinality Cardinality { get; }

		//esriRelNotification Notification { get; }

		bool IsAttributed { get; }

		bool IsComposite { get; }

		IRelationship CreateRelationship(IObject originObject, IObject destinationObject);

		IRelationship GetRelationship(IObject originObject, IObject destinationObject);

		void DeleteRelationship(IObject originObject, IObject destinationObject);

		IEnumerable<IObject> GetObjectsRelatedToObject(IObject anObject);

		//IEnumRelationship GetRelationshipsForObject(IObject anObject);

		void DeleteRelationshipsForObject(IObject anObject);

		IEnumerable<IObject> GetObjectsRelatedToObjectSet(IList<IObject> objectList);
		//IEnumerable<IObject> GetObjectsRelatedToObjectSet(ISet anObjectSet);

		//IEnumRelationship GetRelationshipsForObjectSet(ISet anObjectSet);

		IEnumerable<KeyValuePair<T, IObject>> GetObjectsMatchingObjectSet<T>(
			[NotNull] IEnumerable<T> sourceObjects) where T : IObject;

		void DeleteRelationshipsForObjectSet(ISet anObjectSet);

		//IEnumRule RelationshipRules { get; }

		//void AddRelationshipRule(IRule Rule);

		//void DeleteRelationshipRule(IRule Rule);
	}

	public enum esriRelRole
	{
		esriRelRoleAny = 1,
		esriRelRoleOrigin = 2,
		esriRelRoleDestination = 3,
	}
}

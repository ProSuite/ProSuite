using ArcGIS.Core.Data;
using ProSuite.GIS.Geodatabase.API;

namespace ProSuite.GIS.Geodatabase.AGP
{
	public class ArcRelationship : IRelationship
	{
		private readonly Relationship _relationship;
		private readonly IObject _originRow;
		private readonly IObject _destinationRow;
		private readonly RelationshipClass _relationshipClass;

		public ArcRelationship(Relationship relationship,
		                       RelationshipClass relationshipClass)
		{
			_relationship = relationship;

			_relationshipClass = relationshipClass;
		}

		public ArcRelationship(IObject originRow,
		                       IObject destinationRow,
		                       RelationshipClass relationshipClass)
		{
			_originRow = originRow;
			_destinationRow = destinationRow;

			_relationshipClass = relationshipClass;
		}

		#region Implementation of IRelationship

		public IRelationshipClass RelationshipClass => new ArcRelationshipClass(_relationshipClass);

		public IObject OriginObject
		{
			get
			{
				if (_originRow != null)
				{
					return _originRow;
				}

				Row row = _relationship.GetOriginRow();

				return (IObject) ArcUtils.ToArcRow(row);
			}
		}

		public IObject DestinationObject
		{
			get
			{
				if (_destinationRow != null)
				{
					return _destinationRow;
				}

				Row row = _relationship.GetDestinationRow();

				return (IObject) ArcUtils.ToArcRow(row);
			}
		}

		#endregion
	}
}

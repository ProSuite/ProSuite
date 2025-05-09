using ArcGIS.Core.Data;
using ProSuite.GIS.Geodatabase.API;

namespace ProSuite.GIS.Geodatabase.AGP
{
	public class ArcRelationship : IRelationship
	{
		private readonly Relationship _relationship;
		private readonly IObject _originRow;
		private readonly IObject _destinationRow;

		public ArcRelationship(Relationship relationship,
		                       RelationshipClass proRelationshipClass)
		{
			_relationship = relationship;

			RelationshipClass = new ArcRelationshipClass(proRelationshipClass);
		}

		public ArcRelationship(IObject originRow,
		                       IObject destinationRow,
		                       IRelationshipClass relationshipClass)
		{
			_originRow = originRow;
			_destinationRow = destinationRow;

			RelationshipClass = relationshipClass;
		}

		#region Implementation of IRelationship

		public IRelationshipClass RelationshipClass { get; private set; }

		public IObject OriginObject
		{
			get
			{
				if (_originRow != null)
				{
					return _originRow;
				}

				Row row = _relationship.GetOriginRow();

				return ArcGeodatabaseUtils.ToArcRow(row);
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

				return ArcGeodatabaseUtils.ToArcRow(row);
			}
		}

		#endregion
	}
}

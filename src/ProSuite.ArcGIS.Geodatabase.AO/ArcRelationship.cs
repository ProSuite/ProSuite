extern alias EsriGeodatabase;
namespace ESRI.ArcGIS.Geodatabase
{
	public class ArcRelationship : IRelationship
	{
		private readonly EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IRelationship _relationship;

		public ArcRelationship(EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IRelationship relationship)
		{
			_relationship = relationship;
		}

		#region Implementation of IRelationship

		public IRelationshipClass RelationshipClass
		{
			get
			{
				var aoRelClass = _relationship.RelationshipClass;
				return new ArcRelationshipClass(aoRelClass);
			}
		}

		public IObject OriginObject
		{
			get
			{
				var aoOriginObj = _relationship.OriginObject;

				return aoOriginObj is EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IFeature aoFeature
					? new ArcFeature(aoFeature)
					: new ArcRow(aoOriginObj);
			}
		}

		public IObject DestinationObject
		{
			get
			{
				var aoDestinationObj = _relationship.DestinationObject;

				return aoDestinationObj is EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IFeature aoFeature
					? new ArcFeature(aoFeature)
					: new ArcRow(aoDestinationObj);
			}
		}

		#endregion
	}
}

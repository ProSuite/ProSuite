extern alias EsriGeodatabase;
extern alias EsriSystem;
using ProSuite.ArcGIS.Geodatabase.AO;

namespace ESRI.ArcGIS.Geodatabase
{
	public class ArcRelationshipClass : IRelationshipClass
	{
		private readonly EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IRelationshipClass _aoRelationshipClass;

		public ArcRelationshipClass(EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IRelationshipClass aoRelationshipClass)
		{
			_aoRelationshipClass = aoRelationshipClass;
		}

		public EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IRelationshipClass AoRelationshipClass => _aoRelationshipClass;

		#region Implementation of IRelationshipClass

		public string OriginPrimaryKey => _aoRelationshipClass.OriginPrimaryKey;

		public string DestinationPrimaryKey => _aoRelationshipClass.DestinationPrimaryKey;

		public string OriginForeignKey => _aoRelationshipClass.OriginForeignKey;

		public string DestinationForeignKey => _aoRelationshipClass.DestinationForeignKey;

		public int RelationshipClassID => _aoRelationshipClass.RelationshipClassID;

		public IObjectClass OriginClass => ToArcTable(_aoRelationshipClass.OriginClass);

		public IObjectClass DestinationClass => ToArcTable(_aoRelationshipClass.DestinationClass);

		public string ForwardPathLabel => _aoRelationshipClass.ForwardPathLabel;

		public string BackwardPathLabel => _aoRelationshipClass.BackwardPathLabel;

		public esriRelCardinality Cardinality => (esriRelCardinality)_aoRelationshipClass.Cardinality;

		public bool IsAttributed => _aoRelationshipClass.IsAttributed;

		public bool IsComposite => _aoRelationshipClass.IsComposite;

		public IRelationship CreateRelationship(IObject originObject, IObject destinationObject)
		{
			ArcRow arcOriginObj = (ArcRow)originObject;
			ArcRow arcDestinationObj = (ArcRow)destinationObject;

			var aoRelationship =
				_aoRelationshipClass.CreateRelationship(arcOriginObj.AoObject, arcDestinationObj.AoObject);

			return new ArcRelationship(aoRelationship);
		}

		public IRelationship GetRelationship(IObject originObject, IObject destinationObject)
		{
			var aoOriginObj = ((ArcRow)originObject).AoObject;
			var aoDestinationObj = ((ArcRow)destinationObject).AoObject;

			return new ArcRelationship(_aoRelationshipClass.GetRelationship(aoOriginObj, aoDestinationObj));
		}

		public void DeleteRelationship(IObject originObject, IObject destinationObject)
		{
			var aoOriginObj = ((ArcRow)originObject).AoObject;
			var aoDestinationObj = ((ArcRow)destinationObject).AoObject;

			_aoRelationshipClass.DeleteRelationship(aoOriginObj, aoDestinationObj);
		}

		public ISet GetObjectsRelatedToObject(IObject anObject)
		{
			var aoObject = ((ArcRow)anObject).AoObject;

			EsriSystem::ESRI.ArcGIS.esriSystem.ISet aoSet = _aoRelationshipClass.GetObjectsRelatedToObject(aoObject);

			return new ArcSet(aoSet);
		}

		public void DeleteRelationshipsForObject(IObject anObject)
		{
			var aoObject = ((ArcRow)anObject).AoObject;

			_aoRelationshipClass.DeleteRelationshipsForObject(aoObject);
		}

		public ISet GetObjectsRelatedToObjectSet(ISet anObjectSet)
		{
			var aoInputSet = ((ArcSet)anObjectSet).AoSet;

			EsriSystem::ESRI.ArcGIS.esriSystem.ISet aoSet =
				_aoRelationshipClass.GetObjectsRelatedToObjectSet(aoInputSet);

			return new ArcSet(aoSet);
		}

		public void DeleteRelationshipsForObjectSet(ISet anObjectSet)
		{
			var aoInputSet = ((ArcSet)anObjectSet).AoSet;

			_aoRelationshipClass.DeleteRelationshipsForObjectSet(aoInputSet);
		}

		#endregion

		private static IObjectClass ToArcTable(EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IObjectClass aoTable)
		{
			var result = aoTable is EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IFeatureClass featureClass
				? new ArcFeatureClass(featureClass)
				: (IObjectClass)new ArcTable((EsriGeodatabase::ESRI.ArcGIS.Geodatabase.ITable)aoTable);

			return result;
		}
	}
}

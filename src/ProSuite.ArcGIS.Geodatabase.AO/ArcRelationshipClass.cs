extern alias EsriGeodatabase;
extern alias EsriSystem;
using System.Collections.Generic;
using ProSuite.ArcGIS.Geodatabase.AO;

namespace ESRI.ArcGIS.Geodatabase
{
	public class ArcRelationshipClass : IRelationshipClass
	{
		private readonly EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IRelationshipClass _aoRelationshipClass;
		private readonly EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IDataset _aoDataset;

		public ArcRelationshipClass(EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IRelationshipClass aoRelationshipClass)
		{
			_aoRelationshipClass = aoRelationshipClass;
			_aoDataset = (EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IDataset)aoRelationshipClass;
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

		#region Implementation of IDataset

		public string Name => _aoDataset.Name;

		public IName FullName => new ArcName(_aoDataset.FullName);

		public string BrowseName
		{
			get => _aoDataset.BrowseName;
			set => _aoDataset.BrowseName = value;
		}

		public esriDatasetType Type => (esriDatasetType)_aoDataset.Type;

		public string Category => _aoDataset.Category;

		public IEnumerable<IDataset> Subsets
		{
			get
			{
				EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IEnumDataset enumDataset =
					_aoDataset.Subsets;

				EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IDataset dataset;
				while ((dataset = enumDataset.Next()) != null)
				{
					yield return dataset is EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IFeatureClass
						             ? new ArcFeatureClass((EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IFeatureClass)dataset)
						             : new ArcTable((EsriGeodatabase::ESRI.ArcGIS.Geodatabase.ITable)dataset);
				}

			}
		}

		public IWorkspace Workspace => new ArcWorkspace(_aoDataset.Workspace);

		public bool CanCopy()
		{
			return _aoDataset.CanCopy();
		}

		public bool CanDelete()
		{
			return _aoDataset.CanDelete();
		}

		public void Delete()
		{
			_aoDataset.Delete();
		}

		public bool CanRename()
		{
			return _aoDataset.CanRename();
		}

		public void Rename(string Name)
		{
			_aoDataset.Rename(Name);
		}

		#endregion
	}
}

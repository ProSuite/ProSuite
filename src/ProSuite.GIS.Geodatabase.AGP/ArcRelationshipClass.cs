using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ProSuite.ArcGIS.Geodatabase.AO;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ESRI.ArcGIS.Geodatabase
{
	public class ArcRelationshipClass : IRelationshipClass
	{
		private readonly RelationshipClass _proRelationshipClass;
		private readonly RelationshipClassDefinition _proRelationshipClassDefinition;

		public ArcRelationshipClass(RelationshipClass proRelationshipClass)
		{
			_proRelationshipClass = proRelationshipClass;
			_proRelationshipClassDefinition = proRelationshipClass.GetDefinition();
		}

		public RelationshipClass ProRelationshipClass => _proRelationshipClass;

		#region Implementation of IRelationshipClass

		public string OriginPrimaryKey => _proRelationshipClassDefinition.GetOriginKeyField();

		public string DestinationPrimaryKey
		{
			get
			{
				if (_proRelationshipClassDefinition is
				    AttributedRelationshipClassDefinition attributedRelClass)
				{
					return attributedRelClass.GetDestinationKeyField();
				}

				throw new InvalidOperationException(
					"Only attributed (m:n) relationship classes have a destination key field");
			}
		}

		public string OriginForeignKey =>
			_proRelationshipClassDefinition.GetOriginForeignKeyField();

		public string DestinationForeignKey
		{
			get
			{
				if (_proRelationshipClassDefinition is
				    AttributedRelationshipClassDefinition attributedRelClass)
				{
					return attributedRelClass.GetDestinationForeignKeyField();
				}

				throw new InvalidOperationException(
					"Only attributed (m:n) relationship classes have a destination foreign key field");
			}
		}

		public long RelationshipClassID => _proRelationshipClass.GetID();

		public IObjectClass OriginClass
		{
			get
			{
				global::ArcGIS.Core.Data.Geodatabase geodatabase =
					_proRelationshipClass.GetDatastore() as global::ArcGIS.Core.Data.Geodatabase;

				Assert.NotNull(geodatabase, "No geodatabase could be retrieved from rel class");

				string originClassName = _proRelationshipClassDefinition.GetOriginClass();

				Table originClass = geodatabase.OpenDataset<Table>(originClassName);

				return ArcUtils.ToArcTable(originClass);
			}
		}

		public IObjectClass DestinationClass
		{
			get
			{
				global::ArcGIS.Core.Data.Geodatabase geodatabase =
					_proRelationshipClass.GetDatastore() as global::ArcGIS.Core.Data.Geodatabase;

				Assert.NotNull(geodatabase, "No geodatabase could be retrieved from rel class");

				string destinationClassName = _proRelationshipClassDefinition.GetDestinationClass();

				Table destinationClass = geodatabase.OpenDataset<Table>(destinationClassName);

				return ArcUtils.ToArcTable(destinationClass);
			}
		}

		public string ForwardPathLabel => _proRelationshipClassDefinition.GetForwardPathLabel();

		public string BackwardPathLabel => _proRelationshipClassDefinition.GetBackwardPathLabel();

		public esriRelCardinality Cardinality =>
			(esriRelCardinality) _proRelationshipClassDefinition.GetCardinality();

		public bool IsAttributed =>
			_proRelationshipClassDefinition is AttributedRelationshipClassDefinition;

		public bool IsComposite => _proRelationshipClassDefinition.IsComposite();

		public IRelationship CreateRelationship(IObject originObject, IObject destinationObject)
		{
			ArcRow arcOriginObj = (ArcRow) originObject;
			ArcRow arcDestinationObj = (ArcRow) destinationObject;

			var aoRelationship =
				_proRelationshipClass.CreateRelationship(arcOriginObj.ProRow,
				                                         arcDestinationObj.ProRow);

			return new ArcRelationship(aoRelationship, _proRelationshipClass);
		}

		public IRelationship GetRelationship(IObject originObject, IObject destinationObject)
		{
			throw new NotImplementedException();

			var aoOriginObj = ((ArcRow) originObject).ProRow;
			var aoDestinationObj = ((ArcRow) destinationObject).ProRow;

			// TODO: Is this the correct way to get an existing relationship?
			Relationship relationship =
				_proRelationshipClass.CreateRelationship(aoOriginObj, aoDestinationObj);

			return new ArcRelationship(originObject, destinationObject, _proRelationshipClass);
		}

		public void DeleteRelationship(IObject originObject, IObject destinationObject)
		{
			var originRow = ((ArcRow) originObject).ProRow;
			var destinationRow = ((ArcRow) destinationObject).ProRow;

			_proRelationshipClass.DeleteRelationship(originRow, destinationRow);
		}

		public IEnumerable<IObject> GetObjectsRelatedToObject(IObject anObject)
		{
			long sourceOid = anObject.OID;
			string sourceClassName = anObject.Class.Name;

			IEnumerable<Row> relatedObjects = GetRelatedObjects(sourceOid, sourceClassName);

			return relatedObjects.Select(o => ArcUtils.ToArcRow(o));
		}

		public void DeleteRelationshipsForObject(IObject anObject)
		{
			//long sourceOid = anObject.OID;
			//string sourceClassName = anObject.Class.Name;

			ArcRow sourceRow = (ArcRow) anObject;

			Row sourceRowProRow = sourceRow.ProRow;

			DeleteRelationshipsFor(sourceRowProRow);
		}

		private void DeleteRelationshipsFor(Row sourceRow)
		{
			string sourceClassName = sourceRow.GetTable().GetName();

			foreach (Row relatedRow in GetRelatedObjects(sourceRow.GetObjectID(), sourceClassName))
			{
				if (sourceClassName == _proRelationshipClassDefinition.GetOriginClass())
				{
					_proRelationshipClass.DeleteRelationship(sourceRow, relatedRow);
				}
				else
				{
					Assert.True(
						sourceClassName == _proRelationshipClassDefinition.GetDestinationClass(),
						"Object is neither origin nor destination of relationship class");

					_proRelationshipClass.DeleteRelationship(relatedRow, sourceRow);
				}
			}
		}

		public IEnumerable<IObject> GetObjectsRelatedToObjectSet(ISet anObjectSet)
		{
			if (anObjectSet.Count == 0)
			{
				yield break;
			}

			ICollection<Row> proRows = ((ArcSet) anObjectSet).ProRows;

			List<long> objectIds = proRows.Select(row => row.GetObjectID())
			                              .ToList();

			string sourceClassName = proRows.Select(r => r.GetTable().GetName()).First();

			IEnumerable<Row> relatedObjects = GetRelatedObjects(objectIds, sourceClassName);

			foreach (ArcRow related in relatedObjects.Select(o => ArcUtils.ToArcRow(o)))
			{
				yield return related;
			}
		}

		public void DeleteRelationshipsForObjectSet(ISet anObjectSet)
		{
			if (anObjectSet.Count == 0)
			{
				return;
			}

			ICollection<Row> proRows = ((ArcSet) anObjectSet).ProRows;

			foreach (Row proRow in proRows)
			{
				DeleteRelationshipsFor(proRow);
			}

			//List<long> objectIds = proRows.Select(row => row.GetObjectID())
			//                              .ToList();

			//string sourceClassName = proRows.Select(r => r.GetTable().GetName()).First();

			//IEnumerable<Row> relatedObjects = GetRelatedObjects(objectIds, sourceClassName);

			//foreach (Row relatedObject in relatedObjects)
			//{
			//	DeleteRelationshipsForObject();
			//}

			//_proRelationshipClass.DeleteRelationship(aoInputSet);
		}

		#endregion

		private IEnumerable<Row> GetRelatedObjects(long sourceOid, string sourceClassName)
		{
			return GetRelatedObjects(new[] { sourceOid }, sourceClassName);
		}

		private IEnumerable<Row> GetRelatedObjects([NotNull] IEnumerable<long> sourceOids,
		                                           [NotNull] string sourceClassName)
		{
			IReadOnlyList<Row> relatedObjects;

			if (sourceClassName == _proRelationshipClassDefinition.GetOriginClass())
			{
				relatedObjects =
					_proRelationshipClass.GetRowsRelatedToOriginRows(sourceOids);
			}
			else
			{
				Assert.True(
					sourceClassName == _proRelationshipClassDefinition.GetDestinationClass(),
					"Object is neither origin nor destination of relationship class");

				relatedObjects =
					_proRelationshipClass.GetRowsRelatedToDestinationRows(sourceOids);
			}

			return relatedObjects;
		}

		#region Implementation of IDataset

		public string Name => _proRelationshipClassDefinition.GetName();

		public IName FullName => new ArcName(this);

		public string BrowseName
		{
			get => throw new NotImplementedException();
			set => throw new NotImplementedException();
		}

		public esriDatasetType Type => esriDatasetType.esriDTRelationshipClass;

		public string Category => throw new NotImplementedException();

		public IEnumerable<IDataset> Subsets
		{
			get
			{
				throw new NotImplementedException();

				//EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IEnumDataset enumDataset =
				//	_aoDataset.Subsets;

				//EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IDataset dataset;
				//while ((dataset = enumDataset.Next()) != null)
				//{
				//	yield return dataset is EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IFeatureClass
				//		             ? new ArcFeatureClass((EsriGeodatabase::ESRI.ArcGIS.Geodatabase.IFeatureClass)dataset)
				//		             : new ArcTable((EsriGeodatabase::ESRI.ArcGIS.Geodatabase.ITable)dataset);
				//}
			}
		}

		public IWorkspace Workspace => new ArcWorkspace(
			(global::ArcGIS.Core.Data.Geodatabase) _proRelationshipClass.GetDatastore());

		public bool CanCopy()
		{
			return false;
		}

		public bool CanDelete()
		{
			return false;
		}

		public void Delete()
		{
			throw new NotImplementedException();
		}

		public bool CanRename()
		{
			return false;
		}

		public void Rename(string name)
		{
			throw new NotImplementedException();
		}

		public object NativeImplementation => ProRelationshipClass;

		#endregion

		#region Equality members

		// TODO: Consider implementing operator == / !=

		public bool Equals(ArcRelationshipClass other)
		{
			if (other == null)
			{
				return false;
			}

			return ProRelationshipClass.Handle.Equals(other.ProRelationshipClass.Handle);
		}

		public override bool Equals(object other)
		{
			if (ReferenceEquals(null, other)) return false;

			if (ReferenceEquals(this, other)) return true;

			if (other is ArcRelationshipClass arcRelationshipClass)
			{
				return Equals(arcRelationshipClass);
			}

			if (other is RelationshipClass proRelationshipClass)
			{
				return ProRelationshipClass.Handle.Equals(proRelationshipClass.Handle);
			}

			return false;
		}

		public override int GetHashCode()
		{
			return ProRelationshipClass.Handle.GetHashCode();
		}

		#endregion
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using ArcGIS.Core.Data;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.GIS.Geodatabase.API;

namespace ProSuite.GIS.Geodatabase.AGP
{
	public class ArcRelationshipClass : IRelationshipClass
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private bool _cachePropertiesEagerly;
		private readonly RelationshipClassDefinition _proRelationshipClassDefinition;

		// Property caching for non CIM-thread access:
		[CanBeNull] private string _name;
		private long? _relationshipClassId;
		private string _originPrimaryKey;
		private string _destinationPrimaryKey;
		private string _originForeignKey;
		private string _destinationForeignKey;
		private string _forwardPathLabel;
		private string _backwardPathLabel;
		private esriRelCardinality? _cardinality;
		private bool? _isComposite;
		private IObjectClass _originClass;
		private IObjectClass _destinationClass;

		public static ArcRelationshipClass Create([NotNull] RelationshipClass proRelationshipClass,
		                                          bool cacheEagerly = false)
		{
			var gdb = (ArcGIS.Core.Data.Geodatabase) proRelationshipClass.GetDatastore();

			ArcWorkspace existingWorkspace = ArcWorkspace.GetByHandle(gdb.Handle);

			ArcRelationshipClass found =
				existingWorkspace?.GetRelClassByName(proRelationshipClass.GetName());

			if (found != null)
			{
				if (cacheEagerly)
				{
					found.CacheProperties();
				}

				return found;
			}

			var result = new ArcRelationshipClass(proRelationshipClass, cacheEagerly);

			existingWorkspace?.Cache(result);

			return result;
		}

		public ArcRelationshipClass(RelationshipClass proRelationshipClass,
		                            bool cachePropertiesEagerly = false)
		{
			ProRelationshipClass = proRelationshipClass;
			_proRelationshipClassDefinition = proRelationshipClass.GetDefinition();

			if (cachePropertiesEagerly)
			{
				CacheProperties();
			}
		}

		public RelationshipClass ProRelationshipClass { get; }

		internal void CacheProperties()
		{
			if (_cachePropertiesEagerly)
			{
				// Already cached
				return;
			}

			// In case the method is called on an existing instance:
			_cachePropertiesEagerly = true;

			ArcWorkspace.Create((ArcGIS.Core.Data.Geodatabase) ProRelationshipClass.GetDatastore(),
			                    true);

			_originPrimaryKey = OriginPrimaryKey;
			_originForeignKey = OriginForeignKey;
			_destinationPrimaryKey = DestinationPrimaryKey;
			_destinationForeignKey = DestinationForeignKey;
			_forwardPathLabel = ForwardPathLabel;
			_backwardPathLabel = BackwardPathLabel;
			_cardinality = Cardinality;
			_isComposite = IsComposite;
			_originClass = PrepareCached(OriginClass);
			_destinationClass = PrepareCached(DestinationClass);
		}

		#region Implementation of IRelationshipClass

		public string OriginPrimaryKey
		{
			get
			{
				return _originPrimaryKey ??= _proRelationshipClassDefinition.GetOriginKeyField();
			}
		}

		public string DestinationPrimaryKey
		{
			get
			{
				if (_destinationPrimaryKey == null)
				{
					if (_proRelationshipClassDefinition is AttributedRelationshipClassDefinition
					    attributedRelClass)
					{
						_destinationPrimaryKey = attributedRelClass.GetDestinationKeyField();
					}
					else
					{
						// Only attributed (m:n) relationship classes have a destination key field
						return null;
					}
				}

				return _destinationPrimaryKey;
			}
		}

		public string OriginForeignKey =>
			_originForeignKey ??= _proRelationshipClassDefinition.GetOriginForeignKeyField();

		public string DestinationForeignKey
		{
			get
			{
				if (_destinationForeignKey == null)
				{
					if (_proRelationshipClassDefinition is
					    AttributedRelationshipClassDefinition attributedRelClass)
					{
						return attributedRelClass.GetDestinationForeignKeyField();
					}

					// Only attributed (m:n) relationship classes have a destination foreign key field
					return null;
				}

				return _destinationForeignKey;
			}
		}

		public long RelationshipClassID => _relationshipClassId ??= ProRelationshipClass.GetID();

		public IObjectClass OriginClass
		{
			get
			{
				if (_originClass != null)
				{
					return _originClass;
				}

				ArcGIS.Core.Data.Geodatabase geodatabase =
					ProRelationshipClass.GetDatastore() as ArcGIS.Core.Data.Geodatabase;

				Assert.NotNull(geodatabase, "No geodatabase could be retrieved from rel class");

				string originClassName = _proRelationshipClassDefinition.GetOriginClass();

				Table originClass = geodatabase.OpenDataset<Table>(originClassName);

				return _originClass =
					       ArcGeodatabaseUtils.ToArcTable(originClass, _cachePropertiesEagerly);
			}
		}

		public IObjectClass DestinationClass
		{
			get
			{
				if (_destinationClass != null)
				{
					return _destinationClass;
				}

				ArcGIS.Core.Data.Geodatabase geodatabase =
					ProRelationshipClass.GetDatastore() as ArcGIS.Core.Data.Geodatabase;

				Assert.NotNull(geodatabase, "No geodatabase could be retrieved from rel class");

				string destinationClassName = _proRelationshipClassDefinition.GetDestinationClass();

				Table destinationClass = geodatabase.OpenDataset<Table>(destinationClassName);

				return _destinationClass =
					       ArcGeodatabaseUtils.ToArcTable(destinationClass,
					                                      _cachePropertiesEagerly);
			}
		}

		public string ForwardPathLabel =>
			_forwardPathLabel ??= _proRelationshipClassDefinition.GetForwardPathLabel();

		public string BackwardPathLabel => _backwardPathLabel ??=
			                                   _proRelationshipClassDefinition
				                                   .GetBackwardPathLabel();

		public esriRelCardinality Cardinality =>
			_cardinality ??= (esriRelCardinality) _proRelationshipClassDefinition.GetCardinality();

		public bool IsAttributed => ProRelationshipClass is AttributedRelationshipClass;

		public bool IsComposite => _isComposite ??= _proRelationshipClassDefinition.IsComposite();

		public IRelationship CreateRelationship(IObject originObject, IObject destinationObject)
		{
			ArcRow arcOriginObj = (ArcRow) originObject;
			ArcRow arcDestinationObj = (ArcRow) destinationObject;

			var aoRelationship =
				ProRelationshipClass.CreateRelationship(arcOriginObj.ProRow,
				                                        arcDestinationObj.ProRow);

			return new ArcRelationship(aoRelationship, ProRelationshipClass);
		}

		public IRelationship GetRelationship(IObject originObject, IObject destinationObject)
		{
			return new ArcRelationship(originObject, destinationObject, this);
		}

		public void DeleteRelationship(IObject originObject, IObject destinationObject)
		{
			var originRow = ((ArcRow) originObject).ProRow;
			var destinationRow = ((ArcRow) destinationObject).ProRow;

			ProRelationshipClass.DeleteRelationship(originRow, destinationRow);
		}

		public IEnumerable<IObject> GetObjectsRelatedToObject(IObject anObject)
		{
			long sourceOid = anObject.OID;
			string sourceClassName = anObject.Class.Name;

			IEnumerable<Row> relatedObjects = GetRelatedObjects(sourceOid, sourceClassName);

			return relatedObjects.Select(o => ArcGeodatabaseUtils.ToArcRow(o));
		}

		public void DeleteRelationshipsForObject(IObject anObject)
		{
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
					ProRelationshipClass.DeleteRelationship(sourceRow, relatedRow);
				}
				else
				{
					Assert.True(
						sourceClassName == _proRelationshipClassDefinition.GetDestinationClass(),
						"Object is neither origin nor destination of relationship class");

					ProRelationshipClass.DeleteRelationship(relatedRow, sourceRow);
				}
			}
		}

		public IEnumerable<IObject> GetObjectsRelatedToObjectSet(IList<IObject> objectList)
		{
			if (objectList.Count == 0)
			{
				yield break;
			}

			// TODO: Ensure all objects are of the same class

			List<long> objectIds = objectList.Select(row => row.OID).ToList();

			string sourceClassName = objectList.Select(r => r.Table.Name).First();

			IReadOnlyList<Row> relatedObjects = GetRelatedObjects(objectIds, sourceClassName);

			foreach (ArcRow related in relatedObjects.Select(o => ArcGeodatabaseUtils.ToArcRow(o)))
			{
				yield return related;
			}
		}

		public IEnumerable<KeyValuePair<T, IObject>> GetObjectsMatchingObjectSet<T>(
			IEnumerable<T> sourceObjects) where T : IObject
		{
			Dictionary<long, T> sourceDictionary = sourceObjects.ToDictionary(
				sourceObject => sourceObject.OID,
				sourceObject => sourceObject);

			if (sourceDictionary.Count == 0)
			{
				yield break;
			}

			ITable destinationTable = null;

			int pairCount = 0;

			if (ProRelationshipClass is AttributedRelationshipClass attributedRelationshipClass)
			{
				foreach (AttributedRelationship attributedRelationship in
				         attributedRelationshipClass.GetRelationshipsForOriginRows(
					         sourceDictionary.Keys))
				{
					long originOid = attributedRelationship.GetOriginRow().GetObjectID();

					T sourceObj = sourceDictionary[originOid];

					Row proDestinationRow = attributedRelationship.GetDestinationRow();

					destinationTable ??=
						ArcGeodatabaseUtils.ToArcTable(proDestinationRow.GetTable());

					ArcRow destinationRow =
						ArcGeodatabaseUtils.ToArcRow(proDestinationRow, destinationTable);

					pairCount++;
					yield return new KeyValuePair<T, IObject>(sourceObj, destinationRow);
				}
			}
			else
			{
				string foreignKeyFieldName = OriginForeignKey;
				int originPrimaryKeyIdx = OriginClass.FindField(OriginPrimaryKey);

				foreach (Row proDestinationRow in ProRelationshipClass.GetRowsRelatedToOriginRows(
					         sourceDictionary.Keys))
				{
					// Get the origin object by searching the origin rows by searching values in the
					// origin primary key using the origin foreign key value in the destination row...

					proDestinationRow.GetTable().GetDefinition().FindField(foreignKeyFieldName);

					object foreignKeyValue = proDestinationRow[foreignKeyFieldName];

					// Find the origin object(s):

					int foundCount = 0;
					foreach (T relatedSourceObj in sourceDictionary.Values.Where(
						         d => HasFieldValue(d, originPrimaryKeyIdx, foreignKeyValue)))
					{
						destinationTable ??=
							ArcGeodatabaseUtils.ToArcTable(proDestinationRow.GetTable());

						ArcRow destinationRow =
							ArcGeodatabaseUtils.ToArcRow(proDestinationRow, destinationTable);

						foundCount++;
						pairCount++;
						yield return new KeyValuePair<T, IObject>(relatedSourceObj, destinationRow);
					}

					if (_proRelationshipClassDefinition.GetCardinality() ==
					    RelationshipCardinality.OneToOne &&
					    foundCount != 1)
					{
						_msg.WarnFormat(
							"Found unexpected number of origin objects ({0}) for destination object {1} in relationship class {2}",
							foundCount, proDestinationRow.GetObjectID(),
							_proRelationshipClassDefinition.GetName());
					}

					if (_proRelationshipClassDefinition.GetCardinality() ==
					    RelationshipCardinality.OneToMany && foundCount == 0)
					{
						throw new AssertionException(
							$"Found no origin objects for destination object {proDestinationRow.GetObjectID()} in " +
							$"in relationship class {_proRelationshipClassDefinition.GetName()}");
					}
				}
			}

			if (_msg.IsVerboseDebugEnabled)
			{
				_msg.Debug(
					$"Extracted {pairCount} pairs from {sourceDictionary.Count} source objects");
			}
		}

		private static bool HasFieldValue<T>(T row, int fieldIndex, object compareValue)
			where T : IObject
		{
			return compareValue.Equals(row.get_Value(fieldIndex));
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

		private IObjectClass PrepareCached(IObjectClass objectClass)
		{
			if (_cachePropertiesEagerly && objectClass is ArcTable arcTable)
			{
				arcTable.CacheProperties();
			}

			return objectClass;
		}

		private IEnumerable<Row> GetRelatedObjects(long sourceOid, string sourceClassName)
		{
			return GetRelatedObjects(new[] { sourceOid }, sourceClassName);
		}

		private IReadOnlyList<Row> GetRelatedObjects([NotNull] IEnumerable<long> sourceOids,
		                                             [NotNull] string sourceClassName)
		{
			IReadOnlyList<Row> relatedObjects;

			if (sourceClassName == OriginClass.Name)
			{
				relatedObjects =
					ProRelationshipClass.GetRowsRelatedToOriginRows(sourceOids);
			}
			else
			{
				Assert.True(sourceClassName == DestinationClass.Name,
				            "Object is neither origin nor destination of relationship class");

				relatedObjects =
					ProRelationshipClass.GetRowsRelatedToDestinationRows(sourceOids);
			}

			return relatedObjects;
		}

		#region Implementation of IDataset

		public string Name => _name ??= _proRelationshipClassDefinition.GetName();

		public IName FullName => new ArcDatasetName(this);

		public string BrowseName
		{
			get => throw new NotImplementedException();
			set => throw new NotImplementedException();
		}

		public esriDatasetType Type => esriDatasetType.esriDTRelationshipClass;

		public string Category => throw new NotImplementedException();

		public IEnumerable<IDataset> Subsets
		{
			get { throw new NotImplementedException(); }
		}

		internal ArcWorkspace ArcWorkspace => ArcWorkspace.Create(
			(ArcGIS.Core.Data.Geodatabase) ProRelationshipClass.GetDatastore());

		IWorkspace IDataset.Workspace => ArcWorkspace;

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

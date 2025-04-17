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
				ArcGIS.Core.Data.Geodatabase geodatabase =
					_proRelationshipClass.GetDatastore() as ArcGIS.Core.Data.Geodatabase;

				Assert.NotNull(geodatabase, "No geodatabase could be retrieved from rel class");

				string originClassName = _proRelationshipClassDefinition.GetOriginClass();

				Table originClass = geodatabase.OpenDataset<Table>(originClassName);

				return ArcGeodatabaseUtils.ToArcTable(originClass);
			}
		}

		public IObjectClass DestinationClass
		{
			get
			{
				ArcGIS.Core.Data.Geodatabase geodatabase =
					_proRelationshipClass.GetDatastore() as ArcGIS.Core.Data.Geodatabase;

				Assert.NotNull(geodatabase, "No geodatabase could be retrieved from rel class");

				string destinationClassName = _proRelationshipClassDefinition.GetDestinationClass();

				Table destinationClass = geodatabase.OpenDataset<Table>(destinationClassName);

				return ArcGeodatabaseUtils.ToArcTable(destinationClass);
			}
		}

		public string ForwardPathLabel => _proRelationshipClassDefinition.GetForwardPathLabel();

		public string BackwardPathLabel => _proRelationshipClassDefinition.GetBackwardPathLabel();

		public esriRelCardinality Cardinality =>
			(esriRelCardinality) _proRelationshipClassDefinition.GetCardinality();

		public bool IsAttributed => _proRelationshipClass is AttributedRelationshipClass;

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
			return new ArcRelationship(originObject, destinationObject, this);
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

			if (_proRelationshipClass is AttributedRelationshipClass attributedRelationshipClass)
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

				foreach (Row proDestinationRow in _proRelationshipClass.GetRowsRelatedToOriginRows(
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

		private IEnumerable<Row> GetRelatedObjects(long sourceOid, string sourceClassName)
		{
			return GetRelatedObjects(new[] { sourceOid }, sourceClassName);
		}

		private IReadOnlyList<Row> GetRelatedObjects([NotNull] IEnumerable<long> sourceOids,
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

		public IWorkspace Workspace => ArcWorkspace.Create(
			(ArcGIS.Core.Data.Geodatabase) _proRelationshipClass.GetDatastore());

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

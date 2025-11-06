using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Com;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Notifications;

namespace ProSuite.Commons.AO.Geodatabase
{
	public static class RelationshipClassUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public static bool InvolvesObjectClass(
			[NotNull] IRelationshipClass relationshipClass,
			[NotNull] IObjectClass objectClass)
		{
			Assert.ArgumentNotNull(relationshipClass, nameof(relationshipClass));
			Assert.ArgumentNotNull(objectClass, nameof(objectClass));

			return relationshipClass.OriginClass == objectClass ||
			       relationshipClass.DestinationClass == objectClass;
		}

		/// <summary>
		/// Gets the feature classes involved in an enumeration of relationship classes
		/// </summary>
		/// <param name="relationshipClasses">The relationship classes.</param>
		/// <returns></returns>
		[NotNull]
		public static IEnumerable<IFeatureClass> GetFeatureClasses(
			[NotNull] IEnumerable<IRelationshipClass> relationshipClasses)
		{
			Assert.ArgumentNotNull(relationshipClasses, nameof(relationshipClasses));

			var featureClasses = new HashSet<IFeatureClass>();

			foreach (IRelationshipClass relationshipClass in relationshipClasses)
			{
				foreach (IFeatureClass featureClass in GetFeatureClasses(relationshipClass))
				{
					if (featureClasses.Add(featureClass))
					{
						yield return featureClass;
					}
				}
			}
		}

		/// <summary>
		/// Gets the object classes involved in an enumeration of relationship classes
		/// </summary>
		/// <param name="relationshipClasses">The relationship classes.</param>
		/// <returns></returns>
		[NotNull]
		public static IEnumerable<IObjectClass> GetObjectClasses(
			[NotNull] IEnumerable<IRelationshipClass> relationshipClasses)
		{
			Assert.ArgumentNotNull(relationshipClasses, nameof(relationshipClasses));

			var objectClasses = new HashSet<IObjectClass>();

			foreach (IRelationshipClass relationshipClass in relationshipClasses)
			{
				foreach (IObjectClass objectClass in GetObjectClasses(relationshipClass))
				{
					if (objectClasses.Add(objectClass))
					{
						yield return objectClass;
					}
				}
			}
		}

		/// <summary>
		/// Gets the feature classes involved in a relationship class.
		/// </summary>
		/// <param name="relationshipClass">The relationship class.</param>
		/// <returns></returns>
		[NotNull]
		public static IEnumerable<IFeatureClass> GetFeatureClasses(
			[NotNull] IRelationshipClass relationshipClass)
		{
			Assert.ArgumentNotNull(relationshipClass, nameof(relationshipClass));

			foreach (IObjectClass objectClass in GetObjectClasses(relationshipClass))
			{
				var featureClass = objectClass as IFeatureClass;
				if (featureClass != null)
				{
					yield return featureClass;
				}
			}
		}

		/// <summary>
		/// Gets the origin and destination object classes involved in a relationship class.
		/// </summary>
		/// <param name="relationshipClass">The relationship class.</param>
		/// <returns></returns>
		[NotNull]
		public static IEnumerable<IObjectClass> GetObjectClasses(
			[NotNull] IRelationshipClass relationshipClass)
		{
			Assert.ArgumentNotNull(relationshipClass, nameof(relationshipClass));

			yield return relationshipClass.OriginClass;
			yield return relationshipClass.DestinationClass;
		}

		/// <summary>
		/// Finds object classes participating in the relationship class that
		/// match the specified condition. Candidates are the origin and the
		/// destination class.
		/// </summary>
		/// <param name="relationshipClass">The relationship class 
		/// whose origin/destination is tested.</param>
		/// <param name="match">The condition to be met.</param>
		/// <returns></returns>
		[NotNull]
		public static IList<IObjectClass> FindInvolvedObjectClasses(
			[NotNull] IRelationshipClass relationshipClass,
			[NotNull] Predicate<IObjectClass> match)
		{
			Assert.ArgumentNotNull(relationshipClass, nameof(relationshipClass));
			Assert.ArgumentNotNull(match, nameof(match));

			var result = new List<IObjectClass>();

			IObjectClass originClass = relationshipClass.OriginClass;

			if (match(originClass))
			{
				_msg.DebugFormat("Origin class matches the search criterion.");
				result.Add(originClass);
			}

			IObjectClass destinationClass = relationshipClass.DestinationClass;

			if (match(destinationClass))
			{
				_msg.DebugFormat("Destination class matches the search criterion.");
				result.Add(destinationClass);
			}

			return result;
		}

		/// <summary>
		/// Finds the feature classes of the specified geometry type that participate 
		/// in the specified relationship class.
		/// </summary>
		/// <param name="relationshipClass"></param>
		/// <param name="geometryType"></param>
		/// <returns></returns>
		[NotNull]
		public static IList<IFeatureClass> FindInvolvedFeatureClasses(
			[NotNull] IRelationshipClass relationshipClass,
			esriGeometryType geometryType)
		{
			Assert.ArgumentNotNull(relationshipClass, nameof(relationshipClass));

			IList<IObjectClass> foundClasses = FindInvolvedObjectClasses(
				relationshipClass,
				delegate(IObjectClass testClass)
				{
					var featureClass = testClass as IFeatureClass;

					return featureClass != null && featureClass.ShapeType == geometryType;
				});

			// conversion
			var result = new List<IFeatureClass>();
			foreach (IObjectClass foundClass in foundClasses)
			{
				result.Add(foundClass as IFeatureClass);
			}

			return result;
		}

		/// <summary>
		/// Gets the other object class involved in the specified relationship class.
		/// </summary>
		/// <param name="relationshipClass"></param>
		/// <param name="thisEndClass"></param>
		/// <returns></returns>
		[NotNull]
		public static IObjectClass GetOtherEndObjectClass(
			[NotNull] IRelationshipClass relationshipClass,
			[NotNull] IObjectClass thisEndClass)
		{
			Assert.ArgumentNotNull(relationshipClass, nameof(relationshipClass));
			Assert.ArgumentNotNull(thisEndClass, nameof(thisEndClass));

			bool thisEndIsOrigin = DatasetUtils.IsSameObjectClass(
				relationshipClass.OriginClass, thisEndClass,
				ObjectClassEquality.SameTableAnyVersion);

			IObjectClass otherEndClass = thisEndIsOrigin
				                             ? relationshipClass.DestinationClass
				                             : relationshipClass.OriginClass;

			return otherEndClass;
		}

		/// <summary>
		/// Gets a map of objects related to the objects in a given list. Suitable for 
		/// 1:1 and n:1 relationships classes. Only the objects that have a related 
		/// object are included in the map (i.e. there are no entries object->null)
		/// </summary>
		/// <param name="objects">The objects to get the related objects for.</param>
		/// <param name="relationshipClass">The relationshipClass.</param>
		/// <returns>Dictionary of related objects, indexed by objects in the list.</returns>
		[NotNull]
		public static IDictionary<T, IObject> GetToOneRelatedObjectMap<T>(
			[NotNull] IList<T> objects,
			[NotNull] IRelationshipClass relationshipClass) where T : IObject
		{
			Assert.ArgumentNotNull(objects, nameof(objects));
			Assert.ArgumentNotNull(relationshipClass, nameof(relationshipClass));

			// NOTE: the fk based strategy is significantly faster over relClass.GetObjectsMatchingObjectSet() as of 9.3.1 sp1
			// --> use GetObjectsMatchingObjectSet() only where there is no alternative (i.e. on attributed rel classes)
			return relationshipClass.IsAttributed
				       ? GetToOneRelatedObjectMapByRelClass(objects, relationshipClass)
				       : GetToOneRelatedObjectMapByFK(objects, relationshipClass);
		}

		/// <summary>
		/// Gets a map of objects related to the source objects in a given list. Suitable for 
		/// 1:n relationships classes.
		/// </summary>
		/// <param name="objects">The objects.</param>
		/// <param name="relationshipClass">The relationship class.</param>
		/// <returns>dictionary [source object] -> [list of related objects]. No entries are returned for
		/// source objects that have no related objects.</returns>
		[NotNull]
		public static IDictionary<IObject, IList<IObject>> GetOneToManyRelatedObjectMap(
			[NotNull] IList<IObject> objects,
			[NotNull] IRelationshipClass relationshipClass)
		{
			Assert.ArgumentNotNull(objects, nameof(objects));
			Assert.ArgumentNotNull(relationshipClass, nameof(relationshipClass));

			return relationshipClass.IsAttributed
				       ? GetOneToManyRelatedObjectMapByRelClass(objects, relationshipClass)
				       : GetOneToManyRelatedObjectMapByFK(objects, relationshipClass);
		}

		/// <summary>
		/// Gets a map of related objects by source object, for a given set of source objects and a relationship class
		/// </summary>
		/// <param name="sourceObjects">The source objects.</param>
		/// <param name="relationshipClass">The relationship class.</param>
		/// <returns>dictionary [source object] -> [list of related objects]. No entries are returned for
		/// source objects that have no related objects.</returns>
		[NotNull]
		public static IDictionary<IObject, IList<IObject>> GetRelatedObjectMap(
			[NotNull] ICollection<IObject> sourceObjects,
			[NotNull] IRelationshipClass relationshipClass)
		{
			Assert.ArgumentNotNull(sourceObjects, nameof(sourceObjects));
			Assert.ArgumentNotNull(relationshipClass, nameof(relationshipClass));

			var result = new Dictionary<IObject, IList<IObject>>();

			if (sourceObjects.Count == 0)
			{
				// return empty result
				return result;
			}

			foreach (KeyValuePair<IObject, IObject> pair
			         in GetRelatedObjectPairs(sourceObjects, relationshipClass))
			{
				IObject sourceObject = pair.Key;
				IObject relatedObject = pair.Value;

				IList<IObject> relatedObjects;
				if (! result.TryGetValue(sourceObject, out relatedObjects))
				{
					relatedObjects = new List<IObject>();
					result.Add(sourceObject, relatedObjects);
				}

				relatedObjects.Add(relatedObject);
			}

			return result;
		}

		/// <summary>
		/// Gets a map of source objects by related object, for a given set of source objects and a relationship class
		/// </summary>
		/// <param name="sourceObjects">The source objects.</param>
		/// <param name="relClass">The relationship class.</param>
		/// <returns>Dictionary [related object -> list of source objects]</returns>
		[NotNull]
		public static IDictionary<IObject, IList<IObject>> GetSourceObjectMap(
			[NotNull] IList<IObject> sourceObjects,
			[NotNull] IRelationshipClass relClass)
		{
			Assert.ArgumentNotNull(sourceObjects, nameof(sourceObjects));
			Assert.ArgumentNotNull(relClass, nameof(relClass));

			var result = new Dictionary<IObject, IList<IObject>>();

			if (sourceObjects.Count == 0)
			{
				// return empty result
				return result;
			}

			const int sourceInitialCapacity = 100;

			foreach (KeyValuePair<IObject, IObject> pair
			         in GetRelatedObjectPairs(sourceObjects, relClass))
			{
				IObject thisObject = pair.Key;
				IObject relatedObject = pair.Value;

				IList<IObject> relatedSourceObjects;
				if (! result.TryGetValue(relatedObject, out relatedSourceObjects))
				{
					relatedSourceObjects = new List<IObject>(sourceInitialCapacity);
					result.Add(relatedObject, relatedSourceObjects);
				}

				relatedSourceObjects.Add(thisObject);
			}
			//ISet set = GdbObjectUtils.GetObjectSet(sourceObjects);
			//IRelClassEnumRowPairs relPairs = relClass.GetObjectsMatchingObjectSet(set);
			//relPairs.Reset();

			//IRow sourceRow;
			//IRow targetRow;
			//relPairs.Next(out sourceRow, out targetRow);
			//while (sourceRow != null)
			//{
			//    if (targetRow == null)
			//    {
			//        _msg.WarnFormat("Related object not found for relationship");
			//    }
			//    else
			//    {
			//        var thisObject = (IObject) sourceRow;
			//        var relatedObject = (IObject) targetRow;

			//        IList<IObject> relatedSourceObjects;
			//        if (! result.TryGetValue(relatedObject, out relatedSourceObjects))
			//        {
			//            relatedSourceObjects = new List<IObject>(sourceInitialCapacity);
			//            result.Add(relatedObject, relatedSourceObjects);
			//        }

			//        relatedSourceObjects.Add(thisObject);
			//    }

			//    relPairs.Next(out sourceRow, out targetRow);
			//}

			//relPairs.Reset();

			return result;
		}

		[NotNull]
		public static IEnumerable<KeyValuePair<T, IObject>> GetRelatedObjectPairs<T>(
			[NotNull] IEnumerable<T> sourceObjects,
			[NotNull] IRelationshipClass relationshipClass) where T : IObject
		{
			Assert.ArgumentNotNull(sourceObjects, nameof(sourceObjects));
			Assert.ArgumentNotNull(relationshipClass, nameof(relationshipClass));

			Stopwatch watch = null;
			if (_msg.IsVerboseDebugEnabled)
			{
				watch = _msg.DebugStartTiming();
			}

			ISet set = GdbObjectUtils.GetObjectSet(sourceObjects);

			var pairCount = 0;

			try
			{
				if (set.Count == 0)
				{
					yield break;
				}

				// NOTE: this may return different row instances for the same related row OID
				// (outside of an edit session, for larger result sets)
				IRelClassEnumRowPairs pairs =
					relationshipClass.GetObjectsMatchingObjectSet(set);

				pairs.Reset();

				IRow sourceRow;
				IRow relatedRow;

				pairs.Next(out sourceRow, out relatedRow);

				// identity map to make sure only unique related row instances are returned 
				// (same row instance for a given row oid)
				var relatedIdMap = new Dictionary<long, IRow>();

				while (sourceRow != null)
				{
					pairCount++;

					if (relatedRow == null)
					{
						_msg.WarnFormat("Related object not found for relationship");
					}
					else
					{
						IRow uniqueRelatedRow = GetUniqueRow(relatedRow, relatedIdMap);

						yield return new KeyValuePair<T, IObject>(
							(T) sourceRow, (IObject) uniqueRelatedRow);
					}

					pairs.Next(out sourceRow, out relatedRow);
				}

				pairs.Reset();

				if (_msg.IsVerboseDebugEnabled)
				{
					_msg.DebugStopTiming(
						watch,
						"GetRelatedObjectPairs(): Read {0} related object pairs from " +
						"relationship class {1} based on {2} source objects",
						pairCount, DatasetUtils.GetName(relationshipClass), set.Count);
				}
			}
			finally
			{
				ComUtils.ReleaseComObject(set);
			}
		}

		/// <summary>
		/// Gets the objects related to a given set of objects, via a given relationship class.
		/// </summary>
		/// <param name="objects">The objects.</param>
		/// <param name="relationshipClass">The relationship class.</param>
		/// <param name="predicate"></param>
		/// <returns>List of related objects.</returns>
		[NotNull]
		public static IList<IObject> GetRelatedObjectList(
			[NotNull] IEnumerable<IObject> objects,
			[NotNull] IRelationshipClass relationshipClass,
			[CanBeNull] Predicate<IObject> predicate = null)
		{
			return GetRelatedObjectList<IObject>(objects, relationshipClass, predicate);
		}

		/// <summary>
		/// Gets the features related to a given set of objects, via a given relationship class.
		/// </summary>
		/// <param name="objects">The objects.</param>
		/// <param name="relationshipClass">The relationship class.</param>
		/// <param name="predicate"></param>
		/// <returns>List of related features.</returns>
		[NotNull]
		public static IList<IFeature> GetRelatedFeatureList(
			[NotNull] IEnumerable<IObject> objects,
			[NotNull] IRelationshipClass relationshipClass,
			[CanBeNull] Predicate<IObject> predicate = null)
		{
			return GetRelatedObjectList<IFeature>(objects, relationshipClass, predicate);
		}

		/// <summary>
		/// Gets the objects related to a given set of objects, via a given relationship class.
		/// </summary>
		/// <param name="objects">The objects.</param>
		/// <param name="relationshipClass">The relationship class.</param>
		/// <param name="predicate"></param>
		/// <returns>List of related objects.</returns>
		[NotNull]
		public static IList<T> GetRelatedObjectList<T>(
			[NotNull] IEnumerable<IObject> objects,
			[NotNull] IRelationshipClass relationshipClass,
			[CanBeNull] Predicate<T> predicate = null) where T : class, IObject
		{
			Assert.ArgumentNotNull(objects, nameof(objects));
			Assert.ArgumentNotNull(relationshipClass, nameof(relationshipClass));

			ISet objectSet = GdbObjectUtils.GetObjectSet(objects);

			ISet relatedObjectSet =
				relationshipClass.GetObjectsRelatedToObjectSet(objectSet);

			try
			{
				return GdbObjectUtils.GetObjectSetAsList(relatedObjectSet, predicate);
			}
			finally
			{
				ComUtils.ReleaseComObject(objectSet);
				ComUtils.ReleaseComObject(relatedObjectSet);
			}
		}

		/// <summary>
		/// Gets the objects related to a given object, via a given relationship class.
		/// </summary>
		/// <param name="obj">The object.</param>
		/// <param name="relationshipClass">The relationship class.</param>
		/// <returns>List of related objects.</returns>
		[NotNull]
		public static IList<IObject> GetRelatedObjectList(
			[NotNull] IObject obj,
			[NotNull] IRelationshipClass relationshipClass)
		{
			return GetRelatedObjectList<IObject>(obj, relationshipClass);
		}

		/// <summary>
		/// Gets the features related to a given object, via a given relationship class.
		/// </summary>
		/// <param name="obj">The object.</param>
		/// <param name="relationshipClass">The relationship class.</param>
		/// <returns>List of related features.</returns>
		[NotNull]
		public static IList<IFeature> GetRelatedFeatureList(
			[NotNull] IObject obj,
			[NotNull] IRelationshipClass relationshipClass)
		{
			return GetRelatedObjectList<IFeature>(obj, relationshipClass);
		}

		/// <summary>
		/// Gets the objects related to a given object, via a given relationship class.
		/// </summary>
		/// <param name="obj">The object.</param>
		/// <param name="relationshipClass">The relationship class.</param>
		/// <returns>List of related objects.</returns>
		[NotNull]
		public static IList<T> GetRelatedObjectList<T>(
			[NotNull] IObject obj, [NotNull] IRelationshipClass relationshipClass)
			where T : class, IObject
		{
			Assert.ArgumentNotNull(obj, nameof(obj));
			Assert.ArgumentNotNull(relationshipClass, nameof(relationshipClass));

			ISet objectSet = relationshipClass.GetObjectsRelatedToObject(obj);

			try
			{
				return GdbObjectUtils.GetObjectSetAsList<T>(objectSet);
			}
			finally
			{
				ComUtils.ReleaseComObject(objectSet);
			}
		}

		/// <summary>
		/// Get the unique object that is related to the given source object
		/// through the given relationship class. If no object is related,
		/// return null. If more than one object is related, throw an exception.
		/// </summary>
		/// <param name="sourceObject">The source object.</param>
		/// <param name="relationshipClass">The relationship class.</param>
		/// <returns>The related object or null.</returns>
		[CanBeNull]
		public static IObject GetUniqueRelatedObject(
			[NotNull] IObject sourceObject,
			[NotNull] IRelationshipClass relationshipClass)
		{
			Assert.ArgumentNotNull(sourceObject, nameof(sourceObject));
			Assert.ArgumentNotNull(relationshipClass, nameof(relationshipClass));

			IObject targetObject = null;

			ISet objectSet = relationshipClass.GetObjectsRelatedToObject(sourceObject);
			Assert.NotNull(objectSet, "GetObjectsRelatedToObject returned null");

			try
			{
				objectSet.Reset();

				var relatedObject = (IObject) objectSet.Next();

				while (relatedObject != null)
				{
					Assert.Null(targetObject, "related object not unique");
					targetObject = relatedObject;

					relatedObject = (IObject) objectSet.Next();
				}
			}
			finally
			{
				ComUtils.ReleaseComObject(objectSet);
			}

			return targetObject;
		}

		public static bool UsesRelationshipTable(
			[NotNull] IRelationshipClass relationshipClass)
		{
			return relationshipClass.IsAttributed ||
			       relationshipClass.Cardinality ==
			       esriRelCardinality.esriRelCardinalityManyToMany;
		}

		[NotNull]
		public static IEnumerable<IObject> GetRelatedObjects(
			[NotNull] IObject obj,
			[NotNull] IRelationshipClass relationshipClass)
		{
			return GetRelatedObjects<IObject>(obj, relationshipClass);
		}

		[NotNull]
		public static IEnumerable<T> GetRelatedObjects<T>(
			[NotNull] IObject obj,
			[NotNull] IRelationshipClass relationshipClass)
			where T : class, IObject
		{
			ISet set = relationshipClass.GetObjectsRelatedToObject(obj);

			try
			{
				set.Reset();

				var relatedObject = (T) set.Next();

				while (relatedObject != null)
				{
					yield return relatedObject;

					relatedObject = (T) set.Next();
				}

				set.Reset();
			}
			finally
			{
				Marshal.ReleaseComObject(set);
			}
		}

		[CanBeNull]
		public static IFeature FindSingleRelatedFeature([NotNull] IObject relatedToObj,
		                                                IObjectClass inTargetClass)
		{
			return FindSingleRelatedObject(relatedToObj, inTargetClass) as IFeature;
		}

		[CanBeNull]
		public static IFeature FindSingleRelatedFeature(
			[NotNull] IObject relatedToObj, [NotNull] IRelationshipClass relationshipClass)
		{
			return FindSingleRelatedObject(relatedToObj, relationshipClass) as IFeature;
		}

		[CanBeNull]
		public static IObject FindSingleRelatedObject([NotNull] IObject relatedToObj,
		                                              IObjectClass inTargetClass)
		{
			IRelationshipClass relClass =
				DatasetUtils.FindUniqueRelationshipClass(relatedToObj.Class, inTargetClass);

			if (relClass == null)
			{
				return null;
			}

			return FindSingleRelatedObject(relatedToObj, relClass);
		}

		[CanBeNull]
		public static IObject FindSingleRelatedObject(
			[NotNull] IObject relatedToObj, [NotNull] IRelationshipClass relationshipClass)
		{
			Assert.ArgumentNotNull(relatedToObj, nameof(relatedToObj));
			Assert.ArgumentNotNull(relationshipClass, nameof(relationshipClass));

			IObject result = null;

			var relatedObjects =
				new List<IObject>(GetRelatedObjects(relatedToObj, relationshipClass));

			if (relatedObjects.Count == 1)
			{
				_msg.DebugFormat("Found 1 related object for {0}: {1}",
				                 GdbObjectUtils.ToString(relatedToObj),
				                 GdbObjectUtils.ToString(relatedObjects[0]));

				result = relatedObjects[0];
			}
			else if (relatedObjects.Count > 1)
			{
				_msg.DebugFormat(
					"Found several objects related to {0}. Cannot determine single related feature",
					GdbObjectUtils.ToString(relatedToObj));
			}
			else
			{
				_msg.DebugFormat("No related object found for {0}",
				                 GdbObjectUtils.ToString(relatedToObj));
			}

			return result;
		}

		public static bool EnsureRelationship([NotNull] IRelationshipClass relationshipClass,
		                                      [NotNull] IObject originObject,
		                                      [NotNull] IObject destinationObject)
		{
			IRelationship existingRelationship =
				relationshipClass.GetRelationship(originObject,
				                                  destinationObject);

			if (existingRelationship != null)
			{
				_msg.DebugFormat("Relationship from {0} to {1} already exists.",
				                 GdbObjectUtils.ToString(originObject),
				                 GdbObjectUtils.ToString(destinationObject));

				return false;
			}

			_msg.DebugFormat("Creating relationship from {0} to {1}",
			                 GdbObjectUtils.ToString(originObject),
			                 GdbObjectUtils.ToString(destinationObject));

			relationshipClass.CreateRelationship(originObject, destinationObject);

			return true;
		}

		[CanBeNull]
		public static IRelationship TryCreateRelationship(
			[NotNull] IObject object1, [NotNull] IObject object2,
			[NotNull] IRelationshipClass relationshipClass,
			[CanBeNull] NotificationCollection notifications)
		{
			const bool tryAddMissingPrimaryKey = false;
			const bool overwriteExistingForeignKeys = false;

			return TryCreateRelationship(object1, object2, relationshipClass,
			                             tryAddMissingPrimaryKey, overwriteExistingForeignKeys,
			                             notifications);
		}

		[CanBeNull]
		public static IRelationship TryCreateRelationship(
			[NotNull] IObject obj1, [NotNull] IObject obj2,
			[NotNull] IRelationshipClass relationshipClass,
			bool tryAddMissingPrimaryKey,
			bool overwriteExistingForeignKeys,
			[CanBeNull] NotificationCollection notifications)
		{
			Assert.ArgumentNotNull(obj1, nameof(obj1));
			Assert.ArgumentNotNull(obj2, nameof(obj2));
			Assert.ArgumentNotNull(relationshipClass, nameof(relationshipClass));

			IObject originObj, destinationObj;
			DetermineRoles(obj1, obj2, relationshipClass, out originObj, out destinationObj);

			if (! TryEnsurePrimaryKey(originObj,
			                          relationshipClass.OriginPrimaryKey,
			                          tryAddMissingPrimaryKey, notifications))
			{
				return null;
			}

			if (relationshipClass.Cardinality ==
			    esriRelCardinality.esriRelCardinalityManyToMany)
			{
				if (! TryEnsurePrimaryKey(destinationObj,
				                          relationshipClass.DestinationPrimaryKey,
				                          tryAddMissingPrimaryKey, notifications))
				{
					return null;
				}
			}

			return
				! CanCreateRelationship(originObj, destinationObj, relationshipClass,
				                        overwriteExistingForeignKeys, notifications)
					? null
					: relationshipClass.CreateRelationship(originObj, destinationObj);
		}

		public static bool IsOriginObject([NotNull] IObject obj,
		                                  [NotNull] IRelationshipClass relationshipClass)
		{
			return DatasetUtils.IsSameObjectClass(relationshipClass.OriginClass, obj.Class,
			                                      ObjectClassEquality.SameTableSameVersion);
		}

		/// <summary>
		/// Creates the joined table by joining the specified tables according to the specified
		/// join type. The tables must be the origin/destination tables of the specified
		/// RelationshipClass. The result class has unique OIDs.
		/// </summary>
		/// <param name="relationshipClass">The relationship class that connects the ...</param>
		/// <param name="tables">to be joined</param>
		/// <param name="joinType">The join type w.r.t. the list order of the tables.</param>
		/// <param name="whereClause">Optional where clause</param>
		/// <param name="queryTableName">An optional name for the query table. If not set, it's generated</param>
		/// <returns></returns>
		[NotNull]
		public static IReadOnlyTable GetReadOnlyQueryTable(
			[NotNull] IRelationshipClass relationshipClass,
			[NotNull] IList<ITable> tables,
			JoinType joinType,
			string whereClause = null,
			string queryTableName = null)
		{
			Assert.ArgumentNotNull(relationshipClass, nameof(relationshipClass));
			Assert.ArgumentNotNull(tables, nameof(tables));
			Assert.ArgumentCondition(tables.Count > 1, "2 tables required");

			Assert.ArgumentCondition(
				IsOriginClass(relationshipClass, tables[0]) &&
				IsDestinationClass(relationshipClass, tables[1]) ||
				IsDestinationClass(relationshipClass, tables[0]) &&
				IsOriginClass(relationshipClass, tables[1]),
				"tables must be origin/destination of relationship class");

			return TableJoinUtils.CreateReadOnlyQueryTable(
				relationshipClass,
				AdaptJoinTypeToRelationshipDirection(relationshipClass, tables, joinType),
				whereClause: whereClause, queryTableName: queryTableName,
				includeOnlyOIDFields: false);
		}

		/// <summary>
		/// Creates the joined table by joining the specified tables according to the specified
		/// join type. The tables must be the origin/destination tables of the specified
		/// RelationshipClass.
		/// NOTE: The OIDs of these tables are NOT unique (TOP-5598). Use GetReadOnlyQueryTable instead.
		/// </summary>
		/// <param name="relationshipClass">The relationship class that connects the ...</param>
		/// <param name="tables">to be joined</param>
		/// <param name="joinType">The join type w.r.t. the list order of the tables.</param>
		/// <param name="whereClause">Optional where clause</param>
		/// <param name="queryTableName">An optional name for the query table. If not set, it's generated</param>
		/// <returns></returns>
		[NotNull]
		public static ITable GetQueryTable(
			[NotNull] IRelationshipClass relationshipClass,
			[NotNull] IList<ITable> tables,
			JoinType joinType,
			string whereClause = null,
			string queryTableName = null)
		{
			Assert.ArgumentNotNull(relationshipClass, nameof(relationshipClass));
			Assert.ArgumentNotNull(tables, nameof(tables));
			Assert.ArgumentCondition(tables.Count > 1, "2 tables required");

			Assert.ArgumentCondition(
				IsOriginClass(relationshipClass, tables[0]) &&
				IsDestinationClass(relationshipClass, tables[1]) ||
				IsDestinationClass(relationshipClass, tables[0]) &&
				IsOriginClass(relationshipClass, tables[1]),
				"tables must be origin/destination of relationship class");

			return TableJoinUtils.CreateQueryTable(
				relationshipClass,
				AdaptJoinTypeToRelationshipDirection(relationshipClass, tables, joinType),
				whereClause: whereClause, queryTableName: queryTableName);
		}

		#region Private Methods

		/// <summary>
		/// For a given row, returns the unique row from an identity map (OID -> row). If the 
		/// row is not yet in the identity map, it is added to it.
		/// </summary>
		/// <param name="row">The row.</param>
		/// <param name="identityMap">The identity map.</param>
		/// <returns></returns>
		[NotNull]
		private static IRow GetUniqueRow([NotNull] IRow row,
		                                 [NotNull] IDictionary<long, IRow> identityMap)
		{
			long oid = (long) row.OID;

			IRow existingRow;
			if (identityMap.TryGetValue(oid, out existingRow))
			{
				return existingRow;
			}

			// row is not yet in map
			identityMap.Add(oid, row);
			return row;
		}

		/// <summary>
		/// Gets a map of objects related to the objects in a given list. Suitable for 
		/// 1:1 and n:1 relationships classes. Only the objects that have a related 
		/// object are included in the map (i.e. there are no entries object->null)
		/// </summary>
		/// <param name="objects">The objects to get the related objects for.</param>
		/// <param name="relationshipClass">The relationshipClass.</param>
		/// <returns>Dictionary of related objects, indexed by objects in the list.</returns>
		[NotNull]
		private static IDictionary<T, IObject> GetToOneRelatedObjectMapByRelClass<T>(
			[NotNull] IList<T> objects,
			[NotNull] IRelationshipClass relationshipClass) where T : IObject
		{
			Assert.ArgumentNotNull(objects, nameof(objects));
			Assert.ArgumentNotNull(relationshipClass, nameof(relationshipClass));

			// assert correct cardinality (1:n or 1:1)
			esriRelCardinality cardinality = relationshipClass.Cardinality;
			Assert.True(cardinality == esriRelCardinality.esriRelCardinalityOneToMany ||
			            cardinality == esriRelCardinality.esriRelCardinalityOneToOne,
			            "Unexpected cardinality: {0}", cardinality);

			// initialize result
			var result = new Dictionary<T, IObject>();

			if (objects.Count == 0)
			{
				return result;
			}

			IObjectClass sourceClass = objects[0].Class;

			if (cardinality == esriRelCardinality.esriRelCardinalityOneToMany)
			{
				Assert.True(sourceClass == relationshipClass.DestinationClass,
				            "Source objects must be on Many side of n:1 relationship class");
			}

			foreach (KeyValuePair<T, IObject> pair
			         in GetRelatedObjectPairs(objects, relationshipClass))
			{
				T sourceObject = pair.Key;
				IObject relatedObject = pair.Value;

				if (result.TryGetValue(sourceObject, out IObject _))
				{
					string newLine = Environment.NewLine;

					throw new InvalidDataException(
						string.Format(
							"There exists more than one {0} object related to {1} " +
							"object with {2} = {3}"
							+ newLine + newLine +
							"Please revise the following {0} objects:"
							+ newLine + newLine +
							GetRelatedOIDList(sourceObject, relationshipClass)
							+ newLine + newLine +
							"Make sure that only one of these objects remains related to the {1} object",
							DatasetUtils.GetName(relatedObject.Class),
							DatasetUtils.GetName(sourceObject.Class),
							sourceObject.Class.OIDFieldName,
							sourceObject.OID));
				}

				result.Add(sourceObject, relatedObject);
			}

			return result;
		}

		[NotNull]
		private static IDictionary<T, IObject> GetToOneRelatedObjectMapByFK<T>(
			[NotNull] IList<T> objects,
			[NotNull] IRelationshipClass relationshipClass) where T : IObject
		{
			Assert.ArgumentNotNull(objects, nameof(objects));
			Assert.ArgumentNotNull(relationshipClass, nameof(relationshipClass));

			// assert correct cardinality (1:n or 1:1)
			esriRelCardinality cardinality = relationshipClass.Cardinality;
			Assert.True(cardinality == esriRelCardinality.esriRelCardinalityOneToMany ||
			            cardinality == esriRelCardinality.esriRelCardinalityOneToOne,
			            "Unexpected cardinality: {0}", cardinality);

			// initialize result list
			var result = new Dictionary<T, IObject>();

			if (objects.Count == 0)
			{
				return result;
			}

			// if the object class carries the primary key of the 
			// relationship class: exclude objects with invalid pk values.
			// Otherwise an access violation exception is thrown by
			// relationshipClass.GetRelationshipsForObjectSet(objectSet)

			int sourceKeyFieldIndex;
			int relatedKeyFieldIndex;
			string pkField = relationshipClass.OriginPrimaryKey;
			string fkField = relationshipClass.OriginForeignKey;

			IObjectClass sourceClass = objects[0].Class;
			IObjectClass relatedClass;

			if (cardinality == esriRelCardinality.esriRelCardinalityOneToMany)
			{
				Assert.True(sourceClass == relationshipClass.DestinationClass,
				            "Source objects must be on Many side of n:1 relationship class");
			}

			if (relationshipClass.OriginClass == sourceClass)
			{
				// source objects that don't have a primary key value for the relationship 
				// (e.g. null or empty UUID) will be excluded

				relatedClass = relationshipClass.DestinationClass;

				sourceKeyFieldIndex = sourceClass.FindField(pkField);
				relatedKeyFieldIndex = relatedClass.FindField(fkField);

				// validate field indices
				if (sourceKeyFieldIndex < 0)
				{
					_msg.WarnFormat(
						"Primary key field {0} for relationship class {1} not found in {2}",
						pkField,
						DatasetUtils.GetName(relationshipClass),
						DatasetUtils.GetName(sourceClass));
					return result;
				}

				if (relatedKeyFieldIndex < 0)
				{
					_msg.WarnFormat(
						"Foreign key field {0} for relationship class {1} not found in {2}",
						fkField,
						DatasetUtils.GetName(relationshipClass),
						DatasetUtils.GetName(relatedClass));
					return result;
				}
			}
			else
			{
				// source objects that don't have a foreign key value 
				// for the relationship (e.g. null or empty UUID) will be excluded

				relatedClass = relationshipClass.OriginClass;

				sourceKeyFieldIndex = sourceClass.FindField(fkField);
				relatedKeyFieldIndex = relatedClass.FindField(pkField);

				// validate field indices
				if (sourceKeyFieldIndex < 0)
				{
					_msg.WarnFormat(
						"Foreign key field {0} for relationship class {1} not found in {2}",
						fkField,
						DatasetUtils.GetName(relationshipClass),
						DatasetUtils.GetName(sourceClass));
					return result;
				}

				if (relatedKeyFieldIndex < 0)
				{
					_msg.WarnFormat(
						"Primary key field {0} for relationship class {1} not found in {2}",
						pkField,
						DatasetUtils.GetName(relationshipClass),
						DatasetUtils.GetName(relatedClass));
					return result;
				}
			}

			var converter = new KeyValueConverter(
				sourceClass.Fields.Field[sourceKeyFieldIndex].Type,
				relatedClass.Fields.Field[relatedKeyFieldIndex].Type);

			// prepare map [relationship key on source object] -> [source object]
			// and list of all source objects with non-null relationship key
			Dictionary<object, T> sourceObjectByKey = null;
			Dictionary<object, IList<IObject>> sourceObjectsByKey = null;
			var sourceObjectsWithKey = new List<IObject>();

			if (cardinality == esriRelCardinality.esriRelCardinalityOneToMany)
			{
				sourceObjectsByKey = new Dictionary<object, IList<IObject>>();
			}
			else
			{
				sourceObjectByKey = new Dictionary<object, T>();
			}

			// populate map, and list of all source objects with non-null relationship key
			foreach (T sourceObject in objects)
			{
				object keyValue = converter.GetSourceKey(
					sourceObject.Value[sourceKeyFieldIndex]);

				if (GdbObjectUtils.IsNullOrEmpty(keyValue))
				{
					continue;
				}

				if (cardinality == esriRelCardinality.esriRelCardinalityOneToMany)
				{
					Assert.NotNull(sourceObjectsByKey, "sourceObjectsByKey");

					IList<IObject> sourceObjects;
					if (! sourceObjectsByKey.TryGetValue(keyValue, out sourceObjects))
					{
						sourceObjects = new List<IObject>();
						sourceObjectsByKey.Add(keyValue, sourceObjects);
					}

					sourceObjects.Add(sourceObject);
				}
				else
				{
					Assert.NotNull(sourceObjectByKey, "sourceObjectByKey");

					sourceObjectByKey.Add(keyValue, sourceObject);
				}

				sourceObjectsWithKey.Add(sourceObject);
			}

			if (sourceObjectsWithKey.Count == 0)
			{
				// no source objects with non-null key value found
				return result;
			}

			// get the related objects
			ISet objectSet = GdbObjectUtils.GetObjectSet(sourceObjectsWithKey);
			ISet relatedObjectsSet =
				relationshipClass.GetObjectsRelatedToObjectSet(objectSet);

			IList<IObject> relatedObjects;
			try
			{
				relatedObjects = GdbObjectUtils.GetObjectSetAsList(relatedObjectsSet);
			}
			finally
			{
				ComUtils.ReleaseComObject(relatedObjectsSet);
			}

			// populate the result
			foreach (IObject relatedObject in relatedObjects)
			{
				object keyValue = converter.GetRelatedKey(
					relatedObject.Value[relatedKeyFieldIndex]);

				if (cardinality == esriRelCardinality.esriRelCardinalityOneToMany)
				{
					Assert.NotNull(sourceObjectsByKey, "sourceObjectsByKey");

					IList<IObject> sourceObjects = sourceObjectsByKey[keyValue];

					foreach (IObject o in sourceObjects)
					{
						var sourceObject = (T) o;
						result.Add(sourceObject, relatedObject);
					}
				}
				else // 1:1
				{
					Assert.NotNull(sourceObjectByKey, "sourceObjectByKey");

					T sourceObject = sourceObjectByKey[keyValue];

					if (! result.ContainsKey(sourceObject))
					{
						result.Add(sourceObject, relatedObject);
					}
					else
					{
						string sourceKeyFieldName =
							sourceObject.Fields.Field[sourceKeyFieldIndex].Name;

						var duplicatesStringBuilder = new StringBuilder();
						foreach (IObject existingRelatedObject in relatedObjects)
						{
							if (! Equals(
								    existingRelatedObject.Value[relatedKeyFieldIndex],
								    keyValue))
							{
								continue;
							}

							duplicatesStringBuilder.AppendFormat(
								"- {0} = {1}",
								existingRelatedObject.Class.OIDFieldName,
								existingRelatedObject.OID);
							duplicatesStringBuilder.AppendLine();
						}

						string newLine = Environment.NewLine;
						throw new InvalidDataException(
							string.Format(
								"There exists more than one {0} object related to {1} " +
								"object with {2} = {3}" + newLine + newLine +
								"Please revise the following {0} objects:" + newLine +
								newLine +
								duplicatesStringBuilder + newLine + newLine +
								"Make sure that only one of these objects remains related to the {1} object with {4} = {5}",
								DatasetUtils.GetName(relatedObject.Class),
								DatasetUtils.GetName(sourceObject.Class),
								sourceKeyFieldName, keyValue,
								sourceObject.Class.OIDFieldName, sourceObject.OID));
					}
				}
			}

			return result;
		}

		/// <summary>
		/// Gets a map of objects related to the source objects in a given list. Suitable for 
		/// 1:n relationships classes.
		/// </summary>
		/// <param name="objects">The objects.</param>
		/// <param name="relationshipClass">The relationship class.</param>
		/// <returns>dictionary [source object] -> [list of related objects]. No entries are returned for
		/// source objects that have no related objects.</returns>
		[NotNull]
		private static IDictionary<IObject, IList<IObject>>
			GetOneToManyRelatedObjectMapByRelClass(
				[NotNull] IList<IObject> objects,
				[NotNull] IRelationshipClass relationshipClass)
		{
			Assert.ArgumentNotNull(objects, nameof(objects));
			Assert.ArgumentNotNull(relationshipClass, nameof(relationshipClass));

			// assert correct cardinality (1:n)
			esriRelCardinality cardinality = relationshipClass.Cardinality;
			Assert.True(cardinality == esriRelCardinality.esriRelCardinalityOneToMany,
			            "Unexpected cardinality: {0}", cardinality);

			IObjectClass sourceClass = objects[0].Class;

			Assert.True(sourceClass == relationshipClass.OriginClass,
			            "Objects must be on origin class of relationship");

			IDictionary<IObject, IList<IObject>> result = GetRelatedObjectMap(objects,
				relationshipClass);

			// TODO check for violations of 1:n constraint
			// --> do the same related objects appear for more than one source object? -> throw

			return result;
		}

		[NotNull]
		private static IDictionary<IObject, IList<IObject>> GetOneToManyRelatedObjectMapByFK
		(
			[NotNull] IList<IObject> objects,
			[NotNull] IRelationshipClass relationshipClass)
		{
			Assert.ArgumentNotNull(objects, nameof(objects));
			Assert.ArgumentNotNull(relationshipClass, nameof(relationshipClass));

			// assert correct cardinality (1:n)
			esriRelCardinality cardinality = relationshipClass.Cardinality;
			Assert.True(cardinality == esriRelCardinality.esriRelCardinalityOneToMany,
			            "Unexpected cardinality: {0}", cardinality);

			// initialize result list
			var result = new Dictionary<IObject, IList<IObject>>();

			if (objects.Count == 0)
			{
				return result;
			}

			// if the object class carries the primary key of the 
			// relationship class: exclude objects with invalid pk values.
			// Otherwise an access violation exception is thrown by
			// relationshipClass.GetRelationshipsForObjectSet(objectSet)

			IObjectClass sourceClass = objects[0].Class;

			Assert.True(sourceClass == relationshipClass.OriginClass,
			            "Objects must be on origin class of relationship");

			IObjectClass relatedClass = relationshipClass.DestinationClass;

			string pkField = relationshipClass.OriginPrimaryKey;
			string fkField = relationshipClass.OriginForeignKey;

			int sourceKeyFieldIndex = sourceClass.FindField(pkField);
			int relatedKeyFieldIndex = relatedClass.FindField(fkField);

			// validate field indices
			if (sourceKeyFieldIndex < 0)
			{
				_msg.WarnFormat(
					"Primary key field {0} for relationship class {1} not found in {2}",
					pkField,
					DatasetUtils.GetName(relationshipClass),
					DatasetUtils.GetName(sourceClass));
				return result;
			}

			if (relatedKeyFieldIndex < 0)
			{
				_msg.WarnFormat(
					"Foreign key field {0} for relationship class {1} not found in {2}",
					fkField,
					DatasetUtils.GetName(relationshipClass),
					DatasetUtils.GetName(relatedClass));
				return result;
			}

			var converter = new KeyValueConverter(
				sourceClass.Fields.Field[sourceKeyFieldIndex].Type,
				relatedClass.Fields.Field[relatedKeyFieldIndex].Type);

			// prepare map [relationship key on source object] -> [source objects]
			// and list of source objects with non-null key value
			var sourceObjectsByKey = new Dictionary<object, IList<IObject>>();
			var sourceObjectsWithKey = new List<IObject>();

			// populate map, and list of all source objects with non-null relationship key
			foreach (IObject sourceObject in objects)
			{
				object keyValue = converter.GetSourceKey(
					sourceObject.Value[sourceKeyFieldIndex]);

				if (! GdbObjectUtils.IsNullOrEmpty(keyValue))
				{
					Assert.NotNull(sourceObjectsByKey, "sourceObjectsByKey");

					IList<IObject> sourceObjects;
					if (! sourceObjectsByKey.TryGetValue(keyValue, out sourceObjects))
					{
						sourceObjects = new List<IObject>();
						sourceObjectsByKey.Add(keyValue, sourceObjects);
					}

					sourceObjects.Add(sourceObject);
				}

				sourceObjectsWithKey.Add(sourceObject);
			}

			if (sourceObjectsWithKey.Count == 0)
			{
				// no source objects with non-null key value found
				return result;
			}

			// get the related objects
			ISet objectSet = GdbObjectUtils.GetObjectSet(sourceObjectsWithKey);
			ISet relatedObjectsSet =
				relationshipClass.GetObjectsRelatedToObjectSet(objectSet);

			IList<IObject> relatedObjects;

			try
			{
				relatedObjects = GdbObjectUtils.GetObjectSetAsList(relatedObjectsSet);
			}
			finally
			{
				ComUtils.ReleaseComObject(objectSet);
			}

			// populate the result
			foreach (IObject relatedObject in relatedObjects)
			{
				object keyValue = converter.GetRelatedKey(
					relatedObject.Value[relatedKeyFieldIndex]);

				IList<IObject> sourceObjects = sourceObjectsByKey[keyValue];

				foreach (IObject sourceObject in sourceObjects)
				{
					IList<IObject> relatedToSource;
					if (! result.TryGetValue(sourceObject, out relatedToSource))
					{
						relatedToSource = new List<IObject>();
						result.Add(sourceObject, relatedToSource);
					}

					relatedToSource.Add(relatedObject);
				}
			}

			return result;
		}

		[NotNull]
		private static string GetRelatedOIDList([NotNull] IObject sourceObject,
		                                        [NotNull] IRelationshipClass
			                                        relationshipClass)
		{
			var sb = new StringBuilder();

			foreach (IObject relatedObject in
			         GetRelatedObjectList(sourceObject, relationshipClass))
			{
				sb.AppendFormat("- {0} = {1}",
				                relatedObject.Class.OIDFieldName,
				                relatedObject.OID);
				sb.AppendLine();
			}

			return sb.ToString();
		}

		private static void DetermineRoles(
			[NotNull] IObject object1, [NotNull] IObject object2,
			[NotNull] IRelationshipClass relationshipClass,
			out IObject origin, out IObject destination)
		{
			if (relationshipClass.OriginClass.ObjectClassID == object1.Class.ObjectClassID)
			{
				origin = object1;
				destination = object2;
			}
			else
			{
				destination = object1;
				origin = object2;
			}

			Assert.True(
				DatasetUtils.IsSameObjectClass(relationshipClass.OriginClass, origin.Class,
				                               ObjectClassEquality.SameTableSameVersion),
				"RelationshipClass must be of the same version as object1 and object2");
			Assert.True(
				DatasetUtils.IsSameObjectClass(relationshipClass.DestinationClass,
				                               destination.Class,
				                               ObjectClassEquality.SameTableSameVersion),
				"RelationshipClass must be of the same version as object1 and object2");
		}

		private static bool CanCreateRelationship(
			[NotNull] IObject originObj, [NotNull] IObject destinationObj,
			[NotNull] IRelationshipClass relationshipClass, bool allowOverwriteForeignKey,
			[CanBeNull] NotificationCollection notifications)
		{
			// make sure no existing foreign key value gets overwritten
			if (relationshipClass.Cardinality !=
			    esriRelCardinality.esriRelCardinalityManyToMany)
			{
				if (allowOverwriteForeignKey)
				{
					return true;
				}

				object existingForeignKey = GetFieldValue(destinationObj,
				                                          relationshipClass.OriginForeignKey);

				if (! Convert.IsDBNull(existingForeignKey))
				{
					if (existingForeignKey ==
					    GetFieldValue(originObj, relationshipClass.OriginPrimaryKey))
					{
						NotificationUtils.Add(notifications,
						                      "{0} and {1} already have a relationship",
						                      RowFormat.Format(originObj),
						                      RowFormat.Format(destinationObj));

						// but setting the foreign key again won't do any harm:
						return true;
					}

					// it is still possible that the old origin still exists but the destination was duplicated and we
					// are dealing with a duplicate here (that now points to the wrong origin and we can fix it)
					// Example: The roof (destination) is exploded and in the create-feature event a new grundriss
					//		    is created that should be linked to the roof-copies (overwriting the existing foreign key)
					// this cannot be detected other than with a parameter: allowOverwriteForeignKey

					var preRelatedObjects =
						new List<IObject>(GetRelatedObjects(destinationObj, relationshipClass));

					if (preRelatedObjects.Count == 0)
					{
						// either relational integrity is violated or the other origin was deleted in this very edit operation
						// and the field was not yet nulled - it's ok for a new relationship
						return true;
					}

					NotificationUtils.Add(notifications,
					                      "Destination object {0} already has a relationship to another origin object",
					                      RowFormat.Format(destinationObj));

					return false;
				}

				return true;
			}

			// make sure (if m:n) it does not already exist -> duplicate m:n relationships are not
			// prevented by ArcObjects

			IRelationship existingRelationship = relationshipClass.GetRelationship(originObj,
				destinationObj);

			if (existingRelationship != null && ! relationshipClass.IsAttributed)
			{
				NotificationUtils.Add(notifications, "{0} and {1} already have a relationship",
				                      RowFormat.Format(originObj),
				                      RowFormat.Format(destinationObj));

				// creating an additional relationship would duplicate the existing entry
				return false;
			}

			return true;
		}

		private static object GetFieldValue(IObject obj, string fieldName)
		{
			Assert.ArgumentNotNull(obj, nameof(obj));
			Assert.ArgumentNotNull(fieldName, nameof(fieldName));

			int fieldIndex = DatasetUtils.GetFieldIndex(obj.Class, fieldName);

			Assert.True(fieldIndex >= 0, "Field {0} not found in {1}", fieldName,
			            DatasetUtils.GetName(obj.Class));

			object value = obj.Value[fieldIndex];

			return value;
		}

		private static bool TryEnsurePrimaryKey(
			[NotNull] IObject obj,
			[NotNull] string keyFieldName,
			bool allowAddMissingPrimaryKey,
			[CanBeNull] NotificationCollection notifications)
		{
			Assert.ArgumentNotNull(obj, nameof(obj));
			Assert.ArgumentNotNullOrEmpty(keyFieldName, nameof(keyFieldName));

			int fieldIndex = obj.Class.FindField(keyFieldName);

			Assert.True(fieldIndex >= 0, "Key field {0} does not exist in {1}", keyFieldName,
			            obj.Class.AliasName);

			if (obj.Value[fieldIndex] != DBNull.Value)
			{
				return true;
			}

			if (! allowAddMissingPrimaryKey)
			{
				NotificationUtils.Add(notifications, "Object {0} has no primary key value",
				                      RowFormat.Format(obj));
				return false;
			}

			if (obj.Fields.Field[fieldIndex].Type != esriFieldType.esriFieldTypeGUID)
			{
				// cannot invent primary key for other field types
				NotificationUtils.Add(notifications,
				                      "{0}: Cannot create non-Guid primary key values",
				                      RowFormat.Format(obj));
				return false;
			}

			obj.set_Value(fieldIndex, UIDUtils.CreateUID());

			return true;
		}

		private static bool IsOriginClass(
			[NotNull] IRelationshipClass relationshipClass,
			[NotNull] ITable table)
		{
			return DatasetUtils.IsSameObjectClass(relationshipClass.OriginClass,
			                                      (IObjectClass) table,
			                                      ObjectClassEquality.SameTableAnyVersion);
		}

		private static bool IsDestinationClass(
			[NotNull] IRelationshipClass relationshipClass,
			[NotNull] ITable table)
		{
			return DatasetUtils.IsSameObjectClass(relationshipClass.DestinationClass,
			                                      (IObjectClass) table,
			                                      ObjectClassEquality.SameTableAnyVersion);
		}

		private static JoinType AdaptJoinTypeToRelationshipDirection(
			[NotNull] IRelationshipClass relationshipClass,
			[NotNull] IList<ITable> tables,
			JoinType joinType)
		{
			if (joinType == JoinType.InnerJoin || tables[0] == relationshipClass.OriginClass)
			{
				// return as is
				return joinType;
			}

			// switch the outer join side
			switch (joinType)
			{
				case JoinType.LeftJoin:
					return JoinType.RightJoin;

				case JoinType.RightJoin:
					return JoinType.LeftJoin;

				default:
					throw new AssertionException($"Unhandled join type: {joinType}");
			}
		}

		#endregion
	}
}

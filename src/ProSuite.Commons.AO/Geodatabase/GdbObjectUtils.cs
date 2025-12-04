using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Notifications;
using ProSuite.Commons.Progress;
using ProSuite.Commons.Text;

namespace ProSuite.Commons.AO.Geodatabase
{
	public static class GdbObjectUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull]
		public static string ConcatenateObjectIds<T>([NotNull] IEnumerable<T> list,
		                                             [CanBeNull] string separator)
			where T : IRow
		{
			return StringUtils.Concatenate(list,
			                               obj => Convert.ToString(obj.OID),
			                               separator);
		}

		/// <summary>
		/// Gets an ISet of objects as a list.
		/// </summary>
		/// <param name="objectSet">The object set.</param>
		/// <returns></returns>
		[NotNull]
		public static IList<IObject> GetObjectSetAsList([NotNull] ISet objectSet)
		{
			return GetObjectSetAsList<IObject>(objectSet);
		}

		/// <summary>
		/// Gets an ISet of objects or features as a list.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="objectSet">The object set.</param>
		/// <param name="predicate"></param>
		/// <returns></returns>
		[NotNull]
		public static IList<T> GetObjectSetAsList<T>(
			[NotNull] ISet objectSet,
			[CanBeNull] Predicate<T> predicate = null) where T : class, IObject
		{
			Assert.ArgumentNotNull(objectSet, nameof(objectSet));

			var list = new List<T>();

			objectSet.Reset();
			while (true)
			{
				var obj = (T) objectSet.Next();

				if (obj == null)
				{
					break;
				}

				if (predicate == null || predicate(obj))
				{
					list.Add(obj);
				}
			}

			return list;
		}

		/// <summary>
		/// Creates an object set based on a collection of objects, optionally applying a match predicate.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="objects">The collection of objects.</param>
		/// <param name="match">The predicate that must be true for an object to be included in the set.</param>
		/// <returns></returns>
		[NotNull]
		public static ISet GetObjectSet<T>([NotNull] IEnumerable<T> objects,
		                                   [CanBeNull] Predicate<T> match = null)
			where T : IObject
		{
			ISet set = new SetClass();

			foreach (T obj in objects)
			{
				if (match == null || match(obj))
				{
					set.Add(obj);
				}
			}

			return set;
		}

		/// <summary>
		/// Determines whether a field value in a row is Null.
		/// </summary>
		/// <param name="row">The row to test the value for</param>
		/// <param name="fieldIndex">Index of the field.</param>
		/// <returns>
		/// 	<c>true</c> if the value is Null; otherwise, <c>false</c>.
		/// </returns>
		public static bool IsNullOrEmpty([NotNull] IRow row, int fieldIndex)
		{
			object value = row.Value[fieldIndex];

			return IsNullOrEmpty(value);
		}

		/// <summary>
		/// Determines whether a field value in a row is Null.
		/// </summary>
		/// <param name="fieldValue">The field value.</param>
		/// <returns>
		/// 	<c>true</c> if the value is Null; otherwise, <c>false</c>.
		/// </returns>
		public static bool IsNullOrEmpty([CanBeNull] object fieldValue)
		{
			if (fieldValue == null)
			{
				return true;
			}

			if (fieldValue is DBNull)
			{
				return true;
			}

			return string.IsNullOrEmpty(fieldValue.ToString());
		}

		/// <summary>
		/// Creates and initializes (with the given subtypecode) 
		/// a feature in a given featureclass
		/// </summary>
		/// <param name="featureClass">The featureclass to create the new feature in</param>
		/// <param name="subtypeCode">The subtypeCode for the new feature</param>
		/// <returns>The newly created feature instance</returns>
		[NotNull]
		public static IFeature CreateFeature([NotNull] IFeatureClass featureClass,
		                                     int subtypeCode = -1)
		{
			return (IFeature) CreateRow((ITable) featureClass, subtypeCode);
		}

		/// <summary>
		/// Creates and initializes (with the given subtypecode) 
		/// a row in the given table
		/// </summary>
		/// <param name="table">The table to create the new row in</param>
		/// <param name="subtypeCode">The subtypeCode for the new row</param>
		/// <returns>The newly created row instance</returns>
		[NotNull]
		public static IRow CreateRow([NotNull] ITable table, int subtypeCode = -1)
		{
			Assert.ArgumentNotNull(table, nameof(table));

			IRow row = table.CreateRow();

			var subtypes = table as ISubtypes;

			if (subtypes != null)
			{
				var rowSubtypes = (IRowSubtypes) row;

				if (subtypes.HasSubtype)
				{
					// in case of negative subtype code --> assign default
					int codeToAssign = subtypeCode >= 0
						                   ? subtypeCode
						                   : subtypes.DefaultSubtypeCode;

					try
					{
						rowSubtypes.SubtypeCode = codeToAssign;
					}
					catch (COMException e)
					{
						// Observation on 10.2.2 against 10.0 Gdb: the DefaultSubtypeCode can return 0 
						// even though this is no valid subtype, resulting in an exception upon assignment
						_msg.Debug(
							string.Format("Error setting subtype {0} in {1}",
							              codeToAssign,
							              DatasetUtils.GetName(table)), e);

						throw new InvalidOperationException(string.Format(
							                                    subtypeCode < 0
								                                    ? "Error setting default subtype code {0} in {1}. Make sure a valid default subtype is set using the Set Default Subtype GP Tool."
								                                    : "Error setting subtype code {0} in {1}",
							                                    codeToAssign,
							                                    DatasetUtils.GetName(
								                                    table)),
						                                    e);
					}
				}

				// assign defaults (also if table has no subtype)
				rowSubtypes.InitDefaultValues();
			}

			return row;
		}

		[NotNull]
		public static IRow CreateRow(
			[NotNull] ITable table,
			[NotNull] IEnumerable<KeyValuePair<string, object>> valuesByFieldName)
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNull(valuesByFieldName, nameof(valuesByFieldName));

			IRow row = CreateRow(table);

			return SetValues(row, valuesByFieldName);
		}

		[NotNull]
		public static IRow SetValues(
			[NotNull] IRow row,
			[NotNull] IEnumerable<KeyValuePair<string, object>> valuesByFieldName)
		{
			Assert.ArgumentNotNull(row, nameof(row));
			Assert.ArgumentNotNull(valuesByFieldName, nameof(valuesByFieldName));

			ITable table = row.Table;

			foreach (KeyValuePair<string, object> pair in valuesByFieldName)
			{
				string fieldName = pair.Key;
				object fieldValue = pair.Value;

				int fieldIndex = table.FindField(fieldName);
				Assert.True(fieldIndex >= 0, "Field '{0}' not found in table '{1}'",
				            fieldName,
				            DatasetUtils.GetName(table));

				row.set_Value(fieldIndex, fieldValue ?? DBNull.Value);
			}

			return row;
		}

		/// <summary>
		/// Creates an object in a given class, using a given subtype code and 
		/// the corresponding default values.
		/// </summary>
		/// <param name="objectClass">The object class.</param>
		/// <param name="subtypeCode">The subtype code.</param>
		/// <returns>The newly created gdb object</returns>
		[NotNull]
		public static IObject CreateObject([NotNull] IObjectClass objectClass,
		                                   int subtypeCode)
		{
			return (IObject) CreateRow((ITable) objectClass, subtypeCode);
		}

		/// <summary>
		/// Creates an object in a given class, applying defaults.
		/// </summary>
		/// <param name="objectClass">The object class.</param>
		/// <returns>The newly created gdb object</returns>
		[NotNull]
		public static IObject CreateObject([NotNull] IObjectClass objectClass)
		{
			return (IObject) CreateRow((ITable) objectClass, -1);
		}

		[CanBeNull]
		public static T? ReadRowValue<T>([NotNull] IRow row, int fieldIndex)
			where T : struct
		{
			Assert.ArgumentNotNull(row, nameof(row));

			object value = row.Value[fieldIndex];
			return ReadRowValue<T>(value, fieldIndex, () => GetObjectId(row),
			                       () => DatasetUtils.GetName(row.Table));
		}

		[CanBeNull]
		public static T? ReadRowValue<T>([NotNull] IReadOnlyRow row, int fieldIndex)
			where T : struct
		{
			Assert.ArgumentNotNull(row, nameof(row));

			object value = row.get_Value(fieldIndex);
			return ReadRowValue<T>(value, fieldIndex, () => row.OID, () => row.Table.Name);
		}

		[CanBeNull]
		private static T? ReadRowValue<T>([NotNull] object value,
		                                  int fieldIndex,
		                                  Func<long?> getOid,
		                                  Func<string> getTableName)
			where T : struct
		{
			if (value == DBNull.Value)
			{
				_msg.VerboseDebug(
					() => $"ReadRowValue: Field value at <index> {fieldIndex} of row is null.");

				return null;
			}

			try
			{
				if (typeof(T) == typeof(int))
				{
					if (value is short)
					{
						// Short Integer field type is returned as short, cannot unbox directly to int:
						return Convert.ToInt32(value) as T?;
					}

					if (value is long)
					{
						// Typically for long OID type that currently still is known to be an int.
						// But long object cannot unbox directly to int:
						return Convert.ToInt32(value) as T?;
					}
				}

				if (typeof(T) == typeof(Guid))
				{
					// Guids come back as string
					var guidString = value as string;

					if (string.IsNullOrEmpty(guidString))
					{
						return null;
					}

					TypeConverter converter = TypeDescriptor.GetConverter(typeof(T));

					return (T) Assert.NotNull(converter.ConvertFrom(guidString));
				}

				return (T) value;
			}
			catch (Exception ex)
			{
				long? rowOid = getOid();

				_msg.ErrorFormat(
					"ReadRowValue: Error casting value {0} of type {1} into type {2} for row <oid> {3} at field index {4} in {5}: {6}",
					value, value.GetType(), typeof(T), fieldIndex, rowOid, getTableName(),
					ex.Message);

				throw;
			}
		}

		[CanBeNull]
		public static T? ReadRowValue<T>([NotNull] IRow row, [NotNull] string fieldName)
			where T : struct
		{
			Assert.ArgumentNotNull(row, nameof(row));
			Assert.ArgumentNotNullOrEmpty(fieldName, nameof(fieldName));

			int fieldIndex = row.Fields.FindField(fieldName);
			Assert.True(fieldIndex >= 0, "Field not found: {0}", fieldName);

			return ReadRowValue<T>(row, fieldIndex);
		}

		public static string ReadRowStringValue([NotNull] IRow row,
		                                        [NotNull] string fieldName)
		{
			Assert.ArgumentNotNull(row, nameof(row));
			Assert.ArgumentNotNullOrEmpty(fieldName, nameof(fieldName));

			int fieldIndex = row.Fields.FindField(fieldName);
			Assert.True(fieldIndex >= 0, "Field not found: {0}", fieldName);

			return ReadRowStringValue(row, fieldIndex);
		}

		private static string ReadRowStringValue(IRow row, int fieldIndex)
		{
			object value = row.Value[fieldIndex];

			if (value == DBNull.Value)
			{
				_msg.VerboseDebug(
					() => $"ReadRowValue: Field value at <index> {fieldIndex} of row is null.");

				return null;
			}

			return Convert.ToString(value);
		}

		/// <summary>
		/// Reads the long OID using a known index rather than directly from the property.
		/// </summary>
		/// <param name="row"></param>
		/// <param name="fieldIndex"></param>
		/// <returns></returns>
		public static long? ReadRowOidValue(IRow row, int fieldIndex)
		{
			object value = row.Value[fieldIndex];

			if (value == DBNull.Value)
			{
				return null;
			}

#if ARCGIS_11_0_OR_GREATER
			// long is expected:
			if (value is long longValue)
			{
				return longValue;
			}

			// however, for query tables it is still an Int32!
			return Convert.ToInt64(value);
#else
			// First unbox: (int)object
			// to int, then cast to long (int implicitly converts to long).
			// Directly casting from object (Int32) to long fails!
			return (int) value;
#endif
		}

		/// <summary>
		/// Reads the long OID using a known index rather than directly from the property.
		/// </summary>
		/// <param name="row"></param>
		/// <param name="fieldIndex"></param>
		/// <returns></returns>
		public static long? ReadRowOidValue(IReadOnlyRow row, int fieldIndex)
		{
			object value = row.get_Value(fieldIndex);

			if (value == DBNull.Value)
			{
				return null;
			}
#if ARCGIS_11_0_OR_GREATER
			// long is expected:
			if (value is long longValue)
			{
				return longValue;
			}

			// however, for query tables it is still an Int32!
			return Convert.ToInt64(value);
#else
			// It is an int by convention in AO10 but ReadOnly can supply long object,
			// depending on the implementation:

			// First unbox: (int)object
			// to int, then cast to long (int implicitly converts to long).
			// Directly casting from object (Int32) to long fails!
			if (value is int intValue)
			{
				return intValue;
			}

			// Depending on the implementation, it could already be a long (or DBNull)
			return (long) value;
#endif
		}

		public static bool TryReadBlobValue([NotNull] IRow row,
		                                    int fieldIdx,
		                                    [NotNull] ref IPersistStream intoObject)
		{
			Assert.ArgumentNotNull(row, nameof(row));
			Assert.ArgumentNotNull(intoObject, nameof(intoObject));

			object value = row.Value[fieldIdx];

			if (value == DBNull.Value)
			{
				return false;
			}

			var origBlobStream = value as IMemoryBlobStreamVariant;
			Assert.NotNull(origBlobStream, "blob is not IMemoryBlobStreamVariant");

			var copyBlobStream = new MemoryBlobStreamClass();

			// see RepresentationUtils.LoadFieldOverrideBlob() and 
			// GdbQueryUtilsTest.CannotReadBlobValueLearingTest():
			// without this roundtrip from stream to byte array to stream,
			// the RemoteRead() into the guidBytes array reads zero bytes.
			// Thanks to WIE for finding this roundtrip solution.

			object bytes;
			origBlobStream.ExportToVariant(out bytes);
			copyBlobStream.ImportFromVariant(bytes);
			IObjectStream objectStream = new ObjectStreamClass { Stream = copyBlobStream };

			try
			{
				intoObject.Load(objectStream);
			}
			catch (Exception ex)
			{
				_msg.Debug("Exception reading blob value.", ex);
				return false;
			}
			finally
			{
				Marshal.ReleaseComObject(origBlobStream);
				Marshal.ReleaseComObject(copyBlobStream);
				Marshal.ReleaseComObject(objectStream);
			}

			return true;
		}

		public static void WriteBlobValue([NotNull] IRow row, int fieldIdx,
		                                  [NotNull] IPersistStream objToPersist)
		{
			Assert.ArgumentNotNull(row, nameof(row));
			Assert.ArgumentCondition(fieldIdx >= 0, "fieldIdx must not be negative.");
			Assert.ArgumentNotNull(objToPersist, nameof(objToPersist));

			IMemoryBlobStream memoryBlobStream = new MemoryBlobStreamClass();
			IObjectStream objectStream = new ObjectStreamClass();
			objectStream.Stream = memoryBlobStream;

			objToPersist.Save(objectStream, 0);

			row.set_Value(fieldIdx, memoryBlobStream);

			// very important! The streams must be released otherwise
			// the object won't re-hydrate any more!
			Marshal.ReleaseComObject(memoryBlobStream);
			Marshal.ReleaseComObject(objectStream);
		}

		[CanBeNull]
		public static T? ConvertRowValue<T>([NotNull] IRow row, int fieldIndex)
			where T : struct
		{
			Assert.ArgumentNotNull(row, nameof(row));

			object value = row.Value[fieldIndex];

			if (value == null || value == DBNull.Value)
			{
				_msg.VerboseDebug(
					() => $"ConvertRowValue: Field value at <index> {fieldIndex} of row is null.");

				return null;
			}

			try
			{
				//// work-around for Change-Type not supporting nullable types in .NET 2.0
				//// http://connect.microsoft.com/VisualStudio/feedback/ViewFeedback.aspx?FeedbackID=94624
				//NullableConverter nullableConverter = new NullableConverter(typeof(T));
				//Type conversionType = nullableConverter.UnderlyingType;

				return (T?) Convert.ChangeType(value, typeof(T));
			}
			catch (Exception ex)
			{
				int? rowOid = GetObjectId(row);

				_msg.ErrorFormat(
					"ConvertRowValue: Error converting value {0} of type {1} into type {2} for row <oid> {3} at field index {4} in {5}: {6}",
					value, value.GetType(), typeof(T), fieldIndex, rowOid,
					((IDataset) row.Table).Name, ex.Message);

				throw;
			}
		}

		public static int? GetObjectId([NotNull] IRow row)
		{
			return GetObjectId((IObject) row);
		}

		public static int? GetObjectId([NotNull] IObject obj)
		{
			return obj.HasOID
				       ? (int?) obj.OID
				       : null;
		}

		/// <summary>
		/// Gets the object ids for a given selection set.
		/// </summary>
		/// <param name="selectionSet">The selection set.</param>
		/// <returns></returns>
		[NotNull]
		public static IEnumerable<long> GetObjectIds([NotNull] ISelectionSet selectionSet)
		{
			Assert.ArgumentNotNull(selectionSet, nameof(selectionSet));

			var ids = selectionSet.IDs;

			try
			{
				ids.Reset();

				long currentOid;
				while ((currentOid = ids.Next()) >= 0)
				{
					yield return currentOid;
				}
			}
			finally
			{
				ids.Reset();
			}
		}

		[NotNull]
		public static IEnumerable<long> GetObjectIds<T>([NotNull] IEnumerable<T> rows)
			where T : IRow
		{
			Assert.ArgumentNotNull(rows, nameof(rows));

			foreach (T row in rows)
			{
				if (row.HasOID)
				{
					yield return row.OID;
				}
			}
		}

		[NotNull]
		public static IList<IGeometry> GetGeometries(
			[NotNull] IEnumerable<IFeature> features)
		{
			return features.Select(feature => feature.Shape).ToList();
		}

		public static void StoreGeometries(
			[NotNull] IDictionary<IFeature, IGeometry> geometriesByFeature,
			[CanBeNull] IProgressFeedback progressFeedback = null,
			[CanBeNull] ITrackCancel trackCancel = null)
		{
			progressFeedback?.SetRange(0, geometriesByFeature.Count);

			foreach (KeyValuePair<IFeature, IGeometry> keyValuePair in geometriesByFeature)
			{
				if (trackCancel != null && ! trackCancel.Continue())
				{
					return;
				}

				progressFeedback?.Advance("Storing {0} of {1} feature(s)",
				                          progressFeedback.CurrentValue,
				                          progressFeedback.MaximumValue);

				IFeature feature = keyValuePair.Key;
				IGeometry geometry = keyValuePair.Value;

				Assert.True(DatasetUtils.IsBeingEdited(feature.Class),
				            "Target feature {0} is not being edited.",
				            ToString(feature));

				SetFeatureShape(feature, geometry);

				feature.Store();
			}
		}

		public static IGeometry GetFeatureShape(IReadOnlyFeature roFeature)
		{
			if (roFeature is IFeature feature)
			{
				return GetFeatureShape(feature);
			}

			return roFeature.Shape;
		}

		public static IGeometry GetFeatureShape(IFeature feature)
		{
			IGeometry featureShape;
			try
			{
				featureShape = feature.Shape;
			}
			catch (COMException e)
			{
				// This sometimes happens with some query name based tables (the shape property is null or throws E_FAIL)
				_msg.Debug($"Error getting shape of feature {ToString(feature)}", e);

				var featureClass = (IFeatureClass) feature.Class;

				if (DatasetUtils.IsQueryNameBasedClass(featureClass))
				{
					_msg.DebugFormat(
						"Trying shape field value of query-name based feature class...");

					string shapeFieldName = featureClass.ShapeFieldName;
					int shapeFieldIdx = featureClass.Fields.FindField(shapeFieldName);

					Assert.True(shapeFieldIdx >= 0, "Shape field {0} not found in {1}",
					            shapeFieldName, DatasetUtils.GetName(featureClass));

					featureShape = (IGeometry) feature.Value[shapeFieldIdx];

					_msg.VerboseDebug(
						() =>
							$"The shape extracted from the value of the shape field is {GeometryUtils.ToString(featureShape)}");
				}
				else
				{
					throw;
				}
			}

			return featureShape;
		}

		/// <summary>
		/// Adapts the spatial reference if necessary and sets the feature's shape.
		/// Store is NOT called in this method.
		/// The feature's shape might have a different spatial reference than before. Consider projecting
		/// the feature's shape back to its original spatial reference *after* calling store.
		/// </summary>
		/// <param name="feature"></param>
		/// <param name="newGeometry"></param>
		public static void SetFeatureShape([NotNull] IFeature feature,
		                                   [NotNull] IGeometry newGeometry)
		{
			ISpatialReference targetSpatialRef =
				DatasetUtils.GetSpatialReference(feature);

			if (_msg.IsVerboseDebugEnabled)
			{
				IGeometry oldShape = feature.Shape;

				if (oldShape != null)
				{
					if (! SpatialReferenceUtils.AreEqual(
						    targetSpatialRef, oldShape.SpatialReference))
					{
						// NOTE: If the projection does not happen here (possible for simple features) it will happen in Store() 
						// -> we can't fix it here, caller has to project back to map SR
						_msg.VerboseDebug(
							() =>
								"SetFeatureShape: Spatial reference of feature class and existing feature's shape are not equal. The feature's shape will be left with a different SR.");
					}

					Marshal.ReleaseComObject(oldShape);
				}
			}

			SetFeatureShape((IFeatureBuffer) feature, newGeometry, targetSpatialRef);
		}

		/// <summary>
		/// Adapts the spatial reference if necessary and sets the feature buffer's shape.
		/// The feature buffer's shape might have a different spatial reference than before. Consider projecting
		/// the feature buffer's shape back to its original spatial reference *after* calling store.
		/// </summary>
		/// <param name="target"></param>
		/// <param name="newGeometry"></param>
		/// <param name="targetSpatialRefToEnsure"></param>
		public static void SetFeatureShape(
			[NotNull] IFeatureBuffer target,
			[NotNull] IGeometry newGeometry,
			ISpatialReference targetSpatialRefToEnsure = null)
		{
			IGeometry useGeometry;

			if (targetSpatialRefToEnsure == null)
			{
				useGeometry = newGeometry;
			}
			else if (GeometryUtils.EnsureSpatialReference(
				         newGeometry, targetSpatialRefToEnsure, out useGeometry))
			{
				// this is probably only necessary for non-simple features
				_msg.Debug("SetFeatureShape: Spatial reference of feature class " +
				           "and new geometry are not equal. New geometry was projected.");

				if (_msg.IsVerboseDebugEnabled)
				{
					_msg.DebugFormat(
						"Old Spatial Reference: {0}\nNew Spatial Reference: {1}",
						SpatialReferenceUtils.ToString(newGeometry.SpatialReference),
						SpatialReferenceUtils.ToString(useGeometry.SpatialReference));
				}
			}

			_msg.VerboseDebug(() => $"Setting shape: {GeometryUtils.ToString(newGeometry)}");

			try
			{
				if (target.Shape == useGeometry)
				{
					// Avoid COMException (0x80040574): The modified geometry must be a different
					// geometry instance from the feature's original geometry (e.g., a copy or new instance).
					useGeometry = GeometryFactory.Clone(useGeometry);
				}

				target.Shape = useGeometry;
			}
			catch (COMException comException)
			{
				_msg.Debug($"Error setting shape for {ToString(target)}: " +
				           $"{GeometryUtils.ToString(useGeometry)}", comException);

				if (comException.ErrorCode ==
				    (int) fdoError.FDO_E_GEOMETRY_TYPE_NOT_SUPPORTED)
				{
#if !Server
					if (target is ISimpleEdgeFeature &&
					    ((IGeometryCollection) useGeometry).GeometryCount > 1)
					{
						throw new InvalidOperationException(
							$"Cannot assign multipart geometry to geometric network edge {ToString(target)}",
							comException);
					}
#endif
				}

				// NOTE: the ArcObjects error message for loop geometries assigned to edges is actually ok
				throw;
			}
			catch (Exception e)
			{
				_msg.Debug($"Error setting shape for feature {ToString(target)}: " +
				           $"{GeometryUtils.ToString(useGeometry)}", e);
				throw;
			}

			if (useGeometry != newGeometry)
			{
				Marshal.ReleaseComObject(useGeometry);
			}
		}

		/// <summary>
		/// Duplicates the feature by creating a new. All attribute values are copied, except
		/// if honourGdbSplitRules is true and a domain has a 'Default value' split rule.
		/// </summary>
		/// <param name="feature">The feature.</param>
		/// <param name="exceptShape">Whether the shape should be excluded from copying or not.</param>
		/// <param name="honourGdbSplitRules">If true the split rule of the GDB will be applied,
		/// which means that also the original feature can be changed for 'Default value' split 
		/// rules.</param>
		/// <returns>The newly created feature instance</returns>
		[NotNull]
		public static IFeature DuplicateFeature([NotNull] IFeature feature,
		                                        bool exceptShape,
		                                        bool honourGdbSplitRules = false)
		{
			Assert.ArgumentNotNull(feature, nameof(feature));

			IFeature result = CreateFeature((IFeatureClass) feature.Class);

			if (honourGdbSplitRules)
			{
				const bool applyAlsoToBaseFeature = true;
				SplitAttributes(feature, result, applyAlsoToBaseFeature);
			}

			// NOTE: do not duplicate the shape because it will be overwritten anyway
			//		 and shape copying can result in the 4GB memory limit being hit for large features
			//		 that intersect many times.
			CopyAttributeValues(feature, result, exceptShape);

			return result;
		}

		/// <summary>
		/// Copies the values of the sourceRow to the targetRow
		/// if a matching field in the target is found.
		/// </summary>
		/// <param name="sourceRow">Source row</param>
		/// <param name="targetRow">Target row</param>
		/// <param name="exceptShape">Whether the shape value should be excluded or not</param>
		public static void CopyAttributeValues([NotNull] IRow sourceRow,
		                                       [NotNull] IRow targetRow,
		                                       bool exceptShape = false)
		{
			Assert.ArgumentNotNull(sourceRow, nameof(sourceRow));
			Assert.ArgumentNotNull(targetRow, nameof(targetRow));

			Predicate<IField> includeTarget =
				exceptShape
					? field => field.Type != esriFieldType.esriFieldTypeGeometry
					: (Predicate<IField>) null;

			IDictionary<int, int> copyIndexMatrix = CreateCopyIndexMatrix(
				sourceRow.Table, targetRow.Table, includeTarget);

			CopyAttributeValues(sourceRow, targetRow, copyIndexMatrix);
		}

		/// <summary>
		/// Copies the values of the sourceRow to the targetRow using
		/// the given copyIndexMatrix (should be generated with the 
		/// CreateCopyIndexMatrix method).
		/// </summary>
		/// <param name="sourceRow">Source row</param>
		/// <param name="targetRow">Target row</param>
		/// <param name="includeTargetField">Predicate to determine the inclusion of a given 
		/// target field (optional; field is included if null)</param>
		public static void CopyAttributeValues([NotNull] IRow sourceRow,
		                                       [NotNull] IRow targetRow,
		                                       [CanBeNull] Predicate<IField>
			                                       includeTargetField)
		{
			Assert.ArgumentNotNull(sourceRow, nameof(sourceRow));
			Assert.ArgumentNotNull(targetRow, nameof(targetRow));

			IDictionary<int, int> copyIndexMatrix = CreateCopyIndexMatrix(
				sourceRow.Table, targetRow.Table, includeTargetField);

			CopyAttributeValues(sourceRow, targetRow, copyIndexMatrix);
		}

		/// <summary>
		/// Copies the values of the sourceRow to the targetRow using
		/// the given FieldComparison option to match source with target fields 
		/// and additional predicate to potentially exclude matching target fields.
		/// </summary>
		/// <param name="sourceRow">Source row</param>
		/// <param name="targetRow">Target row</param>
		/// <param name="includeTargetField">Predicate to determine the inclusion of a given 
		/// target field (optional; field is included if null)</param>
		/// <param name="fieldComparison">The comparison options to identify matching source and target fields</param>
		public static void CopyAttributeValues([NotNull] IRow sourceRow,
		                                       [NotNull] IRow targetRow,
		                                       [CanBeNull] Predicate<IField>
			                                       includeTargetField,
		                                       FieldComparison fieldComparison)
		{
			Assert.ArgumentNotNull(sourceRow, nameof(sourceRow));
			Assert.ArgumentNotNull(targetRow, nameof(targetRow));

			IDictionary<int, int> copyIndexMatrix = CreateCopyIndexMatrix(
				sourceRow.Table, targetRow.Table, includeTargetField, fieldComparison);

			CopyAttributeValues(sourceRow, targetRow, copyIndexMatrix);
		}

		/// <summary>
		/// Copies the values of the sourceRow to the targetRow using
		/// the given copyIndexMatrix (should be generated with the 
		/// CreateCopyIndexMatrix method).
		/// </summary>
		/// <param name="sourceRow">Source row</param>
		/// <param name="targetRow">Target row</param>
		/// <param name="copyIndexMatrix">Matrix with the target indices
		/// and matching source indices</param>
		/// <param name="exceptShape">Excludes the shape field from being copied, even if it is in 
		/// the copy index matrix.</param>
		public static void CopyAttributeValues(
			[NotNull] IRow sourceRow,
			[NotNull] IRowBuffer targetRow,
			[NotNull] IDictionary<int, int> copyIndexMatrix,
			bool exceptShape = false)
		{
			Assert.ArgumentNotNull(sourceRow, nameof(sourceRow));
			Assert.ArgumentNotNull(targetRow, nameof(targetRow));
			Assert.ArgumentNotNull(copyIndexMatrix, nameof(copyIndexMatrix));

			foreach (int targetIndex in copyIndexMatrix.Keys)
			{
				int sourceIndex = copyIndexMatrix[targetIndex];

				if (sourceIndex < 0)
				{
					continue;
				}

				if (exceptShape &&
				    sourceRow.Fields.Field[sourceIndex].Type ==
				    esriFieldType.esriFieldTypeGeometry)
				{
					continue;
				}

				CopyFieldValue(sourceRow, sourceIndex, targetRow, targetIndex);
			}
		}

		public static bool TryCopyAttributeValues(
			[NotNull] IRow sourceRow,
			[NotNull] IRow targetRow,
			[NotNull] IDictionary<int, int> copyIndexMatrix,
			NotificationCollection notifications = null)
		{
			Assert.ArgumentNotNull(sourceRow, nameof(sourceRow));
			Assert.ArgumentNotNull(targetRow, nameof(targetRow));
			Assert.ArgumentNotNull(copyIndexMatrix, nameof(copyIndexMatrix));

			var canCopyAll = true;
			foreach (int targetIndex in copyIndexMatrix.Keys)
			{
				int sourceIndex = copyIndexMatrix[targetIndex];
				if (sourceIndex > -1)
				{
					if (
						! TryCopyFieldValue(sourceRow, sourceIndex, targetRow,
						                    targetIndex,
						                    notifications))
					{
						canCopyAll = false;
					}
				}
			}

			return canCopyAll;
		}

		/// <summary>
		/// Creates a matrix holding the target row field index as key and
		/// the matching source row field index in the value. If none matching
		/// source field is found for the target feature, the index will be -1.
		/// </summary>
		/// <param name="sourceClass">Source feature class</param>
		/// <param name="targetClass">Target feature class</param>
		/// <param name="exceptShape">Whether the shape value should be excluded or not</param>
		/// <returns>Dictionary with the indices of the targetClass as key
		/// and the matching index of the sourceClass as value (could be -1)</returns>
		public static IDictionary<int, int> CreateCopyIndexMatrix(
			[NotNull] IClass sourceClass, [NotNull] IClass targetClass,
			bool exceptShape)
		{
			Predicate<IField> includeTarget =
				exceptShape
					? field => field.Type != esriFieldType.esriFieldTypeGeometry
					: (Predicate<IField>) null;

			return CreateCopyIndexMatrix(sourceClass, targetClass, includeTarget);
		}

		/// <summary>
		/// Creates a matrix holding the target row field index as key and
		/// the matching source row field index in the value. If none matching
		/// source field is found for the target feature, the index will be -1.
		/// </summary>
		/// <param name="sourceClass">Source feature class</param>
		/// <param name="targetClass">Target feature class</param>
		/// <param name="includeTargetField">Predicate to determine the inclusion of a given 
		/// target field (optional; field is included if null)</param>
		/// <returns>Dictionary with the indices of the targetClass as key
		/// and the matching index of the sourceClass as value (could be -1)</returns>
		[NotNull]
		public static IDictionary<int, int> CreateCopyIndexMatrix(
			[NotNull] IClass sourceClass,
			[NotNull] IClass targetClass,
			[CanBeNull] Predicate<IField> includeTargetField = null)
		{
			const bool includeReadOnlyFields = false;
			const bool searchJoinedFields = false;
			return CreateMatchingIndexMatrix(sourceClass, targetClass,
			                                 includeReadOnlyFields,
			                                 searchJoinedFields,
			                                 includeTargetField);
		}

		/// <summary>
		/// Creates a matrix holding the target row field index as key and
		/// the matching source row field index in the value. If none matching
		/// source field is found for the target feature, the index will be -1.
		/// </summary>
		/// <param name="sourceClass">Source feature class</param>
		/// <param name="targetClass">Target feature class</param>
		/// <param name="includeTargetField">Predicate to determine the inclusion of a given 
		/// target field (optional; field is included if null)</param>
		/// <returns>Dictionary with the indices of the targetClass as key
		/// and the matching index of the sourceClass as value (could be -1)</returns>
		/// <param name="fieldComparison">The field comparison option to identify matching fields.</param>
		[NotNull]
		public static IDictionary<int, int> CreateCopyIndexMatrix(
			[NotNull] IClass sourceClass,
			[NotNull] IClass targetClass,
			[CanBeNull] Predicate<IField> includeTargetField,
			FieldComparison fieldComparison)
		{
			return CreateMatchingIndexMatrix(sourceClass, targetClass,
			                                 false, false,
			                                 includeTargetField, fieldComparison);
		}

		/// <summary>
		/// Creates a matrix holding the target row field index as key and
		/// the matching source row field index in the value. If none matching
		/// source field is found for the target feature, the index will be -1.
		/// </summary>
		/// <param name="sourceClass">Source feature class</param>
		/// <param name="targetClass">Target feature class</param>
		/// <param name="includeReadOnlyFields">include readonly fields in matrix</param> 
		/// <param name="searchJoinedFields">match fields in source table, that start with 
		/// the table name of the target class</param>
		/// <param name="includeTargetField">Predicate to determine the inclusion of a given 
		/// target field (optional; field is included if null)</param>
		/// <returns>Dictionary with the indices of the targetClass as key
		/// and the matching index of the sourceClass as value (could be -1)</returns>
		/// <param name="fieldComparison">The comparison options to identify matches.</param>
		[NotNull]
		public static IDictionary<int, int> CreateMatchingIndexMatrix(
			[NotNull] IClass sourceClass,
			[NotNull] IClass targetClass,
			bool includeReadOnlyFields = false,
			bool searchJoinedFields = false,
			[CanBeNull] Predicate<IField> includeTargetField = null,
			FieldComparison fieldComparison = FieldComparison.FieldNameDomainName)
		{
			Assert.ArgumentNotNull(sourceClass, nameof(sourceClass));
			Assert.ArgumentNotNull(targetClass, nameof(targetClass));

			IFields sourceFields = sourceClass.Fields;
			IFields targetFields = targetClass.Fields;

			string targetTableName = null;

			if (searchJoinedFields)
			{
				targetTableName = ((IDataset) targetClass).Name;
			}

			return MatchingIndexMatrix(sourceFields, targetFields, includeReadOnlyFields,
			                           targetTableName, includeTargetField, fieldComparison);
		}

		/// <summary>
		/// Creates a matrix holding the target row field index as key and
		/// the matching source row field index in the value. If none matching
		/// source field is found for the target feature, the index will be -1.
		/// </summary>
		/// <param name="sourceTable">Source table</param>
		/// <param name="targetTable">Target table</param>
		/// <param name="includeReadOnlyFields">include readonly fields in matrix</param> 
		/// <param name="searchJoinedFields">match fields in source table, that start with 
		/// the table name of the target class</param>
		/// <param name="includeTargetField">Predicate to determine the inclusion of a given 
		/// target field (optional; field is included if null)</param>
		/// <returns>Dictionary with the indices of the targetClass as key
		/// and the matching index of the sourceClass as value (could be -1)</returns>
		/// <param name="fieldComparison">The comparison options to identify matches.</param>
		[NotNull]
		public static IDictionary<int, int> CreateMatchingIndexMatrix(
			[NotNull] IReadOnlyTable sourceTable,
			[NotNull] IReadOnlyTable targetTable,
			bool includeReadOnlyFields = false,
			bool searchJoinedFields = false,
			[CanBeNull] Predicate<IField> includeTargetField = null,
			FieldComparison fieldComparison = FieldComparison.FieldNameDomainName)
		{
			Assert.ArgumentNotNull(sourceTable, nameof(sourceTable));
			Assert.ArgumentNotNull(targetTable, nameof(targetTable));

			IFields sourceFields = sourceTable.Fields;
			IFields targetFields = targetTable.Fields;

			string targetTableName = null;

			if (searchJoinedFields)
			{
				targetTableName = targetTable.Name;
			}

			return MatchingIndexMatrix(sourceFields, targetFields, includeReadOnlyFields,
			                           targetTableName, includeTargetField, fieldComparison);
		}

		/// <summary>
		/// Creates a matrix holding the target row field index as key and
		/// the matching source row field index in the value. If none matching
		/// source field is found for the target feature, the index will be -1.
		/// </summary>
		/// <param name="sourceFields">Source feature class</param>
		/// <param name="targetFields">Target feature class</param>
		/// <param name="includeReadOnlyFields">include readonly fields in matrix</param> 
		/// <param name="targetTableName">match fields in source fields, that start with 
		/// the specified table name of the target table.</param>
		/// <param name="includeTargetField">Predicate to determine the inclusion of a given 
		/// target field (optional; field is included if null)</param>
		/// <returns>Dictionary with the indices of the targetClass as key
		/// and the matching index of the sourceClass as value (could be -1)</returns>
		/// <param name="fieldComparison">The comparison options to identify matches.</param>
		[NotNull]
		public static IDictionary<int, int> MatchingIndexMatrix(
			[NotNull] IFields sourceFields,
			[NotNull] IFields targetFields,
			bool includeReadOnlyFields = false,
			[CanBeNull] string targetTableName = null,
			[CanBeNull] Predicate<IField> includeTargetField = null,
			FieldComparison fieldComparison = FieldComparison.FieldNameDomainName)
		{
			var result = new Dictionary<int, int>();

			int targetFieldCount = targetFields.FieldCount;

			for (var index = 0; index < targetFieldCount; index++)
			{
				IField targetField = targetFields.Field[index];

				if (! includeReadOnlyFields && ! targetField.Editable)
				{
					continue;
				}

				if (includeTargetField != null && ! includeTargetField(targetField))
				{
					continue;
				}

				int matchingSourceFieldIndex =
					DatasetUtils.FindMatchingFieldIndex(targetField,
					                                    targetTableName,
					                                    sourceFields, fieldComparison);

				if (matchingSourceFieldIndex < 0)
				{
					if (_msg.IsVerboseDebugEnabled)
					{
						_msg.DebugFormat(
							"field not found: {0} (type: {1} table name: {2} comparison: {3})",
							targetField.Name, targetField.Type,
							targetTableName ?? "<null>", fieldComparison);

						int fieldCount = sourceFields.FieldCount;

						for (var sourceFieldIndex = 0;
						     sourceFieldIndex < fieldCount;
						     sourceFieldIndex++)
						{
							IField sourceField = sourceFields.Field[sourceFieldIndex];
							_msg.DebugFormat("- {0}: {1}", sourceField.Name,
							                 sourceField.Type);
						}
					}
				}

				result.Add(index, matchingSourceFieldIndex);
			}

			return result;
		}

		/// <summary>
		/// Applies the attribute rules for splitting a new feature out of an existing base feature.
		/// the new feature will have its attributes updated according to the split policy for the
		/// involved domains.
		/// </summary>
		/// <param name="baseFeature">The base feature.</param>
		/// <param name="newFeature">The new feature.</param>
		/// <remarks>The "Geometry Ratio" policy is not yet supported. 
		/// If this policy is defined for a domain, the value will be duplicated.
		/// </remarks>
		public static void SplitAttributes([NotNull] IFeature baseFeature,
		                                   [NotNull] IFeature newFeature)
		{
			const bool applyAlsoToBaseFeature = false;
			SplitAttributes(baseFeature, newFeature, applyAlsoToBaseFeature);
		}

		/// <summary>
		/// Applies the attribute rules for splitting a new feature out of an existing base feature.
		/// the new feature will have its attributes updated according to the split policy for the
		/// involved domains.
		/// </summary>
		/// <param name="baseFeature">The base feature.</param>
		/// <param name="newFeature">The new feature.</param>
		/// <param name="applyAlsoToBaseFeature">Whether or not the split policy 'Default value'
		/// should also be applied to the base feature. This is the case in the ArcMap standard tools.</param>
		/// <remarks>The "Geometry Ratio" policy is not yet supported. 
		/// If this policy is defined for a domain, the value will be duplicated.
		/// </remarks>
		public static void SplitAttributes([NotNull] IFeature baseFeature,
		                                   [NotNull] IFeature newFeature,
		                                   bool applyAlsoToBaseFeature)
		{
			// TODO: ArcGIS does not honour the split rule if it is an attribute
			//		 used by a representation (and probably annotations too).
			//		 To be consistent these cases should be excluded here as well.
			Assert.ArgumentNotNull(baseFeature, nameof(baseFeature));
			Assert.ArgumentNotNull(newFeature, nameof(newFeature));
			Assert.ArgumentCondition(newFeature.Class == baseFeature.Class,
			                         "Features must be from same feature class");
			Assert.ArgumentCondition(newFeature.OID != baseFeature.OID,
			                         "Features have same OID");

			var featureClass = (IFeatureClass) baseFeature.Class;
			IFields fields = featureClass.Fields;
			int fieldCount = fields.FieldCount;
			var subtypes = featureClass as ISubtypes;

			if (subtypes != null && subtypes.HasSubtype)
			{
				object subtypeValue = baseFeature.Value[subtypes.SubtypeFieldIndex];

				if (! (subtypeValue is DBNull))
				{
					int subtypeCode = Convert.ToInt32(subtypeValue);

					newFeature.set_Value(subtypes.SubtypeFieldIndex, subtypeCode);
					((IRowSubtypes) newFeature).InitDefaultValues();

					for (var fieldIndex = 0; fieldIndex < fieldCount; fieldIndex++)
					{
						IField field = fields.Field[fieldIndex];
						if (! IsEditableAttribute(field))
						{
							continue;
						}

						IDomain domain = subtypes.Domain[subtypeCode, field.Name] ??
						                 field.Domain;

						ApplySplitPolicy(newFeature, baseFeature, fieldIndex, field,
						                 domain,
						                 applyAlsoToBaseFeature);
					}

					// applied policy for subtype
					return;
				}
			}

			// The feature class does not use subtypes, or the subtype is null for the row
			((IRowSubtypes) newFeature).InitDefaultValues();

			for (var fieldIndex = 0; fieldIndex < fieldCount; fieldIndex++)
			{
				IField field = fields.Field[fieldIndex];
				if (! IsEditableAttribute(field))
				{
					continue;
				}

				ApplySplitPolicy(newFeature, baseFeature, fieldIndex, field, field.Domain,
				                 applyAlsoToBaseFeature);
			}
		}

		/// <summary>
		/// Returns a string representation of the <see cref="IObject"/>.
		/// </summary>
		/// <param name="obj">The object to get the string representation for.</param>
		/// <returns></returns>
		[NotNull]
		public static string ToString([NotNull] IObject obj)
		{
			var oid = @"[n/a]";
			if (obj.HasOID)
			{
				oid = obj.OID.ToString(CultureInfo.InvariantCulture);
			}

			string className;
			try
			{
				className = DatasetUtils.GetName(obj.Class);
			}
			catch (Exception)
			{
				className = "[error getting class name]";
			}

			return string.Format("oid={0} class={1}", oid, className);
		}

		/// <summary>
		/// Returns a string representation of the <see cref="IObject"/>.
		/// </summary>
		/// <param name="row">The object to get the string representation for.</param>
		/// <returns></returns>
		[NotNull]
		public static string ToString([NotNull] IRow row)
		{
			string oid;
			try
			{
				oid = row.HasOID
					      ? row.OID.ToString(CultureInfo.InvariantCulture)
					      : @"[n/a]";
			}
			catch (Exception e)
			{
				oid = string.Format("[error getting OID: {0}]", e.Message);
			}

			string tableName;
			try
			{
				tableName = DatasetUtils.GetTableName(row.Table);
			}
			catch (Exception e)
			{
				tableName = string.Format("[error getting table name: {0}]", e.Message);
			}

			return string.Format("oid={0} table={1}", oid, tableName);
		}

		[NotNull]
		public static string ToString([NotNull] IReadOnlyRow row)
		{
			string oid;
			try
			{
				oid = row.HasOID
					      ? row.OID.ToString(CultureInfo.InvariantCulture)
					      : @"[n/a]";
			}
			catch (Exception e)
			{
				oid = string.Format("[error getting OID: {0}]", e.Message);
			}

			string tableName;
			try
			{
				tableName = row.Table.Name;
			}
			catch (Exception e)
			{
				tableName = string.Format("[error getting table name: {0}]", e.Message);
			}

			return string.Format("oid={0} table={1}", oid, tableName);
		}

		[NotNull]
		public static string ToString([NotNull] IFeatureBuffer featureBuffer)
		{
			IFeature feature = featureBuffer as IFeature;

			string featureText = feature == null ? "feature buffer" : ToString(feature);

			return featureText;
		}

		/// <summary>
		/// Determines whether the class of a given object is based on a query. 
		/// </summary>
		/// <param name="obj">The object.</param>
		/// <returns>
		/// 	<c>true</c> if the class of the specified object is based on a query; otherwise, <c>false</c>.
		/// </returns>
		/// <remarks>An object class is considered to be query-based if it is based on a name object that
		/// implements <see cref="IQueryName"/> or if the object class itself implements <see cref="IRelQueryTable"/></remarks>
		public static bool IsQueryBasedObject([CanBeNull] IObject obj)
		{
			return obj != null && DatasetUtils.IsQueryBasedClass(obj.Class);
		}

		/// <summary>
		/// Gets the display value for field value of an object
		/// </summary>
		/// <param name="obj">The object</param>
		/// <param name="fieldIndex">Index of the field.</param>
		/// <returns></returns>
		[CanBeNull]
		public static object GetDisplayValue([NotNull] IObject obj, int fieldIndex)
		{
			Assert.ArgumentNotNull(obj, nameof(obj));

			object value = obj.Value[fieldIndex];

			if (value == null || value is DBNull)
			{
				return null;
			}

			var subtypes = obj.Class as ISubtypes;
			object subtypeValue = null;
			if (subtypes != null && subtypes.HasSubtype)
			{
				subtypeValue = obj.Value[subtypes.SubtypeFieldIndex];
			}

			int? subtypeCode = GetNullableSubtypeCode(subtypeValue);

			return DatasetUtils.GetDisplayValue(obj.Class, fieldIndex, value,
			                                    subtypeCode);
		}

		/// <summary>
		/// Gets the display value for original field value of an updated object
		/// </summary>
		/// <param name="obj">The object</param>
		/// <param name="fieldIndex">Index of the field.</param>
		/// <returns></returns>
		public static object GetOriginalDisplayValue([NotNull] IObject obj,
		                                             int fieldIndex)
		{
			Assert.ArgumentNotNull(obj, nameof(obj));

			var rowChanges = (IRowChanges) obj;

			object originalValue = rowChanges.OriginalValue[fieldIndex];

			if (originalValue == null || originalValue is DBNull)
			{
				return null;
			}

			var subtypes = obj.Class as ISubtypes;
			object originalSubtypeValue = null;
			if (subtypes != null && subtypes.HasSubtype)
			{
				originalSubtypeValue =
					rowChanges.OriginalValue[subtypes.SubtypeFieldIndex];
			}

			int? originalSubtypeCode = GetNullableSubtypeCode(originalSubtypeValue);

			return DatasetUtils.GetDisplayValue(obj.Class, fieldIndex, originalValue,
			                                    originalSubtypeCode);
		}

		/// <summary>
		/// Gets the index of the subtype field in the object class of a given object.
		/// </summary>
		/// <param name="obj">The object to get the subtype field for.</param>
		/// <returns>
		/// The index of the subtype field, or -1
		/// if the object class has no subtype field.
		/// </returns>
		public static int GetSubtypeFieldIndex([NotNull] IObject obj)
		{
			Assert.ArgumentNotNull(obj, nameof(obj));

			return DatasetUtils.GetSubtypeFieldIndex(obj.Class);
		}

		public static bool TryGetSubtypeName([NotNull] IObject obj,
		                                     [NotNull] out string subtypeName)
		{
			Assert.ArgumentNotNull(obj, nameof(obj));

			var subtypes = obj.Class as ISubtypes;

			subtypeName = string.Empty;

			if (subtypes == null || ! subtypes.HasSubtype)
			{
				return false;
			}

			int fieldIndex = subtypes.SubtypeFieldIndex;
			if (fieldIndex < 0)
			{
				return false;
			}

			object subtypeValue = obj.Value[fieldIndex];
			if (subtypeValue == null || subtypeValue is DBNull)
			{
				return false;
			}

			subtypeName = DatasetUtils.GetSubtypeName(obj.Class, subtypes, subtypeValue);
			return true;
		}

		/// <summary>
		/// Determines whether the specified row is deleted.
		/// </summary>
		/// <param name="row">The row.</param>
		/// <returns>
		///   <c>true</c> if the specified row is deleted; otherwise, <c>false</c>.
		/// </returns>
		public static bool IsDeleted([NotNull] IRow row)
		{
			Assert.ArgumentNotNull(row, nameof(row));

			// there does not seem to be an official way to check for this
			// Workaround: try to read the object id (by field index!)
			// -> this throws FDO_E_FEATURE_DELETED
			ITable table = row.Table;

			if (! table.HasOID || ! row.HasOID)
			{
				// can't find out, assume not deleted
				return false;
			}

			int oidIndex = table.FindField(table.OIDFieldName);

			try
			{
				// ReSharper disable once UseIndexedProperty
				row.get_Value(oidIndex);
			}
			catch (COMException e)
			{
				switch (e.ErrorCode)
				{
					case (int) fdoError.FDO_E_OBJECT_IS_DELETED:
					case (int) fdoError.FDO_E_FEATURE_DELETED:

						return true;
					default:
						return false;
				}
			}

			return false;
		}

		/// <summary>
		/// Groups the features in an enumeration by their class. 
		/// </summary>
		/// <param name="features">The features.</param>
		/// <returns></returns>
		/// <remarks>Grouping takes place by feature class reference, i.e. features from the same database table 
		/// may be returned in different groups if retrieved via different workspaces. 
		/// </remarks>
		[NotNull]
		public static IDictionary<IFeatureClass, IList<IFeature>> GroupFeaturesByClass(
			[NotNull] IEnumerable<IFeature> features)
		{
			Assert.ArgumentNotNull(features, nameof(features));

			var result = new Dictionary<IFeatureClass, IList<IFeature>>();

			foreach (IFeature feature in features)
			{
				var key = (IFeatureClass) feature.Class;

				IList<IFeature> list;
				if (! result.TryGetValue(key, out list))
				{
					list = new List<IFeature>();
					result.Add(key, list);
				}

				list.Add(feature);
			}

			return result;
		}

		/// <summary>
		/// Groups the objects in an enumeration by their class. 
		/// </summary>
		/// <param name="objects">The objects.</param>
		/// <returns></returns>
		/// <remarks>Grouping takes place by object class reference, i.e. objects from the same database table 
		/// may be returned in different groups if retrieved via different workspaces. 
		/// </remarks>
		[NotNull]
		public static IDictionary<IObjectClass, IList<IObject>> GroupObjectsByClass<T>(
			[NotNull] IEnumerable<T> objects) where T : IObject
		{
			Assert.ArgumentNotNull(objects, nameof(objects));

			var result = new Dictionary<IObjectClass, IList<IObject>>();

			foreach (T obj in objects)
			{
				IObjectClass key = obj.Class;

				IList<IObject> list;
				if (! result.TryGetValue(key, out list))
				{
					list = new List<IObject>();
					result.Add(key, list);
				}

				list.Add(obj);
			}

			return result;
		}

		[NotNull]
		public static IDictionary<string, IList<T>>
			GroupObjectsByClassName<T>([NotNull] IEnumerable<T> objects) where T : IObject
		{
			Assert.ArgumentNotNull(objects, nameof(objects));

			var result = new Dictionary<string, IList<T>>(
				StringComparer.OrdinalIgnoreCase);

			foreach (T obj in objects)
			{
				string key = ((IDataset) obj.Table).Name;

				IList<T> list;
				if (! result.TryGetValue(key, out list))
				{
					list = new List<T>();
					result.Add(key, list);
				}

				list.Add(obj);
			}

			return result;
		}

		public static IEnumerable<List<T>> GroupRowsByAttributes<T>(
			IEnumerable<T> valuesToGroup,
			Func<T, IReadOnlyRow> getRow,
			IList<string> groupByFields)
		{
			if (! (groupByFields?.Count > 0))
			{
				yield return valuesToGroup.ToList();
				yield break;
			}

			Dictionary<string, int> fieldDict = null;

			Dictionary<List<object>, List<T>> groupDict =
				new Dictionary<List<object>, List<T>>(new ListComparer());
			foreach (T valueToGroup in valuesToGroup)
			{
				IReadOnlyRow row = getRow(valueToGroup);

				if (fieldDict == null)
				{
					fieldDict = new Dictionary<string, int>(groupByFields.Count);
					IFields f = row.Table.Fields;

					foreach (string groupBy in groupByFields)
					{
						int idx = f.FindField(groupBy);
						Assert.True(idx >= 0, $"Unknonw field '{groupBy}'");
						fieldDict.Add(groupBy, idx);
					}
				}

				List<object> key = new List<object>(groupByFields.Count);

				foreach (int idx in fieldDict.Values)
				{
					key.Add(row.get_Value(idx));
				}

				if (! groupDict.TryGetValue(key, out List<T> group))
				{
					group = new List<T>();
					groupDict.Add(key, group);
				}

				group.Add(valueToGroup);
			}

			foreach (KeyValuePair<List<object>, List<T>> pair in groupDict)
			{
				yield return pair.Value;
			}
		}

		/// <summary>
		/// Returns the list of distinct features from the provided features.
		/// </summary>
		/// <param name="features"></param>
		/// <param name="classEquality"></param>
		/// <returns></returns>
		[NotNull]
		public static IList<IFeature> GetDistinctFeatures(
			[NotNull] IEnumerable<IFeature> features,
			ObjectClassEquality classEquality)
		{
			IList<IObject> distinctObjects =
				GetDistinctObjects(features.Cast<IObject>().ToList(), classEquality);

			return distinctObjects.Cast<IFeature>().ToList();
		}

		[NotNull]
		public static IList<IObject> GetDistinctObjects([NotNull] IList<IObject> objects,
		                                                ObjectClassEquality classEquality)
		{
			Assert.ArgumentNotNull(objects, nameof(objects));

			var result = new List<IObject>(objects.Count);

			foreach (IObject obj in objects)
			{
				if (result.Contains(obj))
				{
					continue;
				}

				if (classEquality == ObjectClassEquality.SameInstance ||
				    classEquality == ObjectClassEquality.SameTableSameVersion)
				{
					// Assuming single-instance semantics
					result.Add(obj);
				}
				else
				{
					// Check if the object is in the list (as a different instance)
					bool found = result.Any(resultObj =>
						                        IsSameObject(
							                        obj, resultObj, classEquality));

					if (! found)
					{
						result.Add(obj);
					}
				}
			}

			return result;
		}

		/// <summary>
		/// Determines whether the specified gdb object is deleted.
		/// </summary>
		/// <param name="obj">The gdb object.</param>
		/// <returns>
		///   <c>true</c> if the specified object is deleted; otherwise, <c>false</c>.
		/// </returns>
		public static bool IsDeleted([NotNull] IObject obj)
		{
			return IsDeleted((IRow) obj);
		}

		/// <summary>
		/// Determines whether two gdb objects are the same or not.
		/// </summary>
		/// <param name="obj1"></param>
		/// <param name="obj2"></param>
		/// <param name="classEquality">Equality definition for the objects' dataset comparison.</param>
		/// <returns>
		///   <c>true</c> if the specified objects are the same; otherwise, <c>false</c>.
		/// </returns>
		public static bool IsSameObject([NotNull] IObject obj1, [NotNull] IObject obj2,
		                                ObjectClassEquality classEquality)
		{
			// Test for reference-equals in real ArcObjects IObject instances but also allow
			// synthetic and mock features to provide their own equality implementation:
			if (obj1.Equals(obj2))
			{
				return true;
			}

			// For real geodatabase objects:
			if (obj1.HasOID && obj2.HasOID)
			{
				return obj1.OID == obj2.OID &&
				       DatasetUtils.IsSameObjectClass(obj1.Class, obj2.Class,
				                                      classEquality);
			}

			return false;
		}

		/// <summary>
		/// Gets the subtype code of the specified object or null if it is not defined or its feature class has no subtypes.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		[CanBeNull]
		public static int? GetSubtypeCode([NotNull] IObject obj)
		{
			Assert.ArgumentNotNull(obj, nameof(obj));

			if (! DatasetUtils.HasSubtypes(obj.Class))
			{
				return null;
			}

			var subtypes = (ISubtypes) obj.Class;

			return GetSubtypeCode(obj, subtypes.SubtypeFieldIndex);
		}

		public static int? GetSubtypeCode([NotNull] IRow row, int subtypeFieldIndex)
		{
			if (subtypeFieldIndex < 0)
			{
				return null;
			}

			object code = row.Value[subtypeFieldIndex];

			// NOTE: no direct cast to int - the field could be a short
			return code == null || code == DBNull.Value
				       ? (int?) null
				       : Convert.ToInt32(code);
		}

		/// <summary>
		/// Projects the provided features' shapes into the specified spatial reference
		/// if necessary.
		/// </summary>
		/// <param name="features"></param>
		/// <param name="spatialReference"></param>
		/// <param name="notifications"></param>
		/// <remarks>If the spatial reference is not as expected, the input geometry is
		/// modified by reprojection to the expected spatial reference.</remarks>
		/// <returns>true, if a project was needed, otherwise false</returns>
		public static bool EnsureSpatialReference(
			[NotNull] IEnumerable<IFeature> features,
			[NotNull] ISpatialReference spatialReference,
			[CanBeNull] NotificationCollection notifications = null)
		{
			Assert.ArgumentNotNull(features, nameof(features));
			Assert.ArgumentNotNull(spatialReference, nameof(spatialReference));

			var projected = false;

			foreach (IFeature feature in features)
			{
				projected =
					EnsureSpatialReference(feature, spatialReference, notifications);
			}

			return projected;
		}

		/// <summary>
		/// Projects the provided feature's shape into the specified spatial reference
		/// if necessary.
		/// </summary>
		/// <param name="feature"></param>
		/// <param name="spatialReference"></param>
		/// <param name="notifications"></param>
		/// <remarks>If the spatial reference is not as expected, the input geometry is
		/// modified by reprojection to the expected spatial reference.</remarks>
		/// <returns>true, if a project was needed, otherwise false</returns>
		public static bool EnsureSpatialReference(
			[NotNull] IFeature feature,
			[NotNull] ISpatialReference spatialReference,
			[CanBeNull] NotificationCollection notifications = null)
		{
			var projected = false;

			IGeometry shape = feature.Shape;

			if (GeometryUtils.EnsureSpatialReference(shape, spatialReference))
			{
				projected = true;
				NotificationUtils.Add(notifications,
				                      "The feature {0} was projected into {1}.",
				                      ToString(feature), spatialReference.Name);
			}

			Marshal.ReleaseComObject(shape);

			return projected;
		}

		#region Non-public methods

		private static bool CanCopyFieldValue([NotNull] IRow sourceRow,
		                                      int sourceFieldIndex,
		                                      [NotNull] IRow targetRow,
		                                      int targetFieldIndex,
		                                      [CanBeNull] NotificationCollection
			                                      notifications)
		{
			IField targetField = targetRow.Fields.Field[targetFieldIndex];

			if (! targetField.Editable)
			{
				NotificationUtils.Add(notifications, "Target field {0} is not editable",
				                      targetField.Name);
				return false;
			}

			if (! targetField.IsNullable &&
			    Convert.IsDBNull(sourceRow.Value[sourceFieldIndex]))
			{
				NotificationUtils.Add(notifications, "Target field {0} is not nullable",
				                      targetField.Name);
				return false;
			}

			return true;
		}

		private static bool TryCopyFieldValue([NotNull] IRow sourceRow,
		                                      int sourceFieldIndex,
		                                      [NotNull] IRow targetRow,
		                                      int targetFieldIndex,
		                                      [CanBeNull] NotificationCollection
			                                      notifications)
		{
			if (! CanCopyFieldValue(sourceRow, sourceFieldIndex, targetRow,
			                        targetFieldIndex, notifications))
			{
				return false;
			}

			CopyFieldValue(sourceRow, sourceFieldIndex, targetRow, targetFieldIndex);

			return true;
		}

		private static void CopyFieldValue([NotNull] IRow sourceRow, int sourceFieldIndex,
		                                   [NotNull] IRowBuffer targetRow,
		                                   int targetFieldIndex)
		{
			Assert.ArgumentNotNull(sourceRow, nameof(sourceRow));
			Assert.True(sourceFieldIndex >= 0, "source field index must be >= 0");
			Assert.ArgumentNotNull(targetRow, nameof(targetRow));
			Assert.True(targetFieldIndex >= 0, "target field index must be >= 0");

			IField sourceField = sourceRow.Fields.Field[sourceFieldIndex];

			if (sourceField.Type == esriFieldType.esriFieldTypeGeometry)
			{
				IField targetField = targetRow.Fields.Field[targetFieldIndex];

				Assert.AreEqual(esriFieldType.esriFieldTypeGeometry, targetField.Type,
				                "Unexpected target field type for source geometry field");

				var sourceFeature = (IFeature) sourceRow;
				var targetFeature = (IFeature) targetRow;

				SetFeatureShape(targetFeature, sourceFeature.ShapeCopy);
			}
			else
			{
				object value = sourceRow.Value[sourceFieldIndex] ?? DBNull.Value;

				targetRow.Value[targetFieldIndex] = value;
			}
		}

		private static int? GetNullableSubtypeCode([CanBeNull] object subtypeFieldValue)
		{
			if (subtypeFieldValue == null || subtypeFieldValue is DBNull)
			{
				return null;
			}

			return subtypeFieldValue as int? ?? Convert.ToInt32(subtypeFieldValue);
		}

		private static bool IsEditableAttribute([NotNull] IField field)
		{
			return field.Editable && field.Type != esriFieldType.esriFieldTypeGeometry;
		}

		///// <summary>
		///// Applies the split policy for the specified field to the new feature
		///// </summary>
		///// <param name="newFeature"></param>
		///// <param name="baseFeature"></param>
		///// <param name="fieldIndex"></param>
		///// <param name="field"></param>
		///// <param name="domain"></param>
		//private static void ApplySplitPolicy([NotNull] IFeature newFeature,
		//									 [NotNull] IFeature baseFeature,
		//									 int fieldIndex,
		//									 [NotNull] IField field,
		//									 [CanBeNull] IDomain domain)
		//{
		//	const bool alsoToBaseFeature = false;
		//	ApplySplitPolicy(newFeature, baseFeature, fieldIndex, field, domain,
		//					 alsoToBaseFeature);
		//}

		/// <summary>
		/// Applies the split policy for the specified field to the new feature
		/// </summary>
		/// <param name="newFeature"></param>
		/// <param name="baseFeature"></param>
		/// <param name="fieldIndex"></param>
		/// <param name="field"></param>
		/// <param name="domain"></param>
		/// <param name="alsoToBaseFeature">Whether or not the split policy 'Default' should
		/// also be applied to the base feature as it is the case in the ArcMap standard tools.</param>
		private static void ApplySplitPolicy([NotNull] IFeature newFeature,
		                                     [NotNull] IFeature baseFeature,
		                                     int fieldIndex,
		                                     [NotNull] IField field,
		                                     [CanBeNull] IDomain domain,
		                                     bool alsoToBaseFeature)
		{
			esriSplitPolicyType splitPolicy = domain?.SplitPolicy ??
			                                  esriSplitPolicyType.esriSPTDuplicate;

			// TODO support esriSPTGeometryRatio explicitly. 
			// Problem: this also needs to be applied to the updated base feature - only ONCE
			switch (splitPolicy)
			{
				case esriSplitPolicyType.esriSPTGeometryRatio:
				case esriSplitPolicyType.esriSPTDuplicate:
					newFeature.Value[fieldIndex] = baseFeature.Value[fieldIndex];
					break;

				case esriSplitPolicyType.esriSPTDefaultValue:
					newFeature.Value[fieldIndex] = field.DefaultValue;

					if (alsoToBaseFeature)
					{
						baseFeature.Value[fieldIndex] = field.DefaultValue;
					}

					break;

				default:
					throw new ArgumentOutOfRangeException();
			}
		}

		#endregion

		private class ListComparer : IComparer<List<object>>, IEqualityComparer<List<object>>
		{
			public int Compare(List<object> x, List<object> y)
			{
				if (x == y) return 0;
				if (x == null) return -1;
				if (y == null) return +1;

				int nx = x.Count;
				int ny = y.Count;
				int d = nx.CompareTo(ny);
				if (d != 0)
				{
					return d;
				}

				for (int i = 0; i < nx; i++)
				{
					d = Comparer.Default.Compare(x[i], y[i]);
					if (d != 0)
					{
						return d;
					}
				}

				return 0;
			}

			public bool Equals(List<object> x, List<object> y)
			{
				return Compare(x, y) == 0;
			}

			public int GetHashCode(List<object> x)
			{
				int hashCode = 1;
				foreach (object o in x)
				{
					hashCode = 29 * hashCode + (o?.GetHashCode() ?? 0);
				}

				return hashCode;
			}
		}
	}
}

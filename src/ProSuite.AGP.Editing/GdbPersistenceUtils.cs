using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Editing;
using ArcGIS.Desktop.Editing.Templates;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Core.GeometryProcessing;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using Attribute = ArcGIS.Desktop.Editing.Attributes.Attribute;

namespace ProSuite.AGP.Editing
{
	public static class GdbPersistenceUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public static async Task<bool> SaveInOperationAsync(
			[NotNull] string description,
			[CanBeNull] IDictionary<Feature, Geometry> updates,
			[CanBeNull] IDictionary<Feature, IList<Geometry>> copies = null)
		{
			Assert.ArgumentCondition(updates?.Count > 0 || copies?.Count > 0,
			                         "Neither updates nor inserts have been provided.");

			return await ExecuteInTransactionAsync(
				       editContext => StoreTx(editContext, updates, copies),
				       description, GetDatasetsNonEmpty(updates?.Keys, copies?.Keys));
		}

		/// <summary>
		/// BUG: GOTOP-186: Do not use this method for the time being (3.2.2). It will result in
		/// ghost features, a corrupt display system and edit session.
		/// </summary>
		/// <param name="description"></param>
		/// <param name="updates"></param>
		/// <param name="copies"></param>
		/// <returns></returns>
		public static bool SaveInOperation(
			[NotNull] string description,
			[CanBeNull] IDictionary<Feature, Geometry> updates,
			[CanBeNull] IDictionary<Feature, IList<Geometry>> copies = null)
		{
			return ExecuteInTransaction(
				editContext => StoreTx(editContext, updates, copies), description,
				GetDatasetsNonEmpty(updates?.Keys, copies?.Keys));
		}

		/// <summary>
		/// BUG: GOTOP-186: Do not use this method for the time being (3.2.2). It will result in
		/// ghost features, a corrupt display system and edit session.
		/// </summary>
		/// <param name="function"></param>
		/// <param name="description"></param>
		/// <param name="datasets"></param>
		/// <returns></returns>
		public static bool ExecuteInTransaction(
			Func<EditOperation.IEditContext, bool> function,
			[NotNull] string description,
			IEnumerable<Dataset> datasets)
		{
			var editOperation = new EditOperation();

			EditorTransaction transaction = new EditorTransaction(editOperation);

			bool result = false;
			bool executeResult = transaction.Execute(
				editContext => result = function(editContext),
				description, datasets);

			return result && executeResult;
		}

		public static async Task<bool> ExecuteInTransactionAsync(
			Func<EditOperation.IEditContext, bool> function,
			[NotNull] string description,
			IEnumerable<Dataset> datasets)
		{
			var editOperation = new EditOperation();

			EditorTransaction transaction = new EditorTransaction(editOperation);

			bool result = false;
			bool executeResult = await transaction.ExecuteAsync(
				                     editContext => result = function(editContext),
				                     description, datasets);

			return result && executeResult;
		}

		public static bool StoreTx(
			EditOperation.IEditContext editContext,
			[CanBeNull] IDictionary<Feature, Geometry> updates,
			[CanBeNull] IDictionary<Feature, IList<Geometry>> copies = null)
		{
			_msg.DebugFormat("Saving {0} updates and {1} inserts...", updates?.Count ?? 0,
			                 copies?.Count ?? 0);

			UpdateTx(editContext, updates);

			if (copies != null && copies.Count > 0)
			{
				foreach (Feature feature in InsertTx(editContext, copies))
				{
					_msg.DebugFormat("Stored {0}", GdbObjectUtils.ToString(feature));
				}
			}

			return true;
		}

		public static Feature InsertTx([NotNull] EditOperation.IEditContext editContext,
		                               [NotNull] Feature originalFeature,
		                               [NotNull] Geometry newGeometry,
		                               [CanBeNull] ICollection<string> excludeFields = null)
		{
			FeatureClass featureClass = originalFeature.GetTable();

			// Un-join to prevent NotImplementedException:
			FeatureClass targetTable = DatasetUtils.GetDatabaseFeatureClass(featureClass);

			RowBuffer rowBuffer = CopyRow(originalFeature, targetTable, excludeFields);

			using var classDefinition = featureClass.GetDefinition();
			bool classHasZ = classDefinition.HasZ();
			bool classHasM = classDefinition.HasM();

			Geometry geometryToStore =
				GeometryUtils.EnsureGeometrySchema(newGeometry, classHasZ, classHasM);

			Geometry projected = GeometryUtils.EnsureSpatialReference(
				geometryToStore, featureClass.GetSpatialReference());

			SetShape(rowBuffer, projected, featureClass);

			Feature newFeature = targetTable.CreateRow(rowBuffer);

			StoreShape(newFeature, projected, editContext);

			return newFeature;
		}

		public static IEnumerable<Feature> InsertTx(
			[NotNull] EditOperation.IEditContext editContext,
			[NotNull] IDictionary<Feature, IList<Geometry>> copies,
			[CanBeNull] ICollection<string> excludeFields = null,
			[CanBeNull] CancelableProgressor cancelableProgressor = null)
		{
			int insertCount = 0;

			foreach (KeyValuePair<Feature, IList<Geometry>> keyValuePair in copies)
			{
				Feature originalFeature = keyValuePair.Key;
				IList<Geometry> newGeometries = keyValuePair.Value;

				foreach (Geometry newGeometry in newGeometries)
				{
					if (cancelableProgressor is { CancellationToken.IsCancellationRequested: true })
					{
						yield break;
					}

					yield return InsertTx(editContext, originalFeature, newGeometry, excludeFields);
					insertCount++;
				}
			}

			_msg.InfoFormat("Successfully created {0} new feature(s).", insertCount);
		}

		public static IList<Feature> InsertTx(
			[NotNull] EditOperation.IEditContext editContext,
			[NotNull] FeatureClass targetFeatureClass,
			[CanBeNull] Subtype targetSubtype,
			[NotNull] IEnumerable<Geometry> geometries,
			[CanBeNull] Func<Field, FeatureClassDefinition, Subtype, object> getAttributeValueFunc,
			[CanBeNull] CancelableProgressor cancelableProgressor = null)
		{
			Assert.ArgumentNotNull(editContext, nameof(editContext));
			Assert.ArgumentNotNull(targetFeatureClass, nameof(targetFeatureClass));

			var newFeatures = new List<Feature>();

			RowBuffer rowBuffer = null;

			try
			{
				// Set the attributes
				rowBuffer = targetFeatureClass.CreateRowBuffer();

				using var classDefinition = targetFeatureClass.GetDefinition();
				GeometryType geometryType = classDefinition.GetShapeType();
				bool classHasZ = classDefinition.HasZ();
				bool classHasM = classDefinition.HasM();

				SpatialReference spatialReference = classDefinition.GetSpatialReference();

				foreach (Field field in rowBuffer.GetFields())
				{
					if (! field.IsEditable || field.FieldType == FieldType.Geometry)
					{
						continue;
					}

					rowBuffer[field.Name] =
						getAttributeValueFunc?.Invoke(field, classDefinition, targetSubtype) ??
						DBNull.Value;
				}

				string shapeFieldName = classDefinition.GetShapeField();

				int geometryCount = 0;
				foreach (Geometry geometry in geometries)
				{
					if (cancelableProgressor != null &&
					    cancelableProgressor.CancellationToken.IsCancellationRequested)
					{
						return newFeatures;
					}

					geometryCount++;
					Assert.AreEqual(geometryType, geometry.GeometryType,
					                "Geometry type does not match target feature class' shape type.");

					Geometry geometryToStore =
						GeometryUtils.EnsureGeometrySchema(geometry, classHasZ, classHasM);

					geometryToStore = GeometryUtils.EnsureSpatialReference(
						geometryToStore, spatialReference);

					rowBuffer[shapeFieldName] = geometryToStore;

					var feature = targetFeatureClass.CreateRow(rowBuffer);

					feature.Store();

					// Invalidate display / attribute table
					editContext.Invalidate(feature);

					newFeatures.Add(feature);
				}

				Assert.True(geometryCount > 0, "List of geometries is empty");
			}
			finally
			{
				rowBuffer?.Dispose();
			}

			return newFeatures;
		}

		public static IList<Feature> InsertTx(
			[NotNull] EditOperation.IEditContext editContext,
			[NotNull] FeatureClass targetFeatureClass,
			[NotNull] IList<Geometry> geometries,
			[CanBeNull] IEnumerable<Attribute> attributes)
		{
			Assert.ArgumentNotNull(editContext, nameof(editContext));
			Assert.ArgumentNotNull(targetFeatureClass, nameof(targetFeatureClass));
			Assert.ArgumentCondition(geometries.Count > 0, "List of geometries is empty.");

			var newFeatures = new List<Feature>();

			RowBuffer rowBuffer = null;

			try
			{
				// Set the attributes
				rowBuffer = targetFeatureClass.CreateRowBuffer();

				using var classDefinition = targetFeatureClass.GetDefinition();
				GeometryType geometryType = classDefinition.GetShapeType();
				bool classHasZ = classDefinition.HasZ();
				bool classHasM = classDefinition.HasM();

				SpatialReference spatialReference = classDefinition.GetSpatialReference();

				if (attributes != null)
				{
					CopyAttributeValues(attributes, rowBuffer);
				}

				string shapeFieldName = classDefinition.GetShapeField();

				foreach (Geometry geometry in geometries)
				{
					Assert.AreEqual(geometryType, geometry.GeometryType,
					                "Geometry type does not match target feature class' shape type.");

					Geometry geometryToStore =
						GeometryUtils.EnsureGeometrySchema(geometry, classHasZ, classHasM);

					geometryToStore = GeometryUtils.EnsureSpatialReference(
						geometryToStore, spatialReference);

					rowBuffer[shapeFieldName] = geometryToStore;

					var feature = targetFeatureClass.CreateRow(rowBuffer);

					feature.Store();

					//To Indicate that the attribute table has to be updated
					editContext.Invalidate(feature);

					newFeatures.Add(feature);
				}
			}
			finally
			{
				rowBuffer?.Dispose();
			}

			return newFeatures;
		}

		public static void CopyAttributeValues([NotNull] IEnumerable<Attribute> attributes,
		                                       [NotNull] RowBuffer rowBuffer)
		{
			IReadOnlyList<Field> fields = rowBuffer.GetFields();

			foreach (Attribute attribute in attributes)
			{
				if (attribute.CurrentValue == null || attribute.CurrentValue == DBNull.Value)
				{
					continue;
				}

				int fieldIndex = attribute.Index;

				if (IsEditable(attribute) && ! attribute.IsGeometryField)
				{
					if (attribute.Index >= fields.Count ||
					    fields[attribute.Index].Name != attribute.FieldName)
					{
						// Issue #165: Some fields (presumably the SHAPE_LEN or SHAPE_AREA field) do not
						// exist in the rowBuffer's field list. This happens rarely, with specific layers.
						// Consider using copy index matrix in these cases.
						fieldIndex = rowBuffer.FindField(attribute.FieldName);
					}

					rowBuffer[fieldIndex] = attribute.CurrentValue;
				}
			}
		}

		public static void UpdateTx(EditOperation.IEditContext editContext,
		                            IDictionary<Feature, Geometry> updates)
		{
			if (updates != null && updates.Count > 0)
			{
				foreach (KeyValuePair<Feature, Geometry> keyValuePair in updates)
				{
					StoreShape(keyValuePair, editContext);
				}

				_msg.InfoFormat("Successfully updated {0} feature(s).", updates.Count);
			}
		}

		public static IEnumerable<Feature> InsertTx(
			[NotNull] EditOperation.IEditContext editContext,
			[NotNull] IEnumerable<ResultFeature> insertResults)
		{
			foreach (ResultFeature insert in insertResults)
			{
				Feature originalFeature = insert.OriginalFeature;

				Feature newFeature = InsertTx(editContext, originalFeature, insert.NewGeometry);

				yield return newFeature;

				insert.SetNewOid(newFeature.GetObjectID());
			}
		}

		public static bool CanChange([NotNull] ResultFeature resultFeature,
		                             [NotNull] HashSet<long> editableClassHandles,
		                             params RowChangeType[] allowedChangeType)
		{
			if (allowedChangeType.All(t => t != resultFeature.ChangeType))
			{
				return false;
			}

			Feature feature = resultFeature.OriginalFeature;

			bool result = CanChange(feature, editableClassHandles, out string warning);

			if (! string.IsNullOrEmpty(warning))
			{
				resultFeature.Messages.Add(warning);
				resultFeature.HasWarningMessage = true;
			}

			return result;
		}

		public static bool CanChange([NotNull] Feature feature,
		                             [NotNull] HashSet<long> editableClassHandles,
		                             out string warnings)
		{
			warnings = null;

			using var featureClass = feature.GetTable();

			if (featureClass == null)
			{
				return false;
			}

			long handle = featureClass.Handle.ToInt64();

			if (! editableClassHandles.Contains(handle))
			{
				warnings = "Not updated because the layer is not editable";

				_msg.DebugFormat("Updated feature {0} is not editable!",
				                 GdbObjectUtils.ToString(feature));
				return false;
			}

			return true;
		}

		public static bool IsEditable([NotNull] Attribute attribute)
		{
			if (! attribute.IsEditable)
			{
				return false;
			}

			if (attribute.IsSystemField)
			{
				return false;
			}

			// Bug in Oracle: IsSystemField returns false for Shape fields!
			if (attribute.FieldName == "SHAPE.AREA" ||
			    attribute.FieldName == "SHAPE.LEN")
			{
				return false;
			}

			return true;
		}

		/// <summary>
		/// Returns true if the specified template has a default value for the
		/// specified field name other than null or DB-Null.
		/// </summary>
		/// <param name="fieldName"></param>
		/// <param name="template"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public static bool TryGetFieldValueFromTemplate([NotNull] string fieldName,
		                                                [CanBeNull] EditingTemplate template,
		                                                out object value)
		{
			value = null;
			if (template == null)
			{
				return false;
			}

			if (! template.Inspector.HasAttributes)
			{
				return false;
			}

			Attribute attribute = template.Inspector.FirstOrDefault(
				a => a.FieldName.Equals(fieldName, StringComparison.InvariantCultureIgnoreCase));

			if (attribute == null)
			{
				return false;
			}

			value = attribute.CurrentValue;

			//wenn guid leer ist (alles 0) dann ist Attribute.CurrentValue eine Guid und kein String
			//wenn guid nicht leer ist dann ist Attribute.CurrentValue ein String
			if (value is System.Guid guid)
			{
				value = guid.ToString("B").ToUpper();
			}

			return value != null && value != DBNull.Value;
		}

		public static void SetShape([NotNull] RowBuffer rowBuffer,
		                            [NotNull] Geometry geometry,
		                            FeatureClass featureClass)
		{
			using var classDefinition = featureClass.GetDefinition();
			string shapeFieldName = classDefinition.GetShapeField();

			SetShape(rowBuffer, geometry, shapeFieldName);
		}

		private static void SetShape([NotNull] RowBuffer rowBuffer,
		                             [NotNull] Geometry geometry,
		                             string shapeFieldName)
		{
			try
			{
				rowBuffer[shapeFieldName] = geometry;
			}
			catch (Exception e)
			{
				_msg.VerboseDebug(() => $"Error persisting shape {geometry.ToXml()}");
				throw new DataException($"Error setting shape of new feature: {e.Message}", e);
			}
		}

		private static RowBuffer CopyRow([NotNull] Row sourceRow,
		                                 [NotNull] Table targetTable,
		                                 [CanBeNull] ICollection<string> excludeFields = null,
		                                 bool includeShape = false)
		{
			Table sourceTable = sourceRow.GetTable();

			RowBuffer rowBuffer = sourceTable.CreateRowBuffer();

			TableDefinition sourceTableDefinition = sourceTable.GetDefinition();

			const bool includeReadOnlyFields = false;
			bool searchJoinedFields = sourceTable.IsJoinedTable();

			Predicate<Field> includeTargetField = f => excludeFields?.Contains(f.Name) != true;

			IDictionary<int, int> copyIndexMatrix =
				GdbObjectUtils.CreateMatchingIndexMatrix(
					sourceTableDefinition, targetTable.GetDefinition(),
					includeReadOnlyFields, searchJoinedFields,
					includeTargetField);

			CopyValues(sourceRow, rowBuffer, copyIndexMatrix, includeShape);

			return rowBuffer;
		}

		private static void CopyValues([NotNull] Row fromRow,
		                               [NotNull] RowBuffer toRowBuffer,
		                               [NotNull] IDictionary<int, int> copyIndexMatrix,
		                               bool includeShape = false)
		{
			IReadOnlyList<Field> sourceFields = includeShape ? null : fromRow.GetFields();

			foreach (int targetIndex in copyIndexMatrix.Keys)
			{
				int sourceIndex = copyIndexMatrix[targetIndex];

				if (sourceIndex < 0)
				{
					continue;
				}

				if (sourceFields?[sourceIndex].FieldType == FieldType.Geometry)
				{
					continue;
				}

				toRowBuffer[targetIndex] = fromRow[sourceIndex];
			}
		}

		private static void StoreShape(KeyValuePair<Feature, Geometry> keyValuePair,
		                               EditOperation.IEditContext editContext)
		{
			Feature feature = keyValuePair.Key;
			Geometry geometry = keyValuePair.Value;

			StoreShape(feature, geometry, editContext);
		}

		public static void StoreShape(Feature feature,
		                              Geometry geometry,
		                              EditOperation.IEditContext editContext)
		{
			_msg.DebugFormat("Updating shape of {0}...", GdbObjectUtils.ToString(feature));

			if (geometry == null || geometry.IsEmpty)
			{
				throw new Exception("One or more updates geometries have become empty.");
			}

			try
			{
				// NOTE: Even if the geometry is not projected before setting the shape,
				// the result is the same. After feature.SetShape() the (new) instance of
				// feature.GetShape() is in the feature class' spatial reference.
				Geometry projected = GeometryUtils.EnsureSpatialReference(
					geometry, feature.GetTable().GetSpatialReference());

				feature.SetShape(projected);
				feature.Store();
			}
			catch (Exception e)
			{
				_msg.VerboseDebug(() => $"Error persisting shape {geometry.ToXml()}");
				throw new DataException($"Error updating shape of feature " +
				                        $"{GdbObjectUtils.ToString(feature)}: {e.Message}", e);
			}
			finally
			{
				editContext.Invalidate(feature);
			}
		}

		public static IEnumerable<Dataset> GetDatasetsNonEmpty(
			params IEnumerable<Feature>[] featureLists)
		{
			int datasetCount = 0;

			foreach (Dataset dataset in GetDatasets(featureLists))
			{
				yield return dataset;
				datasetCount++;
			}

			// NOTE: This happens in case of DPS/#19
			Assert.False(datasetCount == 0,
			             "No dataset could be retrieved from the feature list(s).");
		}

		public static IEnumerable<Dataset> GetDatasets(params IEnumerable<Feature>[] featureLists)
		{
			foreach (IEnumerable<Feature> collection in featureLists)
			{
				if (collection == null)
				{
					continue;
				}

				foreach (Feature feature in collection)
				{
					yield return feature.GetTable();
				}
			}
		}
	}
}

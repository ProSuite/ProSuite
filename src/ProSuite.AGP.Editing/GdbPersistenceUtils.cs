using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Editing;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Microservices.Client.AGP.GeometryProcessing;

namespace ProSuite.AGP.Editing
{
	public static class GdbPersistenceUtils
	{
		private static readonly IMsg _msg = new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		public static async Task<bool> SaveInOperationAsync(
			[NotNull] string description,
			[CanBeNull] IDictionary<Feature, Geometry> updates,
			[CanBeNull] IDictionary<Feature, IList<Geometry>> copies = null)
		{
			var editOperation = new EditOperation();

			EditorTransaction transaction = new EditorTransaction(editOperation);

			return await transaction.ExecuteAsync(
				       editContext => StoreTx(editContext, updates, copies),
				       description, GetDatasets(updates?.Keys, copies?.Keys));
		}

		public static bool SaveInOperation(
			[NotNull] string description,
			[CanBeNull] IDictionary<Feature, Geometry> updates,
			[CanBeNull] IDictionary<Feature, IList<Geometry>> copies = null)
		{
			var editOperation = new EditOperation();

			EditorTransaction transaction = new EditorTransaction(editOperation);

			return transaction.Execute(
				editContext => StoreTx(editContext, updates, copies),
				description, GetDatasets(updates?.Keys, copies?.Keys));
		}

		public static bool StoreTx(
			EditOperation.IEditContext editContext,
			[CanBeNull] IDictionary<Feature, Geometry> updates,
			[CanBeNull] IDictionary<Feature, IList<Geometry>> copies = null)
		{
			_msg.DebugFormat("Saving {0} updates and {1} inserts...", updates?.Count ?? 0,
			                 copies?.Count ?? 0);

			if (updates != null && updates.Count > 0)
			{
				foreach (KeyValuePair<Feature, Geometry> keyValuePair in updates)
				{
					StoreShape(keyValuePair, editContext);
				}

				_msg.InfoFormat("Successfully updated {0} feature(s).", updates.Count);
			}

			if (copies != null && copies.Count > 0)
			{
				foreach (KeyValuePair<Feature, IList<Geometry>> keyValuePair in copies)
				{
					Feature originalFeature = keyValuePair.Key;
					IList<Geometry> newGeometries = keyValuePair.Value;

					FeatureClass featureClass = originalFeature.GetTable();

					foreach (Geometry newGeometry in newGeometries)
					{
						RowBuffer rowBuffer = DuplicateRow(originalFeature);

						SetShape(rowBuffer, newGeometry, featureClass);

						Feature newFeature = featureClass.CreateRow(rowBuffer);

						StoreShape(newFeature, newGeometry, editContext);
					}
				}

				_msg.InfoFormat("Successfully created {0} new feature(s).", copies.Count);
			}

			return true;
		}

		public static bool CanChange([NotNull] ResultFeature resultFeature,
		                             [NotNull] HashSet<long> editableClassHandles,
		                             params RowChangeType[] allowedChangeType)
		{
			if (allowedChangeType.All(t => t != resultFeature.ChangeType))
			{
				return false;
			}

			Feature feature = resultFeature.Feature;

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

			FeatureClass featureClass = feature.GetTable();

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

		private static void SetShape([NotNull] RowBuffer rowBuffer,
		                             [NotNull] Geometry geometry,
		                             FeatureClass featureClass)
		{
			string shapeFieldName = featureClass.GetDefinition().GetShapeField();

			SetShape(rowBuffer, geometry, shapeFieldName);
		}

		private static void SetShape([NotNull] RowBuffer rowBuffer,
		                             [NotNull] Geometry geometry,
		                             string shapeFieldName)
		{
			rowBuffer[shapeFieldName] = geometry;
		}

		private static RowBuffer DuplicateRow(Row row, bool includeShape = false)
		{
			RowBuffer rowBuffer = row.GetTable().CreateRowBuffer();

			CopyValues(row, rowBuffer, includeShape);

			return rowBuffer;
		}

		private static void CopyValues(Row fromRow, RowBuffer toRowBuffer,
		                               bool includeShape = false)
		{
			IReadOnlyList<Field> fields = fromRow.GetFields();

			for (int i = 0; i < fields.Count; i++)
			{
				Field field = fields[i];

				if (! field.IsEditable)
				{
					continue;
				}

				if (field.FieldType == FieldType.Geometry && ! includeShape)
				{
					continue;
				}

				toRowBuffer[i] = fromRow[i];
			}
		}

		private static void StoreShape(KeyValuePair<Feature, Geometry> keyValuePair,
		                               EditOperation.IEditContext editContext)
		{
			Feature feature = keyValuePair.Key;
			Geometry geometry = keyValuePair.Value;

			StoreShape(feature, geometry, editContext);
		}

		private static void StoreShape(Feature feature,
		                               Geometry geometry,
		                               EditOperation.IEditContext editContext)
		{
			if (geometry.IsEmpty)
			{
				throw new Exception("One or more updates geometries have become empty.");
			}

			feature.SetShape(geometry);
			feature.Store();

			editContext.Invalidate(feature);
		}

		private static IEnumerable<Dataset> GetDatasets(params IEnumerable<Feature>[] featureLists)
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

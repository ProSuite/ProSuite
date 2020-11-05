using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Editing;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.AGP.Editing
{
	public static class GdbPersistenceUtils
	{
		private static readonly IMsg _msg = new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		public static async Task<bool> SaveInOperationAsync(
			[NotNull] string description,
			[CanBeNull] IDictionary<Feature, Geometry> updates,
			[CanBeNull] IDictionary<Feature, Geometry> copies = null)
		{
			var editOperation = new EditOperation();

			EditorTransaction transaction = new EditorTransaction(editOperation);

			return await transaction.ExecuteAsync(
				       editContext => StoreTx(editContext, updates, copies),
				       description, GetDatasets(updates?.Keys, copies?.Keys));
		}

		public static bool SaveInOperation([NotNull] string description,
		                                   [CanBeNull] IDictionary<Feature, Geometry> updates,
		                                   [CanBeNull] IDictionary<Feature, Geometry> copies = null)
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
			[CanBeNull] IDictionary<Feature, Geometry> copies = null)
		{
			if (updates != null && updates.Count > 0)
			{
				foreach (KeyValuePair<Feature, Geometry> keyValuePair in updates)
				{
					StoreShape(keyValuePair, editContext);
				}

				_msg.InfoFormat("Successfully stored {0} updated feature(s).", updates.Count);
			}

			if (copies != null && copies.Count > 0)
			{
				foreach (KeyValuePair<Feature, Geometry> keyValuePair in copies)
				{
					Feature originalFeature = keyValuePair.Key;
					Geometry newGeometry = keyValuePair.Value;

					RowBuffer rowBuffer = DuplicateRow(originalFeature);

					Feature newFeature = originalFeature.GetTable().CreateRow(rowBuffer);

					StoreShape(newFeature, newGeometry, editContext);
				}

				_msg.InfoFormat("Successfully created {0} new feature(s).", copies.Count);
			}

			return true;
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
				if (fields[i].FieldType == FieldType.Geometry && ! includeShape)
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

			feature.Dispose();
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

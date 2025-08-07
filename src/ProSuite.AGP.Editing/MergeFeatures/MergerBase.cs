using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ProSuite.AGP.Editing.Properties;
using ProSuite.Commons.AGP.Core.Geodatabase;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Notifications;
using ProSuite.Commons.Text;
using ProSuite.Commons.UI.Dialogs;

namespace ProSuite.AGP.Editing.MergeFeatures
{
	public abstract class MergerBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private readonly MergeOperationSurvivor _mergeSurvivor;

		protected MergerBase(
			[NotNull] MergeToolOptions mergeOptions,
			MergeOperationSurvivor knownSurvivor = MergeOperationSurvivor.Undefined)
		{
			MergeOptions = mergeOptions;

			_mergeSurvivor = knownSurvivor == MergeOperationSurvivor.Undefined
				                 ? knownSurvivor
				                 : mergeOptions.MergeSurvivor;
		}

		protected MergeToolOptions MergeOptions { get; }

		/// <summary>
		/// An optional merge condition evaluator that currently only results in warnings
		/// if some condition is violated.
		/// </summary>
		public IMergeConditionEvaluator MergeConditionEvaluator { get; set; }

		public bool CanMerge(IList<Feature> features)
		{
			if (features.Any(feature => feature == null))
			{
				_msg.Info("One of the merge features is null. Cannot merge.");

				return false;
			}

			if (features.Count < 2)
			{
				_msg.Info("Cannot merge less than two features.");

				return false;
			}

			List<Geometry> geometries = features.Select(feature => feature.GetShape())
			                                    .ToList();

			bool differentGeometryTypes = geometries.Select(g => g.GeometryType).Distinct()
			                                        .Count() > 1;

			if (differentGeometryTypes)
			{
				_msg.Info(
					"The selected features do not have the same geometry type. Cannot merge.");

				return false;
			}

			if (! CanMergeCore(features))
			{
				return false;
			}

			// NOTE: Non-simple input (e.g. from FGDBs) can result in disappearing parts
			if (! AssumeStoredGeometryIsSimple &&
			    ! ToolUtils.ReasonablySimple(features))
			{
				Dialog.Warning("Cannot merge features",
				               $"Unable to merge the selected features.{Environment.NewLine}" +
				               $"Please correct the input features first, for example with the clean geometry tool.");
				return false;
			}

			return true;
		}

		public bool AssumeStoredGeometryIsSimple { get; set; }

		protected virtual bool CanMergeCore(IList<Feature> features)
		{
			return true;
		}

		protected virtual bool IsValidMergeResultCore(Geometry mergeResult,
		                                              NotificationCollection notifications)
		{
			return true;
		}

		protected virtual void OnCommitting(Feature updateFeature,
		                                    IList<Feature> deleteFeatures,
		                                    Geometry mergedGeometry) { }

		private bool IsValidMergeResult([NotNull] Geometry mergeResult,
		                                NotificationCollection notifications)
		{
			Assert.ArgumentNotNull(mergeResult, nameof(mergeResult));

			if (mergeResult.IsEmpty)
			{
				NotificationUtils.Add(notifications, "The merge result is empty");

				return false;
			}

			if (! MergeOptions.AllowMultipartResult)
			{
				if (mergeResult.GeometryType == GeometryType.Polygon &&
				    ((Polygon) mergeResult).ExteriorRingCount > 1 ||
				    mergeResult.GeometryType == GeometryType.Polyline &&
				    ((Multipart) mergeResult).PartCount > 1)

				{
					NotificationUtils.Add(notifications,
					                      "The merge result is a multi-part geometry (not allowed/option disabled)");

					return false;
				}
			}

			return IsValidMergeResultCore(mergeResult, notifications);
		}

		public async Task<Feature> MergeFeatures([NotNull] IList<Feature> features,
		                                         [NotNull] Feature survivor)
		{
			var notifications = new NotificationCollection();

			var geometries =
				features.Select(feature => feature.GetShape()).ToList();

			Geometry mergeResult = UnionGeometries(geometries);

			GeometryUtils.Simplify(mergeResult);

			if (! IsValidMergeResult(mergeResult, notifications))
			{
				Dialog.Warning(null,
				               LocalizableStrings.MergeFeaturesTool_Caption,
				               string.Format(
					               LocalizableStrings.MergeFeaturesTool_DisjointGeometriesList,
					               NotificationUtils.Concatenate(notifications, " ")));

				return null;
			}

			ICollection<string> warnings = AddWarningsIfNecessary(features, mergeResult);

			Feature updateFeature = null;
			var deletedFeatures = new List<Feature>();
			foreach (Feature feature in features)
			{
				if (feature.GetObjectID() == survivor.GetObjectID() &&
				    feature.GetTable().GetID() == survivor.GetTable().GetID())
				{
					updateFeature = feature;
				}
				else
				{
					deletedFeatures.Add(feature);
				}
			}

			Assert.NotNull(updateFeature, "No update feature");
			Assert.NotNull(mergeResult, "mergeResult");

			Feature resultFeature =
				await UpdateMergedFeatures(updateFeature, deletedFeatures, mergeResult, warnings);

			return resultFeature;
		}

		protected virtual Geometry UnionGeometries(IList<Geometry> geometries)
		{
			Geometry mergeResult = GeometryUtils.Union(geometries);

			return mergeResult;
		}

		private ICollection<string> AddWarningsIfNecessary(IList<Feature> features,
		                                                   Geometry mergeResult)
		{
			List<string> warnings = new List<string>();

			// TODO: Reconsider MergeConditionEvaluator
			if (MergeConditionEvaluator != null)
			{
				//if (! DatasetUtils.AreSameObjectClass(features.Select(f => f.Class)))
				//{
				//	List<string> distinctClassNames =
				//		features.Select(f => f.Class).Distinct().Select(c => c.AliasName).ToList();

				//	_msg.InfoFormat(
				//		"The merged features belong to different feature classes: {0}",
				//		StringUtils.Concatenate(distinctClassNames, ", "));
				//}
				//else if (((FeatureClass) features[0].Class).ShapeType ==
				//         esriGeometryType.esriGeometryPolyline &&
				//         features.Count == 2)
				{
					// TODO: Enhance, allow XML configuration, use for all geometry types

					var failInfos = new List<MergeFailInfo>();
					MergeConditionEvaluator.IsMergeAllowed(
						features[0], features[1], failInfos, null, true);

					warnings.AddRange(failInfos.Select(f => f.Reason));
				}

				if (! MergeConditionEvaluator.AllowLoops //&& IsLoop(mergeResult)
				   )
				{
					warnings.Add("The result geometry is a closed loop");
				}
			}

			return warnings;
		}

		/// <summary>
		/// Updates the two merged features
		/// </summary>
		/// <param name="updateFeature">Feature that will hold the merged geometries</param>
		/// <param name="deletedFeatures">Feature that was merged and will be deleted</param>
		/// <param name="mergedGeometry">Merged geometry of the given features</param>
		/// <param name="warnings">The warnings that might indicate that the merge is not
		/// a good idea.</param>
		/// <returns>The surviving feature if updating of the merged feature was successful, 
		/// null otherwise</returns>
		private async Task<Feature> UpdateMergedFeatures([NotNull] Feature updateFeature,
		                                                 [NotNull] IList<Feature> deletedFeatures,
		                                                 [NotNull] Geometry mergedGeometry,
		                                                 ICollection<string> warnings)
		{
			Assert.ArgumentNotNull(updateFeature, nameof(updateFeature));
			Assert.ArgumentNotNull(deletedFeatures, nameof(deletedFeatures));
			Assert.ArgumentNotNull(mergedGeometry, nameof(mergedGeometry));

			//string updatedFeatureFormat = RowFormat.Format(
			//	FieldDisplayUtils.GetDefaultRowFormat(updateFeature.Class, true), updateFeature);

			//string deletedFeatureFormat =
			//	StringUtils.Concatenate(deletedFeatures,
			//							f => RowFormat.Format(
			//								FieldDisplayUtils.GetDefaultRowFormat(f.Class, true), f)
			//							, ", ");

			string updatedFeatureFormat = GdbObjectUtils.ToString(updateFeature);
			string deletedFeatureFormat =
				StringUtils.Concatenate(deletedFeatures, f => GdbObjectUtils.ToString(f), ", ");

			bool success = await Commit(updateFeature, deletedFeatures, mergedGeometry);

			if (success)
			{
				LogResult(deletedFeatureFormat, updatedFeatureFormat, warnings);
			}
			else
			{
				_msg.WarnFormat("Unable to merge {0} with {1}", deletedFeatureFormat,
				                updatedFeatureFormat);
			}

			return updateFeature;
		}

		private void LogResult(string deletedFeatureFormat,
		                       string updatedFeatureFormat,
		                       ICollection<string> warnings)
		{
			using (_msg.IncrementIndentation("Successfully merged {0} with {1}",
			                                 deletedFeatureFormat, updatedFeatureFormat))
			{
				if (warnings.Count > 0)
				{
					var sb = new StringBuilder("However, ");
					foreach (string info in warnings)
					{
						sb.Append(info);
						sb.Append(Environment.NewLine);
					}

					sb.Append(Environment.NewLine);

					_msg.WarnFormat(sb.ToString());
				}
				else
				{
					string survivorText =
						_mergeSurvivor == MergeOperationSurvivor.LargerObject
							? "larger feature"
							: "first selected feature";
					_msg.InfoFormat("{0} has been updated ({1})", updatedFeatureFormat,
					                survivorText);
					_msg.InfoFormat("{0} has been deleted", deletedFeatureFormat);
				}
			}
		}

		//private static bool IsLoop(Geometry mergeResult)
		//{
		//	Polyline polyline = mergeResult as Polyline;

		//	if (polyline == null)
		//	{
		//		return false;
		//	}

		//	if (GeometryUtils.GetPartCount(polyline) != 1)
		//	{
		//		return false;
		//	}

		//	return polyline.IsClosed;
		//}

		protected virtual async Task<bool> Commit(Feature updateFeature,
		                                          [NotNull] IList<Feature> deleteFeatures,
		                                          [NotNull] Geometry mergedGeometry)
		{
			Assert.ArgumentNotNull(updateFeature, nameof(updateFeature));
			Assert.ArgumentNotNull(deleteFeatures, nameof(deleteFeatures));
			Assert.ArgumentCondition(deleteFeatures.Count > 0,
			                         "At least one feature must be deleted.");
			Assert.ArgumentNotNull(mergedGeometry, nameof(mergedGeometry));

			var datasets = GdbPersistenceUtils
			               .GetDatasetsNonEmpty(deleteFeatures.Append(updateFeature)).ToList();

			bool saved = await GdbPersistenceUtils.ExecuteInTransactionAsync(
				             editContext =>
				             {
					             if (MergeOptions.TransferRelationships)
					             {
						             foreach (Feature deleteFeature in
						                      deleteFeatures.OrderBy(f => GeometryUtils
								                      .GetGeometrySize(f.GetShape())))
						             {
							             TransferRelationships(deleteFeature, updateFeature);
						             }
					             }

					             OnCommitting(updateFeature, deleteFeatures, mergedGeometry);

					             _msg.DebugFormat("Saving one updates and {0} deletes...",
					                              deleteFeatures.Count);

					             try
					             {
						             GdbPersistenceUtils.StoreShape(
							             updateFeature, mergedGeometry, editContext);

						             foreach (Feature deleteFeature in deleteFeatures)
						             {
							             editContext.Invalidate(deleteFeature);
							             deleteFeature.Delete();
						             }
					             }
					             catch (Exception ex)
					             {
						             _msg.Error("Error during merge operation", ex);
						             editContext.Abort($"Merge failed: {ex.Message}");
						             return false;
					             }

					             return true;
				             },
				             "Merge features", datasets);
			return saved;
		}

		/// <summary>
		/// Copies as many relationships as possible from the sourceFeature to the target feature.
		/// </summary>
		/// <param name="sourceFeature">Source feature holding relationships that could be
		/// transfered to the targetObject</param>
		/// <param name="targetFeature">Target feature that will get the relationships of
		/// the sourceFeature</param>
		private static void TransferRelationships([NotNull] Feature sourceFeature,
		                                          [NotNull] Feature targetFeature)
		{
			QueuedTask.Run(() =>
			{
				FeatureClass sourceClass = sourceFeature.GetTable();
				Geodatabase gdb = (Geodatabase) sourceClass.GetDatastore();

				var relClasses = RelationshipClassUtils.GetRelationshipClassDefinitions(
					gdb, rc => rc.GetOriginClass() == sourceClass.GetName() ||
					           rc.GetDestinationClass() == sourceClass.GetName()).ToList();

				foreach (RelationshipClassDefinition relClassDefinition in
				         relClasses.Where(rc => rc.GetCardinality() !=
				                                RelationshipCardinality.OneToOne))
				{
					TransferRelationships(sourceFeature, targetFeature, relClassDefinition);
				}
			});
		}

		/// <summary>
		/// Transfers the relations from the given relationshipClass found on
		/// the sourceFeature to the targetObject
		/// </summary>
		/// <param name="sourceFeature">Source feature holding the relations to transfer</param>
		/// <param name="targetFeature">Target feature the will get the relations</param>
		/// <param name="relationshipClassDefinition">Relationshipclass used to get all relations of
		/// both feature to prevent coping a relationship that allready exists</param>
		private static void TransferRelationships(
			[NotNull] Feature sourceFeature,
			[NotNull] Feature targetFeature,
			[NotNull] RelationshipClassDefinition relationshipClassDefinition)
		{
			// Origin: contains primary key
			// Destination: contains foreign key
			bool sourceFeatureIsOrigin =
				IsFeatureFromOriginClass(sourceFeature, relationshipClassDefinition);

			Geodatabase geodatabase = (Geodatabase) sourceFeature.GetTable().GetDatastore();

			var relationshipClass =
				geodatabase.OpenDataset<RelationshipClass>(relationshipClassDefinition.GetName());

			IReadOnlyList<Row> relatedToSourceFeature =
				RelationshipClassUtils.GetRelatedRows(sourceFeature, relationshipClass,
				                                      sourceFeatureIsOrigin);

			if (! DatasetUtils.IsSameObjectClass(sourceFeature.GetTable(),
			                                     targetFeature.GetTable()))
			{
				// NOTE: Theoretically some related objects could be related to the source feature through a different rel-class,
				//       such as TLM-names. However this is not necessarily correct because these relationships can have different meanings
				//       or even be ambiguous if there are several relationship classes between the same two object classes.
				if (relatedToSourceFeature.Count > 0)
				{
					_msg.InfoFormat(
						"The relationships of {0} cannot be transferred to {1} because they are not from the same feature class.",
						GdbObjectUtils.ToString(sourceFeature),
						GdbObjectUtils.ToString(targetFeature));
				}

				return;
			}

			// Get target relationships only once we know the target is from the same class (TOP-4570)
			IReadOnlyList<Row> relatedToTargetFeature =
				RelationshipClassUtils.GetRelatedRows(targetFeature, relationshipClass,
				                                      sourceFeatureIsOrigin);

			foreach (Row relatedToSourceRow in relatedToSourceFeature)
			{
				if (relatedToSourceRow == null ||
				    IsFeatureInList(relatedToSourceRow, relatedToTargetFeature))
				{
					continue;
				}

				// avoid TOP-4786, but allow overwriting foreign key if the (single) related feature
				// is going to be deleted by the merge:
				bool overwriteExistingForeignKeys = sourceFeatureIsOrigin &&
				                                    relationshipClassDefinition.GetCardinality() ==
				                                    RelationshipCardinality.OneToMany;

				var notifications = new NotificationCollection();

				Row originObject, destinationObject;
				if (sourceFeatureIsOrigin)
				{
					// Source and target are in the same feature class:
					originObject = targetFeature;
					destinationObject = relatedToSourceRow;
				}
				else
				{
					destinationObject = targetFeature;
					originObject = relatedToSourceRow;
				}

				Relationship newRelationship = RelationshipClassUtils.TryCreateRelationship(
					originObject, destinationObject, relationshipClass, false,
					overwriteExistingForeignKeys, notifications);

				string relationshipLabel = sourceFeatureIsOrigin
					                           ? relationshipClassDefinition.GetForwardPathLabel()
					                           : relationshipClassDefinition.GetBackwardPathLabel();

				string relatedRowFormat = GdbObjectUtils.ToString(relatedToSourceRow);

				if (newRelationship == null)
				{
					_msg.InfoFormat(
						"Relationship ({0}) to {1} was not transferred to remaining feature. {2}",
						relationshipLabel, relatedRowFormat,
						NotificationUtils.Concatenate(notifications, ". "));
				}
				else
				{
					_msg.InfoFormat("Transferring relationship ({0}) to {1} to remaining feature.",
					                relationshipLabel, relatedRowFormat);
				}
			}
		}

		/// <summary>
		/// Check if the given object is found in the list of relations.
		/// </summary>
		/// <param name="relRow">Related object searched in the given list</param>
		/// <param name="rowList">List of rows that already have a relationship of a specific type.
		/// may belong to</param>
		/// <returns>TRUE if the related object is found, FALSE otherwise</returns>
		private static bool IsFeatureInList(
			[NotNull] Row relRow,
			[NotNull] IReadOnlyList<Row> rowList)
		{
			foreach (Row row in rowList)
			{
				if (relRow.GetObjectID() == row.GetObjectID())
				{
					return true;
				}
			}

			return false;
		}

		/// <summary>
		/// Check if the given feature belongs to the originclass of the given
		/// relationship class.
		/// If the class of the feature and the originfeatureclass do not share
		/// the same name, then false is returned, there is no check, if the class
		/// of the given features does belong the relationshipClass
		/// </summary>
		/// <param name="feature">Feature to check</param>
		/// <param name="relationshipClassDefinition">RelationshipClass used to get the originClass</param>
		/// <returns>TRUE if the feature is from the originClass, FALSE otherwise</returns>
		private static bool IsFeatureFromOriginClass(
			[NotNull] Feature feature,
			[NotNull] RelationshipClassDefinition relationshipClassDefinition)
		{
			string featureClassName = feature.GetTable().GetName();
			string originClassName = relationshipClassDefinition.GetOriginClass();

			return featureClassName.Equals(originClassName);
		}
	}
}

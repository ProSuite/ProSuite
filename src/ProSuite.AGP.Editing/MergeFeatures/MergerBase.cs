using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.AGP.Editing.Properties;
using ProSuite.Commons.AGP.Core.Spatial;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Notifications;
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

		//protected IMxApplication MxApplication
		//	=> _mxApplication ?? (_mxApplication = ArcMapUtils.GetMxApplication());

		//protected IEditor Editor
		//	=> _editor ?? (_editor = ArcMapUtils.GetEditor(MxApplication));

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
				_msg.Info("Can not merge less than two features.");

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
				    mergeResult.GeometryType == GeometryType.Polyline //&&
				    //((IGeometryCollection)mergeResult).GeometryCount > 1)
				    //polyline.Parts.Count > 1
				    //((Polyline)mergeResult.PartCount>1)
				   )
				{
					NotificationUtils.Add(notifications,
					                      "The merge result is a multi-part geometry (not allowed/option disabled)");

					return false;
				}
			}

			return IsValidMergeResultCore(mergeResult, notifications);
		}

		public Feature MergeFeatures([NotNull] IList<Feature> features,
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
				if (feature == survivor)
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
				UpdateMergedFeatures(updateFeature, deletedFeatures, mergeResult, warnings);

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
				//if (!DatasetUtils.AreSameObjectClass(features.Select(f => f.Class)))
				//{
				//	List<string> distinctClassNames =
				//		features.Select(f => f.Class).Distinct().Select(c => c.AliasName).ToList();

				//	_msg.InfoFormat(
				//		"The merged features belong to different feature classes: {0}",
				//		StringUtils.Concatenate(distinctClassNames, ", "));
				//}
				//else if (((FeatureClass)features[0].Class).ShapeType ==
				//		 esriGeometryType.esriGeometryPolyline &&
				//		 features.Count == 2)
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
		private Feature UpdateMergedFeatures([NotNull] Feature updateFeature,
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

			Task<bool> success = Commit(updateFeature, deletedFeatures, mergedGeometry);

			//if (success)
			//{
			//	LogResult(deletedFeatureFormat, updatedFeatureFormat, warnings);

			//	EditToolUtils.EnsureMapSpatialReference(updateFeature, Editor.Map);

			//	DisplayUtils.InvalidateFeature(ArcMapUtils.GetAppDisplay(MxApplication),
			//								   updateFeature);
			//	DisplayUtils.InvalidateFeatures(ArcMapUtils.GetAppDisplay(MxApplication),
			//									deletedFeatures);
			//}
			//else
			//{
			//	_msg.WarnFormat("Unable to merge {0} with {1}", deletedFeatureFormat,
			//					updatedFeatureFormat);
			//	ArcMapUtils.RefreshAll(MxApplication);
			//}

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

			//var featuresToDelete = new List<Feature>(deleteFeatures);

			// IMPORTANT: Ensure the update feature is not in the delete list
			// (Should never happen, but prevents errors if it does)
			//featuresToDelete.RemoveAll(f =>
			//	                           f.GetObjectID() == updateFeature.GetObjectID() &&
			//	                           f.GetTable().GetID() == updateFeature.GetTable().GetID());

			var datasets = GdbPersistenceUtils
			               .GetDatasetsNonEmpty(deleteFeatures.Append(updateFeature)).ToList();

			bool saved = await GdbPersistenceUtils.ExecuteInTransactionAsync(
				             editContext =>
				             {
					             //// Re-query the updateFeature to ensure it's not recycled
					             //long updateFeatureOid = updateFeature.GetObjectID();
					             //Table featureTable = updateFeature.GetTable();
					             //Feature freshUpdateFeature = null;

					             //using (RowCursor cursor = featureTable.Search(
					             //        new QueryFilter { WhereClause = $"OBJECTID = {updateFeatureOid}" }, false))
					             //{
					             // if (cursor.MoveNext())
					             // {
					             //	 freshUpdateFeature = (Feature)cursor.Current;
					             // }
					             //}

					             //if (freshUpdateFeature == null)
					             //{
					             // _msg.Error($"Could not re-query survivor feature with OID {updateFeatureOid}");
					             // editContext.Abort($"Could not re-query survivor feature with OID {updateFeatureOid}");
					             // return false;
					             //}

					             if (MergeOptions.TransferRelationships)
					             {
						             foreach (Feature deleteFeature in
						                      deleteFeatures.OrderBy(f => GeometryUtils
							                      .GetGeometrySize(f.GetShape())))
						             {
							             // TransferRelationships(deleteFeature, freshUpdateFeature);
						             }
					             }

					             OnCommitting(updateFeature, deleteFeatures, mergedGeometry);

					             //// also delete the default junction type if it is not the orphan class?
					             //// -> make configurable?
					             //Predicate<ISimpleJunctionFeature> deleteIntermediateJunction =
					             //	simpleJunctionFeature =>
					             //		NetworkUtils.IsOrphanJunctionFeature(
					             //			(IFeature)simpleJunctionFeature) ||
					             //		NetworkUtils.IsDefaultJunctionForAllEdges(simpleJunctionFeature);

					             //NetworkUtils.StoreFeatureMerge(updateFeature, deleteFeatures, mergedGeometry,
					             //							   deleteIntermediateJunction, out _);

					             _msg.DebugFormat("Saving one updates and {0} deletes...",
					                              deleteFeatures.Count);

					             try
					             {
						             GdbPersistenceUtils.StoreShape(
							             updateFeature, mergedGeometry, editContext);

						             foreach (Feature featureToDelete in deleteFeatures)
						             {
							             featureToDelete.Delete();
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

		///// <summary>
		///// Copies as many relationships as possible from the sourceFeature to the target feature.
		///// </summary>
		///// <param name="sourceFeature">Source feature holding relationships that could be
		///// transfered to the targetObject</param>
		///// <param name="targetObject">Target feature that will get the relationships of
		///// the sourceFeature</param>
		//private static void TransferRelationships([NotNull] Feature sourceFeature,
		//										  [NotNull] Object targetObject)
		//{
		//	IObjectClass sourceClass = sourceFeature.Class;

		//	IEnumerable<Relationship> transferableRelationshipClasses =
		//		DatasetUtils.GetRelationshipClasses(sourceClass).Where(
		//			relClass =>
		//				relClass.Cardinality !=
		//				esriRelCardinality.esriRelCardinalityOneToOne);

		//	foreach (RelationshipClass relationshipClass in transferableRelationshipClasses)
		//	{
		//		TransferRelationships(sourceFeature, targetObject, relationshipClass);
		//	}
		//}

		///// <summary>
		///// Transfers the relations from the given relationshipClass found on
		///// the sourceFeature to the targetObject
		///// </summary>
		///// <param name="sourceFeature">Source feature holding the relations to transfere</param>
		///// <param name="targetObject">Target feature the will get the relations</param>
		///// <param name="relationshipClass">Relationshipclass used to get all relations of
		///// both feature to prevent coping a relationship that allready exists</param>
		//private static void TransferRelationships(
		//	[NotNull] Feature sourceFeature,
		//	[NotNull] Object targetObject,
		//	[NotNull] RelationshipClass  relationshipClass)
		//{
		//	IList<Relationship> sourceRelations =
		//		GetRelationships(sourceFeature, relationshipClass);

		//	if (!DatasetUtils.IsSameObjectClass(sourceFeature.Class, targetObject.Class))
		//	{
		//		// NOTE: Theoretically some related objects could be related to the source feature through a different rel-class,
		//		//       such as TLM-names. However this is not necessarily correct because these relationships can have different meanings
		//		//       or even be ambiguous if there are several relationship classes between the same two object classes.
		//		if (sourceRelations.Count > 0)
		//		{
		//			_msg.InfoFormat(
		//				"The relationships of {0} cannot be transferred to {1} because they are not from the same feature class.",
		//				RowFormat.Format(sourceFeature, true),
		//				RowFormat.Format(targetObject, true));
		//		}

		//		return;
		//	}

		//	// Get target relationships only once we know the target is from the same class (TOP-4570)
		//	IList<Relationship> targetRelations =
		//		GetRelationships(targetObject, relationshipClass);

		//	// Origin: contains primary key
		//	// Destination: contains foreign key
		//	bool isOrigin = IsFeatureFromOriginClass(sourceFeature, relationshipClass);

		//	foreach (Relationship relation in sourceRelations)
		//	{
		//		Object relatedObject = isOrigin
		//									? relation.DestinationObject
		//									: relation.OriginObject;

		//		if (relatedObject == null ||
		//			IsObjectInRelationList(relatedObject, targetRelations, !isOrigin))
		//		{
		//			continue;
		//		}

		//		// avoid TOP-4786, but allow overwriting foreign key if the (single) related feature
		//		// is going to be deleted by the merge:
		//		bool overwriteExistingForeignKeys = isOrigin &&
		//											relationshipClass.Cardinality ==
		//											esriRelCardinality.esriRelCardinalityOneToMany;

		//		var notifications = new NotificationCollection();

		//		IRelationship newRelationship = RelationshipClassUtils.TryCreateRelationship(
		//			targetObject, relatedObject, relationshipClass, false,
		//			overwriteExistingForeignKeys, notifications);

		//		string relationshipLabel = isOrigin
		//									   ? relationshipClass.ForwardPathLabel
		//									   : relationshipClass.BackwardPathLabel;

		//		string relatedObjFormat = RowFormat.Format(relatedObject, true);

		//		if (newRelationship == null)
		//		{
		//			_msg.InfoFormat(
		//				"Relationship ({0}) to {1} was not transferred to remaining feature. {2}",
		//				relationshipLabel, relatedObjFormat,
		//				NotificationUtils.Concatenate(notifications, ". "));
		//		}
		//		else
		//		{
		//			_msg.InfoFormat("Transferring relationship ({0}) to {1} to remaining feature.",
		//							relationshipLabel, relatedObjFormat);
		//		}
		//	}
		//}

		///// <summary>
		///// Check if the given object is found in the list of relations.
		///// </summary>
		///// <param name="relObject">Related object searched in the given list</param>
		///// <param name="relations">List of relationships where the given related object
		///// may belong to</param>
		///// <param name="objectIsOrigin">Flag if the given related object is from 
		///// the originClass of the relationshipclass</param>
		///// <returns>TRUE if the related object is found, FALSE otherwise</returns>
		//private static bool IsObjectInRelationList(
		//	Object relObject,
		//	[NotNull] ICollection<Relationship> relations, bool objectIsOrigin)
		//{
		//	if (relObject == null || relations.Count < 1)
		//	{
		//		return false;
		//	}

		//	Assert.True(relObject.HasOID,
		//				"Selected feature does not have an object ID.");

		//	foreach (Relationship relation in relations)
		//	{
		//		Object tempObject = objectIsOrigin
		//								 ? relation.OriginObject
		//								 : relation.DestinationObject;

		//		if (tempObject != null && tempObject.GetObjectID() == relObject.GetObjectID())
		//		{
		//			return true;
		//		}
		//	}

		//	return false;
		//}

		///// <summary>
		///// Check if the given feature belongs to the originclass of the given
		///// relationship class.
		///// If the class of the feature and the originfeatureclass do not share
		///// the same name, then false is returned, there is no check, if the class
		///// of the given features does belong the the relationshipClass
		///// </summary>
		///// <param name="feature">Feature to check</param>
		///// <param name="relationshipClass">RelationshipClass used to get the originClass</param>
		///// <returns>TRUE if the feature is from the originClass, FALSE otherwise</returns>
		//private static bool IsFeatureFromOriginClass(
		//	[NotNull] Feature feature,
		//	[NotNull] RelationshipClass  relationshipClass)
		//{
		//	string featureClassName = ((IDataset)feature.Class).Name;
		//	string originClassName = ((IDataset)relationshipClass.OriginClass).Name;

		//	return featureClassName.Equals(originClassName);
		//}

		///// <summary>
		///// Gets the list of relationships where the given object is one part of.
		///// </summary>
		///// <param name="gdbObject">Feature that must belong to the returned relationship</param>
		///// <param name="relationshipClass">RelationshipClass that holds the information
		///// about the relationships with the given object</param>
		///// <returns>List with Relationship instances, could be empty</returns>
		//[NotNull]
		//private static IList<Relationship> GetRelationships(
		//	[NotNull] Object gdbObject,
		//	[NotNull] RelationshipClass  relationshipClass)
		//{
		//	var result = new List<Relationship>();

		//	IEnumRelationship relations = relationshipClass.GetRelationshipsForObject(gdbObject);

		//	if (relations != null)
		//	{
		//		relations.Reset();
		//		IRelationship relationship;
		//		while ((relationship = relations.Next()) != null)
		//		{
		//			result.Add(relationship);
		//		}
		//	}

		//	return result;
		//}
	}
}

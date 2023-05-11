using System;
using System.Collections.Generic;
using System.Diagnostics;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.TableBased;
using ProSuite.Commons.AO.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Container;

namespace ProSuite.DomainServices.AO.QA
{
	internal static class ReferenceGeometryUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		/// <remarks>
		/// Do not change this string, it is persisted on <see cref="QaError"/>
		/// </remarks>
		internal static string ReferencedGeometryInfo => "; (referenced geometry stored)";

		[CanBeNull]
		internal static IGeometry CreateReferenceGeometry(
			[NotNull] IEnumerable<InvolvedRow> involvedRows,
			[NotNull] IVerificationContext verificationContext,
			[NotNull] QualityCondition qualityCondition,
			int maximumPointCount,
			[NotNull] IQualityConditionObjectDatasetResolver datasetResolver,
			[CanBeNull] Predicate<VectorDataset> isRelevantVectorDataset)
		{
			var relatedGeometries = new List<IGeometry>();

			foreach (InvolvedRow involvedRow in involvedRows)
			{
				relatedGeometries.AddRange(GetReferenceGeometries(involvedRow, qualityCondition,
					                           verificationContext,
					                           datasetResolver,
					                           isRelevantVectorDataset));
			}

			return UnionReferenceGeometry(relatedGeometries, maximumPointCount);
		}

		[NotNull]
		internal static QaError CreateReferenceGeometryError(
			[NotNull] QaError qaError,
			[NotNull] IGeometry referenceGeometry)
		{
			string description = string.Concat(qaError.Description, ReferencedGeometryInfo);

			return new QaError(qaError.Test,
			                   description,
			                   qaError.InvolvedRows,
			                   referenceGeometry,
			                   qaError.IssueCode,
			                   qaError.AffectedComponent);
		}

		[NotNull]
		internal static HashSet<long> GetOidsByRelatedGeometry(
			[NotNull] IReadOnlyTable table,
			[NotNull] IEnumerable<IList<IRelationshipClass>> relClassChains,
			[NotNull] IGeometry testPerimeter,
			[NotNull] ITest testWithTable)
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNull(relClassChains, nameof(relClassChains));
			Assert.ArgumentNotNull(testPerimeter, nameof(testPerimeter));

			string whereClause = string.Empty;
			string postfixClause = string.Empty;
			string tableName = table.Name;

			Stopwatch watch =
				_msg.DebugStartTiming("Getting row OIDs by related geometry for {0}",
				                      tableName);

			var result = new HashSet<long>();

			IObjectClass objectClass = table as IObjectClass;

			if (objectClass == null)
			{
				ReadOnlyTable roTable = table as ReadOnlyTable;

				if (roTable != null)
				{
					objectClass = (IObjectClass) roTable.BaseTable;
				}
			}

			Assert.NotNull(objectClass, "Unknown IReadOnlyTable implementation");

			foreach (IList<IRelationshipClass> relClassChain in relClassChains)
			{
				// NOTE:
				// - if only the OID is in the subfields, then ArcMap crashes without 
				//   catchable exception in RARE cases
				// - if only the OID plus the Shape field of the involved feature class are in the subfields, then
				//   in those same cases a "Shape Integrity Error" exception is thrown.
				foreach (FieldMappingRowProxy row in
				         GdbQueryUtils.GetRowProxys(objectClass,
				                                    testPerimeter,
				                                    whereClause,
				                                    relClassChain,
				                                    postfixClause,
				                                    subfields: null, includeOnlyOIDFields: true,
				                                    recycle: true))
				{
					result.Add(row.OID);
				}
			}

			_msg.DebugStopTiming(watch, "GetOIDsByRelatedGeometry() table: {0} test: {1}",
			                     tableName, testWithTable);

			return result;
		}

		[CanBeNull]
		private static IGeometry UnionReferenceGeometry(
			[NotNull] IList<IGeometry> relatedGeometries,
			int maximumPointCount)
		{
			Assert.ArgumentNotNull(relatedGeometries, nameof(relatedGeometries));

			if (relatedGeometries.Count == 0)
			{
				return null;
			}

			int pointCount = GeometryUtils.GetPointCount(relatedGeometries);

			if (pointCount > maximumPointCount)
			{
				return GeometryUtils.UnionGeometryEnvelopes(relatedGeometries);
			}

			if (relatedGeometries.Count == 1)
			{
				return GeometryFactory.Clone(relatedGeometries[0]);
			}

			var copies = new List<IGeometry>(relatedGeometries.Count);
			foreach (IGeometry relatedGeometry in relatedGeometries)
			{
				copies.Add(GeometryFactory.Clone(relatedGeometry));
			}

			const int toleranceFactor = 100;
			double bufferDistance = GeometryUtils.GetXyTolerance(copies[0]) * toleranceFactor;

			return GeometryFactory.CreateUnion(copies, bufferDistance);
		}

		[NotNull]
		private static ICollection<IList<IRelationshipClass>>
			GetRelationshipClassChainsToVerifiedFeatureClassesCore(
				[NotNull] IObjectDataset objectDataset,
				[NotNull] ICollection<IRelationshipClass> precedingRelationshipClasses,
				[NotNull] IWorkspaceContextLookup workspaceContextLookup,
				[CanBeNull] Predicate<VectorDataset> isRelevantVectorDataset,
				out bool hasAnyAssociationsToFeatureClasses)
		{
			var result = new List<IList<IRelationshipClass>>();

			var relClassesToFeatureClasses = new List<IRelationshipClass>();
			var associationEndsToTables = new List<AssociationEnd>();

			IWorkspaceContext workspaceContext = null;

			hasAnyAssociationsToFeatureClasses = false;

			foreach (AssociationEnd associationEnd in objectDataset.GetAssociationEnds())
			{
				ObjectDataset oppositeDataset = associationEnd.OppositeDataset;

				var oppositeVectorDataset = oppositeDataset as VectorDataset;
				if (oppositeVectorDataset != null)
				{
					if (associationEnd.Association.NotUsedForDerivedTableGeometry)
					{
						continue;
					}

					hasAnyAssociationsToFeatureClasses = true;

					if (isRelevantVectorDataset != null &&
					    ! isRelevantVectorDataset(oppositeVectorDataset))
					{
						continue;
					}

					if (workspaceContext == null)
					{
						workspaceContext =
							workspaceContextLookup.GetWorkspaceContext(objectDataset);
						Assert.NotNull(workspaceContext,
						               "Unable to determine workspace context for dataset {0}",
						               objectDataset);
					}

					IRelationshipClass relationshipClass =
						workspaceContext.OpenRelationshipClass(associationEnd.Association);

					if (relationshipClass != null)
					{
						relClassesToFeatureClasses.Add(relationshipClass);
					}
				}
				else if (oppositeDataset is TableDataset)
				{
					associationEndsToTables.Add(associationEnd);
				}
			}

			if (relClassesToFeatureClasses.Count > 0)
			{
				foreach (IRelationshipClass relClass in relClassesToFeatureClasses)
				{
					result.Add(new List<IRelationshipClass>(precedingRelationshipClasses)
					           {
						           relClass
					           });
				}
			}
			else
			{
				foreach (AssociationEnd associationEndToTable in associationEndsToTables)
				{
					if (workspaceContext == null)
					{
						workspaceContext =
							workspaceContextLookup.GetWorkspaceContext(objectDataset);
						Assert.NotNull(workspaceContext,
						               "Unable to determine workspace context for dataset {0}",
						               objectDataset);
					}

					IRelationshipClass relClassToTable =
						workspaceContext.OpenRelationshipClass(associationEndToTable.Association);
					if (relClassToTable == null)
					{
						continue;
					}

					if (precedingRelationshipClasses.Contains(relClassToTable))
					{
						continue;
					}

					var relClassChainToTable =
						new List<IRelationshipClass>(precedingRelationshipClasses)
						{
							relClassToTable
						};

					bool hasAnyIndirectAssociationsToFeatureClasses;
					ICollection<IList<IRelationshipClass>> indirectRelClassChains =
						GetRelationshipClassChainsToVerifiedFeatureClassesCore(
							associationEndToTable.OppositeDataset,
							relClassChainToTable,
							workspaceContextLookup,
							isRelevantVectorDataset,
							out hasAnyIndirectAssociationsToFeatureClasses);

					if (hasAnyIndirectAssociationsToFeatureClasses)
					{
						hasAnyAssociationsToFeatureClasses = true;
					}

					result.AddRange(indirectRelClassChains);

					// TODO REVISE: use only the first association end to a table which leads to a verified feature class
					if (indirectRelClassChains.Count > 0)
					{
						break;
					}
				}
			}

			return result;
		}

		[NotNull]
		internal static IEnumerable<IList<IRelationshipClass>>
			GetRelationshipClassChainsToVerifiedFeatureClasses(
				[NotNull] IObjectDataset objectDataset,
				[NotNull] IWorkspaceContextLookup workspaceContextLookup,
				[CanBeNull] Predicate<VectorDataset> isRelevantVectorDataset,
				out bool hasAnyAssociationsToFeatureClasses)
		{
			return GetRelationshipClassChainsToVerifiedFeatureClassesCore(
				objectDataset,
				new List<IRelationshipClass>(),
				workspaceContextLookup,
				isRelevantVectorDataset,
				out hasAnyAssociationsToFeatureClasses);
		}

		[NotNull]
		private static IEnumerable<IGeometry> GetReferenceGeometries(
			[NotNull] InvolvedRow involvedRow,
			[NotNull] QualityCondition qualityCondition,
			[NotNull] IVerificationContext verificationContext,
			[NotNull] IQualityConditionObjectDatasetResolver datasetResolver,
			[CanBeNull] Predicate<VectorDataset> isRelevantVectorDataset)
		{
			Assert.ArgumentNotNull(involvedRow, nameof(involvedRow));
			Assert.ArgumentNotNull(qualityCondition, nameof(qualityCondition));
			Assert.ArgumentNotNull(verificationContext, nameof(involvedRow));

			if (involvedRow.RepresentsEntireTable)
			{
				yield break;
			}

			IObjectDataset objectDataset;
			IObject obj = GetInvolvedObject(involvedRow, qualityCondition, verificationContext,
			                                datasetResolver, out objectDataset);

			var feature = obj as IFeature;

			if (feature != null)
			{
				IGeometry shape = feature.Shape;
				if (shape != null && ! shape.IsEmpty)
				{
					yield return shape;
				}
			}
			else if (objectDataset != null && obj != null)
			{
				foreach (IList<IRelationshipClass> relClassChain
				         in GetRelationshipClassChainsToVerifiedFeatureClasses(
					         objectDataset,
					         verificationContext,
					         isRelevantVectorDataset,
					         out bool _))
				{
					foreach (IGeometry shape in
					         GetReferenceGeometries(obj, relClassChain))
					{
						if (shape != null && ! shape.IsEmpty)
						{
							yield return shape;
						}
					}
				}
			}
		}

		[NotNull]
		private static IEnumerable<IGeometry> GetReferenceGeometries(
			[NotNull] IObject obj,
			[NotNull] IList<IRelationshipClass> relationshipChainToFeatureClass)
		{
			if (relationshipChainToFeatureClass.Count == 0)
			{
				yield break;
			}

			if (relationshipChainToFeatureClass.Count == 1)
			{
				foreach (IObject relatedObject in
				         GdbQueryUtils.GetRelatedObjects(obj, relationshipChainToFeatureClass))
				{
					var relatedFeature = relatedObject as IFeature;

					if (relatedFeature != null)
					{
						yield return relatedFeature.Shape;
					}
					else
					{
						_msg.DebugFormat("Related object in spatial relation is not a feature: {0}",
						                 GdbObjectUtils.ToString(relatedObject));
					}
				}
			}
			else
			{
				int? shapeFieldIndex = null;

				foreach (IRow joinedRow in GetJoinedRows(obj, relationshipChainToFeatureClass))
				{
					// determine shape field index based on the first row
					if (shapeFieldIndex == null)
					{
						int index;
						if (TryGetShapeFieldIndex(joinedRow.Fields, out index))
						{
							shapeFieldIndex = index;
						}
						else
						{
							_msg.WarnFormat(
								"Shape field not found in joined table for getting reference geometry for {0}",
								DatasetUtils.GetName(obj.Class));
							yield break;
						}
					}

					yield return joinedRow.Value[shapeFieldIndex.Value] as IGeometry;
				}
			}
		}

		[NotNull]
		private static IEnumerable<IRow> GetJoinedRows(
			[NotNull] IObject obj,
			[NotNull] IList<IRelationshipClass> relationshipChainToFeatureClass)
		{
			string whereClause = string.Format(
				"{0} = {1}",
				DatasetUtils.QualifyFieldName(obj.Class, obj.Class.OIDFieldName),
				obj.OID);

			const IGeometry intersectedGeometry = null;
			string postfixClause = null;
			string subfields = null;
			const bool includeOnlyOIDFields = true;
			const bool recycle = false;

			foreach (FieldMappingRowProxy rowProxy in GdbQueryUtils.GetRowProxys(
				         obj.Class,
				         intersectedGeometry,
				         whereClause,
				         relationshipChainToFeatureClass,
				         postfixClause,
				         subfields, includeOnlyOIDFields, recycle))
			{
				yield return rowProxy.BaseRow;
			}
		}

		private static bool TryGetShapeFieldIndex([NotNull] IFields fields,
		                                          out int shapeFieldIndex)
		{
			int fieldCount = fields.FieldCount;

			for (var fieldIndex = 0; fieldIndex < fieldCount; fieldIndex++)
			{
				IField field = fields.Field[fieldIndex];

				if (field.Type == esriFieldType.esriFieldTypeGeometry)
				{
					shapeFieldIndex = fieldIndex;
					return true;
				}
			}

			shapeFieldIndex = -1;
			return false;
		}

		[CanBeNull]
		private static IObject GetInvolvedObject(
			[NotNull] InvolvedRow involvedRow,
			[NotNull] QualityCondition qualityCondition,
			[NotNull] IDatasetContext datasetContext,
			[NotNull] IQualityConditionObjectDatasetResolver datasetResolver,
			[CanBeNull] out IObjectDataset objectDataset)
		{
			Assert.ArgumentNotNull(involvedRow, nameof(involvedRow));
			Assert.ArgumentNotNull(qualityCondition, nameof(qualityCondition));
			Assert.ArgumentCondition(! involvedRow.RepresentsEntireTable,
			                         "involved row represents entire table");
			Assert.ArgumentNotNull(datasetContext, nameof(datasetContext));

			string gdbTableName = involvedRow.TableName;

			objectDataset =
				datasetResolver.GetDatasetByGdbTableName(gdbTableName, qualityCondition);

			if (objectDataset == null)
			{
				// E.g. if the involved row comes from a TrMakeTable transformer using any table outside the model:
				_msg.VerboseDebug(() => $"object dataset not found for {involvedRow.TableName}");
				return null;
			}

			ITable table = datasetContext.OpenTable(objectDataset);
			// TODO REFACTORMODEL revise null handling
			Assert.NotNull(table, "Dataset not found in workspace: {0}", objectDataset.Name);

			// TODO batch!
			IObject result = GdbQueryUtils.GetObject((IObjectClass) table, involvedRow.OID);
			return Assert.NotNull(result);
		}
	}
}

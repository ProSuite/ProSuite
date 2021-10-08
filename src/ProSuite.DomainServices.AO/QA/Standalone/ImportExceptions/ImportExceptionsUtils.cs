using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Com;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;
using ProSuite.DomainServices.AO.Properties;
using ProSuite.DomainServices.AO.QA.Exceptions;
using ProSuite.DomainServices.AO.QA.Issues;

namespace ProSuite.DomainServices.AO.QA.Standalone.ImportExceptions
{
	public static class ImportExceptionsUtils
	{
		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		public static void Import(
			[CanBeNull] string importWhereClause,
			[NotNull] IList<IObjectClass> targetExceptionClasses,
			[NotNull] IList<IObjectClass> importExceptionClasses,
			[NotNull] string importOriginValue,
			DateTime importDate)
		{
			IIssueTableFields importFields = GetImportFields(importExceptionClasses);
			IIssueTableFields targetFields = GetTargetFields(targetExceptionClasses);

			IDictionary<Guid, QualityConditionExceptions> targetExceptionsByConditionVersion;

			using (_msg.IncrementIndentation(
				"Reading existing exceptions in target workspace"))
			{
				targetExceptionsByConditionVersion = ReadTargetExceptions(targetExceptionClasses,
				                                                          targetFields);
			}

			var replacedExceptionObjects = new Dictionary<esriGeometryType, HashSet<int>>();

			using (_msg.IncrementIndentation("Importing new exceptions from issue datasets...")
			)
			{
				foreach (ITable importTable in importExceptionClasses.Cast<ITable>())
				{
					using (_msg.IncrementIndentation("from {0}...",
					                                 DatasetUtils.GetName(importTable)))
					{
						ITable targetTable = GetTargetTable(importTable, targetExceptionClasses);
						if (targetTable == null)
						{
							_msg.Warn(
								"No matching table in target workspace, ignoring import table");
							continue;
						}

						var factory = new ExceptionObjectFactory(
							importTable, importFields,
							defaultStatus: ExceptionObjectStatus.Inactive);

						var newCount = 0;
						var updateCount = 0;
						var ignoredCount = 0;

						using (var writer = new ExceptionWriter(importTable, importFields,
						                                        targetTable, targetFields))
						{
							foreach (IRow row in GdbQueryUtils.GetRows(importTable,
							                                           GetQueryFilter(
								                                           importWhereClause),
							                                           recycle: true))
							{
								int matchCount;
								bool added = ImportException(row, importOriginValue, importDate,
								                             factory,
								                             targetExceptionsByConditionVersion,
								                             replacedExceptionObjects,
								                             writer,
								                             out matchCount);
								if (! added)
								{
									ignoredCount++;
								}
								else if (matchCount == 0)
								{
									newCount++;
								}
								else
								{
									updateCount++;
								}
							}
						}

						_msg.InfoFormat("{0:N0} exception(s) imported as new", newCount);
						_msg.InfoFormat("{0:N0} exception(s) imported as updates", updateCount);
						if (ignoredCount > 0)
						{
							_msg.InfoFormat("{0:N0} exception(s) ignored", ignoredCount);
						}
					}
				}
			}

			using (_msg.IncrementIndentation("Processing replaced exceptions..."))
			{
				foreach (ITable targetTable in targetExceptionClasses.Cast<ITable>())
				{
					using (_msg.IncrementIndentation("Target table {0}...",
					                                 DatasetUtils.GetName(targetTable)))
					{
						int fixedStatusCount;
						int updateCount = ProcessReplacedExceptions(targetTable, targetFields,
						                                            replacedExceptionObjects,
						                                            importDate,
						                                            out fixedStatusCount);

						_msg.InfoFormat("{0:N0} replaced exception(s) updated", updateCount);
						if (fixedStatusCount > 0)
						{
							_msg.InfoFormat("Status value of {0:N0} old exception version(s) fixed",
							                fixedStatusCount);
						}
					}
				}
			}
		}

		public static void Update(
			[CanBeNull] string whereClause,
			[NotNull] IList<IObjectClass> targetExceptionClasses,
			[NotNull] IList<IObjectClass> updateExceptionClasses,
			[NotNull] string updateOriginValue,
			DateTime updateDate,
			bool requireOriginalVersionExists = true)
		{
			IIssueTableFields updateFields = GetUpdateFields(updateExceptionClasses);
			IIssueTableFields targetFields = GetTargetFields(targetExceptionClasses);

			var editableAttributes = new[]
			                         {
				                         IssueAttribute.ExceptionStatus,
				                         IssueAttribute.ExceptionCategory,
				                         IssueAttribute.ExceptionNotes,
				                         IssueAttribute.ExceptionOrigin,
				                         IssueAttribute.ExceptionDefinitionDate,
				                         IssueAttribute.ExceptionLastRevisionDate,
				                         IssueAttribute.ExceptionRetirementDate,
				                         IssueAttribute.IssueAssignment
			                         };

			using (_msg.IncrementIndentation(
				"Updating exceptions based on exported exception datasets..."))
			{
				foreach (ITable updateTable in updateExceptionClasses.Cast<ITable>())
				{
					using (_msg.IncrementIndentation("from {0}...",
					                                 DatasetUtils.GetName(updateTable)))
					{
						ITable targetTable = GetTargetTable(updateTable, targetExceptionClasses);
						if (targetTable == null)
						{
							_msg.Warn(
								"No matching table in target workspace, ignoring import table");
							continue;
						}

						var targetExceptionFactory = new ManagedExceptionVersionFactory(
							targetTable, targetFields, editableAttributes);
						var updateExceptionFactory = new ManagedExceptionVersionFactory(
							updateTable, updateFields, editableAttributes);

						ExceptionUpdateDetector updateDetector = GetUpdateDetector(
							targetTable,
							targetExceptionFactory,
							editableAttributes);

						var replacedOids = new HashSet<int>();

						var updatedRowsCount = 0;
						var rowsWithConflictsCount = 0;

						using (var exceptionWriter = new ExceptionWriter(updateTable, updateFields,
						                                                 targetTable, targetFields))
						{
							foreach (IRow updateRow in GdbQueryUtils.GetRows(
								updateTable, GetQueryFilter(whereClause), recycle: true))
							{
								ManagedExceptionVersion updateExceptionVersion =
									updateExceptionFactory.CreateExceptionVersion(updateRow);

								ManagedExceptionVersion mergedException;
								ManagedExceptionVersion replacedExceptionVersion;
								IList<ExceptionAttributeConflict> conflicts;

								if (updateDetector.HasChange(updateExceptionVersion,
								                             out mergedException,
								                             out replacedExceptionVersion,
								                             out conflicts))
								{
									if (replacedExceptionVersion == null)
									{
										string message = string.Format(
											"Exception version {0} not found in lineage {1} (target table: {2})",
											ExceptionObjectUtils.FormatGuid(
												updateExceptionVersion.VersionUuid),
											ExceptionObjectUtils.FormatGuid(
												updateExceptionVersion.LineageUuid),
											DatasetUtils.GetName(targetTable));

										if (requireOriginalVersionExists)
										{
											throw new InvalidDataException(message);
										}

										_msg.WarnFormat(
											"{0}. Creating new version with attributes from update row.",
											message);
									}

									updatedRowsCount++;

									string versionImportStatus;
									if (conflicts.Count == 0)
									{
										versionImportStatus = null;
									}
									else
									{
										versionImportStatus =
											FormatConflicts(conflicts, targetFields);

										rowsWithConflictsCount++;

										LogConflicts(conflicts, targetFields);
									}

									exceptionWriter.Write(updateRow, updateDate, mergedException,
									                      FormatOriginValue(updateOriginValue,
									                                        replacedExceptionVersion),
									                      updateOriginValue, versionImportStatus);

									if (replacedExceptionVersion != null)
									{
										replacedOids.Add(replacedExceptionVersion.ObjectID);
									}
								}
							}
						}

						_msg.InfoFormat("{0:N0} exception(s) updated", updatedRowsCount);
						if (rowsWithConflictsCount > 0)
						{
							_msg.WarnFormat("{0:N0} exception(s) with conflicts",
							                rowsWithConflictsCount);
						}

						if (replacedOids.Count > 0)
						{
							int fixedStatusCount;
							int updateCount = ProcessReplacedExceptions(targetTable, targetFields,
							                                            updateDate, replacedOids,
							                                            out fixedStatusCount);

							_msg.DebugFormat("{0:N0} replaced exception version(s) updated",
							                 updateCount);
							if (fixedStatusCount > 0)
							{
								_msg.WarnFormat(
									"Status value of {0:N0} old exception version(s) fixed",
									fixedStatusCount);
							}
						}
					}
				}
			}
		}

		public static bool IsUpdateWorkspace([NotNull] IFeatureWorkspace workspace)
		{
			IIssueTableFields fields =
				IssueTableFieldsFactory.GetIssueTableFields(
					addExceptionFields: true,
					useDbfFieldNames: WorkspaceUtils.IsShapefileWorkspace((IWorkspace) workspace),
					addManagedExceptionFields: true);

			var requiredAttributes = new[]
			                         {
				                         IssueAttribute.ManagedExceptionLineageUuid,
				                         IssueAttribute.ManagedExceptionVersionUuid
			                         };

			return IssueRepositoryUtils.GetIssueObjectClasses(workspace)
			                           .Cast<ITable>()
			                           .All(tbl => requiredAttributes.All(
				                                att => HasField(
					                                tbl, att, fields)));
		}

		public static void AssertSupportedWorkspaceType(
			[NotNull] IFeatureWorkspace featureWorkspace)
		{
			var workspace = (IWorkspace) featureWorkspace;
			if (WorkspaceUtils.IsFileGeodatabase(workspace) ||
			    WorkspaceUtils.IsShapefileWorkspace(workspace))
			{
				return;
			}

			throw new AssertionException(
				string.Format(
					"Unsupported workspace type: {0}. Only file geodatabases and shapefile directories are supported",
					WorkspaceUtils.GetWorkspaceDisplayText(workspace)));
		}

		[NotNull]
		public static List<IObjectClass> GetTargetExceptionClasses(
			[NotNull] IFeatureWorkspace targetWorkspace,
			[NotNull] ICollection<IObjectClass> importExceptionClasses)
		{
			List<IObjectClass> result =
				IssueRepositoryUtils.GetIssueObjectClasses(targetWorkspace)
				                    .ToList();

			IWorkspace importWorkspace =
				DatasetUtils.GetUniqueWorkspace(importExceptionClasses);
			if (importWorkspace == null)
			{
				return result;
			}

			var targetNames =
				new HashSet<string>(result.Select(DatasetUtils.GetName),
				                    StringComparer.OrdinalIgnoreCase);

			List<IObjectClass> importClassesWithMissingTarget =
				importExceptionClasses.Where(importClass => ! targetNames.Contains(
					                                            DatasetUtils.GetName(importClass)))
				                      .ToList();

			if (importClassesWithMissingTarget.Count > 0)
			{
				// assert that import workspace and target workspace have same type
				Assert.AreEqual(WorkspaceUtils.IsShapefileWorkspace(importWorkspace),
				                WorkspaceUtils.IsShapefileWorkspace((IWorkspace) targetWorkspace),
				                "Add missing exception classes is not supported when workspace types are different");

				using (_msg.IncrementIndentation("Creating missing target exception classes"))
				{
					result.AddRange(importClassesWithMissingTarget.Select(
						                cls => CreateTargetClass(targetWorkspace, cls)));
				}
			}

			return result;
		}

		private static void LogConflicts(
			[NotNull] ICollection<ExceptionAttributeConflict> conflicts,
			[NotNull] IIssueTableFields fields)
		{
			if (conflicts.Count == 0)
			{
				return;
			}

			_msg.WarnFormat("{0} attribute conflict(s) exist:", conflicts.Count);

			using (_msg.IncrementIndentation())
			{
				foreach (ExceptionAttributeConflict conflict in conflicts)
				{
					_msg.WarnFormat("- {0}:", fields.GetName(conflict.Attribute));

					using (_msg.IncrementIndentation())
					{
						_msg.WarnFormat("- original value: {0}", conflict.OriginalValue);
						_msg.WarnFormat("- new value: {0}", conflict.UpdateValue);
						_msg.WarnFormat("- previous value (overwritten): {0}",
						                conflict.CurrentValue);
						_msg.WarnFormat("- origin of previous value: {0}",
						                conflict.CurrentValueOrigin);
						_msg.WarnFormat("- import date of previous value: {0}",
						                conflict.CurrentValueImportDate);
					}
				}
			}
		}

		[CanBeNull]
		private static string FormatConflicts(
			[NotNull] ICollection<ExceptionAttributeConflict> conflicts,
			[NotNull] IIssueTableFields fields)
		{
			if (conflicts.Count == 0)
			{
				return null;
			}

			return string.Format(
				LocalizableStrings.ImportExceptionsFunctionBase_FormatConflicts,
				StringUtils.Concatenate(conflicts,
				                        c => FormatConflict(c, fields),
				                        ","));
		}

		[NotNull]
		private static string FormatConflict([NotNull] ExceptionAttributeConflict conflict,
		                                     [NotNull] IIssueTableFields fields)
		{
			return string.Format("{0} ({1} [{2}])",
			                     fields.GetName(conflict.Attribute),
			                     FormatConflictValue(conflict),
			                     conflict.CurrentValueOrigin);
		}

		[NotNull]
		private static string FormatConflictValue([NotNull] ExceptionAttributeConflict conflict)
		{
			string text = conflict.CurrentValue?.ToString();

			if (text == null)
			{
				return "<null>";
			}

			const int maxLength = 60;
			const string ellipsis = "...";

			if (text.Length <= maxLength)
			{
				return text;
			}

			return text.Substring(0, maxLength - ellipsis.Length) + ellipsis;
		}

		[NotNull]
		private static string FormatOriginValue(
			[NotNull] string updateOriginValue,
			[CanBeNull] ManagedExceptionVersion replacedExceptionVersion)
		{
			var origins = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
			              {
				              updateOriginValue
			              };

			if (replacedExceptionVersion != null)
			{
				foreach (string replacedOrigin in
					ExceptionObjectUtils.ParseOrigins(replacedExceptionVersion.ImportOrigin))
				{
					origins.Add(replacedOrigin);
				}
			}

			return ExceptionObjectUtils.FormatOrigins(origins);
		}

		[NotNull]
		private static ExceptionUpdateDetector GetUpdateDetector(
			[NotNull] ITable targetTable,
			[NotNull] ManagedExceptionVersionFactory targetExceptionVersionFactory,
			[NotNull] IEnumerable<IssueAttribute> editableAttributes)
		{
			var result = new ExceptionUpdateDetector(editableAttributes);

			foreach (IRow targetRow in GdbQueryUtils.GetRows(targetTable, recycle: true))
			{
				result.AddExistingException(
					targetExceptionVersionFactory.CreateExceptionVersion(targetRow));
			}

			return result;
		}

		[NotNull]
		private static IIssueTableFields GetUpdateFields(
			[NotNull] IEnumerable<IObjectClass> updateExceptionClasses)
		{
			IWorkspace updateWorkspace =
				Assert.NotNull(DatasetUtils.GetUniqueWorkspace(updateExceptionClasses));

			return IssueTableFieldsFactory.GetIssueTableFields(
				addExceptionFields: true,
				useDbfFieldNames: WorkspaceUtils.IsShapefileWorkspace(updateWorkspace),
				addManagedExceptionFields: true);
		}

		[NotNull]
		private static IIssueTableFields GetImportFields(
			[NotNull] IEnumerable<IObjectClass> importExceptionClasses)
		{
			IWorkspace workspace =
				Assert.NotNull(DatasetUtils.GetUniqueWorkspace(importExceptionClasses));

			return IssueTableFieldsFactory.GetIssueTableFields(
				addExceptionFields: true,
				useDbfFieldNames: WorkspaceUtils.IsShapefileWorkspace(workspace));
		}

		[NotNull]
		private static IIssueTableFields GetTargetFields(
			[NotNull] ICollection<IObjectClass> targetExceptionClasses,
			bool ensureRequiredFields = true)
		{
			IWorkspace workspace =
				Assert.NotNull(DatasetUtils.GetUniqueWorkspace(targetExceptionClasses));

			IIssueTableFieldManagement fields =
				IssueTableFieldsFactory.GetIssueTableFields(
					addExceptionFields: true,
					useDbfFieldNames: WorkspaceUtils.IsShapefileWorkspace(workspace),
					addManagedExceptionFields: true);

			if (ensureRequiredFields)
			{
				using (_msg.IncrementIndentation("Ensuring required target fields"))
				{
					int addedFields = EnsureTargetFields(targetExceptionClasses, fields);

					if (addedFields == 0)
					{
						_msg.Info("All required fields exist in target datasets");
					}
				}
			}

			return fields;
		}

		private static bool ImportException(
			[NotNull] IRow row, [NotNull] string importOriginValue, DateTime importDate,
			[NotNull] ExceptionObjectFactory factory,
			[NotNull]
			IDictionary<Guid, QualityConditionExceptions> targetExceptionsByConditionVersion,
			[NotNull] IDictionary<esriGeometryType, HashSet<int>> replacedExceptionObjects,
			[NotNull] ExceptionWriter writer,
			out int matchCount)
		{
			ExceptionObject exceptionObject = factory.CreateExceptionObject(row);

			matchCount = 0;

			// import only active exceptions
			if (exceptionObject.Status != ExceptionObjectStatus.Active)
			{
				return false;
			}

			var matchingLineageUuids = new DistinctValues<Guid>();

			var origins = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
			              {
				              importOriginValue
			              };

			foreach (ExceptionObject matchingExceptionObject in GetMatchingExceptionObjects(
				targetExceptionsByConditionVersion,
				exceptionObject))
			{
				// add to Uuids of matching exceptions (usually, should be unique)
				if (matchingExceptionObject.ManagedLineageUuid != null)
				{
					// also if matching exception is inactive
					// --> lineage of inactive exceptions may be continued (resurrection of inactive exceptions)
					matchingLineageUuids.Add(matchingExceptionObject.ManagedLineageUuid.Value);

					// NOTE: consider *preferring* active exceptions, only if none: resurrect inactive exception lineage
				}

				// include the origin values of active replaced exceptions
				// Note: not for resurrected exceptions (inactive/with end date -> active)
				if (matchingExceptionObject.ManagedVersionEndDate == null)
				{
					foreach (string replacedOrigin in
						ExceptionObjectUtils.ParseOrigins(matchingExceptionObject.ManagedOrigin))
					{
						origins.Add(replacedOrigin);
					}
				}

				matchCount++;

				AddToReplacedExceptionObjects(matchingExceptionObject, replacedExceptionObjects);
			}

			writer.Write(row,
			             importDate,
			             ExceptionObjectUtils.FormatOrigins(origins),
			             GetLineageUuid(matchingLineageUuids),
			             versionOriginValue: importOriginValue,
			             statusValue: "Active");

			return true;
		}

		private static Guid GetLineageUuid([NotNull] DistinctValues<Guid> distinctGuids)
		{
			Guid mostFrequentLineageGuid;
			return distinctGuids.TryGetMostFrequentValue(
				       out mostFrequentLineageGuid, out int _)
				       ? mostFrequentLineageGuid
				       : Guid.NewGuid();
		}

		private static void AddToReplacedExceptionObjects(
			[NotNull] ExceptionObject matchingExceptionObject,
			[NotNull] IDictionary<esriGeometryType, HashSet<int>> replacedExceptionObjects)
		{
			esriGeometryType shapeType = matchingExceptionObject.ShapeType ??
			                             esriGeometryType.esriGeometryNull;

			HashSet<int> oids;
			if (! replacedExceptionObjects.TryGetValue(shapeType, out oids))
			{
				oids = new HashSet<int>();
				replacedExceptionObjects.Add(shapeType, oids);
			}

			oids.Add(matchingExceptionObject.Id);
		}

		[NotNull]
		private static IDictionary<Guid, QualityConditionExceptions> ReadTargetExceptions(
			[NotNull] IList<IObjectClass> targetExceptionClasses,
			[NotNull] IIssueTableFields fields)
		{
			Assert.ArgumentNotNull(targetExceptionClasses, nameof(targetExceptionClasses));
			Assert.ArgumentCondition(targetExceptionClasses.Count > 0, "no exception classes");

			var result = new Dictionary<Guid, QualityConditionExceptions>();

			foreach (ITable targetTable in targetExceptionClasses.Cast<ITable>())
			{
				var factory = new ExceptionObjectFactory(
					targetTable, fields,
					defaultStatus: ExceptionObjectStatus.Inactive,
					includeManagedExceptionAttributes: true);

				foreach (IRow row in GdbQueryUtils.GetRows(targetTable, recycle: true))
				{
					ExceptionObject exceptionObject = factory.CreateExceptionObject(row);

					Guid qconVersionUuid = exceptionObject.QualityConditionVersionUuid;

					QualityConditionExceptions exceptions;
					if (! result.TryGetValue(qconVersionUuid, out exceptions))
					{
						exceptions = new QualityConditionExceptions(qconVersionUuid, null);
						result.Add(qconVersionUuid, exceptions);
					}

					exceptions.Add(exceptionObject);
				}
			}

			return result;
		}

		private static IEnumerable<ExceptionObject> GetMatchingExceptionObjects(
			[NotNull] IDictionary<Guid, QualityConditionExceptions> exceptionsByConditionVersion,
			[NotNull] ExceptionObject exceptionObject)
		{
			QualityConditionExceptions exceptions;
			if (! exceptionsByConditionVersion.TryGetValue(
				    exceptionObject.QualityConditionVersionUuid, out exceptions))
			{
				yield break; // no exception for this quality condition version
			}

			List<ExceptionObject> allMatches =
				exceptions.GetMatchingExceptions(exceptionObject).ToList();

			if (allMatches.Count == 0)
			{
				yield break; // no matches
			}

			if (allMatches.Count == 1)
			{
				yield return allMatches[0]; // one match
			}

			var anyOpenStateFound = false;
			foreach (ExceptionObject matchingExceptionObject in allMatches)
			{
				if (matchingExceptionObject.ManagedVersionEndDate == null)
				{
					yield return matchingExceptionObject;
					anyOpenStateFound = true;
				}
			}

			if (anyOpenStateFound)
			{
				yield break;
			}

			// only closed exception states exist - get the newest (for transferring the lineage)
			DateTime maximumDate = DateTime.MinValue;
			ExceptionObject latestClosedExceptionObject = null;

			foreach (ExceptionObject matchingExceptionObject in allMatches)
			{
				if (matchingExceptionObject.ManagedLineageUuid == null)
				{
					continue; // ignore closed exceptions without UUID (goal is to transfer UUID)
				}

				if (matchingExceptionObject.ManagedVersionEndDate > maximumDate)
				{
					maximumDate = matchingExceptionObject.ManagedVersionEndDate.Value;
					latestClosedExceptionObject = matchingExceptionObject;
				}
			}

			if (latestClosedExceptionObject != null)
			{
				yield return latestClosedExceptionObject;
			}
		}

		private static int ProcessReplacedExceptions(
			[NotNull] ITable targetTable,
			[NotNull] IIssueTableFields targetFields,
			[NotNull] IDictionary<esriGeometryType, HashSet<int>> replacedExceptionObjects,
			DateTime importDate,
			out int fixedStatusCount)
		{
			esriGeometryType shapeType = GetShapeType(targetTable);

			HashSet<int> replacedOids;
			if (! replacedExceptionObjects.TryGetValue(shapeType, out replacedOids))
			{
				// no replaced exception objects for this shape type
				fixedStatusCount = 0;
				return 0;
			}

			return ProcessReplacedExceptions(targetTable, targetFields,
			                                 importDate, replacedOids,
			                                 out fixedStatusCount);
		}

		private static int ProcessReplacedExceptions(
			[NotNull] ITable targetTable,
			[NotNull] IIssueTableFields targetFields,
			DateTime importDate,
			[NotNull] ICollection<int> replacedOids,
			out int fixedStatusCount)
		{
			fixedStatusCount = 0;
			int statusFieldIndex = targetFields.GetIndex(IssueAttribute.ExceptionStatus,
			                                             targetTable);
			int versionEndDateFieldIndex =
				targetFields.GetIndex(IssueAttribute.ManagedExceptionVersionEndDate, targetTable);

			ICursor cursor = targetTable.Update(null, Recycling: true);

			var updateCount = 0;

			const string statusInactive = "Inactive";

			try
			{
				for (IRow row = cursor.NextRow();
				     row != null;
				     row = cursor.NextRow())
				{
					object oldEndDate = row.Value[versionEndDateFieldIndex];
					bool oldVersionIsClosed = ! (oldEndDate == null || oldEndDate is DBNull);
					string oldStatus = (row.Value[statusFieldIndex] as string)?.Trim();

					var anyChanges = false;

					if (replacedOids.Contains(row.OID))
					{
						if (! statusInactive.Equals(oldStatus, StringComparison.OrdinalIgnoreCase))
						{
							// the row status is different from "Inactive" --> set to "Inactive"
							row.Value[statusFieldIndex] = statusInactive;
							anyChanges = true;
						}

						if (! oldVersionIsClosed)
						{
							// the row does not have an end date set --> set importDate as end date
							row.Value[versionEndDateFieldIndex] = importDate;
							anyChanges = true;
						}

						if (anyChanges)
						{
							updateCount++;
						}
					}
					else
					{
						// fix status if not 'inactive' for a closed version
						if (oldVersionIsClosed &&
						    ! statusInactive.Equals(oldStatus, StringComparison.OrdinalIgnoreCase))
						{
							row.Value[statusFieldIndex] = statusInactive;

							anyChanges = true;
							fixedStatusCount++;
						}
					}

					if (anyChanges)
					{
						cursor.UpdateRow(row);
					}
				}
			}
			finally
			{
				ComUtils.ReleaseComObject(cursor);
			}

			return updateCount;
		}

		private static int EnsureTargetFields(
			[NotNull] IEnumerable<IObjectClass> targetExceptionClasses,
			[NotNull] IIssueTableFieldManagement targetFields)
		{
			var addedFields = 0;

			var attributes = new[]
			                 {
				                 // fields specific to managed exceptions
				                 IssueAttribute.ManagedExceptionOrigin,
				                 IssueAttribute.ManagedExceptionLineageUuid,
				                 IssueAttribute.ManagedExceptionVersionBeginDate,
				                 IssueAttribute.ManagedExceptionVersionEndDate,
				                 IssueAttribute.ManagedExceptionVersionUuid,
				                 IssueAttribute.ManagedExceptionVersionOrigin,
				                 IssueAttribute.ManagedExceptionVersionImportStatus,

				                 // issue fields added after initial version
				                 IssueAttribute.IssueAssignment
			                 };

			foreach (IObjectClass targetObjectClass in targetExceptionClasses)
			{
				foreach (IssueAttribute attribute in attributes)
				{
					if (targetFields.GetIndex(attribute, (ITable) targetObjectClass,
					                          optional: true) >= 0)
					{
						// field already exists
						continue;
					}

					Add(targetObjectClass, targetFields.CreateField(attribute));
					addedFields++;
				}
			}

			return addedFields;
		}

		private static void Add([NotNull] IObjectClass objectClass,
		                        [NotNull] IField field)
		{
			_msg.InfoFormat("Adding field {0} to {1}", field.Name,
			                DatasetUtils.GetName(objectClass));

			objectClass.AddField(field);
		}

		private static esriGeometryType GetShapeType([NotNull] ITable table)
		{
			var featureClass = table as IFeatureClass;
			return featureClass?.ShapeType ?? esriGeometryType.esriGeometryNull;
		}

		[CanBeNull]
		private static ITable GetTargetTable([NotNull] ITable importTable,
		                                     [NotNull] IEnumerable<IObjectClass> targetClasses)
		{
			var importFeatureClass = importTable as IFeatureClass;
			if (importFeatureClass == null)
			{
				return targetClasses.Where(targetClass => ! (targetClass is IFeatureClass))
				                    .Cast<ITable>()
				                    .FirstOrDefault();
			}

			esriGeometryType importShapeType = importFeatureClass.ShapeType;

			return targetClasses.OfType<IFeatureClass>()
			                    .Where(target => target.ShapeType == importShapeType)
			                    .Cast<ITable>()
			                    .FirstOrDefault();
		}

		[CanBeNull]
		private static IQueryFilter GetQueryFilter([CanBeNull] string whereClause)
		{
			return StringUtils.IsNullOrEmptyOrBlank(whereClause)
				       ? null
				       : new QueryFilterClass {WhereClause = whereClause};
		}

		[NotNull]
		private static IObjectClass CreateTargetClass(
			[NotNull] IFeatureWorkspace targetWorkspace,
			[NotNull] IObjectClass importClass)
		{
			var fields = (IFields) ((IClone) importClass.Fields).Clone();

			string tableName = DatasetUtils.GetName(importClass);

			_msg.InfoFormat("{0}", tableName);

			return importClass is IFeatureClass
				       ? DatasetUtils.CreateSimpleFeatureClass(targetWorkspace, tableName, fields)
				       : (IObjectClass) DatasetUtils.CreateTable(targetWorkspace,
				                                                 tableName, null,
				                                                 fields);
		}

		private static bool HasField([NotNull] ITable table,
		                             IssueAttribute attribute,
		                             [NotNull] IIssueTableFields fields)
		{
			return fields.GetIndex(attribute, table, optional: true) >= 0;
		}
	}
}

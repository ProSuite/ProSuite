using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DomainServices.AO.QA.Issues
{
	// TODO: Consider refactoring into proper, non-static, AO-free Fields*Definition*Factory as
	// opposed to the FieldFactory
	// It could then go to DomainModel.Core
	public static class IssueTableFieldsFactory
	{
		private const string _correctionStatusDomainName = "CORRECTION_STATUS_CD";

		public static bool AddStatusField { get; set; }

		[NotNull]
		public static IIssueTableFieldManagement GetIssueTableFields(
			bool addExceptionFields,
			bool useDbfFieldNames = false,
			bool addManagedExceptionFields = false,
			bool addExportedExceptionFields = false)
		{
			return new IssueTableFields(GetFields(addExceptionFields,
			                                      useDbfFieldNames,
			                                      addManagedExceptionFields,
			                                      addExportedExceptionFields));
		}

		[NotNull]
		private static IEnumerable<IssueTableFields.Field> GetFields(
			bool addExceptionFields,
			bool useDbfFieldNames,
			bool addManagedExceptionFields,
			bool addExportedExceptionFields)
		{
			yield return Map(IssueAttribute.IssueDescription,
			                 new TextFieldDefintion(useDbfFieldNames
				                                        ? "Descript"
				                                        : "Description",
			                                        4000,
			                                        IssueTableFieldAliases.IssueDescription));

			yield return Map(IssueAttribute.IssueCode,
			                 new TextFieldDefintion("Code",
			                                        255, IssueTableFieldAliases.IssueCode));

			yield return Map(IssueAttribute.IssueCodeDescription,
			                 new TextFieldDefintion(
				                 useDbfFieldNames
					                 ? "CodeDesc"
					                 : "CodeDescription",
				                 500, IssueTableFieldAliases.IssueCodeDescription));

			yield return Map(IssueAttribute.InvolvedObjects,
			                 new TextFieldDefintion(
				                 useDbfFieldNames
					                 ? "Objects"
					                 : "InvolvedObjects",
				                 4000, IssueTableFieldAliases.InvolvedObjects));

			yield return Map(IssueAttribute.QualityConditionName,
			                 new TextFieldDefintion(
				                 useDbfFieldNames
					                 ? "Condition"
					                 : "QualityCondition",
				                 255, IssueTableFieldAliases.QualityCondition));

			yield return Map(IssueAttribute.TestName,
			                 new TextFieldDefintion("TestName", 255,
			                                        IssueTableFieldAliases.TestName));

			yield return Map(IssueAttribute.TestDescription,
			                 new TextFieldDefintion(
				                 useDbfFieldNames
					                 ? "TestDesc"
					                 : "TestDescription",
				                 4000, IssueTableFieldAliases.TestDescription));

			yield return Map(IssueAttribute.TestType,
			                 new TextFieldDefintion("TestType",
			                                        255, IssueTableFieldAliases.TestType));

			yield return Map(IssueAttribute.IssueSeverity,
			                 new TextFieldDefintion("IssueType",
			                                        20, IssueTableFieldAliases.IssueSeverity));

			yield return Map(IssueAttribute.IsStopCondition,
			                 new TextFieldDefintion(
				                 useDbfFieldNames
					                 ? "StopCond"
					                 : "StopCondition",
				                 20, IssueTableFieldAliases.IsStopCondition));

			yield return Map(IssueAttribute.Category,
			                 new TextFieldDefintion("Category", 1000,
			                                        IssueTableFieldAliases.Category));

			yield return Map(IssueAttribute.AffectedComponent,
			                 new TextFieldDefintion(
				                 useDbfFieldNames
					                 ? "Component"
					                 : "AffectedComponent",
				                 255, IssueTableFieldAliases.AffectedComponent));

			yield return Map(IssueAttribute.Url,
			                 new TextFieldDefintion("Url", 1000, IssueTableFieldAliases.Url));

			yield return Map(IssueAttribute.DoubleValue1,
			                 new DoubleFieldDefinition("DblValue1",
			                                           IssueTableFieldAliases.NumericValue1));

			yield return Map(IssueAttribute.DoubleValue2,
			                 new DoubleFieldDefinition("DblValue2",
			                                           IssueTableFieldAliases.NumericValue2));

			yield return Map(IssueAttribute.TextValue,
			                 new TextFieldDefintion("TextValue", 255,
			                                        IssueTableFieldAliases.TextValue));

			yield return Map(IssueAttribute.IssueAssignment,
			                 new TextFieldDefintion("IssueAssignment", 255,
			                                        IssueTableFieldAliases.IssueAssignment));

			if (AddStatusField)
			{
				yield return Map(
					IssueAttribute.CorrectionStatus,
					new IntegerFieldDefinition("Status")
					{
						Domain = GetCorrectionStatusDomain()
					});
			}

			if (! addExceptionFields)
			{
				yield break;
			}

			yield return Map(IssueAttribute.QualityConditionUuid,
			                 new TextFieldDefintion(
				                 useDbfFieldNames
					                 ? "CondUuid"
					                 : "QualityConditionUuid",
				                 36, IssueTableFieldAliases.QualityConditionUuid));

			yield return Map(IssueAttribute.QualityConditionVersionUuid,
			                 new TextFieldDefintion(
				                 useDbfFieldNames
					                 ? "ConVerUuid"
					                 : "QualityConditionVersionUuid",
				                 36, IssueTableFieldAliases.QualityConditionVersionUuid));

			yield return Map(IssueAttribute.ExceptionStatus,
			                 new TextFieldDefintion(
				                 useDbfFieldNames
					                 ? "ExStatus"
					                 : "ExceptionStatus",
				                 15, IssueTableFieldAliases.ExceptionStatus));

			yield return Map(IssueAttribute.ExceptionNotes,
			                 new TextFieldDefintion(
				                 useDbfFieldNames
					                 ? "ExNotes"
					                 : "ExceptionNotes",
				                 4000, IssueTableFieldAliases.ExceptionNotes));

			yield return Map(IssueAttribute.ExceptionCategory,
			                 new TextFieldDefintion(
				                 useDbfFieldNames
					                 ? "ExCategory"
					                 : "ExceptionCategory",
				                 60, IssueTableFieldAliases.ExceptionCategory));

			yield return Map(IssueAttribute.ExceptionOrigin,
			                 new TextFieldDefintion(
				                 useDbfFieldNames
					                 ? "ExOrigin"
					                 : "ExceptionOrigin",
				                 255, IssueTableFieldAliases.ExceptionOrigin));

			yield return Map(IssueAttribute.ExceptionDefinitionDate,
			                 new DateFieldDefinition(
				                 useDbfFieldNames
					                 ? "ExDefDate"
					                 : "ExceptionDefinedDate",
				                 IssueTableFieldAliases.ExceptionDefinitionDate));

			yield return Map(IssueAttribute.ExceptionLastRevisionDate,
			                 new DateFieldDefinition(
				                 useDbfFieldNames
					                 ? "ExRevDate"
					                 : "ExceptionLastRevisionDate",
				                 IssueTableFieldAliases.ExceptionLastRevisionDate));

			yield return Map(IssueAttribute.ExceptionRetirementDate,
			                 new DateFieldDefinition(
				                 useDbfFieldNames
					                 ? "ExRetDate"
					                 : "ExceptionRetirementDate",
				                 IssueTableFieldAliases.ExceptionRetirementDate));

			yield return Map(IssueAttribute.ExceptionShapeMatchCriterion,
			                 new TextFieldDefintion(
				                 useDbfFieldNames
					                 ? "ExShpMatch"
					                 : "ExceptionShapeMatchCriterion",
				                 15, IssueTableFieldAliases.ExceptionShapeMatchCriterion));

			if (! addManagedExceptionFields)
			{
				yield break;
			}

			yield return Map(IssueAttribute.ManagedExceptionLineageUuid,
			                 new TextFieldDefintion(
				                 useDbfFieldNames
					                 ? "ExLinUuid"
					                 : "ExceptionLineageUuid",
				                 36, IssueTableFieldAliases.ExceptionLineageUuid));

			yield return Map(IssueAttribute.ManagedExceptionOrigin,
			                 new TextFieldDefintion(
				                 useDbfFieldNames
					                 ? "ExImpOrig"
					                 : "ExceptionImportOrigin",
				                 500,
				                 IssueTableFieldAliases.ExceptionImportOrigin));

			yield return Map(IssueAttribute.ManagedExceptionVersionBeginDate,
			                 new DateFieldDefinition(
				                 useDbfFieldNames
					                 ? "ExVrBegDat"
					                 : "ExceptionVersionBeginDate",
				                 IssueTableFieldAliases
					                 .ExceptionVersionBeginDate));

			yield return Map(IssueAttribute.ManagedExceptionVersionEndDate,
			                 new DateFieldDefinition(
				                 useDbfFieldNames
					                 ? "ExVrEndDat"
					                 : "ExceptionVersionEndDate",
				                 IssueTableFieldAliases.ExceptionVersionEndDate));

			yield return Map(IssueAttribute.ManagedExceptionVersionUuid,
			                 new TextFieldDefintion(
				                 useDbfFieldNames
					                 ? "ExVrUuid"
					                 : "ExceptionVersionUuid",
				                 36, IssueTableFieldAliases.ExceptionVersionUuid));

			yield return Map(IssueAttribute.ManagedExceptionVersionOrigin,
			                 new TextFieldDefintion(
				                 useDbfFieldNames
					                 ? "ExVrImOrig"
					                 : "ExceptionVersionImportOrigin",
				                 255,
				                 IssueTableFieldAliases.ExceptionVersionImportOrigin));

			yield return Map(IssueAttribute.ManagedExceptionVersionImportStatus,
			                 new TextFieldDefintion(
				                 useDbfFieldNames
					                 ? "ExVrImStat"
					                 : "ExceptionVersionImportStatus",
				                 500,
				                 IssueTableFieldAliases.ExceptionVersionImportStatus));

			if (! addExportedExceptionFields)
			{
				yield return Map(IssueAttribute.ExportedExceptionUsageCount,
				                 new IntegerFieldDefinition(
					                 useDbfFieldNames
						                 ? "ExUsageCt"
						                 : "ExceptionUsageCount",
					                 IssueTableFieldAliases.ExportedExceptionUsageCount));

				yield return Map(IssueAttribute.ExportedExceptionObjectId,
				                 new IntegerFieldDefinition(
					                 useDbfFieldNames
						                 ? "ExOID"
						                 : "ExceptionOID",
					                 IssueTableFieldAliases.ExportedExceptionOID));
			}
		}

		/// <summary>
		/// Shorthand method for creating an attribute -> field definition assignment tuple
		/// </summary>
		/// <param name="attribute"></param>
		/// <param name="definition"></param>
		/// <returns></returns>
		[NotNull]
		private static IssueTableFields.Field Map(IssueAttribute attribute,
		                                          [NotNull] FieldDefinition definition)
		{
			return new IssueTableFields.Field(attribute, definition);
		}

		private static CodedValueDomainDefinition GetCorrectionStatusDomain()
		{
			return new CodedValueDomainDefinition(
				_correctionStatusDomainName,
				esriFieldType.esriFieldTypeInteger,
				new List<CodedValue>
				{
					new CodedValue((int) IssueCorrectionStatus.NotCorrected,
					               "Not Corrected"),
					new CodedValue((int) IssueCorrectionStatus.Corrected, "Corrected")
				});
		}
	}
}

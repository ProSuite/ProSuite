using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainServices.AO.Properties;
using ProSuite.DomainServices.AO.QA.Issues;
using ProSuite.QA.Container;

namespace ProSuite.DomainServices.AO.QA
{
	public class IssueStatisticsWriter
	{
		private readonly IFeatureWorkspace _featureWorkspace;
		private readonly IIssueStatisticsTableFieldNames _fieldNames;

		public IssueStatisticsWriter([NotNull] IFeatureWorkspace featureWorkspace)
		{
			Assert.ArgumentNotNull(featureWorkspace, nameof(featureWorkspace));

			_featureWorkspace = featureWorkspace;
			_fieldNames = GetFieldNames(featureWorkspace);
		}

		[NotNull]
		public IIssueStatisticsTable WriteStatistics(
			[NotNull] IssueStatistics issueStatistics)
		{
			Assert.ArgumentNotNull(issueStatistics, nameof(issueStatistics));

			// create table
			ITable table = CreateTable("IssueStatistics", _featureWorkspace, _fieldNames);
			DatasetUtils.TrySetAliasName(table, LocalizableStrings.IssueStatisticsTableName);

			// write rows
			WriteRows(table, issueStatistics, _fieldNames);

			return new IssueStatisticsTable(table, _fieldNames);
		}

		private static void WriteRows([NotNull] ITable table,
		                              [NotNull] IssueStatistics issueStatistics,
		                              [NotNull] IIssueStatisticsTableFieldNames fieldNames)
		{
			// NOTE: Avoid locks due to buffering!
			const bool useBuffering = false;
			ICursor insertCursor = table.Insert(useBuffering);

			IRowBuffer rowBuffer = table.CreateRowBuffer();

			var attributeWriter = new AttributeWriter(table, fieldNames);

			foreach (IssueGroup issueGroup in issueStatistics.GetIssueGroups())
			{
				attributeWriter.Write(issueGroup, rowBuffer);

				insertCursor.InsertRow(rowBuffer);
			}

			insertCursor.Flush();
		}

		[CanBeNull]
		private static IIssueStatisticsTableFieldNames GetFieldNames(
			IFeatureWorkspace featureWorkspace)
		{
			var workspace = (IWorkspace) featureWorkspace;

			if (WorkspaceUtils.IsFileGeodatabase(workspace))
			{
				return IssueStatisticsTableFieldNamesFactory.GetFileGdbTableFieldNames();
			}

			if (WorkspaceUtils.IsShapefileWorkspace(workspace))
			{
				return IssueStatisticsTableFieldNamesFactory.GetDbfTableFieldNames();
			}

			throw new ArgumentException(
				string.Format("Unsupported workspace for writing issue statistics: {0}",
				              WorkspaceUtils.GetConnectionString(workspace, true)));
		}

		[NotNull]
		private static ITable CreateTable(
			[NotNull] string name,
			[NotNull] IFeatureWorkspace workspace,
			[NotNull] IIssueStatisticsTableFieldNames fieldNames)
		{
			return DatasetUtils.CreateTable(workspace, name, null,
			                                FieldUtils.CreateFields(
				                                CreateAttributeFields(fieldNames)));
		}

		[NotNull]
		private static IEnumerable<IField> CreateAttributeFields(
			[NotNull] IIssueStatisticsTableFieldNames fieldNames)
		{
			yield return FieldUtils.CreateOIDField();
			yield return FieldUtils.CreateTextField(fieldNames.IssueDescriptionField, 4000);
			yield return FieldUtils.CreateTextField(fieldNames.IssueCodeField, 255);
			yield return FieldUtils.CreateTextField(fieldNames.IssueCodeDescriptionField, 500);
			yield return FieldUtils.CreateTextField(fieldNames.QualityConditionField, 255);
			yield return
				FieldUtils.CreateTextField(fieldNames.QualityConditionDescriptionField, 4000);
			yield return FieldUtils.CreateTextField(fieldNames.TestNameField, 255);
			yield return FieldUtils.CreateTextField(fieldNames.TestDescriptionField, 4000);
			yield return FieldUtils.CreateTextField(fieldNames.TestTypeField, 255);
			yield return FieldUtils.CreateTextField(fieldNames.IssueTypeField, 20);
			yield return FieldUtils.CreateTextField(fieldNames.StopConditionField, 20);
			yield return FieldUtils.CreateTextField(fieldNames.CategoriesField, 1000);
			yield return FieldUtils.CreateTextField(fieldNames.AffectedComponentField, 255);
			yield return FieldUtils.CreateTextField(fieldNames.UrlField, 1000);
			yield return FieldUtils.CreateIntegerField(fieldNames.IssueCountField);
		}

		private class AttributeWriter : AttributeWriterBase
		{
			private readonly int _issueDescriptionFieldIndex;
			private readonly int _issueCodeFieldIndex;
			private readonly int _issueCodeDescriptionFieldIndex;
			private readonly int _qualityConditionFieldIndex;
			private readonly int _qualityConditionDescriptionFieldIndex;
			private readonly int _testNameFieldIndex;
			private readonly int _testDescriptionFieldIndex;
			private readonly int _testTypeFieldIndex;
			private readonly int _issueTypeFieldIndex;
			private readonly int _stopConditionFieldIndex;
			private readonly int _categoriesFieldIndex;
			private readonly int _affectedComponentFieldIndex;
			private readonly int _urlFieldIndex;
			private readonly int _issueCountFieldIndex;

			public AttributeWriter([NotNull] ITable table,
			                       [NotNull] IIssueStatisticsTableFieldNames fields)
				: base(table)
			{
				_issueDescriptionFieldIndex = Find(table, fields.IssueDescriptionField);
				_issueCodeFieldIndex = Find(table, fields.IssueCodeField);
				_issueCodeDescriptionFieldIndex = Find(table, fields.IssueCodeDescriptionField);
				_qualityConditionFieldIndex = Find(table, fields.QualityConditionField);
				_qualityConditionDescriptionFieldIndex =
					Find(table, fields.QualityConditionDescriptionField);
				_testTypeFieldIndex = Find(table, fields.TestTypeField);
				_testNameFieldIndex = Find(table, fields.TestNameField);
				_testDescriptionFieldIndex = Find(table, fields.TestDescriptionField);
				_issueTypeFieldIndex = Find(table, fields.IssueTypeField);
				_stopConditionFieldIndex = Find(table, fields.StopConditionField);
				_categoriesFieldIndex = Find(table, fields.CategoriesField);
				_affectedComponentFieldIndex = Find(table, fields.AffectedComponentField);
				_urlFieldIndex = Find(table, fields.UrlField);
				_issueCountFieldIndex = Find(table, fields.IssueCountField);
			}

			public void Write([NotNull] IssueGroup issueGroup, [NotNull] IRowBuffer rowBuffer)
			{
				Assert.ArgumentNotNull(issueGroup, nameof(issueGroup));
				Assert.ArgumentNotNull(rowBuffer, nameof(rowBuffer));

				IssueCode issueCode = issueGroup.IssueCode;

				WriteText(rowBuffer, _issueDescriptionFieldIndex, issueGroup.IssueDescription);
				WriteText(rowBuffer, _issueCodeFieldIndex, issueCode?.ID);
				WriteText(rowBuffer, _issueCodeDescriptionFieldIndex, issueCode?.Description);
				WriteText(rowBuffer, _qualityConditionFieldIndex,
				          issueGroup.QualityCondition.Name);
				WriteText(rowBuffer, _qualityConditionDescriptionFieldIndex,
				          issueGroup.QualityCondition.Description,
				          warnIfTooLong: false);
				WriteText(rowBuffer, _testTypeFieldIndex,
				          GetTestTypeName(issueGroup.QualityCondition));
				WriteText(rowBuffer, _testNameFieldIndex,
				          GetTestName(issueGroup.QualityCondition));
				WriteText(rowBuffer, _testDescriptionFieldIndex,
				          GetTestDescription(issueGroup.QualityCondition));
				WriteText(rowBuffer, _issueTypeFieldIndex, GetIssueTypeValue(issueGroup));
				WriteText(rowBuffer, _stopConditionFieldIndex, issueGroup.StopCondition
					                                               ? "Yes"
					                                               : "No");
				WriteText(rowBuffer, _categoriesFieldIndex,
				          GetCategoryValue(issueGroup.QualityCondition));
				WriteText(rowBuffer, _affectedComponentFieldIndex, issueGroup.AffectedComponent);
				WriteText(rowBuffer, _urlFieldIndex, GetUrl(issueGroup.QualityCondition));

				rowBuffer.set_Value(_issueCountFieldIndex, issueGroup.IssueCount);
			}

			[NotNull]
			private static string GetIssueTypeValue([NotNull] IssueGroup issueGroup)
			{
				return issueGroup.Allowable
					       ? "Warning"
					       : "Error";
			}
		}
	}
}

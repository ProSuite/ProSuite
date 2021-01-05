using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;

namespace ProSuite.DomainServices.AO.QA.Issues
{
	public class IssueAttributeWriter : AttributeWriterBase, IIssueAttributeWriter
	{
		private readonly int _issueDescriptionFieldIndex;
		private readonly int _issueCodeFieldIndex;
		private readonly int _issueCodeDescriptionFieldIndex;
		private readonly int _involvedObjectsFieldIndex;
		private readonly int _qualityConditionFieldIndex;
		private readonly int _testNameFieldIndex;
		private readonly int _testDescriptionFieldIndex;
		private readonly int _testTypeFieldIndex;
		private readonly int _issueSeverityFieldIndex;
		private readonly int _stopConditionFieldIndex;
		private readonly int _categoryFieldIndex;
		private readonly int _affectedComponentFieldIndex;
		private readonly int _urlFieldIndex;

		// optional fields
		private readonly int _qualityConditionUuidFieldIndex;

		private readonly int _qualityConditionVersionUuidFieldIndex;
		private readonly int _doubleValue1FieldIndex;
		private readonly int _doubleValue2FieldIndex;
		private readonly int _textValueFieldIndex;

		#region Constructors

		[CLSCompliant(false)]
		public IssueAttributeWriter([NotNull] ITable table,
		                            [NotNull] IIssueTableFields fields) : base(table)
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNull(fields, nameof(fields));

			_issueDescriptionFieldIndex = fields.GetIndex(IssueAttribute.IssueDescription,
			                                              table);
			_issueCodeFieldIndex = fields.GetIndex(IssueAttribute.IssueCode, table);
			_issueCodeDescriptionFieldIndex =
				fields.GetIndex(IssueAttribute.IssueCodeDescription, table);
			_involvedObjectsFieldIndex =
				fields.GetIndex(IssueAttribute.InvolvedObjects, table);
			_qualityConditionFieldIndex = fields.GetIndex(IssueAttribute.QualityConditionName,
			                                              table);
			_testTypeFieldIndex = fields.GetIndex(IssueAttribute.TestType, table);
			_testNameFieldIndex = fields.GetIndex(IssueAttribute.TestName, table);
			_testDescriptionFieldIndex =
				fields.GetIndex(IssueAttribute.TestDescription, table);
			_issueSeverityFieldIndex = fields.GetIndex(IssueAttribute.IssueSeverity, table);
			_stopConditionFieldIndex = fields.GetIndex(IssueAttribute.IsStopCondition, table);
			_categoryFieldIndex = fields.GetIndex(IssueAttribute.Category, table);
			_affectedComponentFieldIndex = fields.GetIndex(IssueAttribute.AffectedComponent,
			                                               table);
			_urlFieldIndex = fields.GetIndex(IssueAttribute.Url, table);

			// optional fields
			_doubleValue1FieldIndex = fields.GetIndex(
				IssueAttribute.DoubleValue1,
				table, optional: true);
			_doubleValue2FieldIndex = fields.GetIndex(
				IssueAttribute.DoubleValue2,
				table, optional: true);
			_textValueFieldIndex = fields.GetIndex(
				IssueAttribute.TextValue,
				table, optional: true);

			_qualityConditionUuidFieldIndex = fields.GetIndex(
				IssueAttribute.QualityConditionUuid,
				table, optional: true);
			_qualityConditionVersionUuidFieldIndex = fields.GetIndex(
				IssueAttribute.QualityConditionVersionUuid,
				table, optional: true);
		}

		#endregion

		[CLSCompliant(false)]
		public void WriteAttributes(Issue issue, IRowBuffer rowBuffer)
		{
			Assert.ArgumentNotNull(issue, nameof(issue));
			Assert.ArgumentNotNull(rowBuffer, nameof(rowBuffer));

			IssueCode issueCode = issue.IssueCode;

			WriteText(rowBuffer, _issueDescriptionFieldIndex, issue.Description);
			WriteText(rowBuffer, _issueCodeFieldIndex, issueCode?.ID);
			WriteText(rowBuffer, _issueCodeDescriptionFieldIndex, issueCode?.Description);
			WriteText(rowBuffer, _involvedObjectsFieldIndex,
			          IssueUtils.FormatInvolvedTables(issue.InvolvedTables));
			WriteText(rowBuffer, _qualityConditionFieldIndex, issue.QualityCondition.Name);

			WriteText(rowBuffer, _testTypeFieldIndex, GetTestTypeName(issue.QualityCondition));
			WriteText(rowBuffer, _testNameFieldIndex, GetTestName(issue.QualityCondition));
			WriteText(rowBuffer, _testDescriptionFieldIndex,
			          GetTestDescription(issue.QualityCondition));

			WriteText(rowBuffer, _issueSeverityFieldIndex, GetIssueSeverityValue(issue));
			WriteText(rowBuffer, _stopConditionFieldIndex, GetStopConditionValue(issue));
			WriteText(rowBuffer, _categoryFieldIndex,
			          GetCategoryValue(issue.QualityCondition));
			WriteText(rowBuffer, _affectedComponentFieldIndex, GetAffectedComponent(issue));
			WriteText(rowBuffer, _urlFieldIndex, GetUrl(issue.QualityCondition));

			if (_qualityConditionUuidFieldIndex >= 0)
			{
				WriteText(rowBuffer, _qualityConditionUuidFieldIndex,
				          issue.QualityCondition.Uuid);
			}

			if (_qualityConditionVersionUuidFieldIndex >= 0)
			{
				WriteText(rowBuffer, _qualityConditionVersionUuidFieldIndex,
				          issue.QualityCondition.VersionUuid);
			}

			WriteValues(rowBuffer, issue);
		}

		private void WriteValues([NotNull] IRowBuffer rowBuffer, [NotNull] Issue issue)
		{
			if (_doubleValue1FieldIndex < 0 &&
			    _doubleValue2FieldIndex < 0 &&
			    _textValueFieldIndex < 0)
			{
				// there are no error attribute fields
				return;
			}

			double? doubleValue1;
			double? doubleValue2;
			string textValue;
			IssueUtils.GetValues(issue.Values,
			                     out doubleValue1,
			                     out doubleValue2,
			                     out textValue);

			if (_doubleValue1FieldIndex >= 0)
			{
				WriteDouble(rowBuffer, _doubleValue1FieldIndex, doubleValue1);
			}

			if (_doubleValue2FieldIndex >= 0)
			{
				WriteDouble(rowBuffer, _doubleValue2FieldIndex, doubleValue2);
			}

			if (_textValueFieldIndex >= 0)
			{
				WriteText(rowBuffer, _textValueFieldIndex, textValue);
			}
		}

		[NotNull]
		private static string GetAffectedComponent([NotNull] Issue issue)
		{
			return issue.AffectedComponent ?? string.Empty;
		}

		[NotNull]
		private static string GetStopConditionValue([NotNull] Issue issue)
		{
			return issue.StopCondition
				       ? "Yes"
				       : "No";
		}

		[NotNull]
		private static string GetIssueSeverityValue([NotNull] Issue issue)
		{
			return issue.Allowable
				       ? "Warning"
				       : "Error";
		}
	}
}

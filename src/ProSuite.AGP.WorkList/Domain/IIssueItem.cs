using System.Collections.Generic;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Domain;

public interface IIssueItem : IWorkItem
{
	string ExceptionRetirementDate { get; set; }
	string ExceptionLastRevisionDate { get; set; }
	string ExceptionDefinedDate { get; set; }
	string ExceptionOrigin { get; set; }
	string ExceptionCategory { get; set; }
	string ExceptionNotes { get; set; }
	string ExceptionStatus { get; set; }
	string QualityConditionVersionUuid { get; set; }
	string QualityConditionUuid { get; set; }
	string IssueAssignment { get; set; }
	string TextValue { get; set; }
	string ExceptionShapeMatchCriterion { get; set; }
	double? DoubleValue2 { get; set; }
	double? DoubleValue1 { get; set; }
	string Url { get; set; }
	string AffectedComponent { get; set; }
	string Category { get; set; }
	string StopCondition { get; set; }
	string TestType { get; set; }
	string TestDescription { get; set; }
	string TestName { get; set; }
	string QualityCondition { get; set; }
	string IssueSeverity { get; set; }
	string InvolvedObjects { get; set; }
	string IssueCode { get; set; }
	string IssueCodeDescription { get; set; }
	string IssueDescription { get; set; }

	[CanBeNull]
	IList<InvolvedTable> InvolvedTables { get; set; }
}

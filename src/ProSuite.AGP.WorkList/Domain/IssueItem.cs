using System.Collections.Generic;
using ArcGIS.Core.Data;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Domain
{
	public class IssueItem : WorkItem, IIssueItem
	{
		private string _issueCodeDescription;

		// todo daro: use Factory Method on abstract ISourceClass base to create item
		//			  instead of passing in reader
		public IssueItem(long objectId, long uniqueTableId, [NotNull] Row row)
			: base(uniqueTableId, row) { }

		public string ExceptionRetirementDate { get; set; }

		public string ExceptionLastRevisionDate { get; set; }

		public string ExceptionDefinedDate { get; set; }

		public string ExceptionOrigin { get; set; }

		public string ExceptionCategory { get; set; }

		public string ExceptionNotes { get; set; }

		public string ExceptionStatus { get; set; }

		public string QualityConditionVersionUuid { get; set; }

		public string QualityConditionUuid { get; set; }

		public string IssueAssignment { get; set; }

		public string TextValue { get; set; }

		public string ExceptionShapeMatchCriterion { get; set; }

		public double? DoubleValue2 { get; set; }

		public double? DoubleValue1 { get; set; }

		public string Url { get; set; }

		public string AffectedComponent { get; set; }

		public string Category { get; set; }

		public string StopCondition { get; set; }

		public string TestType { get; set; }

		public string TestDescription { get; set; }

		public string TestName { get; set; }

		public string QualityCondition { get; set; }

		public string IssueSeverity { get; set; }

		public string InvolvedObjects { get; set; }

		public string IssueCode { get; set; }

		public string IssueCodeDescription
		{
			get => _issueCodeDescription;
			set
			{
				_issueCodeDescription = value;
				OnPropertyChanged();
			}
		}

		public string IssueDescription
		{
			get => _issueCodeDescription;
			set
			{
				_issueCodeDescription = value;
				OnPropertyChanged();
			}
		}

		public IList<InvolvedTable> InvolvedTables { get; set; }
	}
}

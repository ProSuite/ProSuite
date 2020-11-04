using System.Collections.Generic;
using ArcGIS.Core.Data;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Domain
{
	// todo daro: find correct folder and namespace for this class
	public class IssueItem : WorkItem
	{
		private string _issueCodeDescription;

		// todo daro: use Factory Method on abstract ISourceClass base to create item
		//			  instead of passing in reader
		public IssueItem(int id, [NotNull] Row row, IAttributeReader reader) : base(id, row)
		{
			IssueCode = reader.GetValue<string>(row, Attributes.IssueCode);
			IssueCodeDescription = reader.GetValue<string>(row, Attributes.IssueCodeDescription);
			InvolvedObjects = reader.GetValue<string>(row, Attributes.InvolvedObjects);
			QualityCondition = reader.GetValue<string>(row, Attributes.QualityConditionName);
			TestName = reader.GetValue<string>(row, Attributes.TestName);
			TestDescription = reader.GetValue<string>(row, Attributes.TestDescription);
			TestType = reader.GetValue<string>(row, Attributes.TestType);
			IssueSeverity = reader.GetValue<string>(row, Attributes.IssueSeverity);
			StopCondition = reader.GetValue<string>(row, Attributes.IsStopCondition);
			Category = reader.GetValue<string>(row, Attributes.Category);
			AffectedComponent = reader.GetValue<string>(row, Attributes.AffectedComponent);
			Url = reader.GetValue<string>(row, Attributes.Url);
			DoubleValue1 = reader.GetValue<double?>(row, Attributes.DoubleValue1);
			DoubleValue2 = reader.GetValue<double?>(row, Attributes.DoubleValue2);
			TextValue = reader.GetValue<string>(row, Attributes.TextValue);
			IssueAssignment = reader.GetValue<string>(row, Attributes.IssueAssignment);
			QualityConditionUuid = reader.GetValue<string>(row, Attributes.QualityConditionUuid);
			QualityConditionVersionUuid = reader.GetValue<string>(row, Attributes.QualityConditionVersionUuid);
			ExceptionStatus = reader.GetValue<string>(row, Attributes.ExceptionStatus);
			ExceptionNotes = reader.GetValue<string>(row, Attributes.ExceptionNotes);
			ExceptionCategory = reader.GetValue<string>(row, Attributes.ExceptionCategory);
			ExceptionOrigin = reader.GetValue<string>(row, Attributes.ExceptionOrigin);
			ExceptionDefinedDate = reader.GetValue<string>(row, Attributes.ExceptionDefinedDate);
			ExceptionLastRevisionDate = reader.GetValue<string>(row, Attributes.ExceptionLastRevisionDate);
			ExceptionRetirementDate = reader.GetValue<string>(row, Attributes.ExceptionRetirementDate);
			ExceptionShapeMatchCriterion = reader.GetValue<string>(row, Attributes.ExceptionShapeMatchCriterion);

			InIssueInvolvedTables = IssueUtils.ParseInvolvedTables(InvolvedObjects);
		}

		// todo daro: format
		public string ExceptionRetirementDate { get; }

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

		public IList<InvolvedTable> InIssueInvolvedTables { get; set; }
	}
}

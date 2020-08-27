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

		public IssueItem(int id, [NotNull] Row row, IAttributeReader reader) : base(id, row)
		{
			ObjectID = reader.GetValue<int>(row, Attributes.ObjectID);

			IssueCodeDescription = reader.GetValue<string>(row, Attributes.IssueCodeDescription);
			QualityCondition = reader.GetValue<string>(row, Attributes.QualityConditionName);
			InvolvedObjects = reader.GetValue<string>(row, Attributes.InvolvedObjects);
			IssueSeverity = reader.GetValue<string>(row, Attributes.IssueSeverity);
			IssueCode = reader.GetValue<string>(row, Attributes.IssueCode);
			InIssueInvolvedTables = IssueUtils.ParseInvolvedTables(InvolvedObjects);
		}

		public string QualityCondition { get; set; }

		public string IssueSeverity { get; set; }

		public string InvolvedObjects { get; set; }

		public string IssueCode { get; set; }

		public int ObjectID { get; set; }

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

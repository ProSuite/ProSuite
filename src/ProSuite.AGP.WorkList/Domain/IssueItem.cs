using ArcGIS.Core.Data;
using ProSuite.AGP.WorkList.Contracts;

namespace ProSuite.AGP.WorkList.Domain
{
	// todo daro: find correct folder and namespace for this class
	public class IssueItem : WorkItem
	{
		private string _issueCodeDescription;

		public IssueItem(int id, Row row,
		                 IAttributeReader reader,
		                 double extentExpansionFactor = 1.1,
		                 double minimumSizeDegrees = 15,
		                 double minimumSizeProjected = 0.001) : base(
			id, row, extentExpansionFactor, minimumSizeDegrees, minimumSizeProjected)
		{
			ObjectID = reader.GetValue<int>(row, Attributes.ObjectID);
			IssueCodeDescription = reader.GetValue<string>(row, Attributes.IssueCodeDescription);
		}

		public int ObjectID { get; }

		public string IssueCodeDescription
		{
			get => _issueCodeDescription;
			set
			{
				_issueCodeDescription = value;
				OnPropertyChanged();
			}
		}
	}
}

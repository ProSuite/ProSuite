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

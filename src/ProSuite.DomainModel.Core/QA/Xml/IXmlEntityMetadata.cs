using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.QA.Xml
{
	public interface IXmlEntityMetadata
	{
		[CanBeNull]
		string CreatedDate { get; set; }

		[CanBeNull]
		string CreatedByUser { get; set; }

		[CanBeNull]
		string LastChangedDate { get; set; }

		[CanBeNull]
		string LastChangedByUser { get; set; }
	}
}

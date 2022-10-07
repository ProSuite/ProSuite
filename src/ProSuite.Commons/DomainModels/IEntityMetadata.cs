using System;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.DomainModels
{
	public interface IEntityMetadata
	{
		DateTime? CreatedDate { get; set; }

		[CanBeNull]
		string CreatedByUser { get; set; }

		DateTime? LastChangedDate { get; set; }

		[CanBeNull]
		string LastChangedByUser { get; set; }
	}
}

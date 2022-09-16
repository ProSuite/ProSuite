using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.QA.Repositories
{
	[UsedImplicitly]
	public class ReferenceCount
	{
		public int EntityId { get; set; }
		public int UsageCount { get; set; }
	}
}

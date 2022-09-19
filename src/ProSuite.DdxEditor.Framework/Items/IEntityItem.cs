using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DdxEditor.Framework.Items
{
	public interface IEntityItem
	{
		bool IsBasedOn([NotNull] Entity entity);

		int EntityId { get; }
	}
}

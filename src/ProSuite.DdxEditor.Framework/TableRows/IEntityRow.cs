using ProSuite.Commons.DomainModels;

namespace ProSuite.DdxEditor.Framework.TableRows
{
	public interface IEntityRow
	{
		Entity Entity { get; }
	}

	public interface IEntityRow<T> where T : Entity
	{
		T Entity { get; }
	}
}

namespace ProSuite.Commons.DomainModels
{
	public interface IEntity
	{
		int Id { get; }

		bool IsPersistent { get; }
	}
}

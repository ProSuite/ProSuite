namespace ProSuite.Commons.DomainModels
{
	public interface IVersionedEntity : IEntity
	{
		int Version { get; }
	}
}

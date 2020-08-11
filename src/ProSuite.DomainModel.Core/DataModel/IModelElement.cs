namespace ProSuite.DomainModel.Core.DataModel
{
	public interface IModelElement : IRegisteredGdbObject
	{
		string DisplayName { get; }

		DdxModel Model { get; }

		string Name { get; set; }

		string Description { get; set; }

		string UnqualifiedName { get; }

		string GetNameWithoutCatalog();
	}
}
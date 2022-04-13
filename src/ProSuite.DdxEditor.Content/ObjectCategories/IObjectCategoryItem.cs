namespace ProSuite.DdxEditor.Content.ObjectCategories
{
	public interface IObjectCategoryItem
	{
		ObjectDataset ObjectDataset { get; }

		DdxModel Model { get; }
	}
}

using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.DataModel
{
	public interface IDdxDataset : IEntity, IModelElement
	{
		string Abbreviation { get; set; }

		/// <summary>
		/// Gets or sets the alias name of the dataset.
		/// </summary>
		/// <value>The alias name.</value>
		string AliasName { get; set; }

		[CanBeNull]
		DatasetCategory DatasetCategory { get; set; }

		GeometryType GeometryType { get; set; }

		[NotNull]
		string TypeDescription { get; }
	}
}
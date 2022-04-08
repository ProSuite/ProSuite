using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;

namespace ProSuite.DdxEditor.Content.SimpleTerrains
{
	public class AddSimpleTerrainDatasetCommand : AddItemCommandBase<SimpleTerrainDatasetsItem>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="AddSimpleTerrainDatasetCommand"/> class.
		/// </summary>
		/// <param name="simpleTerrainDatasetsItem">The simple terrain datasets item.</param>
		/// <param name="applicationController">The application controller.</param>
		public AddSimpleTerrainDatasetCommand(
			[NotNull] SimpleTerrainDatasetsItem simpleTerrainDatasetsItem,
			[NotNull] IApplicationController applicationController)
			: base(simpleTerrainDatasetsItem, applicationController) { }

		public override string Text => "Add Simple Terrain";

		protected override void ExecuteCore()
		{
			Item.AddSimpleTerrainDatasetItem();
		}
	}
}

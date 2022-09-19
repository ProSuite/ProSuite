using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;
using ProSuite.DdxEditor.Framework.Items;

namespace ProSuite.DdxEditor.Content.SimpleTerrains
{
	internal abstract class ExchangeSimpleTerrainDatasetsCommand<T> : ExchangeCommand<T>
		where T : Item
	{
		public const string DefaultExtension = "sf.xml";

		public const string FileFilter =
			"SF xml files (*.sf.xml)|*.sf.xml|Xml files (*.xml)|*.xml";

		protected ExchangeSimpleTerrainDatasetsCommand([NotNull] T item,
		                                               [NotNull] IApplicationController
			                                               applicationController)
			: base(item, applicationController, DefaultExtension, FileFilter) { }
	}
}

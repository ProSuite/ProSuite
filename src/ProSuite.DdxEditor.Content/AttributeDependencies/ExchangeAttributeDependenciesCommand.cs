using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;
using ProSuite.DdxEditor.Framework.Items;

namespace ProSuite.DdxEditor.Content.AttributeDependencies
{
	public abstract class ExchangeAttributeDependenciesCommand<T> : ExchangeCommand<T>
		where T : Item
	{
		public const string DefaultExtension = "ad.xml";

		public const string FileFilter =
			"AD xml files (*.ad.xml)|*.ad.xml|Xml files (*.xml)|*.xml";

		protected ExchangeAttributeDependenciesCommand([NotNull] T item,
		                                               [NotNull] IApplicationController
			                                               applicationController)
			: base(item, applicationController, DefaultExtension, FileFilter) { }
	}
}

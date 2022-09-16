using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;
using ProSuite.DdxEditor.Framework.Items;

namespace ProSuite.DdxEditor.Content.LinearNetworks
{
	internal abstract class ExchangeLinearNetworksCommand<T> : ExchangeCommand<T>
		where T : Item
	{
		public const string DefaultExtension = "ln.xml";

		public const string FileFilter =
			"LN xml files (*.ln.xml)|*.ln.xml|Xml files (*.xml)|*.xml";

		protected ExchangeLinearNetworksCommand([NotNull] T item,
		                                        [NotNull] IApplicationController
			                                        applicationController)
			: base(item, applicationController, DefaultExtension, FileFilter) { }
	}
}

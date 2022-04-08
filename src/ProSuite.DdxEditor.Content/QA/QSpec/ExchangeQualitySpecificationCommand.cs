using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;
using ProSuite.DdxEditor.Framework.Items;

namespace ProSuite.DdxEditor.Content.QA.QSpec
{
	public abstract class ExchangeQualitySpecificationCommand<T> : ExchangeCommand<T>
		where T : Item
	{
		private const string _defaultExtension = "qa.xml";

		private const string _fileFilter =
			"QA xml files (*.qa.xml)|*.qa.xml|Xml files (*.xml)|*.xml";

		protected ExchangeQualitySpecificationCommand(
			[NotNull] T item,
			[NotNull] IApplicationController applicationController)
			: base(item, applicationController, _defaultExtension, _fileFilter) { }

		[NotNull]
		protected string DefaultExtension => _defaultExtension;

		[NotNull]
		protected string FileFilter => _fileFilter;
	}
}

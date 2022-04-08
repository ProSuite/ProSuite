using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;
using ProSuite.DdxEditor.Framework.Items;

namespace ProSuite.DdxEditor.Content.QA.QSpec
{
	public abstract class ExportDatasetDependenciesCommandBase<T> : ExchangeCommand<T>
		where T : Item
	{
		private const string _defaultExtension = "graphml";
		private const string _fileFilter = "GraphML files (*.graphml)|*.graphml";

		protected ExportDatasetDependenciesCommandBase(
			[NotNull] T item,
			[NotNull] IApplicationController applicationController)
			: base(item, applicationController, _defaultExtension, _fileFilter) { }

		public override string Text => "Export Dataset Dependencies...";

		protected override bool EnabledCore => ! Item.IsDirty;

		[NotNull]
		protected string DefaultExtension => _defaultExtension;

		[NotNull]
		protected string FileFilter => _fileFilter;
	}
}

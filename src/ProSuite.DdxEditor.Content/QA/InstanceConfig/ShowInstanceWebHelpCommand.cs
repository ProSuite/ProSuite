using System.Drawing;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.QA.AlgorithmUI;
using ProSuite.DdxEditor.Content.QA.QCon;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DdxEditor.Framework.Properties;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DdxEditor.Content.QA.InstanceConfig
{
	public class ShowInstanceWebHelpCommand<T> : ItemCommandBase<T> where T : Item
	{
		private static readonly Image _image;

		/// <summary>
		/// Initializes the <see cref="ShowInstanceWebHelpCommand{T}"/> class.
		/// </summary>
		static ShowInstanceWebHelpCommand()
		{
			_image = Resources.ShowOnlineHelpCmd;
		}

		[CanBeNull] private readonly IInstanceDocumentationProvider _documentationProvider;

		public ShowInstanceWebHelpCommand(
			[NotNull] T item,
			[NotNull] IApplicationController applicationController,
			[CanBeNull] IInstanceDocumentationProvider documentationProvider = null)
			: base(item, applicationController)
		{
			_documentationProvider = documentationProvider;

			// This could be made more generic to support Html help of other entities.
			InstanceDescriptor descriptor = GetInstanceDescriptor(Item);
			if (descriptor is TransformerDescriptor)
			{
				Text = "Transformer Documentation";
			}
			else if (descriptor is IssueFilterDescriptor)
			{
				Text = "Issue Filter Documentation";
			}
			else
			{
				Text = "Test Documentation";
			}
		}

		#region Overrides of CommandBase

		public override Image Image => _image;

		public override string Text { get; }

		protected override void ExecuteCore()
		{
			InstanceDescriptor descriptor = GetInstanceDescriptor(Item);

			if (descriptor == null)
			{
				// No descriptor assigned yet (e.g. algorithm-first UI, before the first
				// save, with no algorithm chosen; or - in principle - a brand-new classic
				// condition/config before its descriptor is picked): show a friendly
				// notice instead.
				ApplicationController.ShowItemHelp(Text, NoDescriptorHtml);
				return;
			}

			string html = InstanceDocumentationPage.Render(
				_documentationProvider, descriptor, out string title);

			ApplicationController.ShowItemHelp(title, html);
		}

		[CanBeNull]
		private static InstanceDescriptor GetInstanceDescriptor([NotNull] Item item)
		{
			if (item is QualityConditionItem qualityConditionItem)
			{
				return qualityConditionItem.GetWebHelpDescriptor();
			}

			if (item is InstanceConfigurationItem instanceConfigItem)
			{
				return instanceConfigItem.GetWebHelpDescriptor();
			}

			return null;
		}

		/// <summary>
		/// Minimal, dark-mode-safe notice shown in place of the documentation when no
		/// descriptor is available (same explicit-colors pattern as
		/// HtmlReportBuilder.GetStyles / AlgorithmParametersControl.ShowParameterHelp, so
		/// the help pane never renders black-on-black under a dark UA color scheme).
		/// </summary>
		private const string NoDescriptorHtml =
			"<html><head><style>:root{color-scheme:light;} " +
			"body{background-color:#ffffff;color:#000000;" +
			"font-family:Verdana, Arial;}</style></head><body>" +
			"<p><i>No documentation available yet. Choose an algorithm " +
			"(or save the condition) first.</i></p></body></html>";

		#endregion
	}
}

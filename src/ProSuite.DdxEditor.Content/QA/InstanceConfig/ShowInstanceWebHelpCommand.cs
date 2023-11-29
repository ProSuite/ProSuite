using System;
using System.Drawing;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Content.QA.QCon;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DdxEditor.Framework.Properties;
using ProSuite.DomainModel.AO.QA.TestReport;
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

		public ShowInstanceWebHelpCommand([NotNull] T item,
		                                  [NotNull] IApplicationController applicationController)
			: base(item, applicationController)
		{
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
				throw new InvalidOperationException("No instance descriptor available.");
			}

			string title = descriptor.TypeDisplayName;
			string html = TestReportUtils.WriteDescriptorDoc(descriptor);
			ApplicationController.ShowItemHelp(title, html);
		}

		[CanBeNull]
		private static InstanceDescriptor GetInstanceDescriptor([NotNull] Item item)
		{
			InstanceConfiguration instanceConfiguration;
			if (item is QualityConditionItem qualityConditionItem)
			{
				instanceConfiguration = qualityConditionItem.GetEntity();
			}
			else if (item is InstanceConfigurationItem instanceConfigItem)
			{
				instanceConfiguration = instanceConfigItem.GetEntity();
			}
			else
			{
				return null;
			}

			InstanceDescriptor descriptor = instanceConfiguration?.InstanceDescriptor;

			return descriptor;
		}

		#endregion
	}
}

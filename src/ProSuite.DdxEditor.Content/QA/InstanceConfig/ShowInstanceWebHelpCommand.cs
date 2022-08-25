using System;
using System.Drawing;
using System.IO;
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
		/// Initializes the <see cref="CopyQualityConditionCommand"/> class.
		/// </summary>
		static ShowInstanceWebHelpCommand()
		{
			_image = Resources.ShowOnlineHelpCmd;
		}

		public IApplicationController ApplicationController { get; }

		public ShowInstanceWebHelpCommand([NotNull] T item,
		                                  IApplicationController applicationController)
			: base(item)
		{
			ApplicationController = applicationController;

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

			StringWriter stringWriter = new StringWriter();
			TestReportUtils.WriteDescriptorDoc(descriptor, stringWriter);

			ApplicationController.ShowHelpForm(descriptor.TypeDisplayName,
			                                   stringWriter.ToString());
		}

		private static InstanceDescriptor GetInstanceDescriptor(Item item)
		{
			var instanceConfigItem = item as InstanceConfigurationItem;

			if (instanceConfigItem == null)
			{
				return null;
			}

			InstanceConfiguration instanceConfiguration = instanceConfigItem.GetEntity();
			InstanceDescriptor descriptor = instanceConfiguration?.InstanceDescriptor;
			return descriptor;
		}

		#endregion
	}
}

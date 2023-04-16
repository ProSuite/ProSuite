using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Reflection;
using ProSuite.DdxEditor.Content.QA.TestDescriptors;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;
using ProSuite.DdxEditor.Framework.Items;

namespace ProSuite.DdxEditor.Content.QA.InstanceDescriptors
{
	public class AddAllAlgorithmDescriptorsCommand : AddItemCommandBase<Item>
	{
		private readonly ICollection<string> _assemblyNames;

		public AddAllAlgorithmDescriptorsCommand(
			[NotNull] Item parentItem,
			[NotNull] IApplicationController applicationController,
			[NotNull] ICollection<string> assemblyNames)
			: base(parentItem, applicationController)
		{
			_assemblyNames = assemblyNames;
		}

		#region Overrides of CommandBase

		public override string Text => "Add all Algorithm Descriptors";

		protected override void ExecuteCore()
		{
			Assembly currentAssembly = Assembly.GetExecutingAssembly();
			string assemblyDirectory = ReflectionUtils.GetAssemblyDirectory(currentAssembly);

			foreach (string assemblyName in _assemblyNames)
			{
				string dllFilePath = Path.Combine(assemblyDirectory, assemblyName);

				foreach (Item childItem in Item.Children)
				{
					switch (childItem)
					{
						case TestDescriptorsItem testDescriptorsItem:
							testDescriptorsItem.AddTestDescriptors(
								dllFilePath, ApplicationController);
							break;
						case TransformerDescriptorsItem transformerDescriptorsItem:
							transformerDescriptorsItem.AddInstanceDescriptors(
								dllFilePath, ApplicationController);
							break;
						case IssueFilterDescriptorsItem filterDescriptorsItem:
							filterDescriptorsItem.AddInstanceDescriptors(
								dllFilePath, ApplicationController);
							break;
						default:
							Assert.CantReach("Unexpected child item in algorithm descriptors");
							break;
					}
				}
			}
		}

		#endregion
	}
}

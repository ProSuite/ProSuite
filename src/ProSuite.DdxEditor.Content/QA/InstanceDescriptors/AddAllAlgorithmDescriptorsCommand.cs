using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Reflection;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;

namespace ProSuite.DdxEditor.Content.QA.InstanceDescriptors
{
	public class AddAllAlgorithmDescriptorsCommand
		: AddItemCommandBase<AlgorithmDescriptorsItem>
	{
		private readonly ICollection<string> _assemblyNames;

		public AddAllAlgorithmDescriptorsCommand(
			[NotNull] AlgorithmDescriptorsItem parentItem,
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
				Item.AddDescriptors(dllFilePath, ApplicationController);
			}
		}

		#endregion
	}
}

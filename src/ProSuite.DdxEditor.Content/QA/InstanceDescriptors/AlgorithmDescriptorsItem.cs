using System.Collections.Generic;
using System.Drawing;
using System.Reflection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Essentials.System;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI.WinForms;
using ProSuite.DdxEditor.Content.Properties;
using ProSuite.DdxEditor.Content.QA.TestDescriptors;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DomainModel.AO.QA.TestReport;

namespace ProSuite.DdxEditor.Content.QA.InstanceDescriptors
{
	public class AlgorithmDescriptorsItem : GroupItem
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull] private readonly CoreDomainModelItemModelBuilder _modelBuilder;
		[NotNull] private static readonly Image _image;
		[NotNull] private static readonly Image _selectedImage;

		static AlgorithmDescriptorsItem()
		{
			_image = ItemUtils.GetGroupItemImage(Resources.TestDescriptorsOverlay);
			_selectedImage = ItemUtils.GetGroupItemSelectedImage(
				Resources.TestDescriptorsOverlay);
		}

		public AlgorithmDescriptorsItem([NotNull] CoreDomainModelItemModelBuilder modelBuilder)
			: base("Algorithm Descriptors", "Test, transformer and filter implementations")
		{
			_modelBuilder = modelBuilder;
		}

		public override Image Image => _image;

		public override Image SelectedImage => _selectedImage;

		protected override bool AllowDeleteSelectedChildren => false;

		protected override bool SortChildren => false;

		protected override IEnumerable<Item> GetChildren()
		{
			yield return RegisterChild(new TestDescriptorsItem(_modelBuilder));

			yield return RegisterChild(new TransformerDescriptorsItem(_modelBuilder));
			yield return RegisterChild(new IssueFilterDescriptorsItem(_modelBuilder));
		}

		#region Overrides of Item

		protected override void CollectCommands(List<ICommand> commands,
		                                        IApplicationController applicationController)
		{
			base.CollectCommands(commands, applicationController);

			commands.Add(new ImportInstanceDescriptorsCommand(this, applicationController,
			                                                  _modelBuilder
				                                                  .DefaultTestDescriptorsXmlFile));
			commands.Add(new CreateReportForAssemblyTestsCommand(this, applicationController));
		}

		#endregion

		public void ImportInstanceDescriptors([NotNull] string fileName)
		{
			Assert.ArgumentNotNullOrEmpty(fileName, nameof(fileName));

			using (new WaitCursor())
			{
				using (_msg.IncrementIndentation("Importing all descriptors from {0}", fileName))
				{
					const bool updateDescriptorNames = true;
					const bool updateDescriptorProperties = false;

					_modelBuilder.DataQualityImporter.ImportInstanceDescriptors(
						fileName, updateDescriptorNames, updateDescriptorProperties);
				}

				// TODO report stats (inserted, updated descriptors)

				_msg.InfoFormat("Descriptors imported from {0}", fileName);
			}
		}

		public void CreateReport([NotNull] Assembly assembly,
		                         [NotNull] string htmlFileName,
		                         bool overwrite)
		{
			Assert.ArgumentNotNull(assembly, nameof(assembly));
			Assert.ArgumentNotNullOrEmpty(htmlFileName, nameof(htmlFileName));

			string location = assembly.Location;
			Assert.NotNull(location, "assembly location is null");

			TestReportUtils.WriteTestReport(new[] {assembly}, htmlFileName, overwrite);

			_msg.InfoFormat(
				"Report of test, transformer and filter implementations in assembly {0} created: {1}",
				assembly.Location, htmlFileName);

			_msg.Info("Opening report...");
			ProcessUtils.StartProcess(htmlFileName);
		}
	}
}

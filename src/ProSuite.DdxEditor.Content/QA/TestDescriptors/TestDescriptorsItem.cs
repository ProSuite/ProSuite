using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.WinForms;
using ProSuite.DdxEditor.Content.Properties;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DomainModel.AO.QA.TestReport;

namespace ProSuite.DdxEditor.Content.QA.TestDescriptors
{
	public class TestDescriptorsItem : EntityTypeItem<TestDescriptor>
	{
		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		[NotNull] private readonly CoreDomainModelItemModelBuilder _modelBuilder;
		[NotNull] private static readonly Image _image;
		[NotNull] private static readonly Image _selectedImage;

		static TestDescriptorsItem()
		{
			_image = ItemUtils.GetGroupItemImage(Resources.TestDescriptorsOverlay);
			_selectedImage = ItemUtils.GetGroupItemSelectedImage(
				Resources.TestDescriptorsOverlay);
		}

		public TestDescriptorsItem([NotNull] CoreDomainModelItemModelBuilder modelBuider)
			: base("Test Descriptors", "Test implementations")
		{
			Assert.ArgumentNotNull(modelBuider, nameof(modelBuider));

			_modelBuilder = modelBuider;
		}

		public override Image Image => _image;

		public override Image SelectedImage => _selectedImage;

		protected override bool AllowDeleteSelectedChildren => true;

		protected override bool SortChildren => true;

		protected override IEnumerable<Item> GetChildren()
		{
			return _modelBuilder.GetChildren(this);
		}

		protected override void CollectCommands(List<ICommand> commands,
		                                        IApplicationController
			                                        applicationController)
		{
			base.CollectCommands(commands, applicationController);

			commands.Add(new ImportTestDescriptorsCommand(
				             this, applicationController,
				             _modelBuilder.DefaultTestDescriptorsXmlFile));
			commands.Add(new AddTestDescriptorCommand(this, applicationController));
			commands.Add(
				new AddTestDescriptorsFromAssemblyCommand(this, applicationController));
			commands.Add(new CreateReportForAssemblyTestsCommand(this, applicationController));
			commands.Add(
				new CreateReportForRegisteredTestsCommand(this, applicationController));
			commands.Add(new DeleteAllChildItemsCommand(this, applicationController));
		}

		[NotNull]
		public TestDescriptorItem AddTestDescriptorItem()
		{
			var testDescriptor = new TestDescriptor();

			var item = new TestDescriptorItem(_modelBuilder, testDescriptor,
			                                  _modelBuilder.TestDescriptors);

			AddChild(item);

			item.NotifyChanged();

			return item;
		}

		protected override Control CreateControlCore(IItemNavigation itemNavigation)
		{
			return CreateTableControl(GetTableRows, itemNavigation);
		}

		[NotNull]
		protected virtual IEnumerable<TestDescriptorTableRow> GetTableRows()
		{
			ITestDescriptorRepository repository = _modelBuilder.TestDescriptors;

			IList<TestDescriptor> testDescriptors = repository.GetAll()
			                                                  .OrderBy(t => t.Name)
			                                                  .ToList();

			IDictionary<int, int> refCountMap =
				repository.GetReferencingQualityConditionCount();

			foreach (TestDescriptor testDescriptor in testDescriptors)
			{
				int refCount;
				if (! refCountMap.TryGetValue(testDescriptor.Id, out refCount))
				{
					refCount = 0;
				}

				yield return new TestDescriptorTableRow(testDescriptor, refCount);
			}
		}

		public void CreateTestReport([NotNull] string htmlFileName, bool overwrite)
		{
			Assert.ArgumentNotNullOrEmpty(htmlFileName, nameof(htmlFileName));

			if (overwrite && File.Exists(htmlFileName))
			{
				File.Delete(htmlFileName);
			}

			using (TextWriter writer = new StreamWriter(htmlFileName))
			{
				var reportBuilder = new HtmlReportBuilder(writer, "Registered Tests")
				                    {
					                    IncludeObsolete = false,
					                    IncludeAssemblyInfo = true
				                    };

				_modelBuilder.ReadOnlyTransaction(
					delegate
					{
						IList<TestDescriptor> testDescriptors =
							_modelBuilder.TestDescriptors.GetAll();

						foreach (TestDescriptor descriptor in testDescriptors)
						{
							if (descriptor.TestClass != null)
							{
								reportBuilder.IncludeTest(descriptor.TestClass.GetInstanceType(),
								                          descriptor.TestConstructorId);
							}
							else if (descriptor.TestFactoryDescriptor != null)
							{
								reportBuilder.IncludeTestFactory(
									descriptor.TestFactoryDescriptor.GetInstanceType());
							}
							else
							{
								_msg.WarnFormat(
									"Neither test class nor factory defined for descriptor {0}",
									descriptor.Name);
							}
						}

						reportBuilder.WriteReport();
					});
			}

			_msg.InfoFormat("Report of registered tests created: {0}", htmlFileName);

			_msg.Info("Opening report...");
			Process.Start(htmlFileName);
		}

		public void CreateTestReport([NotNull] Assembly assembly,
		                             [NotNull] string htmlFileName,
		                             bool overwrite)
		{
			Assert.ArgumentNotNull(assembly, nameof(assembly));
			Assert.ArgumentNotNullOrEmpty(htmlFileName, nameof(htmlFileName));

			string location = assembly.Location;
			Assert.NotNull(location, "assembly location is null");

			TestReportUtils.WriteTestReport(new[] {assembly}, htmlFileName, overwrite);

			_msg.InfoFormat("Report of tests in assembly {0} created: {1}",
			                assembly.Location, htmlFileName);

			_msg.Info("Opening report...");
			Process.Start(htmlFileName);
		}

		public void TryAddTestDescriptors(
			[NotNull] IEnumerable<TestDescriptor> testDescriptors)
		{
			Assert.ArgumentNotNull(testDescriptors, nameof(testDescriptors));

			ITestDescriptorRepository repository = _modelBuilder.TestDescriptors;

			var addedCount = 0;
			_modelBuilder.NewTransaction(
				delegate
				{
					Dictionary<string, TestDefinition> definitions =
						repository.GetAll()
						          .Select(descriptor => new TestDefinition(descriptor))
						          .ToDictionary(definition => definition.Name);

					foreach (TestDescriptor testDescriptor in testDescriptors)
					{
						var definition = new TestDefinition(testDescriptor);

						// Note daro: hack for TOP-5464
						// In DDX schema there is an unique constraint on NAME
						// and
						// FCTRY_TYPENAME, FCTRY_ASSEMBLYNAME, TEST_TYPENAME, TEST_ASSEMBLYNAME, TEST_CTROID

						// 1st check: name
						if (definitions.ContainsKey(definition.Name))
						{
							_msg.InfoFormat(
								"A test descriptor with the same definition as '{0}' is already registered",
								testDescriptor.Name);
						}
						// 2nd check: equality with rest of object
						else if (! definitions.ContainsValue(definition))
						{
							_msg.DebugFormat("Registering new test descriptor {0}", testDescriptor);

							definitions.Add(definition.Name, definition);

							repository.Save(testDescriptor);

							addedCount++;
						}
					}
				});

			_msg.InfoFormat("{0} test descriptor(s) added", addedCount);

			RefreshChildren();
		}

		public void ImportTestDescriptors([NotNull] string fileName)
		{
			Assert.ArgumentNotNullOrEmpty(fileName, nameof(fileName));

			using (new WaitCursor())
			{
				using (_msg.IncrementIndentation(
					       "Importing all test descriptors from {0}", fileName))
				{
					const bool updateTestDescriptorNames = true;
					const bool updateTestDescriptorProperties = false;

					_modelBuilder.DataQualityImporter.ImportTestDescriptors(
						fileName, updateTestDescriptorNames,
						updateTestDescriptorProperties);
				}

				// TODO report stats (inserted, updated qcons and testdescs)

				_msg.InfoFormat("Test descriptors imported from {0}", fileName);
			}
		}
	}
}

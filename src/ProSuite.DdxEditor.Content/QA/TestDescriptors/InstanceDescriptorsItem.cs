using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI.WinForms;
using ProSuite.DdxEditor.Content.Properties;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.AO.QA.TestReport;
using ProSuite.DomainModel.Core;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Repositories;
using ProSuite.QA.Container;

namespace ProSuite.DdxEditor.Content.QA.TestDescriptors
{
	public class InstanceDescriptorsItem<T> : EntityTypeItem<T> where T : InstanceDescriptor
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull] private readonly CoreDomainModelItemModelBuilder _modelBuilder;
		[NotNull] private static readonly Image _image;
		[NotNull] private static readonly Image _selectedImage;

		static InstanceDescriptorsItem()
		{
			_image = ItemUtils.GetGroupItemImage(Resources.TransformOverlay);
			_selectedImage = ItemUtils.GetGroupItemSelectedImage(Resources.TransformOverlay);
		}

		public InstanceDescriptorsItem([NotNull] CoreDomainModelItemModelBuilder modelBuider)
			: base("Transformer Descriptors", "Transformer implementations")
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

		protected override void CollectCommands(
			List<ICommand> commands,
			IApplicationController applicationController)
		{
			base.CollectCommands(commands, applicationController);

			// TODO: Consider automatically detect the relevant assemblies and load ALL transformers etc.
			//commands.Add(new ImportTestDescriptorsCommand(
			//				 this, applicationController,
			//				 _modelBuilder.DefaultTestDescriptorsXmlFile));
			//commands.Add(new AddTestDescriptorCommand(this, applicationController));
			commands.Add(CreateAddFromAssemblyCommand(applicationController));
			//commands.Add(new CreateReportForAssemblyTestsCommand(this, applicationController));
			//commands.Add(
			//	new CreateReportForRegisteredTestsCommand(this, applicationController));
			commands.Add(new DeleteAllChildItemsCommand(this, applicationController));
		}

		private AddInstanceDescriptorsFromAssemblyCommand<T> CreateAddFromAssemblyCommand(
			IApplicationController applicationController)
		{
			if (typeof(T) == typeof(TransformerDescriptor))
			{
				return new AddInstanceDescriptorsFromAssemblyCommand<T>(
					this, typeof(ITableTransformer),
					(t, c) => (T) CreateTransformerDescriptor(t, c), "transformer descriptor",
					applicationController);
			}

			throw new NotImplementedException();
		}

		private static InstanceDescriptor CreateTransformerDescriptor(Type type, int constructor)
		{
			return new TransformerDescriptor(
				InstanceFactoryUtils.GetDefaultDescriptorName(type, constructor),
				new ClassDescriptor(type), constructor);
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
		protected virtual IEnumerable<InstanceDescriptorTableRow> GetTableRows()
		{
			IInstanceDescriptorRepository repository = _modelBuilder.InstanceDescriptors;

			if (! typeof(TransformerDescriptor).IsAssignableFrom(typeof(T)))
			{
				throw new NotImplementedException();
			}

			IList<TransformerDescriptor> transformerDescriptors = repository
				.GetTransformerDescriptors()
				.OrderBy(t => t.Name)
				.ToList();

			IDictionary<int, int> refCountMap =
				repository.GetReferencingConfigurationCount<TransformerConfiguration>();

			foreach (TransformerDescriptor testDescriptor in transformerDescriptors)
			{
				int refCount;
				if (! refCountMap.TryGetValue(testDescriptor.Id, out refCount))
				{
					refCount = 0;
				}

				yield return new InstanceDescriptorTableRow(testDescriptor, refCount);
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

		public void TryAddInstanceDescriptors(
			[NotNull] IEnumerable<InstanceDescriptor> descriptors)
		{
			Assert.ArgumentNotNull(descriptors, nameof(descriptors));

			IInstanceDescriptorRepository repository = _modelBuilder.InstanceDescriptors;

			var addedCount = 0;
			_modelBuilder.NewTransaction(
				delegate
				{
					Dictionary<string, InstanceDefinition> definitions =
						repository.GetAll()
						          .Select(descriptor => new InstanceDefinition(descriptor))
						          .ToDictionary(definition => definition.Name);

					foreach (InstanceDescriptor descriptor in descriptors)
					{
						var definition = new InstanceDefinition(descriptor);

						// Note daro: hack for TOP-5464
						// In DDX schema there is an unique constraint on NAME
						// and
						// FCTRY_TYPENAME, FCTRY_ASSEMBLYNAME, TEST_TYPENAME, TEST_ASSEMBLYNAME, TEST_CTROID

						// 1st check: name
						if (definitions.ContainsKey(definition.Name))
						{
							_msg.InfoFormat(
								"An {0} with the same definition as '{1}' is already registered",
								descriptor.TypeDisplayName, descriptor.Name);
						}
						// 2nd check: equality with rest of object
						else if (! definitions.ContainsValue(definition))
						{
							_msg.DebugFormat("Registering new {0} {1}", descriptor.TypeDisplayName,
							                 descriptor);

							definitions.Add(definition.Name, definition);

							repository.Save(descriptor);

							addedCount++;
						}
					}
				});

			_msg.InfoFormat("{0} instance descriptor(s) added", addedCount);

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

using System;
using System.Collections.Generic;
using System.Reflection;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;
using ProSuite.DomainModel.AO.QA;

namespace ProSuite.DdxEditor.Content.QA.TestDescriptors
{
	public class AddTestDescriptorsFromAssemblyCommand :
		AddItemCommandBase<TestDescriptorsItem>
	{
		private readonly IApplicationController _applicationController;

		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// Initializes a new instance of the <see cref="AddTestDescriptorsFromAssemblyCommand"/> class.
		/// </summary>
		/// <param name="testDescriptorsItem">The test descriptors item.</param>
		/// <param name="applicationController">The application controller.</param>
		public AddTestDescriptorsFromAssemblyCommand(
			[NotNull] TestDescriptorsItem testDescriptorsItem,
			[NotNull] IApplicationController applicationController)
			: base(testDescriptorsItem, applicationController)
		{
			_applicationController = applicationController;
		}

		public override string Text => "Add Test Descriptors from Assembly";

		protected override void ExecuteCore()
		{
			string dllFilePath = TestAssemblyUtils.ChooseAssemblyFileName();

			if (dllFilePath == null)
			{
				return;
			}

			using (_msg.IncrementIndentation(
				       "Adding test descriptors from assembly {0}", dllFilePath))
			{
				Assembly assembly = Assembly.LoadFile(dllFilePath);

				var newDescriptors = new List<TestDescriptor>();

				const bool includeObsolete = false;
				const bool includeInternallyUsed = false;

				const bool stopOnError = false;
				const bool allowErrors = true;

				// TODO allow specifying naming convention
				// TODO optionally use alternate display name 
				// TODO allow selection of types/constructors
				// TODO optionally change properties of existing descriptors with same definition
				var testCount = 0;

				foreach (Type testType in TestFactoryUtils.GetTestClasses(
					         assembly, includeObsolete, includeInternallyUsed))
				{
					foreach (int constructorIndex in
					         TestFactoryUtils.GetTestConstructorIndexes(testType,
						         includeObsolete,
						         includeInternallyUsed))
					{
						testCount++;
						newDescriptors.Add(
							new TestDescriptor(
								TestFactoryUtils.GetDefaultTestDescriptorName(
									testType, constructorIndex),
								new ClassDescriptor(testType),
								constructorIndex,
								stopOnError, allowErrors));
					}
				}

				var testFactoryCount = 0;

				foreach (Type testFactoryType in TestFactoryUtils.GetTestFactoryClasses(
					         assembly, includeObsolete, includeInternallyUsed))
				{
					testFactoryCount++;
					newDescriptors.Add(
						new TestDescriptor(
							TestFactoryUtils.GetDefaultTestDescriptorName(testFactoryType),
							new ClassDescriptor(testFactoryType),
							stopOnError, allowErrors));
				}

				_msg.InfoFormat("The assembly contains {0} tests and {1} test factories",
				                testCount, testFactoryCount);

				_applicationController.GoToItem(Item);

				Item.TryAddTestDescriptors(newDescriptors);
			}
		}
	}
}

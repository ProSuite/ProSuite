using System;
using System.Collections.Generic;
using System.Reflection;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DdxEditor.Framework;
using ProSuite.DdxEditor.Framework.Commands;
using ProSuite.DdxEditor.Framework.Items;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DdxEditor.Content.QA.TestDescriptors
{
	public class AddInstanceDescriptorsFromAssemblyCommand<T> : AddItemCommandBase<Item>
		where T : InstanceDescriptor
	{
		[NotNull] private readonly InstanceDescriptorsItem<T> _descriptorsItem;
		[NotNull] private readonly Type _implementationBaseType;
		[NotNull] private readonly Func<Type, int, T> _instanceFactoryMethod;
		[NotNull] private readonly string _instanceTypeDisplayName;
		private readonly IApplicationController _applicationController;

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		/// <summary>
		/// Initializes a new instance of the <see cref="AddInstanceDescriptorsFromAssemblyCommand{T}"/> class.
		/// </summary>
		/// <param name="descriptorsItem">The test descriptors item.</param>
		/// <param name="implementationBaseType"></param>
		/// <param name="instanceFactoryMethod"></param>
		/// <param name="instanceTypeDisplayName"></param>
		/// <param name="applicationController">The application controller.</param>
		public AddInstanceDescriptorsFromAssemblyCommand(
			[NotNull] InstanceDescriptorsItem<T> descriptorsItem,
			[NotNull] Type implementationBaseType,
			[NotNull] Func<Type, int, T> instanceFactoryMethod,
			[NotNull] string instanceTypeDisplayName,
			[NotNull] IApplicationController applicationController)
			: base(descriptorsItem, applicationController)
		{
			_descriptorsItem = descriptorsItem;
			_implementationBaseType = implementationBaseType;
			_instanceFactoryMethod = instanceFactoryMethod;
			_instanceTypeDisplayName = instanceTypeDisplayName;
			_applicationController = applicationController;
		}

		public override string Text => $"Add {_instanceTypeDisplayName} from Assembly";

		protected override void ExecuteCore()
		{
			string dllFilePath = TestAssemblyUtils.ChooseAssemblyFileName();

			if (dllFilePath == null)
			{
				return;
			}

			using (_msg.IncrementIndentation(
				       "Adding {0} from assembly {1}", _instanceTypeDisplayName, dllFilePath))
			{
				Assembly assembly = Assembly.LoadFile(dllFilePath);

				var newDescriptors = new List<InstanceDescriptor>();

				const bool includeObsolete = false;
				const bool includeInternallyUsed = false;

				// TODO allow specifying naming convention
				// TODO optionally use alternate display name 
				// TODO allow selection of types/constructors
				// TODO optionally change properties of existing descriptors with same definition
				var count = 0;

				foreach (Type instanceType in InstanceFactoryUtils.GetClasses(
					         assembly, _implementationBaseType, includeObsolete,
					         includeInternallyUsed))
				{
					foreach (int constructorIndex in
					         InstanceFactoryUtils.GetConstructorIndexes(instanceType,
						         includeObsolete,
						         includeInternallyUsed))
					{
						count++;
						newDescriptors.Add(_instanceFactoryMethod(instanceType, constructorIndex));
					}
				}

				_msg.InfoFormat("The assembly contains {0} {1}s", count, _instanceTypeDisplayName);

				_applicationController.GoToItem(Item);

				_descriptorsItem.TryAddInstanceDescriptors(newDescriptors);
			}
		}
	}
}

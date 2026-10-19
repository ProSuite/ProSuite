using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Validation;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Repositories;
using ProSuite.QA.Container;
using ProSuite.QA.Core;

namespace ProSuite.DdxEditor.Content.QA.InstanceDescriptors
{
	public static class InstanceDescriptorItemUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public static IEnumerable<InstanceDescriptorTableRow> GetIssueFilterDescriptorTableRows(
			[NotNull] IInstanceDescriptorRepository repository)
		{
			return GetInstanceDescriptorTableRows<IssueFilterDescriptor, IssueFilterConfiguration>(
				repository);
		}

		public static IEnumerable<InstanceDescriptorTableRow> GetTransformerDescriptorTableRows(
			IInstanceDescriptorRepository instanceDescriptors)
		{
			return GetInstanceDescriptorTableRows<TransformerDescriptor, TransformerConfiguration>(
				instanceDescriptors);
		}

		public static int TryAddInstanceDescriptorsTx<T>(IEnumerable<T> descriptors,
		                                                 IRepository<T> repository)
			where T : InstanceDescriptor
		{
			int addedCount = 0;

			Dictionary<string, InstanceDefinition> definitions =
				repository.GetAll()
				          .Select(InstanceDefinition.CreateFrom)
				          .ToDictionary(definition => definition.Name);

			foreach (T descriptor in descriptors)
			{
				var definition = InstanceDefinition.CreateFrom(descriptor);

				// Note daro: hack for TOP-5464
				// In DDX schema there is an unique constraint on NAME
				// and
				// FCTRY_TYPENAME, FCTRY_ASSEMBLYNAME, TEST_TYPENAME, TEST_ASSEMBLYNAME, TEST_CTROID

				// 1st check: name
				if (definitions.ContainsKey(definition.Name))
				{
					_msg.InfoFormat(
						"{0} with the same definition as '{1}' is already registered",
						descriptor.TypeDisplayName, descriptor.Name);
				}
				// 2nd check: equality with rest of object
				else if (! definitions.ContainsValue(definition))
				{
					_msg.InfoFormat("Registering new {0} '{1}'", descriptor.TypeDisplayName,
					                descriptor.Name);

					definitions.Add(definition.Name, definition);

					repository.Save(descriptor);

					addedCount++;
				}
			}

			return addedCount;
		}

		/// <summary>
		/// Harvests all test and test-factory descriptors from the given assembly.
		/// Pure reflection over the assembly's metadata - does not touch the repository,
		/// a transaction or the item tree.
		/// </summary>
		public static IList<TestDescriptor> CreateTestDescriptors([NotNull] Assembly assembly)
		{
			Assert.ArgumentNotNull(assembly, nameof(assembly));

			const bool includeObsolete = false;
			const bool includeInternallyUsed = false;
			const bool stopOnError = false;
			const bool allowErrors = true;

			var result = new List<TestDescriptor>();
			var testCount = 0;

			foreach (Type testType in TestFactoryUtils.GetTestClasses(
				         assembly, includeObsolete, includeInternallyUsed))
			{
				foreach (int constructorIndex in InstanceUtils.GetConstructorIndexes(testType))
				{
					testCount++;
					result.Add(
						new TestDescriptor(
							TestFactoryUtils.GetDefaultTestDescriptorName(
								testType, constructorIndex),
							new ClassDescriptor(testType),
							constructorIndex, stopOnError, allowErrors));
				}
			}

			var testFactoryCount = 0;

			foreach (Type testFactoryType in TestFactoryUtils.GetTestFactoryClasses(
				         assembly, includeObsolete, includeInternallyUsed))
			{
				testFactoryCount++;
				result.Add(
					new TestDescriptor(
						TestFactoryUtils.GetDefaultTestDescriptorName(testFactoryType),
						new ClassDescriptor(testFactoryType),
						stopOnError, allowErrors));
			}

			_msg.InfoFormat("The assembly contains {0} tests and {1} test factories",
			                testCount, testFactoryCount);

			return result;
		}

		/// <summary>
		/// Harvests all transformer descriptors from the given assembly (see
		/// <see cref="CreateInstanceDescriptors"/>).
		/// </summary>
		public static IList<InstanceDescriptor> CreateTransformerDescriptors(
			[NotNull] Assembly assembly)
		{
			return CreateInstanceDescriptors(assembly, typeof(ITableTransformer),
			                                 "Transformer Descriptor",
			                                 CreateTransformerDescriptor);
		}

		/// <summary>
		/// Harvests all issue-filter descriptors from the given assembly (see
		/// <see cref="CreateInstanceDescriptors"/>).
		/// </summary>
		public static IList<InstanceDescriptor> CreateIssueFilterDescriptors(
			[NotNull] Assembly assembly)
		{
			return CreateInstanceDescriptors(assembly, typeof(IIssueFilter),
			                                 "Issue Filter Descriptor",
			                                 CreateIssueFilterDescriptor);
		}

		/// <summary>
		/// Harvests all instance descriptors of the given base type from the assembly.
		/// Pure reflection - does not touch the repository, a transaction or the item tree.
		/// </summary>
		public static IList<InstanceDescriptor> CreateInstanceDescriptors(
			[NotNull] Assembly assembly,
			[NotNull] Type instanceBaseType,
			[NotNull] string descriptorTypeDisplayName,
			[NotNull] Func<Type, int, InstanceDescriptor> createDescriptor)
		{
			Assert.ArgumentNotNull(assembly, nameof(assembly));
			Assert.ArgumentNotNull(instanceBaseType, nameof(instanceBaseType));
			Assert.ArgumentNotNullOrEmpty(descriptorTypeDisplayName,
			                              nameof(descriptorTypeDisplayName));
			Assert.ArgumentNotNull(createDescriptor, nameof(createDescriptor));

			const bool includeObsolete = false;
			const bool includeInternallyUsed = false;

			var result = new List<InstanceDescriptor>();
			var count = 0;

			foreach (Type instanceType in InstanceFactoryUtils.GetClasses(
				         assembly, instanceBaseType, includeObsolete, includeInternallyUsed))
			{
				foreach (int constructorIndex in
				         InstanceUtils.GetConstructorIndexes(instanceType))
				{
					count++;
					result.Add(createDescriptor(instanceType, constructorIndex));
				}
			}

			_msg.InfoFormat("The assembly contains {0} {1}s", count, descriptorTypeDisplayName);

			return result;
		}

		/// <summary>
		/// The single definition of how a <see cref="TransformerDescriptor"/> is built from
		/// an implementation type and constructor (used both when registering a single
		/// assembly and when adding all algorithm descriptors).
		/// </summary>
		public static InstanceDescriptor CreateTransformerDescriptor(
			[NotNull] Type type, int constructor)
		{
			Assert.ArgumentNotNull(type, nameof(type));

			return new TransformerDescriptor(
				InstanceFactoryUtils.GetDefaultDescriptorName(type, constructor),
				new ClassDescriptor(type), constructor);
		}

		/// <summary>
		/// The single definition of how an <see cref="IssueFilterDescriptor"/> is built from
		/// an implementation type and constructor.
		/// </summary>
		public static InstanceDescriptor CreateIssueFilterDescriptor(
			[NotNull] Type type, int constructor)
		{
			Assert.ArgumentNotNull(type, nameof(type));

			return new IssueFilterDescriptor(
				InstanceFactoryUtils.GetDefaultDescriptorName(type, constructor),
				new ClassDescriptor(type), constructor);
		}

		/// <summary>
		/// Registers the given descriptors in the repository (within a new transaction),
		/// skipping any whose name or definition is already registered.
		/// </summary>
		public static void RegisterDescriptors<T>(
			[NotNull] CoreDomainModelItemModelBuilder modelBuilder,
			[NotNull] IEnumerable<T> descriptors,
			[NotNull] IRepository<T> repository,
			[NotNull] string descriptorTypeDisplayName)
			where T : InstanceDescriptor
		{
			Assert.ArgumentNotNull(modelBuilder, nameof(modelBuilder));
			Assert.ArgumentNotNull(descriptors, nameof(descriptors));
			Assert.ArgumentNotNull(repository, nameof(repository));
			Assert.ArgumentNotNullOrEmpty(descriptorTypeDisplayName,
			                              nameof(descriptorTypeDisplayName));

			var addedCount = 0;

			modelBuilder.NewTransaction(
				delegate { addedCount = TryAddInstanceDescriptorsTx(descriptors, repository); });

			_msg.InfoFormat("{0} {1}(s) added", addedCount, descriptorTypeDisplayName);
		}

		public static void ValidateDescriptorAgainstDuplicateName(InstanceDescriptor entity,
			InstanceDescriptor descriptorWithSameName,
			Notification notification)
		{
			if (descriptorWithSameName != null &&
			    descriptorWithSameName.Id != entity.Id)
			{
				notification.RegisterMessage(
					"Name",
					$"A {descriptorWithSameName.TypeDisplayName} with the same name already exists",
					Severity.Error);
			}
		}

		public static void ValidateDescriptorAgainstDuplicateImplementation(
			[NotNull] InstanceDescriptor entity,
			[CanBeNull] InstanceDescriptor descriptorWithSameImplementation,
			[NotNull] Notification notification)
		{
			if (descriptorWithSameImplementation == null ||
			    descriptorWithSameImplementation.Id == entity.Id)
			{
				return;
			}

			string typeDisplayName = descriptorWithSameImplementation.TypeDisplayName;

			notification.RegisterMessage(
				$"{typeDisplayName} {descriptorWithSameImplementation.Name} " +
				"already has the same implementation as the current instance (factory or class/constructor). " +
				$"Please use the existing {typeDisplayName}.",
				Severity.Error);
		}

		private static IEnumerable<InstanceDescriptorTableRow> GetInstanceDescriptorTableRows<D, C>(
			IInstanceDescriptorRepository repository)
			where D : InstanceDescriptor
			where C : InstanceConfiguration
		{
			IList<D> transformerDescriptors = repository.GetInstanceDescriptors<D>();

			IDictionary<int, int> refCountMap =
				repository.GetReferencingConfigurationCount<C>();

			return CreateInstanceDescriptorTableRows(transformerDescriptors, refCountMap);
		}

		private static IEnumerable<InstanceDescriptorTableRow> CreateInstanceDescriptorTableRows(
			[NotNull] IEnumerable<InstanceDescriptor> descriptors,
			[NotNull] IDictionary<int, int> refCountById)
		{
			foreach (InstanceDescriptor descriptor in descriptors)
			{
				if (! refCountById.TryGetValue(descriptor.Id, out int refCount))
				{
					refCount = 0;
				}

				yield return new InstanceDescriptorTableRow(descriptor, refCount);
			}
		}
	}
}

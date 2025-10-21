using System;
using System.Reflection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Reflection;
using ProSuite.QA.Core;

namespace ProSuite.DomainModel.Core.QA
{
	public static class InstanceDescriptorUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		/// <summary>
		/// Gets the test implementation info. Requires the test class or the test factory descriptor to be defined.
		/// </summary>
		/// <param name="testDescriptor"></param>
		/// <param name="tryAlgorithmDefinition">Whether to allow using the platform-independent definition type.</param>
		/// <returns>InstanceInfo or null if neither the test class nor the test factory descriptor are defined.</returns>
		[CanBeNull]
		private static IInstanceInfo GetInstanceInfo([NotNull] TestDescriptor testDescriptor,
		                                             bool tryAlgorithmDefinition = true)
		{
			Assert.ArgumentNotNull(testDescriptor, nameof(testDescriptor));

			if (testDescriptor.TestClass != null)
			{
				if (tryAlgorithmDefinition &&
				    TryGetAlgorithmDefinitionType(testDescriptor.TestClass,
				                                  out Type definitionType))
				{
					return new InstanceInfo(definitionType, testDescriptor.ConstructorId);
				}

				return new InstanceInfo(testDescriptor.TestClass.AssemblyName,
				                        testDescriptor.TestClass.TypeName,
				                        testDescriptor.TestConstructorId);
			}

			if (testDescriptor.TestFactoryDescriptor != null)
			{
				if (tryAlgorithmDefinition &&
				    TryGetTestFactoryDefinition(
					    testDescriptor, out TestFactoryDefinition factoryDefinition))
				{
					return factoryDefinition;
				}

				return testDescriptor.TestFactoryDescriptor
				                     .CreateInstance<IInstanceInfo>();
			}

			return null;
		}

		/// <summary>
		/// Gets the test implementation info. A cached <see cref="InstanceDescriptor.InstanceInfo"/>
		/// value will be returned if available. The resulting value will be cached in the descriptor.
		/// </summary>
		/// <param name="descriptor"></param>
		/// <param name="tryAlgorithmDefinition"></param>
		/// <returns></returns>
		[CanBeNull]
		public static IInstanceInfo GetInstanceInfo([NotNull] InstanceDescriptor descriptor,
		                                            bool tryAlgorithmDefinition = true)
		{
			Assert.ArgumentNotNull(descriptor, nameof(descriptor));

			if (descriptor.InstanceInfo != null)
			{
				return descriptor.InstanceInfo;
			}

			IInstanceInfo result = null;

			if (descriptor is TestDescriptor testDescriptor)
			{
				result = GetInstanceInfo(testDescriptor, tryAlgorithmDefinition);
			}
			else if (descriptor.Class != null)
			{
				if (tryAlgorithmDefinition &&
				    TryGetAlgorithmDefinitionType(descriptor.Class, out Type definitionType))
				{
					result = new InstanceInfo(definitionType, descriptor.ConstructorId);
				}
				else
				{
					result = new InstanceInfo(descriptor.Class.AssemblyName,
					                          descriptor.Class.TypeName,
					                          descriptor.ConstructorId);
				}
			}

			// Cache it
			descriptor.InstanceInfo = result;

			return result;
		}

		/// <summary>
		/// Applies the fixed look-up logic for the assembly/type name in the descriptor and
		/// attempts loading the respective TestFactoryDefinition.
		/// </summary>
		/// <param name="descriptor"></param>
		/// <returns></returns>
		[NotNull]
		public static TestFactoryDefinition GetTestFactoryDefinition(
			[NotNull] TestDescriptor descriptor)
		{
			ClassDescriptor classDescriptor =
				Assert.NotNull(descriptor.TestFactoryDescriptor, "No test factory descriptor");

			TestFactoryDefinition testFactoryDefinition = GetTestFactoryDefinition(classDescriptor);

			return testFactoryDefinition;
		}

		/// <summary>
		/// Applies the fixed look-up logic to the assembly/type name in the descriptor and
		/// attempts loading the respective TestFactoryDefinition.
		/// </summary>
		/// <param name="classDescriptor"></param>
		/// <returns></returns>
		public static TestFactoryDefinition GetTestFactoryDefinition(
			[NotNull] ClassDescriptor classDescriptor)
		{
			Type factoryDefType = GetDefinitionType(classDescriptor);

			TestFactoryDefinition testFactoryDefinition =
				(TestFactoryDefinition) Activator.CreateInstance(factoryDefType);

			return testFactoryDefinition;
		}

		public static bool TryGetTestFactoryDefinition(
			[NotNull] TestDescriptor descriptor,
			out TestFactoryDefinition testFactoryDefinition)
		{
			try
			{
				testFactoryDefinition = GetTestFactoryDefinition(descriptor);

				return true;
			}
			catch (Exception)
			{
				_msg.Debug(
					$"Test factory definition {descriptor.TestFactoryDescriptor} could not be loaded. The test type will be used directly");

				testFactoryDefinition = null;
				return false;
			}
		}

		public static bool TryGetAlgorithmDefinitionType([NotNull] ClassDescriptor descriptor,
		                                                 out Type definitionType)
		{
			try
			{
				definitionType = GetDefinitionType(descriptor);
				return true;
			}
			catch (Exception)
			{
				_msg.Debug(
					$"Instance definition {descriptor} could not be loaded. The instance type will be used directly.");

				definitionType = null;

				return false;
			}
		}

		public static string GetCanonicalInstanceDescriptorName(
			[NotNull] string typeName,
			int constructorId)
		{
			int start = typeName.LastIndexOf('.') + 1;
			string className = typeName.Substring(start);

			return constructorId < 0 ? className : $"{className}({constructorId})";
		}

		public static bool TryExtractClassInfo([NotNull] string descriptorName,
		                                       out Type type,
		                                       out int constructorIdx)
		{
			constructorIdx = -1;

			_msg.VerboseDebug(
				() =>
					$"Trying to extract class info from instance descriptor '{descriptorName}'...");

			if (! TryExtractClassNameWithConstructor(descriptorName,
			                                         out string className, out constructorIdx))
			{
				className = descriptorName;
			}

			type = Type.GetType(className);

			if (_msg.IsVerboseDebugEnabled)
			{
				int ctrIdx = constructorIdx;

				if (type != null)
				{
					_msg.VerboseDebug(
						() => $"Successfully extracted {className} (constructor {ctrIdx}) " +
						      "from instance descriptor");
				}
				else
				{
					_msg.VerboseDebug(() => "..., however, no valid type could be extracted.");
				}
			}

			return type != null;
		}

		/// <summary>
		/// Extracts the class name and constructor index from the descriptorName
		/// if it is in the canonical form "TypeName(ConstructorIndex)" or
		/// if it is in the fully qualified form "AssemblyQualifiedTypeName(ConstructorIndex)".
		/// </summary>
		/// <param name="descriptorName"></param>
		/// <param name="className"></param>
		/// <param name="constructorIdx"></param>
		/// <returns></returns>
		private static bool TryExtractClassNameWithConstructor([NotNull] string descriptorName,
		                                                       out string className,
		                                                       out int constructorIdx)
		{
			className = null;
			constructorIdx = -1;

			if (! descriptorName.EndsWith(")"))
			{
				return false;
			}

			int indexOfOpenBracket = descriptorName.LastIndexOf('(');

			if (indexOfOpenBracket == -1)
			{
				return false;
			}

			int lengthOfConstructor = descriptorName.Length - 2 - indexOfOpenBracket;
			string constructorStr =
				descriptorName.Substring(indexOfOpenBracket + 1, lengthOfConstructor);

			if (! int.TryParse(constructorStr, out constructorIdx))
			{
				return false;
			}

			className = descriptorName.Substring(0, indexOfOpenBracket);

			return true;
		}

		private static Type GetDefinitionType([NotNull] ClassDescriptor classDescriptor)
		{
			string assemblyName = GetDefinitionsAssemblyName(classDescriptor, true);

			string typeName = GetDefinitionTypeName(classDescriptor);

			Type factoryDefType = PrivateAssemblyUtils.LoadType(assemblyName, typeName);

			return factoryDefType;
		}

		private static string GetDefinitionTypeName([NotNull] ClassDescriptor classDescriptor)
		{
			string typeName = Assert.NotNullOrEmpty(classDescriptor.TypeName, "No type name");

			string assemblyName =
				Assert.NotNullOrEmpty(classDescriptor.AssemblyName, "No assembly name");

			// Substitute first. Definition based tests can never come from the legacy assemblies
			if (PrivateAssemblyUtils.KnownSubstitutes.TryGetValue(
				    assemblyName, out string substituteAssembly))
			{
				typeName = typeName.Replace(assemblyName, substituteAssembly);
			}

			return InstanceUtils.GetAlgorithmDefinitionName(typeName);
		}

		private static string GetDefinitionsAssemblyName([NotNull] ClassDescriptor classDescriptor,
		                                                 bool fullName)
		{
			string assemblyName =
				Assert.NotNullOrEmpty(classDescriptor.AssemblyName, "No assembly name");

			string definitionName = InstanceUtils.GetDefinitionsAssemblyName(assemblyName);

			if (! fullName)
			{
				return definitionName;
			}

			AssemblyName resultAssemblyName = Assembly.GetExecutingAssembly().GetName();

			resultAssemblyName.Name = definitionName;

			return resultAssemblyName.FullName;
		}
	}
}

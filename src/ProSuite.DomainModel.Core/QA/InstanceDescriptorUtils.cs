using System;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
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
		/// <returns>InstanceInfo or null if neither the test class nor the test factory descriptor are defined.</returns>
		[CanBeNull]
		public static IInstanceInfo GetInstanceInfo([NotNull] TestDescriptor testDescriptor)
		{
			Assert.ArgumentNotNull(testDescriptor, nameof(testDescriptor));

			if (testDescriptor.TestClass != null)
			{
				return new InstanceInfo(testDescriptor.TestClass.AssemblyName,
				                        testDescriptor.TestClass.TypeName,
				                        testDescriptor.TestConstructorId);
			}

			if (testDescriptor.TestFactoryDescriptor != null)
			{
				return testDescriptor.TestFactoryDescriptor
				                     .CreateInstance<IInstanceInfo>();
			}

			return null;
		}

		[CanBeNull]
		public static IInstanceInfo GetInstanceInfo([NotNull] InstanceDescriptor descriptor)
		{
			Assert.ArgumentNotNull(descriptor, nameof(descriptor));

			if (descriptor is TestDescriptor testDescriptor)
			{
				return GetInstanceInfo(testDescriptor);
			}

			if (descriptor.Class != null)
			{
				return new InstanceInfo(descriptor.Class.AssemblyName,
				                        descriptor.Class.TypeName,
				                        descriptor.ConstructorId);
			}

			return null;
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
	}
}

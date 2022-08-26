using System;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using static ProSuite.Commons.Reflection.PrivateAssemblyUtils;

namespace ProSuite.DomainModel.Core.QA
{
	public class InstanceDefinition : IEquatable<InstanceDefinition>
	{
		private readonly string _assemblyName;
		private readonly int _constructorIndex;
		private readonly string _testFactoryAssemblyName;
		private readonly string _testFactoryTypeName;
		private readonly string _typeName;

		/// <summary>
		/// Initializes a new instance of the <see cref="InstanceDefinition" /> class.
		/// </summary>
		/// <param name="testDescriptor">The test descriptor for which the definition should be created.</param>
		public InstanceDefinition([NotNull] TestDescriptor testDescriptor)
		{
			Assert.ArgumentNotNull(testDescriptor, nameof(testDescriptor));

			Name = testDescriptor.Name;

			if (testDescriptor.TestClass != null)
			{
				_typeName = testDescriptor.TestClass.TypeName;
				_assemblyName = testDescriptor.TestClass.AssemblyName;
				_constructorIndex = testDescriptor.TestConstructorId;
			}
			else if (testDescriptor.TestFactoryDescriptor != null)
			{
				_testFactoryTypeName = testDescriptor.TestFactoryDescriptor.TypeName;
				_testFactoryAssemblyName = testDescriptor.TestFactoryDescriptor.AssemblyName;
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="InstanceDefinition" /> class.
		/// </summary>
		/// <param name="instanceDescriptor">The instance descriptor for which the definition should be created.</param>
		public InstanceDefinition([NotNull] InstanceDescriptor instanceDescriptor)
		{
			Assert.ArgumentNotNull(instanceDescriptor, nameof(instanceDescriptor));

			Name = instanceDescriptor.Name;

			if (instanceDescriptor.Class != null)
			{
				_typeName = instanceDescriptor.Class.TypeName;
				_assemblyName = instanceDescriptor.Class.AssemblyName;
				_constructorIndex = instanceDescriptor.ConstructorId;
			}
		}

		[NotNull]
		public string Name { get; }

		#region IEquatable<InstanceDefinition> Members

		public bool Equals(InstanceDefinition other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			if (other._constructorIndex != _constructorIndex)
			{
				return false;
			}

			if (Equals(other._typeName, _typeName) &&
			    Equals(other._assemblyName, _assemblyName) &&
			    Equals(other._testFactoryTypeName, _testFactoryTypeName) &&
			    Equals(other._testFactoryAssemblyName, _testFactoryAssemblyName))
			{
				return true;
			}

			if (PrivateTypeEquals(other._assemblyName, other._typeName, _assemblyName, _typeName)
			    && PrivateTypeEquals(other._testFactoryAssemblyName, other._testFactoryTypeName,
			                         _testFactoryAssemblyName, _testFactoryTypeName))
			{
				return true;
			}

			return false;
		}

		private static bool PrivateTypeEquals(
			[CanBeNull] string xAssembly, [CanBeNull] string xType,
			[CanBeNull] string yAssembly, [CanBeNull] string yType)
		{
			if (xType == null != (yType == null)) return false;
			if (xAssembly == null != (yAssembly == null)) return false;

			if (xType == yType && xAssembly == yAssembly)
			{
				return true;
			}

			if (GetCoreName(xType) != GetCoreName(yType))
			{
				return false;
			}

			if (GetSubsituteType(Assert.NotNull(xAssembly), Assert.NotNull(xType))
			    != GetSubsituteType(yAssembly, yType))
			{
				return false;
			}

			return true;
		}

		#endregion

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}

			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			if (obj.GetType() != typeof(InstanceDefinition))
			{
				return false;
			}

			return Equals((InstanceDefinition) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int result = GetCoreName(_typeName)?.GetHashCode() ?? 0;
				result = (result * 397) ^ (GetCoreName(_assemblyName)?.GetHashCode() ?? 0);
				result = (result * 397) ^ _constructorIndex;
				result = (result * 397) ^ (GetCoreName(_testFactoryTypeName)?.GetHashCode() ?? 0);
				result = (result * 397) ^
				         (GetCoreName(_testFactoryAssemblyName)?.GetHashCode() ?? 0);
				result = (result * 397) ^ (GetCoreName(Name)?.GetHashCode() ?? 0);
				return result;
			}
		}
	}
}

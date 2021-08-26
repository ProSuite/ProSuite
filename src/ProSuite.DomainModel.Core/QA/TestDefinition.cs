using System;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.QA
{
	public class TestDefinition : IEquatable<TestDefinition>
	{
		private readonly string _testAssemblyName;
		private readonly int _testConstructorIndex;
		private readonly string _testFactoryAssemblyName;
		private readonly string _testFactoryTypeName;
		private readonly string _testTypeName;
		private readonly string _testName;

		/// <summary>
		/// Initializes a new instance of the <see cref="TestDefinition" /> class.
		/// </summary>
		/// <param name="testDescriptor">The test descriptor for which the definition should be created.</param>
		public TestDefinition([NotNull] TestDescriptor testDescriptor)
		{
			Assert.ArgumentNotNull(testDescriptor, nameof(testDescriptor));
			
			_testName = testDescriptor.Name;

			if (testDescriptor.TestClass != null)
			{
				_testTypeName = testDescriptor.TestClass.TypeName;
				_testAssemblyName = testDescriptor.TestClass.AssemblyName;
				_testConstructorIndex = testDescriptor.TestConstructorId;
			}
			else if (testDescriptor.TestFactoryDescriptor != null)
			{
				_testFactoryTypeName = testDescriptor.TestFactoryDescriptor.TypeName;
				_testFactoryAssemblyName = testDescriptor.TestFactoryDescriptor.AssemblyName;
			}
		}

		[NotNull]
		public string Name => _testName;

		#region IEquatable<TestDefinition> Members

		public bool Equals(TestDefinition other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			return Equals(other._testTypeName, _testTypeName) &&
			       Equals(other._testAssemblyName, _testAssemblyName) &&
			       other._testConstructorIndex == _testConstructorIndex &&
			       Equals(other._testFactoryTypeName, _testFactoryTypeName) &&
			       Equals(other._testFactoryAssemblyName, _testFactoryAssemblyName) ||
			       Equals(other._testName, _testName);
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

			if (obj.GetType() != typeof(TestDefinition))
			{
				return false;
			}

			return Equals((TestDefinition) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int result = (_testTypeName != null
					              ? _testTypeName.GetHashCode()
					              : 0);
				result = (result * 397) ^ (_testAssemblyName != null
					                           ? _testAssemblyName.GetHashCode()
					                           : 0);
				result = (result * 397) ^ _testConstructorIndex;
				result = (result * 397) ^ (_testFactoryTypeName != null
					                           ? _testFactoryTypeName.GetHashCode()
					                           : 0);
				result = (result * 397) ^ (_testFactoryAssemblyName != null
					                           ? _testFactoryAssemblyName.GetHashCode()
					                           : 0);
				result = (result * 397) ^ (_testName != null ? _testName.GetHashCode() : 0);
				return result;
			}
		}
	}
}

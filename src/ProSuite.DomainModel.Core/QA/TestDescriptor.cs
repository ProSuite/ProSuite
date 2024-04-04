using System;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.QA
{
	// TODO: Validate that Class OR _testFactoryDescriptor = null
	public class TestDescriptor : InstanceDescriptor,
	                              IEquatable<TestDescriptor>
	{
		[UsedImplicitly] private bool _stopOnError;
		[UsedImplicitly] private bool _allowErrors = true;
		[UsedImplicitly] private int? _executionPriority;
		[UsedImplicitly] private bool _reportIndividualErrors = true;

		[UsedImplicitly] private ClassDescriptor _testFactoryDescriptor;
		[UsedImplicitly] private ClassDescriptor _testConfigurator;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="TestDescriptor"/> class.
		/// </summary>
		/// <remarks>Required for NHibernate</remarks>
		public TestDescriptor() { }

		public TestDescriptor([NotNull] string name,
		                      [NotNull] ClassDescriptor testFactoryDescriptor,
		                      bool stopOnError = false,
		                      bool allowErrors = false,
		                      [CanBeNull] string description = null)
			: base(name, description)
		{
			Assert.ArgumentNotNull(testFactoryDescriptor, nameof(testFactoryDescriptor));

			_testFactoryDescriptor = testFactoryDescriptor;
			_stopOnError = stopOnError;
			_allowErrors = allowErrors;
		}

		public TestDescriptor([NotNull] string name,
		                      [NotNull] ClassDescriptor testClass,
		                      int testConstructorId,
		                      bool stopOnError = false,
		                      bool allowErrors = false,
		                      string description = null)
			: base(name, testClass, testConstructorId, description)
		{
			_stopOnError = stopOnError;
			_allowErrors = allowErrors;
		}

		#endregion

		[UsedImplicitly]
		public bool StopOnError
		{
			get => _stopOnError;
			set => _stopOnError = value;
		}

		[UsedImplicitly]
		public bool AllowErrors
		{
			get => _allowErrors;
			set => _allowErrors = value;
		}

		[UsedImplicitly]
		public int? ExecutionPriority
		{
			get => _executionPriority;
			set => _executionPriority = value;
		}

		[UsedImplicitly]
		public bool ReportIndividualErrors
		{
			get => _reportIndividualErrors;
			set => _reportIndividualErrors = value;
		}

		public int TestConstructorId
		{
			get => ConstructorId;
			set { ConstructorId = value; }
		}

		[CanBeNull]
		public ClassDescriptor TestClass
		{
			get => Class;
			set { Class = value; }
		}

		[CanBeNull]
		public ClassDescriptor TestFactoryDescriptor
		{
			get => _testFactoryDescriptor;
			set
			{
				if (value == _testFactoryDescriptor)
				{
					return;
				}

				_testFactoryDescriptor = value;

				if (value != null)
				{
					TestClass = null;
					TestConstructorId = 0;
					_testConfigurator = null;
				}
			}
		}

		[CanBeNull]
		public ClassDescriptor TestConfigurator
		{
			get => _testConfigurator;
			set => _testConfigurator = value;
		}

		protected override void OnSetConstructorId()
		{
			_testConfigurator = null;
		}

		protected override void OnSetClass()
		{
			_testFactoryDescriptor = null;
			_testConfigurator = null;
		}

		public override string TypeDisplayName => "Test Descriptor";

		/// <summary>
		/// Gets the name of the assembly which contains the implementation.
		/// </summary>
		/// <value>The name of the assembly.</value>
		[CanBeNull]
		public override string AssemblyName =>
			Class?.AssemblyName ?? _testFactoryDescriptor?.AssemblyName;

		[NotNull]
		public static TestDescriptor CreateDisplayableTestDescriptor(
			[NotNull] string typeName,
			int constructorId)
		{
			var result = new TestDescriptor();

			string description = Environment.NewLine + "Original Type: " + typeName;

			if (constructorId >= 0)
			{
				description += "; ConstrId: " + constructorId;
			}

			string name = typeName;
			if (constructorId >= 0)
			{
				name += ";" + constructorId;
			}

			result.Name = name;
			result.Description = description;

			return result;
		}

		public override InstanceConfiguration CreateConfiguration()
		{
			throw new NotImplementedException();
		}

		public override string GetCanonicalName()
		{
			if (TestFactoryDescriptor != null)
			{
				return InstanceDescriptorUtils.GetCanonicalInstanceDescriptorName(
					TestFactoryDescriptor.TypeName, -1);
			}

			if (TestClass != null)
			{
				return InstanceDescriptorUtils.GetCanonicalInstanceDescriptorName(
					TestClass.TypeName, ConstructorId);
			}

			return null;
		}

		public override string ToString()
		{
			return $"Test Descriptor '{Name}'";
		}

		#region Equality members

		public override bool Equals(InstanceDescriptor other)
		{
			var otherTestDescriptor = other as TestDescriptor;

			return Equals(otherTestDescriptor);
		}

		public bool Equals(TestDescriptor other)
		{
			if (other == null)
			{
				return false;
			}

			return base.Equals(other) &&
			       Equals(other._testFactoryDescriptor, _testFactoryDescriptor);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int result = base.GetHashCode();
				result = (result * 397) ^ (_testFactoryDescriptor != null
					                           ? _testFactoryDescriptor.GetHashCode()
					                           : 0);
				return result;
			}
		}

		#endregion

		[Obsolete("Most likely obsolete - to be deleted.")]
		public bool HasEqualImplementation(TestDescriptor other)
		{
			if (this == other)
			{
				return true;
			}

			if (! AreEqual(TestClass, other.TestClass))
			{
				return false;
			}

			if (TestClass != null && TestConstructorId != other.TestConstructorId)
			{
				return false;
			}

			return AreEqual(TestFactoryDescriptor, other.TestFactoryDescriptor);
		}
	}
}

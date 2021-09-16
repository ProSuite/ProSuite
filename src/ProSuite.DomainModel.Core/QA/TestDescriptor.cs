using System;
using System.Reflection;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Reflection;
using ProSuite.Commons.Validation;

namespace ProSuite.DomainModel.Core.QA
{
	// TODO: Validate that _testClass OR _testFactoryDescriptor = null
	public class TestDescriptor : EntityWithMetadata, INamed, IAnnotated,
	                              IEquatable<TestDescriptor>
	{
		[UsedImplicitly] private string _name;
		[UsedImplicitly] private string _description;
		[UsedImplicitly] private bool _stopOnError;
		[UsedImplicitly] private bool _allowErrors = true;
		[UsedImplicitly] private int? _executionPriority;
		[UsedImplicitly] private bool _reportIndividualErrors = true;

		[UsedImplicitly] private ClassDescriptor _testFactoryDescriptor;
		[UsedImplicitly] private ClassDescriptor _testClass;
		[UsedImplicitly] private ClassDescriptor _testConfigurator;
		[UsedImplicitly] private int _testConstructorId;

		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

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
		{
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));
			Assert.ArgumentNotNull(testFactoryDescriptor, nameof(testFactoryDescriptor));

			_name = name;
			_testFactoryDescriptor = testFactoryDescriptor;
			_stopOnError = stopOnError;
			_allowErrors = allowErrors;
			_description = description;
		}

		public TestDescriptor([NotNull] string name,
		                      [NotNull] ClassDescriptor testClass,
		                      int testConstructorId,
		                      bool stopOnError = false,
		                      bool allowErrors = false,
		                      string description = null)
		{
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));
			Assert.ArgumentNotNull(testClass, nameof(testClass));

			_name = name;
			_testClass = testClass;
			_testConstructorId = testConstructorId;
			_stopOnError = stopOnError;
			_allowErrors = allowErrors;
			_description = description;
		}

		#endregion

		#region INamed Members

		[Required]
		public string Name
		{
			get { return _name; }
			set { _name = value; }
		}

		#endregion

		#region IAnnotated Members

		public string Description
		{
			get { return _description; }
			set { _description = value; }
		}

		#endregion

		[UsedImplicitly]
		public bool StopOnError
		{
			get { return _stopOnError; }
			set { _stopOnError = value; }
		}

		[UsedImplicitly]
		public bool AllowErrors
		{
			get { return _allowErrors; }
			set { _allowErrors = value; }
		}

		[UsedImplicitly]
		public int? ExecutionPriority
		{
			get { return _executionPriority; }
			set { _executionPriority = value; }
		}

		[UsedImplicitly]
		public bool ReportIndividualErrors
		{
			get { return _reportIndividualErrors; }
			set { _reportIndividualErrors = value; }
		}

		[CanBeNull]
		public ClassDescriptor TestFactoryDescriptor
		{
			get { return _testFactoryDescriptor; }
			set
			{
				if (value == _testFactoryDescriptor)
				{
					return;
				}

				_testFactoryDescriptor = value;

				if (value != null)
				{
					_testClass = null;
					_testConstructorId = 0;
					_testConfigurator = null;
				}
			}
		}

		[CanBeNull]
		public ClassDescriptor TestClass
		{
			get { return _testClass; }
			set
			{
				if (value == _testClass)
				{
					return;
				}

				_testClass = value;
				if (value == null)
				{
					_testConstructorId = 0;
				}
				else
				{
					try
					{
						_testConstructorId = GetDefaultConstructorId(_testClass);
					}
					catch (Exception e)
					{
						_msg.WarnFormat("Error determining default constructor id: {0}", e.Message);
						_testConstructorId = 0;
					}

					_testFactoryDescriptor = null;
					_testConfigurator = null;
				}
			}
		}

		private static int GetDefaultConstructorId(
			[NotNull] ClassDescriptor testClassDescriptor)
		{
			Assert.ArgumentNotNull(testClassDescriptor, nameof(testClassDescriptor));

			Type testType = testClassDescriptor.GetInstanceType();

			var constructorIndex = 0;
			foreach (ConstructorInfo ctorInfo in testType.GetConstructors())
			{
				// return the first non-obsolete
				if (! ReflectionUtils.IsObsolete(ctorInfo))
				{
					return constructorIndex;
				}

				constructorIndex++;
			}

			// if there is no non-obsolete constructor, just return the first
			return 0;
		}

		public int TestConstructorId
		{
			get { return _testConstructorId; }
			set
			{
				if (value == _testConstructorId)
				{
					return;
				}

				_testConstructorId = value;
				_testConfigurator = null;
			}
		}

		[CanBeNull]
		public ClassDescriptor TestConfigurator
		{
			get { return _testConfigurator; }
			set { _testConfigurator = value; }
		}

		/// <summary>
		/// Gets the name of the assembly which contains the test implementation.
		/// </summary>
		/// <value>The name of the assembly.</value>
		[CanBeNull]
		public string TestAssemblyName
		{
			get
			{
				if (_testClass?.AssemblyName != null)
				{
					return _testClass.AssemblyName;
				}

				if (_testFactoryDescriptor?.AssemblyName != null)
				{
					return _testFactoryDescriptor.AssemblyName;
				}

				return null;
			}
		}

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

		public override string ToString()
		{
			return string.IsNullOrEmpty(Name)
				       ? "<Unknown>"
				       : Name;
		}

		public bool Equals(TestDescriptor other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			return Equals(other._name, _name) &&
			       other._testConstructorId == _testConstructorId &&
			       Equals(other._testClass, _testClass) &&
			       Equals(other._testFactoryDescriptor, _testFactoryDescriptor);
		}

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

			if (obj.GetType() != typeof(TestDescriptor))
			{
				return false;
			}

			return Equals((TestDescriptor) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int result = _name != null ? _name.GetHashCode() : 0;
				result = (result * 397) ^ (_testClass != null
					                           ? _testClass.GetHashCode()
					                           : 0);
				result = (result * 397) ^ _testConstructorId;
				result = (result * 397) ^ (_testFactoryDescriptor != null
					                           ? _testFactoryDescriptor.GetHashCode()
					                           : 0);
				return result;
			}
		}

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

		private static bool AreEqual(ClassDescriptor x, ClassDescriptor y)
		{
			if ((x == null) != (y == null))
			{
				return false;
			}

			if (x == null)
			{
				Assert.Null(y, "Invalid program");
				return true;
			}

			return x.HasEqualType(y);
		}
	}
}

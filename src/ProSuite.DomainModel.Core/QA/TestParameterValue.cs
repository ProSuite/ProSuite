using System;
using System.Reflection;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.DomainModel.Core.QA
{
	public abstract class TestParameterValue : EntityWithMetadata,
	                                           IEquatable<TestParameterValue>
	{
		private static readonly IMsg _msg = new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		[UsedImplicitly] private readonly string _testParameterName;
		private Type _dataType;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="TestParameterValue"/> class.
		/// </summary>
		/// <remarks>Required for NHibernate</remarks>
		protected TestParameterValue() { }

		/// <summary>
		/// Initializes a new instance of the <see cref="TestParameterValue"/> class.
		/// </summary>
		/// <param name="testParameter">The test parameter.</param>
		protected TestParameterValue([NotNull] TestParameter testParameter)
			: this(testParameter.Name, testParameter.Type) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="TestParameterValue"/> class.
		/// </summary>
		/// <param name="testParameterName">The test parameter's name.</param>
		/// <param name="dataType">Type of the test parameter.</param>
		protected TestParameterValue([NotNull] string testParameterName,
		                             [CanBeNull] Type dataType)
		{
			Assert.ArgumentNotNull(testParameterName, nameof(testParameterName));

			_testParameterName = testParameterName;
			_dataType = dataType;
		}

		#endregion

		public string TestParameterName
		{
			get => _testParameterName;
		}

		/// <summary>
		/// The Type of the corresponding <see cref="TestParameter"/>. It can be null if this instance
		/// is loaded from persistence and it has not yet been initialized.
		/// </summary>
		[CanBeNull]
		public Type DataType
		{
			get { return _dataType; }
			set
			{
				if (_dataType == null)
				{
					_dataType = value;
				}
				else
				{
					if (_dataType != value)
					{
						_msg.DebugFormat("Changing DataType! From {0} to {1}", _dataType, value);
					}

					_dataType = value;
				}
			}
		}

		public abstract string StringValue { get; set; }

		[NotNull]
		public abstract TestParameterValue Clone();

		public abstract bool UpdateFrom(TestParameterValue updateValue);

		#region Non-public methods

		[NotNull]
		internal abstract string TypeString { get; }

		#endregion

		#region IEquatable<TestParameterValue> Members

		public abstract bool Equals(TestParameterValue other);

		#endregion
	}
}
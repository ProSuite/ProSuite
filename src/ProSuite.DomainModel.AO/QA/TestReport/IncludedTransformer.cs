using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Reflection;
using ProSuite.Commons.Text;
using ProSuite.QA.Container;

namespace ProSuite.DomainModel.AO.QA.TestReport
{
	public class IncludedTransformer : IncludedTestBase, IComparable<IncludedTransformer>
	{
		private readonly Type _testType;

		private readonly List<IncludedTestConstructor> _testConstructors =
			new List<IncludedTestConstructor>();

		public IncludedTransformer([NotNull] Type testType)
			: base(GetTitle(testType),
			       testType.Assembly,
			       ReflectionUtils.IsObsolete(testType),
			       TestFactoryUtils.IsInternallyUsed(testType),
			       ReflectionUtils.GetCategories(testType))
		{
			Assert.ArgumentNotNull(testType, nameof(testType));

			_testType = testType;
		}

		[NotNull]
		public IncludedTestConstructor CreateTestConstructor(int constructorIndex)
		{
			return IncludedTestConstructor.CreateInstance(_testType, constructorIndex);
		}

		public void IncludeConstructor([NotNull] IncludedTestConstructor testConstructor)
		{
			Assert.ArgumentNotNull(testConstructor, nameof(testConstructor));
			Assert.ArgumentCondition(_testType == testConstructor.TestType,
			                         "Test constructor does not belong to this test class");

			if (_testConstructors.Contains(testConstructor))
			{
				return;
			}

			_testConstructors.Add(testConstructor);
		}

		public ICollection<IncludedTestConstructor> TestConstructors => _testConstructors;

		private static string GetTitle(Type testType)
		{
			return string.Format("{0}", testType.Name);
		}

		#region Overrides of IncludedTestBase

		public override string Key =>
			_testConstructors.Count > 0
				? _testConstructors[0].Key
				: string.Format("{0}", _testType.FullName);

		public override string IndexTooltip
		{
			get
			{
				string description = ReflectionUtils.GetDescription(_testType);
				if (StringUtils.IsNotEmpty(description))
				{
					return description;
				}

				return _testConstructors.Count > 0
					       ? _testConstructors[0].IndexTooltip
					       : string.Empty;
			}
		}

		public override Type TestType => _testType;

		public override IList<IssueCode> IssueCodes => IssueCodeUtils.GetIssueCodes(_testType);

		[CanBeNull]
		public override string Description =>
			ReflectionUtils.GetDescription(_testType, inherit: false);

		#endregion

		#region Implementation of IComparable<IncludedTestClass>

		public int CompareTo(IncludedTransformer other)
		{
			return base.CompareTo(other);
		}

		#endregion


	}
}

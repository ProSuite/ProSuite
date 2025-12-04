using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Container;

namespace ProSuite.DomainServices.AO.QA
{
	/// <summary>
	/// Encapsulates the dictionaries used to navigate the conditions, tests, specification
	/// elements, the ConditionVerifications and TestVerifications.
	/// </summary>
	public class VerificationElements : IQualityConditionLookup
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull]
		private readonly IDictionary<QualityConditionVerification, QualitySpecificationElement>
			_elementsByConditionVerification;

		private Dictionary<ITest, QualitySpecificationElement> _elementsByTest;

		public VerificationElements(
			[NotNull] IDictionary<ITest, TestVerification> testVerifications,
			[NotNull] IDictionary<QualityCondition, IList<ITest>> testsByCondition,
			[NotNull] IDictionary<QualityConditionVerification, QualitySpecificationElement>
				elementsByConditionVerification)
		{
			TestVerifications = testVerifications;
			TestsByCondition = testsByCondition;
			_elementsByConditionVerification = elementsByConditionVerification;
		}

		[NotNull]
		public IDictionary<ITest, TestVerification> TestVerifications { get; }

		[NotNull]
		public IDictionary<QualityCondition, IList<ITest>> TestsByCondition { get; }

		public IDictionary<ITest, QualitySpecificationElement> ElementsByTest
		{
			get
			{
				if (_elementsByTest == null)
				{
					_elementsByTest = new Dictionary<ITest, QualitySpecificationElement>();

					foreach (KeyValuePair<QualityConditionVerification, QualitySpecificationElement>
						         kvp in _elementsByConditionVerification)
					{
						QualityCondition condition = kvp.Key.QualityCondition;

						if (condition == null)
						{
							continue;
						}

						QualitySpecificationElement element = kvp.Value;

						foreach (ITest test in TestsByCondition[condition])
						{
							_elementsByTest.Add(test, element);
						}
					}
				}

				return _elementsByTest;
			}
		}

		public IEnumerable<QualitySpecificationElement> Elements => ElementsByTest.Values;

		public IList<ITest> GetTests([NotNull] QualityCondition condition)
		{
			return TestsByCondition[condition];
		}

		public QualityCondition GetQualityCondition(ITest test)
		{
			return Assert.NotNull(
				GetQualityConditionVerification(test).QualityCondition,
				"no quality condition for test");
		}

		public QualityConditionVerification GetQualityConditionVerification(ITest test)
		{
			TestVerification testVerification = GetTestVerification(test);

			return testVerification.QualityConditionVerification;
		}

		[NotNull]
		public TestVerification GetTestVerification([NotNull] ITest test)
		{
			if (! TestVerifications.TryGetValue(test, out TestVerification result))
			{
				_msg.Debug(
					$"Searched test not found, which could be indicative of a threading issue: {test}. " +
					$"Tables: {StringUtils.Concatenate(test.InvolvedTables, t => t.Name, ", ")}. " +
					$"Hashcode: {test.GetHashCode()}");

				throw new ArgumentException(
					$@"No quality condition found for test instance of type {test.GetType()}",
					nameof(test));
			}

			return result;
		}

		public QualitySpecificationElement GetSpecificationElement(
			[NotNull] QualityConditionVerification conditionVerification)
		{
			return _elementsByConditionVerification[conditionVerification];
		}
	}
}

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Resources;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container
{
	public abstract class TestIssueCodes : ITestIssueCodes
	{
		[NotNull] private readonly string _testIdPrefix;

		[NotNull] private readonly Dictionary<string, IssueCode> _issueCodesById =
			new Dictionary<string, IssueCode>(StringComparer.OrdinalIgnoreCase);

		[NotNull] private readonly IList<IssueCode> _issueCodes;

		protected TestIssueCodes([NotNull] string testId,
		                         [NotNull] ResourceManager resourceManager,
		                         bool includeLocalCodesFromStringConstants,
		                         params string[] localCodes)
		{
			Assert.ArgumentNotNullOrEmpty(testId, nameof(testId));
			Assert.ArgumentNotNull(resourceManager, nameof(resourceManager));

			TestId = testId;
			_testIdPrefix = string.Concat(testId, ".");

			var allLocalCodes = new List<string>(localCodes);

			if (includeLocalCodesFromStringConstants)
			{
				allLocalCodes.AddRange(GetStringValuesFromPublicConstants(GetType()));
			}

			foreach (string localCode in allLocalCodes)
			{
				string id = GetID(localCode);

				Assert.False(_issueCodesById.ContainsKey(id),
				             "Issue id '{0}' is not unique within {1}", localCode,
				             GetType().FullName);

				_issueCodesById.Add(id, CreateIssueCode(id, resourceManager));
			}

			_issueCodes = new List<IssueCode>(_issueCodesById.Values).AsReadOnly();
		}

		[NotNull]
		private static IEnumerable<string> GetStringValuesFromPublicConstants(
			[NotNull] Type type)
		{
			foreach (FieldInfo fieldInfo in type.GetFields(BindingFlags.Public |
			                                               BindingFlags.Static |
			                                               BindingFlags.DeclaredOnly))
			{
				if (! fieldInfo.IsLiteral)
				{
					continue;
				}

				var value = fieldInfo.GetRawConstantValue() as string;

				if (value != null)
				{
					yield return value;
				}
			}
		}

		[NotNull]
		public string TestId { get; }

		// TODO: revise, [NotNull] to fail fast?
		[CanBeNull]
		public IssueCode this[string code] => GetIssueCode(code);

		// TODO: revise, [NotNull] to fail fast?
		public IssueCode GetIssueCode(string code)
		{
			Assert.ArgumentNotNullOrEmpty(code, nameof(code));

			bool isKnownGlobal;
			string globalCode;
			if (code.StartsWith(_testIdPrefix, StringComparison.OrdinalIgnoreCase))
			{
				globalCode = code;
				isKnownGlobal = false;
			}
			else
			{
				globalCode = GetID(code);
				isKnownGlobal = true;
			}

			IssueCode issueCode;
			return _issueCodesById.TryGetValue(globalCode, out issueCode)
				       ? issueCode
				       // Try again with full qualified code
				       : ! isKnownGlobal &&
				         _issueCodesById.TryGetValue(GetID(code), out issueCode)
					       ? issueCode
					       : null;
		}

		public IList<IssueCode> GetIssueCodes()
		{
			return _issueCodes;
		}

		[NotNull]
		private string GetID([NotNull] string localCode)
		{
			return string.Concat(_testIdPrefix, localCode);
		}

		[NotNull]
		private static IssueCode CreateIssueCode([NotNull] string id,
		                                         [NotNull] ResourceManager resourceManager)
		{
			return new IssueCode(id, GetDescription(id, resourceManager));
		}

		[CanBeNull]
		private static string GetDescription([NotNull] string id,
		                                     [NotNull] ResourceManager resourceManager)
		{
			const char conjunctionOperator = '+';
			const char hierarchySeparator = '.';
			const char resourceSeparator = '_';

			string resourceName = id.Replace(hierarchySeparator, resourceSeparator);

			if (resourceName.IndexOf(conjunctionOperator) >= 0)
			{
				resourceName = resourceName.Replace(conjunctionOperator, resourceSeparator);
			}

			return resourceManager.GetString(resourceName);
		}
	}
}

using System;
using System.Collections.Generic;
using System.Reflection;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.QA.Core.IssueCodes
{
	public static class IssueCodeUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private static readonly IDictionary<Type, ITestIssueCodes> _testIssueCodesByType =
			new Dictionary<Type, ITestIssueCodes>();

		[CanBeNull]
		public static IssueCode GetIssueCode([NotNull] string code, [NotNull] Type testType)
		{
			ITestIssueCodes testIssueCodes = GetTestIssueCodes(testType);

			return testIssueCodes?.GetIssueCode(code);
		}

		[NotNull]
		public static IList<IssueCode> GetIssueCodes([NotNull] Type testType)
		{
			ITestIssueCodes testIssueCodes = GetTestIssueCodes(testType);

			return testIssueCodes?.GetIssueCodes() ?? new List<IssueCode>();
		}

		[CanBeNull]
		private static ITestIssueCodes GetTestIssueCodes([NotNull] Type testType)
		{
			ITestIssueCodes testIssueCodes;

			if (! _testIssueCodesByType.TryGetValue(testType, out testIssueCodes))
			{
				testIssueCodes = GetTestIssueCodesCore(testType);
				_testIssueCodesByType.Add(testType, testIssueCodes);
			}

			return testIssueCodes;
		}

		[CanBeNull]
		private static ITestIssueCodes GetTestIssueCodesCore([NotNull] Type testType)
		{
			Type issueCodesType = typeof(ITestIssueCodes);

			IList<PropertyInfo> properties = GetCandidateProperties(testType, issueCodesType);

			if (properties.Count > 1)
			{
				// TODO combine into one?
				_msg.DebugFormat("Multiple test issue type properties found in type '{0}'",
				                 testType);
			}

			foreach (PropertyInfo propertyInfo in properties)
			{
				object value = GetStaticPropertyValue(propertyInfo);
				if (value != null)
				{
					return (ITestIssueCodes) value;
				}
			}

			// no property found; search fields

			IList<FieldInfo> fields = GetCandidateFields(testType, issueCodesType);

			if (fields.Count > 1)
			{
				// TODO combine into one?
				_msg.DebugFormat("Multiple test issue type fields found in type '{0}'", testType);
			}

			foreach (FieldInfo fieldInfo in fields)
			{
				object value = GetStaticFieldValue(fieldInfo);
				if (value != null)
				{
					return (ITestIssueCodes) value;
				}
			}

			return null;
		}

		[NotNull]
		private static IList<FieldInfo> GetCandidateFields([NotNull] Type type,
		                                                   [NotNull] Type fieldType)
		{
			var result = new List<FieldInfo>();

			foreach (FieldInfo fieldInfo in type.GetFields(BindingFlags.Public |
			                                               BindingFlags.NonPublic |
			                                               BindingFlags.Static))
			{
				if (fieldInfo.IsStatic && fieldType.IsAssignableFrom(fieldInfo.FieldType))
				{
					result.Add(fieldInfo);
				}
			}

			return result;
		}

		[NotNull]
		private static IList<PropertyInfo> GetCandidateProperties(
			[NotNull] Type type,
			[NotNull] Type propertyType)
		{
			var result = new List<PropertyInfo>();

			foreach (PropertyInfo propertyInfo in type.GetProperties(
				         BindingFlags.Public |
				         BindingFlags.NonPublic |
				         BindingFlags.Static |
				         BindingFlags.FlattenHierarchy))
			{
				if (propertyInfo.CanRead &&
				    propertyType.IsAssignableFrom(propertyInfo.PropertyType))
				{
					result.Add(propertyInfo);
				}
			}

			return result;
		}

		[CanBeNull]
		private static object GetStaticPropertyValue([NotNull] PropertyInfo propertyInfo)
		{
			return propertyInfo.GetValue(null, null);
		}

		[CanBeNull]
		private static object GetStaticFieldValue([NotNull] FieldInfo fieldInfo)
		{
			// NOTE: R# is wrong for static fields (obj can be null)

			// ReSharper disable AssignNullToNotNullAttribute
			return fieldInfo.GetValue(null);
			// ReSharper restore AssignNullToNotNullAttribute
		}
	}
}

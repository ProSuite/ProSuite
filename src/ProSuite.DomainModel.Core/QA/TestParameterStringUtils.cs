using System;
using System.Collections.Generic;
using System.Text;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.QA.Core;

namespace ProSuite.DomainModel.Core.QA
{
	public static class TestParameterStringUtils
	{
		private static readonly string _overflowText = FormatTestParameterValue(
			"...", ScalarTestParameterValue.ScalarTypeString, "...");

		/// <summary>
		/// Returns dummy test parameter values for display purposes from the provided text.
		/// </summary>
		/// <param name="text"></param>
		/// <returns></returns>
		[NotNull]
		public static ICollection<TestParameterValue> ParseTestParameterValues(
			[NotNull] string text)
		{
			Assert.ArgumentNotNull(text, nameof(text));

			var result = new List<TestParameterValue>();

			string[] testParameterStrings = text.Split(
				new[] {'{', '}'}, StringSplitOptions.RemoveEmptyEntries);

			foreach (string testParameterString in testParameterStrings)
			{
				string parameterName;
				string parameterTypeToken;
				string parameterValueToken;
				if (! TryParseTestParameterString(testParameterString,
				                                  out parameterName,
				                                  out parameterTypeToken,
				                                  out parameterValueToken))
				{
					continue;
				}

				result.Add(CreateTestParameterValue(
					           parameterName, parameterTypeToken, parameterValueToken));
			}

			return result;
		}

		public static string FormatTestParameterValue(
			[NotNull] TestParameterValue testParameterValue)
		{
			ScalarTestParameterValue scParameterValue =
				testParameterValue as ScalarTestParameterValue;

			string value = scParameterValue != null
				               ? scParameterValue.GetDisplayValue()
				               : testParameterValue.StringValue;

			return FormatTestParameterValue(testParameterValue.TestParameterName,
			                                testParameterValue.TypeString, value);
		}

		[NotNull]
		public static string FormatTestParameterValue([NotNull] string parameterName,
		                                              [NotNull] string parameterTypeToken,
		                                              [NotNull] string parameterValueToken)
		{
			return "{" +
			       parameterName + ":" +
			       parameterTypeToken + ":" +
			       parameterValueToken +
			       "}";
		}

		[NotNull]
		public static string FormatParameterValues(
			[NotNull] IEnumerable<TestParameterValue> testParameterValues,
			int maxLength)
		{
			Assert.ArgumentNotNull(testParameterValues, nameof(testParameterValues));

			string overflowText = _overflowText;
			int overflowTextLength = overflowText.Length;

			var sb = new StringBuilder();
			StringBuilder sbRemainder = null;

			foreach (TestParameterValue parameterValue in testParameterValues)
			{
				string formatAsText = FormatTestParameterValue(parameterValue);

				if (sbRemainder == null &&
				    sb.Length + formatAsText.Length + overflowTextLength <= maxLength)
				{
					sb.Append(formatAsText);
				}
				else // string may get too long
				{
					if (sbRemainder == null)
					{
						sbRemainder = new StringBuilder();
					}

					sbRemainder.Append(formatAsText);

					if (sb.Length + sbRemainder.Length > maxLength)
					{
						// string is too long
						if (overflowTextLength > maxLength)
						{
							return string.Empty;
						}

						sb.Append(overflowText);
						return sb.ToString();
					}
				}
			}

			if (sbRemainder != null)
			{
				sb.Append(sbRemainder);
			}

			return sb.ToString();
		}

		private static bool TryParseTestParameterString(
			[NotNull] string testParameterString,
			[NotNull] out string parameterName,
			[NotNull] out string parameterTypeToken,
			[NotNull] out string parameterValueToken)
		{
			int nameSeparatorIndex = testParameterString.IndexOf(':');

			if (nameSeparatorIndex <= 0)
			{
				parameterName = string.Empty;
				parameterTypeToken = string.Empty;
				parameterValueToken = string.Empty;

				return false;
			}

			parameterName = testParameterString.Substring(0, nameSeparatorIndex);
			string typeAndValue = testParameterString.Substring(nameSeparatorIndex + 1);

			int separatorIndex = typeAndValue.IndexOf(':');

			if (separatorIndex > 0)
			{
				parameterTypeToken = typeAndValue.Substring(0, separatorIndex);
				parameterValueToken = typeAndValue.Substring(separatorIndex + 1);
			}
			else
			{
				parameterTypeToken = ScalarTestParameterValue.ScalarTypeString;
				parameterValueToken = typeAndValue.Substring(separatorIndex + 1);
			}

			return true;
		}

		[NotNull]
		private static TestParameterValue CreateTestParameterValue(
			[NotNull] string parameterName,
			[NotNull] string parameterTypeToken,
			[NotNull] string parameterValueToken)
		{
			return Equals(parameterTypeToken, DatasetTestParameterValue.DatasetTypeString)
				       ? (TestParameterValue) new DummyDatasetValue(
					       parameterName, parameterValueToken)
				       : new DummyScalar(parameterName, parameterValueToken);
		}

		#region Nested types

		/// <summary>
		/// Test parameter implementation that allows for displaying parameter values
		/// that were persisted in a verification. Dummy parameter values are useful
		/// if the originally used condition does not exist any more in the data dictionary.
		/// </summary>
		private class DummyScalar : TestParameterValue
		{
			private string _stringValue;

			public DummyScalar([NotNull] string name, string value) : base(name, null)
			{
				_stringValue = value;
			}

			public override string StringValue
			{
				get { return _stringValue; }
				set { _stringValue = value; }
			}

			internal override string TypeString => ScalarTestParameterValue.ScalarTypeString;

			public override bool Equals(TestParameterValue other)
			{
				var o = other as DummyScalar;
				if (o == null)
				{
					return false;
				}

				bool equal = _stringValue == o.StringValue;
				return equal;
			}

			public override TestParameterValue Clone()
			{
				return new DummyScalar(TestParameterName, StringValue);
			}

			public override bool UpdateFrom(TestParameterValue updateValue)
			{
				var scalarUpdate = (DummyScalar) updateValue;

				var hasUpdates = false;
				if (StringValue != scalarUpdate.StringValue)
				{
					StringValue = scalarUpdate.StringValue;
					hasUpdates = true;
				}

				return hasUpdates;
			}
		}

		/// <summary>
		/// Test parameter implementation that allows for displaying parameter values
		/// that were persisted in a verification. Dummy parameter values are useful
		/// if the originally used condition does not exist any more in the data dictionary.
		/// </summary>
		private class DummyDatasetValue : DatasetTestParameterValue
		{
			private readonly string _stringValue;

			public DummyDatasetValue(string name, string value)
				: base(new TestParameter(name, typeof(Dataset)))
			{
				_stringValue = value;
				string[] tokens = value.Split(';');
				string datasetName = tokens[0].Trim();

				FilterExpression = tokens.Length > 1
					                   ? value.Substring(value.IndexOf(';') + 1).Trim()
					                   : null;

				DatasetValue = new DummyDataset(datasetName);
			}

			public override string StringValue
			{
				get { return _stringValue; }
				set { throw new InvalidOperationException(); }
			}

			#region Nested type: DummyDataset

			private class DummyDataset : Dataset
			{
				public DummyDataset(string name)
					: base(name) { }

				#region Overrides of Dataset

				public override DatasetType DatasetType => DatasetType.Null;

				#endregion
			}

			#endregion
		}

		#endregion
	}
}

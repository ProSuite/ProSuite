using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.QA.Container;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;

namespace ProSuite.QA.Tests.Schema
{
	public class FieldSpecification
	{
		private readonly esriFieldType _expectedFieldType;
		private readonly int _expectedFieldLength;
		[CanBeNull] private readonly string _expectedDomainName;

		/// <summary>
		/// Initializes a new instance of the <see cref="FieldSpecification"/> class.
		/// </summary>
		/// <param name="fieldName">Name of the field.</param>
		/// <param name="expectedFieldType">Expected type of the field.</param>
		/// <param name="expectedFieldLength">Expected length of the field.</param>
		/// <param name="expectedAliasName">Expected alias name of the field (optional).</param>
		/// <param name="expectedDomainName">Expected name of the domain (optional).</param>
		/// <param name="fieldIsOptional">if set to <c>true</c> the field is not required to exist.</param>
		public FieldSpecification([NotNull] string fieldName,
		                          esriFieldType expectedFieldType,
		                          int expectedFieldLength,
		                          [CanBeNull] string expectedAliasName,
		                          [CanBeNull] string expectedDomainName,
		                          bool fieldIsOptional)
		{
			Assert.ArgumentNotNullOrEmpty(fieldName, nameof(fieldName));

			FieldName = fieldName.Trim();
			_expectedFieldType = expectedFieldType;
			_expectedFieldLength = expectedFieldLength;
			ExpectedAliasName = expectedAliasName?.Trim();
			_expectedDomainName = expectedDomainName?.Trim();
			FieldIsOptional = fieldIsOptional;
		}

		[NotNull]
		public string FieldName { get; }

		public bool FieldIsOptional { get; }

		[CanBeNull]
		public string ExpectedAliasName { get; }

		[NotNull]
		public IEnumerable<KeyValuePair<string, IssueCode>> GetIssues(
			[NotNull] IField field,
			[CanBeNull] IFieldSpecificationIssueCodes issueCodes)
		{
			if (field.Type != _expectedFieldType)
			{
				yield return new KeyValuePair<string, IssueCode>(
					GetUnexpectedFieldTypeMessage(field),
					issueCodes?.UnexpectedFieldType);
			}

			if (_expectedFieldLength > 0)
			{
				if (field.Length != _expectedFieldLength)
				{
					yield return new KeyValuePair<string, IssueCode>(
						GetUnexpectedFieldLengthMessage(field),
						issueCodes?.UnexpectedFieldLength);
				}
			}

			if (StringUtils.IsNotEmpty(ExpectedAliasName) &&
			    ! Equals(field.AliasName, ExpectedAliasName))
			{
				yield return new KeyValuePair<string, IssueCode>(
					GetUnexpectedFieldAliasMessage(field),
					issueCodes?.UnexpectedAlias);
			}

			// TODO keyword for "no domain allowed"
			if (StringUtils.IsNotEmpty(_expectedDomainName))
			{
				IDomain domain = field.Domain;

				if (domain == null)
				{
					yield return new KeyValuePair<string, IssueCode>(
						GetNoDomainMessage(),
						issueCodes?.NoDomain);
				}
				else if (! Equals(domain.Name, _expectedDomainName))
				{
					yield return new KeyValuePair<string, IssueCode>(
						GetUnexpectedDomainMessage(domain),
						issueCodes?.UnexpectedDomain);
				}
			}
		}

		[NotNull]
		private string GetUnexpectedDomainMessage([NotNull] IDomain domain)
		{
			return string.Format(
				"Expected domain for field '{0}': '{1}'. Actual domain name: '{2}'",
				FieldName, _expectedDomainName, domain.Name);
		}

		[NotNull]
		private string GetNoDomainMessage()
		{
			return string.Format(
				"Expected domain for field '{0}': '{1}'. The field does not have a domain assigned.",
				FieldName, _expectedDomainName);
		}

		[NotNull]
		private string GetUnexpectedFieldAliasMessage([NotNull] IField field)
		{
			return string.Format(
				"Expected alias name for field '{0}': '{1}'. Actual alias name: '{2}'",
				FieldName, ExpectedAliasName, field.AliasName);
		}

		[NotNull]
		private string GetUnexpectedFieldLengthMessage([NotNull] IField field)
		{
			return string.Format(
				"Expected field length for field '{0}': {1}. Actual field length: {2}",
				FieldName, _expectedFieldLength, field.Length);
		}

		[NotNull]
		private string GetUnexpectedFieldTypeMessage([NotNull] IField field)
		{
			return string.Format(
				"Expected field type for field '{0}': {1}. Actual field type: {2}",
				FieldName,
				FieldUtils.GetFieldTypeDisplayText(_expectedFieldType),
				FieldUtils.GetFieldTypeDisplayText(field.Type));
		}
	}
}

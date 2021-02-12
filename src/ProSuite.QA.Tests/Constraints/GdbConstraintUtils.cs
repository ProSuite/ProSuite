using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.QA.Container;
using ProSuite.QA.Tests.IssueCodes;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests.Constraints
{
	public static class GdbConstraintUtils
	{
		private static readonly CultureInfo _culture = new CultureInfo("", false);

		#region issue codes

		[CanBeNull] private static TestIssueCodes _codes;

		[NotNull]
		[UsedImplicitly]
		public static TestIssueCodes Codes =>
			_codes ?? (_codes = new GdbConstraintIssueCodes());

		#endregion

		[NotNull]
		public static List<ConstraintNode> GetGdbConstraints(
			[NotNull] ITable table,
			bool allowNullForCodedValueDomains = true,
			bool allowNullForRangeDomains = true)
		{
			Assert.ArgumentNotNull(table, nameof(table));

			var rootNodes = new List<ConstraintNode>();

			if (DatasetUtils.IsRegisteredAsObjectClass(table))
			{
				var subtypes = table as ISubtypes;
				if (subtypes != null)
				{
					rootNodes.AddRange(GetAttributeRuleNodes(table,
					                                         subtypes,
					                                         allowNullForCodedValueDomains,
					                                         allowNullForRangeDomains));
				}
			}

			if (table.HasOID)
			{
				rootNodes.Add(CreateObjectIDConstraint(table));
			}

			return rootNodes;
		}

		[NotNull]
		public static IList<string> GetUuidFields([NotNull] ITable table)
		{
			Assert.ArgumentNotNull(table, nameof(table));

			var result = new List<string>();

			foreach (IField field in DatasetUtils.GetFields(table))
			{
				if (field.Type == esriFieldType.esriFieldTypeGlobalID ||
				    field.Type == esriFieldType.esriFieldTypeGUID)
				{
					result.Add(field.Name);
				}
			}

			return result;
		}

		[NotNull]
		private static IEnumerable<ConstraintNode> GetAttributeRuleNodes(
			[NotNull] ITable table,
			[NotNull] ISubtypes subtypes,
			bool allowNullForCodedValueDomains,
			bool allowNullForRangeDomains)
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNull(subtypes, nameof(subtypes));

			var rootNodes = new List<ConstraintNode>();

			string subtypeField = subtypes.SubtypeFieldName;

			if (! string.IsNullOrEmpty(subtypeField))
			{
				rootNodes.Add(CreateValidSubtypesConstraint(subtypes, subtypeField));
			}

			var subtypeNodes = new Dictionary<int, ConstraintNode>();
			var workspaceDomains = DatasetUtils.GetWorkspace(table) as IWorkspaceDomains;

			DomainConstraints domainConstraints =
				workspaceDomains != null
					? new DomainConstraints(workspaceDomains,
					                        allowNullForCodedValueDomains,
					                        allowNullForRangeDomains)
					: null;

			IEnumRule rules = ((IValidation) table).Rules;
			rules.Reset();

			IRule rule;
			while ((rule = rules.Next()) != null)
			{
				if (rule.Type != esriRuleType.esriRTAttribute)
				{
					continue;
				}

				var attributeRule = (IAttributeRule) rule;
				int subtype = attributeRule.SubtypeCode;

				IField field = TryGetField(table, attributeRule);
				if (field == null)
				{
					continue;
				}

				ConstraintNode subtypeParentNode = null;
				if (! string.IsNullOrEmpty(subtypeField))
				{
					if (! subtypeNodes.TryGetValue(subtype, out subtypeParentNode))
					{
						subtypeParentNode = CreateSubtypeParentNode(subtype,
						                                            subtypeField);

						subtypeNodes.Add(subtype, subtypeParentNode);

						rootNodes.Add(subtypeParentNode);
					}
				}

				DomainConstraint domainConstraint =
					domainConstraints?.GetConstraint(attributeRule);

				if (domainConstraint != null)
				{
					ConstraintNode domainNode = CreateDomainValueConstraint(field,
					                                                        domainConstraint);

					if (subtypeParentNode != null)
					{
						// add as child node to subtype selection node
						subtypeParentNode.Nodes.Add(domainNode);
					}
					else
					{
						// add as top-level node
						rootNodes.Add(domainNode);
					}
				}
			}

			return rootNodes;
		}

		[NotNull]
		private static ConstraintNode CreateSubtypeParentNode(
			int subtype,
			[NotNull] string subtypeField)
		{
			string condition = string.Format(_culture, "{0} = {1}",
			                                 subtypeField, subtype);

			return new ConstraintNode(condition,
			                          "Invalid subtype",
			                          Codes[GdbConstraintIssueCodes.InvalidSubtype],
			                          subtypeField);
		}

		[NotNull]
		private static ConstraintNode CreateValidSubtypesConstraint(
			[NotNull] ISubtypes subtypeInfo,
			[NotNull] string subtypeField)
		{
			return new ConstraintNode(
				GetSubtypeConstaint(subtypeInfo, subtypeField),
				"Invalid subtype",
				Codes[GdbConstraintIssueCodes.InvalidSubtype],
				subtypeField);
		}

		[NotNull]
		private static ConstraintNode CreateDomainValueConstraint(
			[NotNull] IField field,
			[NotNull] DomainConstraint domainConstraint)
		{
			string description = string.Format("Domain {0}", domainConstraint.DomainName);

			return new ConstraintNode(
				domainConstraint.GetConstraint(field),
				description,
				Codes[GdbConstraintIssueCodes.ValueNotValidForDomain],
				field.Name);
		}

		[NotNull]
		private static ConstraintNode CreateObjectIDConstraint([NotNull] ITable table)
		{
			// Note: for shapefiles, valid FIDs start at 0
			// TODO test for > 0 if in a gdb?
			string constraint = string.Format(_culture, "{0} >= 0", table.OIDFieldName);

			return new ConstraintNode(constraint,
			                          "Invalid object ID",
			                          Codes[GdbConstraintIssueCodes.InvalidObjectID],
			                          table.OIDFieldName);
		}

		[CanBeNull]
		private static IField TryGetField([NotNull] ITable table,
		                                  [NotNull] IAttributeRule attributeRule)
		{
			string fieldName = attributeRule.FieldName;

			int fieldIndex = table.FindField(fieldName);
			if (fieldIndex < 0)
			{
				// TODO revise; mismatch risk
				fieldIndex = table.Fields.FindFieldByAliasName(fieldName);
			}

			return fieldIndex < 0
				       ? null
				       : table.Fields.Field[fieldIndex];
		}

		[NotNull]
		private static string GetSubtypeConstaint([NotNull] ISubtypes subtypes,
		                                          [NotNull] string fieldName)
		{
			IEnumSubtype enumSubtypes = subtypes.Subtypes;
			int subtypeCode;
			var sb = new StringBuilder();

			for (string subtype = enumSubtypes.Next(out subtypeCode);
			     subtype != null;
			     subtype = enumSubtypes.Next(out subtypeCode))
			{
				sb.AppendFormat(", {0}", subtypeCode);
			}

			string subtypeConstraint = fieldName + " IN (" +
			                           sb.ToString(1, sb.Length - 1) + ")";
			return subtypeConstraint;
		}

		#region Nested type: DomainConstraints

		private class DomainConstraints
		{
			private readonly bool _allowNullForCodedValueDomains;
			private readonly bool _allowNullForRangeDomains;
			[NotNull] private readonly IWorkspaceDomains _domains;

			[NotNull] private readonly IDictionary<string, DomainConstraint> _domainConstraints =
				new Dictionary<string, DomainConstraint>(
					StringComparer.OrdinalIgnoreCase);

			public DomainConstraints([NotNull] IWorkspaceDomains domains,
			                         bool allowNullForCodedValueDomains,
			                         bool allowNullForRangeDomains)
			{
				Assert.ArgumentNotNull(domains, nameof(domains));

				_domains = domains;
				_allowNullForCodedValueDomains = allowNullForCodedValueDomains;
				_allowNullForRangeDomains = allowNullForRangeDomains;
			}

			[CanBeNull]
			public DomainConstraint GetConstraint([NotNull] IAttributeRule attributeRule)
			{
				DomainConstraint result;
				if (! _domainConstraints.TryGetValue(attributeRule.DomainName,
				                                     out result))
				{
					IDomain domain = _domains.DomainByName[attributeRule.DomainName];

					result = GetDomainConstraint(domain,
					                             _allowNullForCodedValueDomains,
					                             _allowNullForRangeDomains);
					_domainConstraints.Add(attributeRule.DomainName, result);
				}

				return result;
			}

			[CanBeNull]
			private static DomainConstraint GetDomainConstraint(
				[NotNull] IDomain domain,
				bool allowNullForCodedValueDomains,
				bool allowNullForRangeDomains)
			{
				var codedValueDomain = domain as ICodedValueDomain;

				if (codedValueDomain != null)
				{
					int codeCount = codedValueDomain.CodeCount;
					bool isFloat = IsFloatingPointField(domain.FieldType);

					if (codeCount == 0)
					{
						// Empty coded value domains are sometimes used to prevent manual edits to fields
						// The actual field value is not NULL however (which would strictly be the only
						// valid value for a empty value list)
						return null;
						// return new IsNullConstraint();
					}

					var sb = new StringBuilder();

					for (var i = 0; i < codeCount; i++)
					{
						if (sb.Length > 0)
						{
							sb.Append(", ");
						}

						var codedValue = codedValueDomain.Value[i];

						string literal = GetInListValue(codedValue, isFloat);

						sb.Append(literal);
					}

					var constraintFormat =
						isFloat
							? "Convert({0}, 'System.Single') IN (" + sb + ")"
							: "{0} IN (" + sb + ")";

					return new RegularDomainConstraint(domain.Name,
					                                   constraintFormat,
					                                   allowNullForCodedValueDomains);
				}

				var rangeDomain = domain as IRangeDomain;
				if (rangeDomain != null)
				{
					var constraintFormat = GetConstraintFormat(rangeDomain);

					return new RegularDomainConstraint(domain.Name,
					                                   constraintFormat,
					                                   allowNullForRangeDomains);
				}

				throw new ArgumentOutOfRangeException(
					$"Domain {domain.Name} has unsupported type: {domain.Type}");
			}

			[NotNull]
			private static string GetConstraintFormat([NotNull] IRangeDomain rangeDomain)
			{
				string minString;
				string maxString;
				if (rangeDomain.MinValue is DateTime)
				{
					minString = string.Format(_culture, "#{0}#", rangeDomain.MinValue);
					maxString = string.Format(_culture, "#{0}#", rangeDomain.MaxValue);
				}
				else
				{
					minString = string.Format(_culture, "{0}", rangeDomain.MinValue);
					maxString = string.Format(_culture, "{0}", rangeDomain.MaxValue);
				}

				return "{0} >= " + minString + " AND {0} <= " + maxString;
			}

			private static string GetInListValue(object codedValue, bool isFloat)
			{
				var stringCode = codedValue as string;
				if (stringCode != null)
				{
					// escape apostrophes, if present
					if (stringCode.IndexOf("'", StringComparison.Ordinal) >= 0)
					{
						stringCode = stringCode.Replace("'", "''");
					}

					codedValue = string.Format(_culture, "'{0}'", stringCode);
				}

				string literal = string.Format(_culture, isFloat
					                                         ? "Convert({0}, 'System.Single')"
					                                         : "{0}",
				                               codedValue);
				return literal;
			}

			private static bool IsFloatingPointField(esriFieldType fieldType)
			{
				switch (fieldType)
				{
					case esriFieldType.esriFieldTypeSingle:
					case esriFieldType.esriFieldTypeDouble:
						return true;

					default:
						return false;
				}
			}
		}

		#endregion

		#region Nested type: DomainConstraint

		private abstract class DomainConstraint
		{
			protected DomainConstraint([NotNull] string domainName)
			{
				Assert.ArgumentNotNullOrEmpty(domainName, nameof(domainName));

				DomainName = domainName;
			}

			[NotNull]
			public string DomainName { get; }

			[NotNull]
			public abstract string GetConstraint([NotNull] IField field);
		}

		#endregion

		#region Nested type: RegularDomainConstraint

		private class RegularDomainConstraint : DomainConstraint
		{
			[NotNull] private readonly string _constraintFormat;
			private readonly bool _allowNull;

			public RegularDomainConstraint([NotNull] string domainName,
			                               [NotNull] string constraintFormat,
			                               bool allowNull) : base(
				domainName)
			{
				Assert.ArgumentNotNullOrEmpty(constraintFormat, nameof(constraintFormat));

				_constraintFormat = constraintFormat;
				_allowNull = allowNull;
			}

			public override string GetConstraint(IField field)
			{
				Assert.ArgumentNotNull(field, nameof(field));

				string constraint = string.Format(_constraintFormat, field.Name);

				if (! field.IsNullable || ! _allowNull)
				{
					return constraint;
				}

				return string.Format("{0} IS NULL OR ({1})", field.Name, constraint);
			}
		}

		#endregion
	}
}

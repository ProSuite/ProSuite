using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainServices.AO.QA.Issues
{
	public class IssueTableFields : IIssueTableFieldManagement
	{
		[NotNull] private readonly IDictionary<IssueAttribute, FieldDefinition>
			_definitionsByAttribute;

		[NotNull] private readonly List<Field> _fields;

		private readonly List<IDomain> _domains = new List<IDomain>();

		public IssueTableFields([NotNull] IEnumerable<Field> fields)
		{
			Assert.ArgumentNotNull(fields, nameof(fields));

			_fields = fields.ToList();
			_definitionsByAttribute = _fields.ToDictionary(field => field.Attribute,
			                                               field => field.Definition);
		}

		public string GetName(IssueAttribute attribute, bool optional = false)
		{
			FieldDefinition fieldDefinition = GetFieldDefinition(attribute);

			if (fieldDefinition == null)
			{
				if (optional)
				{
					return null;
				}

				throw new ArgumentException(
					$@"No field definition for attribute {attribute}",
					nameof(attribute));
			}

			return fieldDefinition.Name;
		}

		[CanBeNull]
		private FieldDefinition GetFieldDefinition(IssueAttribute attribute)
		{
			FieldDefinition definition;
			return _definitionsByAttribute.TryGetValue(attribute, out definition)
				       ? definition
				       : null;
		}

		public int GetIndex(IssueAttribute attribute, ITable table, bool optional = false)
		{
			Assert.ArgumentNotNull(table, nameof(table));

			FieldDefinition fieldDefinition = GetFieldDefinition(attribute);

			if (fieldDefinition != null)
			{
				string fieldName = fieldDefinition.Name;

				return GetIndex(fieldName, table, optional);
			}

			if (optional)
			{
				return -1;
			}

			throw new ArgumentException(
				$@"No field definition for attribute {attribute}",
				nameof(attribute));
		}

		public bool HasField(IssueAttribute attribute, ITable table)
		{
			return GetIndex(attribute, table, optional: true) >= 0;
		}

		private int GetIndex([NotNull] string fieldName, [NotNull] ITable table,
		                     // ReSharper disable once ParameterOnlyUsedForPreconditionCheck.Local
		                     bool optional = false)
		{
			int fieldIndex = table.FindField(fieldName);

			if (fieldIndex < 0 && ! optional)
			{
				throw new ArgumentException(
					string.Format("Field {0} not found in table {1}",
					              fieldName,
					              DatasetUtils.GetName(table)),
					nameof(fieldName));
			}

			return fieldIndex;
		}

		public IEnumerable<IField> CreateFields()
		{
			return _fields.Select(field => FieldFactory.CreateField(field.Definition, _domains));
		}

		public IField CreateField(IssueAttribute attribute, bool optional = false)
		{
			FieldDefinition definition = GetFieldDefinition(attribute);

			if (definition == null)
			{
				if (optional)
				{
					return null;
				}

				throw new ArgumentException($@"Field definition not found for {attribute}",
				                            nameof(attribute));
			}

			IField result = FieldFactory.CreateField(definition, _domains);

			return result;
		}

		public void CreateReferencedDomains(IFeatureWorkspace featureWorkspace)
		{
			_domains.Clear();

			foreach (DomainDefinition domainDefinition in GetReferencedDomains())
			{
				IDomain domain = EnsureDomainExists(featureWorkspace, domainDefinition);

				_domains.Add(domain);
			}
		}

		private static IDomain EnsureDomainExists(
			[NotNull] IFeatureWorkspace featureWorkspace,
			[NotNull] DomainDefinition domainDef)
		{
			IDomain result = DomainUtils.GetDomain(featureWorkspace, domainDef.Name);

			if (result != null)
			{
				return result;
			}

			if (domainDef is CodedValueDomainDefinition codedValueDomainDef)
			{
				result = (IDomain) DomainUtils.CreateCodedValueDomain(
					domainDef.Name, domainDef.FieldType,
					codedValueDomainDef.CodedValues.ToArray());

				return DomainUtils.AddDomain(featureWorkspace, result);
			}

			// TODO:
			throw new NotImplementedException("Unsupported domain type");
		}

		private IEnumerable<DomainDefinition> GetReferencedDomains()
		{
			var domainDefinitions = new List<DomainDefinition>();

			foreach (var field in _fields)
			{
				DomainDefinition domainDef = field.Definition.Domain;

				if (domainDef != null)
				{
					string domainName = domainDef.Name;

					if (domainDefinitions.All(dd => dd.Name != domainName))
					{
						domainDefinitions.Add(domainDef);
					}
				}
			}

			return domainDefinitions;
		}

		public class Field
		{
			public Field(IssueAttribute attribute, [NotNull] FieldDefinition definition)
			{
				Assert.ArgumentNotNull(definition, nameof(definition));

				Attribute = attribute;
				Definition = definition;
			}

			public IssueAttribute Attribute { get; }

			[NotNull]
			public FieldDefinition Definition { get; }
		}
	}
}

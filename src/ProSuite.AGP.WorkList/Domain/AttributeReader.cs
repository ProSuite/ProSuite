using System.Collections.Generic;
using ArcGIS.Core.Data;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList.Domain
{
	// todo daro: extract super class?
	public interface IAttributeReader
	{
		T GetValue<T>(Row row, Attributes attribute);
	}

	public class AttributeReader : IAttributeReader
	{
		[NotNull] private readonly IDictionary<Attributes, int> _fieldIndexByAttribute =
			new Dictionary<Attributes, int>();

		[NotNull] private readonly IDictionary<Attributes, string> _fieldNameByIssueAttribute =
			new Dictionary<Attributes, string>();

		public AttributeReader(TableDefinition definition, params Attributes[] attributes)
		{
			// todo daro: add all
			_fieldNameByIssueAttribute.Add(Attributes.IssueCodeDescription, "Description");
			_fieldNameByIssueAttribute.Add(Attributes.IssueCode, "Code");

			foreach (Attributes attribute in attributes)
			{
				// todo daro: inline
				int fieldIndex = definition.FindField(GetName(attribute));
				_fieldIndexByAttribute.Add(attribute, fieldIndex);
			}
		}

		[CanBeNull]
		private string GetName(Attributes attribute)
		{
			return _fieldNameByIssueAttribute.TryGetValue(attribute, out string fieldName)
				       ? fieldName
				       : null;
		}

		[CanBeNull]
		public T GetValue<T>([NotNull] Row row, Attributes attribute)
		{
			return (T) (_fieldIndexByAttribute.TryGetValue(attribute, out int fieldIndex)
				            ? row[fieldIndex]
				            : null);
		}
	}
}

using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;
using ProSuite.DomainServices.AO.QA.Issues;

namespace ProSuite.DomainServices.AO.QA.Standalone.XmlBased.Options
{
	public class FieldConfigurator
	{
		[NotNull] private readonly IDictionary<IssueAttribute, XmlFieldOptions>
			_fieldOptionsByRole = new Dictionary<IssueAttribute, XmlFieldOptions>();

		[NotNull] private readonly IDictionary<string, XmlFieldOptions>
			_fieldOptionsByName = new Dictionary<string, XmlFieldOptions>(
				StringComparer.OrdinalIgnoreCase);

		private const string _rolePrefix = "@";

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public FieldConfigurator(
			[NotNull] IEnumerable<XmlFieldOptions> fieldOptionsCollection)
		{
			Assert.ArgumentNotNull(fieldOptionsCollection, nameof(fieldOptionsCollection));

			foreach (XmlFieldOptions fieldOptions in fieldOptionsCollection)
			{
				if (string.IsNullOrEmpty(fieldOptions.Field))
				{
					continue;
				}

				string trimmedField = fieldOptions.Field.Trim();

				if (trimmedField.StartsWith(_rolePrefix))
				{
					string roleName = trimmedField.Substring(1);

					IssueAttribute role;
					if (! EnumUtils.TryParse(roleName, ignoreCase: true, result: out role))
					{
						_msg.WarnFormat("Unknown attribute role: {0}", roleName);
						continue;
					}

					if (_fieldOptionsByRole.ContainsKey(role))
					{
						_msg.WarnFormat(
							"Duplicate field options configuration for attribute role {0}",
							role);
						continue;
					}

					_fieldOptionsByRole.Add(role, fieldOptions);
				}
				else
				{
					if (_fieldOptionsByName.ContainsKey(trimmedField))
					{
						_msg.WarnFormat("Duplicate field options configuration for field name {0}",
						                trimmedField);
						continue;
					}

					_fieldOptionsByName.Add(trimmedField, fieldOptions);
				}
			}
		}

		public void Configure([NotNull] ITable table,
		                      [NotNull] ITableFields tableFields,
		                      [NotNull] IIssueTableFields fields)
		{
			Assert.ArgumentNotNull(table, nameof(table));
			Assert.ArgumentNotNull(tableFields, nameof(tableFields));
			Assert.ArgumentNotNull(fields, nameof(fields));

			foreach (KeyValuePair<IssueAttribute, XmlFieldOptions> pair in _fieldOptionsByRole)
			{
				int fieldIndex = fields.GetIndex(pair.Key, table, optional: true);
				if (fieldIndex < 0)
				{
					_msg.DebugFormat(
						"No field with role {0} found in {1}; field configuration ignored",
						pair.Key, DatasetUtils.GetName(table));
					continue;
				}

				var fieldInfo = (IFieldInfo) tableFields.FieldInfo[fieldIndex];

				Configure(fieldInfo, pair.Value);
			}

			foreach (KeyValuePair<string, XmlFieldOptions> pair in _fieldOptionsByName)
			{
				int fieldIndex = table.FindField(pair.Key);
				if (fieldIndex < 0)
				{
					_msg.DebugFormat("Field not found in {0}: {1}; field configuration ignored",
					                 DatasetUtils.GetName(table),
					                 pair.Key);
					continue;
				}

				var fieldInfo = (IFieldInfo) tableFields.FieldInfo[fieldIndex];

				Configure(fieldInfo, pair.Value);
			}
		}

		private static void Configure([NotNull] IFieldInfo fieldInfo,
		                              [NotNull] XmlFieldOptions fieldOptions)
		{
			if (StringUtils.IsNotEmpty(fieldOptions.AliasName))
			{
				fieldInfo.Alias = fieldOptions.AliasName.Trim();
			}

			if (fieldOptions.Visible != TrueFalseDefault.@default)
			{
				fieldInfo.Visible = fieldOptions.Visible == TrueFalseDefault.@true;
			}

#if Server11 || ARCGIS_12_0_OR_GREATER
			IFieldInfo fieldInfoExt = fieldInfo;
#else
			IFieldInfo3 fieldInfoExt = (IFieldInfo3) fieldInfo;
#endif

			if (fieldOptions.Highlight != TrueFalseDefault.@default)
			{
				fieldInfoExt.Highlight = fieldOptions.Highlight == TrueFalseDefault.@true;
			}

			if (fieldOptions.ReadOnly != TrueFalseDefault.@default)
			{
				fieldInfoExt.Readonly = fieldOptions.ReadOnly == TrueFalseDefault.@true;
			}
		}
	}
}

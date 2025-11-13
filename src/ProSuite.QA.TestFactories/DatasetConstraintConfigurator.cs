using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Text;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.Core;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Tests.Constraints;

namespace ProSuite.QA.TestFactories
{
	public class DatasetConstraintConfigurator
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private const string _nullValue = "<NULL>";

		private string _subtypeField;
		private Dictionary<int, string> _subtypes;

		[NotNull]
		public static DatasetConstraintConfigurator Create([NotNull] TextReader reader,
		                                                   [NotNull] IList<Dataset> datasets)
		{
			Assert.ArgumentNotNull(reader, nameof(reader));
			Assert.ArgumentNotNull(datasets, nameof(datasets));

			var configurator = new DatasetConstraintConfigurator();

			configurator.CreateCore(reader, datasets);

			return configurator;
		}

		#region Constructors

		protected DatasetConstraintConfigurator() { }

		public DatasetConstraintConfigurator([NotNull] ObjectDataset dataset,
		                                     [NotNull] IList<ConstraintNode> constraints)
		{
			Dataset = dataset;
			Constraints = constraints;
		}

		public DatasetConstraintConfigurator(
			[NotNull] IEnumerable<TestParameterValue> parameterValues)
		{
			Assert.ArgumentNotNull(parameterValues, nameof(parameterValues));

			var constraints = new List<string>();

			foreach (TestParameterValue value in parameterValues)
			{
				if (Equals(value.TestParameterName, QaDatasetConstraintFactoryDefinition.TableAttribute))
				{
					Assert.Null(Dataset,
					            "Multiple Attribute " +
					            QaDatasetConstraintFactoryDefinition.TableAttribute);

					Dataset = (ObjectDataset) ((DatasetTestParameterValue) value).DatasetValue;
				}
				else if (Equals(value.TestParameterName,
				                QaDatasetConstraintFactoryDefinition.ConstraintAttribute))
				{
					constraints.Add(value.StringValue);
				}
				else
				{
					Assert.False(true, "Unexpected attribute");
				}
			}

			Assert.NotNull(Dataset,
			               "Missing Attribute: {0}", QaDatasetConstraintFactoryDefinition.TableAttribute);

			Constraints = HierarchicalConstraintUtils.GetConstraintHierarchy(constraints);
		}

		#endregion

		public ObjectDataset Dataset { get; private set; }

		public IList<ConstraintNode> Constraints { get; private set; }

		public string GeneralConstraintColumnName { get; set; } = "<Generell>";

		public string NonApplicableValueName { get; set; } = "k_W";

		public string UnknownValueName { get; set; } = "ub";

		public static string NullValue => _nullValue;

		public string ToCsv()
		{
			var csvBuilder = new StringBuilder();

			Dictionary<string, FieldValues> fieldNames = GetFieldNames();

			// Fields
			csvBuilder.AppendFormat("{0};;{1};", Dataset.Name, GeneralConstraintColumnName);
			foreach (FieldValues field in fieldNames.Values)
			{
				csvBuilder.AppendFormat("{0};", field.Name);
				int nConstr = field.CodeNames.Count;
				for (var iConstr = 1; iConstr < nConstr; iConstr++)
				{
					csvBuilder.Append(";");
				}
			}

			csvBuilder.AppendLine();

			// Field CodeNames
			csvBuilder.AppendFormat(";;;");
			foreach (FieldValues field in fieldNames.Values)
			{
				int nConstr = field.CodeNames.Count;
				if (nConstr == 0)
				{
					csvBuilder.Append(";");
				}
				else
				{
					for (var iConstr = 0; iConstr < nConstr; iConstr++)
					{
						csvBuilder.AppendFormat("{0};", field.CodeNames[iConstr]);
					}
				}
			}

			csvBuilder.AppendLine();

			string generalCondition = null;
			var domainConditions = new Dictionary<string, IList<int>>();

			foreach (ConstraintNode node in Constraints)
			{
				AppendConstraintNode(node, fieldNames, csvBuilder, ref generalCondition,
				                     domainConditions);
			}

			AppendConditions(csvBuilder, null, null,
			                 ref generalCondition, domainConditions, fieldNames);

			return csvBuilder.ToString();
		}

		public QualityCondition ToQualityCondition()
		{
			var clsDesc = new ClassDescriptor(typeof(QaDatasetConstraintFactory));
			var testDesc = new TestDescriptor("QaDatasetConstraintFactory", clsDesc);
			var qc = new QualityCondition("qc_dataset_" + Dataset.Name, testDesc);

			InstanceConfigurationUtils.AddParameterValue(
				qc, QaDatasetConstraintFactoryDefinition.TableAttribute, Dataset);
			AddParameters(qc, Constraints, "");

			return qc;
		}

		#region Non-public methods

		protected string SubtypeField
		{
			get
			{
				if (_subtypeField == null)
				{
					GetFieldNamesCore();
				}

				return _subtypeField;
			}
		}

		protected Dictionary<int, string> Subtypes
		{
			get
			{
				if (_subtypes == null)
				{
					GetFieldNamesCore();
				}

				return _subtypes;
			}
		}

		protected void CreateCore([NotNull] TextReader reader,
		                          [NotNull] IEnumerable<Dataset> datasets)
		{
			Assert.ArgumentNotNull(reader, nameof(reader));
			Assert.ArgumentNotNull(datasets, nameof(datasets));

			string attributesLine = reader.ReadLine();

			Assert.NotNull(attributesLine, "attributesLine");

			string[] attributesStrings = attributesLine.Split(';');

			string conditionLine = reader.ReadLine();

			Assert.NotNull(conditionLine, "conditionLine");

			string[] conditionStrings = conditionLine.Split(';');

			string datasetName = attributesStrings[0];

			Dataset = Assert.NotNull(
				ConfiguratorUtils.GetDataset<ObjectDataset>(datasetName, datasets),
				"Dataset {0} not found", datasetName);

			IObjectClass objectClass = ConfiguratorUtils.OpenFromDefaultDatabase(Dataset);

			Assert.True(string.IsNullOrEmpty(attributesStrings[1]), "Invalid Matrix Format");

			Assert.True(string.IsNullOrEmpty(attributesStrings[2]) ||
			            attributesStrings[2].Equals(GeneralConstraintColumnName,
			                                        StringComparison.InvariantCultureIgnoreCase),
			            "Invalid Matrix Format");

			Assert.True(string.IsNullOrEmpty(conditionStrings[0]),
			            "Invalid Matrix Header Format: expected '', got '{0}'",
			            conditionStrings[0]);
			Assert.True(string.IsNullOrEmpty(conditionStrings[1]),
			            "Invalid Matrix Header Format: expected '', got '{0}'",
			            conditionStrings[1]);
			Assert.True(string.IsNullOrEmpty(conditionStrings[2]),
			            "Invalid Matrix Header Format: expected '', got '{0}'",
			            conditionStrings[0]);
			//if (fieldName == null)
			//{
			//    fieldName = ((ISubtypes)table).SubtypeFieldName;
			//}
			//int fieldIdx = table.FindField(fieldName);
			//IField field = table.Fields.get_Field(fieldIdx);

			IList<Code> possibleCodes = GetCodes(attributesStrings, conditionStrings,
			                                     objectClass);

			int n = possibleCodes.Count;

			string attr0 = null;
			SortedDictionary<string, object> codeValues = null;
			var allConstraints = new List<ConstraintNode>();

			for (string codeLine = reader.ReadLine();
			     codeLine != null;
			     codeLine = reader.ReadLine())
			{
				IList<string> codes = codeLine.Split(';');

				string attr = codes[0];
				string code = codes[1];

				var rowConstraints = new List<ConstraintNode>();

				string generelConstraint = codes[2];
				if (string.IsNullOrEmpty(generelConstraint) == false)
				{
					rowConstraints.Add(new ConstraintNode(generelConstraint));
				}

				var allowedCodes = new List<Code>();
				for (var i = 0; i < n; i++)
				{
					string value = codes[i + 3].Trim();
					if (possibleCodes[i] == null)
					{
						Assert.True(string.IsNullOrEmpty(value),
						            "Unhandled value {0} for empty Code",
						            value);
					}
					else if (value == "1")
					{
						allowedCodes.Add(possibleCodes[i]);
					}
					else if (value == "0") { }
					else
					{
						Assert.True(false, "Unhandled value {0}", value);
					}
				}

				allowedCodes.Sort(Code.SortField);

				string field0 = null;
				List<Code> fieldCodes = null;
				foreach (Code allowedCode in allowedCodes)
				{
					if (allowedCode.Field.Equals(field0,
					                             StringComparison.InvariantCultureIgnoreCase) ==
					    false)
					{
						if (fieldCodes != null)
						{
							ConstraintNode node = GetConstraint(fieldCodes);
							if (node != null)
							{
								rowConstraints.Add(node);
							}
						}

						fieldCodes = new List<Code>();
						field0 = allowedCode.Field;
					}

					Assert.NotNull(fieldCodes, "fieldCodes is null");
					fieldCodes.Add(allowedCode);
				}

				if (fieldCodes != null)
				{
					rowConstraints.Add(GetConstraint(fieldCodes));
				}

				if (string.IsNullOrEmpty(code))
				{
					if (string.IsNullOrEmpty(attr))
					{
						allConstraints.AddRange(rowConstraints);
					}
					else
					{
						var constraintConstraint = new ConstraintNode(attr);

						foreach (ConstraintNode constraint in allConstraints)
						{
							constraintConstraint.Nodes.Add(constraint);
						}
					}
				}
				else
				{
					if (! string.IsNullOrEmpty(attr) &&
					    ! attr.Equals(attr0, StringComparison.InvariantCultureIgnoreCase))
					{
						codeValues = GetCodeValues(attr, objectClass);
						attr0 = attr;
					}

					Assert.NotNull(codeValues, "No coded value field defined");

					code = AdaptCode(attr0, code, codeValues);

					Code lineCode = code != _nullValue
						                ? new Code(attr0, code, codeValues[code])
						                : new Code(attr0, codeValues[_nullValue]);

					string constraint = string.Format("{0} = {1}", attr0, lineCode.CodeIdString());
					var codeConstraint = new ConstraintNode(constraint);

					foreach (ConstraintNode node in rowConstraints)
					{
						codeConstraint.Nodes.Add(node);
					}

					allConstraints.Add(codeConstraint);
				}
			}

			Constraints = allConstraints;
		}

		protected Dictionary<string, FieldValues> GetFieldNames()
		{
			return GetFieldNamesCore();
		}

		protected virtual Dictionary<string, FieldValues> GetFieldNamesCore()
		{
			IObjectClass objectClass = ConfiguratorUtils.OpenFromDefaultDatabase(Dataset);

			var subs = (ISubtypes) objectClass;

			var fieldNames = new Dictionary<string, FieldValues>();

			// get coded value domains 
			int iSub = subs.SubtypeFieldIndex;
			IObjectClass table = objectClass;

			IFields fields = table.Fields;
			int fieldCount = fields.FieldCount;

			for (var fieldIndex = 0; fieldIndex < fieldCount; fieldIndex++)
			{
				if (fieldIndex == iSub && _subtypes == null)
				{
					_subtypeField = subs.SubtypeFieldName;

					SortedDictionary<string, object> t =
						GetCodeValues(_subtypeField, (IObjectClass) subs);
					_subtypes = new Dictionary<int, string>();

					foreach (KeyValuePair<string, object> pair in t)
					{
						Subtypes.Add((int) pair.Value, pair.Key);
					}

					continue;
				}

				IField field = fields.Field[fieldIndex];

				if (field.Domain is ICodedValueDomain)
				{
					fieldNames.Add(field.Name.ToUpper(),
					               new FieldValues(field, fieldIndex, NullValue));
				}
			}

			return fieldNames;
		}

		protected bool TryGetSubtype(string condition, out string code)
		{
			code = null;
			IList<string> terms = Parse(condition);

			if (terms.Count == 3 && terms[1] == "=")
			{
				bool success = TryGetSubtype(terms, out code);
				return success;
			}

			return false;
		}

		private static void AddParameters(QualityCondition qualityCondition,
		                                  IEnumerable<ConstraintNode> nodes, string prefix)
		{
			Assert.ArgumentNotNull(qualityCondition, nameof(qualityCondition));
			Assert.ArgumentNotNull(nodes, nameof(nodes));

			string subPrefix = prefix + "+";
			foreach (ConstraintNode node in nodes)
			{
				InstanceConfigurationUtils.AddParameterValue(
					qualityCondition, QaDatasetConstraintFactoryDefinition.ConstraintAttribute,
					prefix + node.Condition);

				AddParameters(qualityCondition, node.Nodes, subPrefix);
			}
		}

		private void AppendConstraintNode(ConstraintNode node,
		                                  Dictionary<string, FieldValues> fieldNames,
		                                  StringBuilder csvBuilder,
		                                  ref string generalCondition,
		                                  Dictionary<string, IList<int>> domainConditions)
		{
			string condition = node.Condition;

			if (node.Nodes.Count == 0)
			{
				AddCondition(csvBuilder, null, null, condition, fieldNames,
				             ref generalCondition, domainConditions);
			}
			else
			{
				string field = null;
				string code = null;

				IList<string> terms = Parse(condition);

				if (terms.Count == 3 && terms[1] == "=")
				{
					FieldValues fieldValues;

					if (TryGetSubtype(terms, out code))
					{
						field = _subtypeField;
					}

					if (field == null &&
					    fieldNames.TryGetValue(terms[0].ToUpper(), out fieldValues))
					{
						int idx = fieldValues.IndexOfCodeValueString(terms[2].Trim('\''));
						if (idx >= 0)
						{
							field = fieldValues.Name;
							code = fieldValues.CodeNames[idx];
						}
					}
				}

				if (field == null)
				{
					field = condition;
					code = null;
				}

				string codeGenerellCondition = null;
				var codeDomainConditions =
					new Dictionary<string, IList<int>>();

				foreach (ConstraintNode codeNode in node.Nodes)
				{
					AddCondition(csvBuilder, field, code, codeNode.Condition, fieldNames,
					             ref codeGenerellCondition, codeDomainConditions);
				}

				AppendConditions(csvBuilder, field, code,
				                 ref codeGenerellCondition, codeDomainConditions, fieldNames);
			}
		}

		private bool TryGetSubtype([NotNull] IList<string> terms, out string code)
		{
			code = null;

			if (terms[0].Equals(_subtypeField, StringComparison.InvariantCultureIgnoreCase))
			{
				if (int.TryParse(terms[2], out int idx))
				{
					if (Subtypes.TryGetValue(idx, out code))
					{
						return true;
					}
				}
			}

			return false;
		}

		private static void AddCondition(StringBuilder csv, string attribute, string code,
		                                 string condition,
		                                 Dictionary<string, FieldValues> fieldNames,
		                                 ref string generellCondition,
		                                 Dictionary<string, IList<int>> domainConditions)
		{
			FieldValues fieldValues;
			IList<int> idx = DomainCondition(condition, fieldNames, out fieldValues);

			if (idx == null)
			{
				// generell condition
				if (bool.TryParse(condition, out bool bCond) && bCond)
				{
					condition = "";
				}

				if (generellCondition != null)
				{
					AppendConditions(csv, attribute, code, ref generellCondition, domainConditions,
					                 fieldNames);
				}

				generellCondition = condition;
			}
			else
			{
				domainConditions.Add(fieldValues.Name, idx);
			}
		}

		private static List<int> DomainCondition(string condition,
		                                         IDictionary<string, FieldValues> fieldNames,
		                                         out FieldValues fieldValues)
		{
			fieldValues = null;

			IList<string> terms = Parse(condition);
			int nTerms = terms.Count;

			string field;
			bool isNull;
			int iStart;
			if (terms[0].Equals("IsNull", StringComparison.InvariantCultureIgnoreCase))
			{
				if (! (nTerms > 7))
				{
					return null;
				}

				if (! Equals(terms[1], "("))
				{
					return null;
				}

				if (! Equals(terms[3], ","))
				{
					return null;
				}

				if (! Equals(terms[5], ")"))
				{
					return null;
				}

				field = terms[2];
				isNull = true;

				iStart = 6;
			}
			else
			{
				field = terms[0];
				isNull = false;

				iStart = 1;
			}

			field = field.ToUpper();

			if (fieldNames.TryGetValue(field.ToUpper(), out fieldValues) == false)
			{
				return null;
			}

			var codes = new List<int>();
			if (terms[iStart] == "=")
			{
				if (iStart != nTerms - 2)
				{
					return null;
				}

				string subCode = isNull == false
					                 ? terms[nTerms - 1].Trim('\'')
					                 : _nullValue;

				int idx = fieldValues.IndexOfCodeValueString(subCode);

				codes.Add(idx);
			}
			else if (terms[iStart].Equals("in", StringComparison.InvariantCultureIgnoreCase))
			{
				if (! Equals(terms[iStart + 1], "("))
				{
					return null;
				}

				if (! Equals(terms[nTerms - 1], ")"))
				{
					return null;
				}

				for (int iTerm = iStart + 2; iTerm < nTerms - 1; iTerm += 2)
				{
					if (! (iTerm == nTerms - 2 || terms[iTerm + 1] == ","))
					{
						return null;
					}

					string subCode = terms[iTerm].Trim('\'');
					int idx = fieldValues.IndexOfCodeValueString(subCode);
					codes.Add(idx);
				}

				if (isNull)
				{
					int idx = fieldValues.IndexOfCodeValueString(_nullValue);
					codes.Add(idx);
				}
			}
			else
			{
				return null;
			}

			return codes;
		}

		private static void AppendConditions(StringBuilder csv, string field, string code,
		                                     ref string generalCondition,
		                                     Dictionary<string, IList<int>> domainConditions,
		                                     Dictionary<string, FieldValues> fieldNames)
		{
			if (generalCondition == null && domainConditions.Count == 0)
			{
				return;
			}

			csv.AppendFormat("{0};{1};{2};", field, code, generalCondition);

			foreach (KeyValuePair<string, FieldValues> fieldValues in fieldNames)
			{
				var codes = new int[fieldValues.Value.CodeNames.Count];

				if (domainConditions.ContainsKey(fieldValues.Key))
				{
					IList<int> allowedPositions = domainConditions[fieldValues.Key];

					foreach (int allowed in allowedPositions)
					{
						codes[allowed] = 1;
					}
				}

				foreach (int i in codes)
				{
					csv.AppendFormat("{0};", i);
				}
			}

			csv.AppendLine();

			generalCondition = null;
			domainConditions.Clear();
		}

		[CanBeNull]
		private ConstraintNode GetConstraint([CanBeNull] IList<Code> allowedCodes)
		{
			string condition;
			if (allowedCodes == null || allowedCodes.Count == 0)
			{
				condition = null;
			}
			else if (allowedCodes.Count == 1)
			{
				Code code = allowedCodes[0];
				if (code.IsNull)
				{
					condition = string.Format("ISNULL({0},{1}) = {1}",
					                          code.Field, code.CodeIdString());
				}
				else if (code.CodeName == GeneralConstraintColumnName)
				{
					condition = string.Format("ISNULL({0},{1}) <> {1}",
					                          code.Field, code.CodeIdString());
				}
				else
				{
					condition = string.Format("{0} = {1}", code.Field, code.CodeIdString());
				}
			}
			else
			{
				condition = GetGeneralCondition(allowedCodes);

				if (condition == null)
				{
					var sb = new StringBuilder();

					string field = allowedCodes[0].Field;
					string existing = null;
					Code nullCode = null;

					foreach (Code code in allowedCodes)
					{
						if (code.CodeName == GeneralConstraintColumnName)
						{
							Assert.False(true, "Unhandled general code");
						}

						if (code.IsNull == false)
						{
							if (sb.Length > 0)
							{
								sb.Append(",");
							}

							existing = code.CodeIdString();
							sb.Append(existing);
						}
						else
						{
							nullCode = code;
						}
					}

					if (nullCode == null)
					{
						condition = string.Format("{0} IN ({1})", field, sb);
					}
					else
					{
						condition = string.Format("ISNULL({0},{1}) IN ({2})",
						                          field, existing, sb);
					}
				}
			}

			ConstraintNode node = condition != null
				                      ? new ConstraintNode(condition)
				                      : null;

			return node;
		}

		[CanBeNull]
		private string GetGeneralCondition([NotNull] ICollection<Code> allowedCodes)
		{
			Assert.ArgumentNotNull(allowedCodes, nameof(allowedCodes));

			if (allowedCodes.Count != 2)
			{
				return null;
			}

			string condition = null;
			Code generell = null;
			Code nullCode = null;
			Code keinWertCode = null;
			foreach (Code code in allowedCodes)
			{
				if (code.CodeName == GeneralConstraintColumnName)
				{
					generell = code;
				}
				else if (code.IsNull)
				{
					nullCode = code;
				}
				else if (code.CodeName == NonApplicableValueName)
				{
					keinWertCode = code;
				}
			}

			if (generell != null)
			{
				if (nullCode != null)
				{
					condition = string.Format("IsNull({0},{1}) <> {2}",
					                          generell.Field, nullCode.CodeIdString(),
					                          generell.CodeIdString());
				}
				else if (keinWertCode != null)
				{
					condition = string.Format("{0} = {0}", generell.Field);
					// returns false for DbNull.Value
				}
				else
				{
					Assert.False(true, "Unhandled generell code");
				}
			}

			return condition;
		}

		[NotNull]
		private IList<Code> GetCodes([NotNull] IList<string> attributesStrings,
		                             [NotNull] IList<string> conditionStrings,
		                             [NotNull] IObjectClass table)
		{
			int n = attributesStrings.Count;
			string attrName0 = null;
			SortedDictionary<string, object> codeValues = null;

			var result = new Code[n - 3];

			for (var i = 3; i < n; i++)
			{
				string attrName = attributesStrings[i];
				if (attrName != attrName0 && string.IsNullOrEmpty(attrName) == false)
				{
					codeValues = GetCodeValues(attrName, table);

					attrName0 = attrName;
				}

				Assert.True(codeValues != null, "No coded value field defined");

				string codeName = conditionStrings[i];
				if (string.IsNullOrEmpty(codeName))
				{
					continue;
				}

				codeName = AdaptCode(attrName0, codeName, codeValues);

				Code code = codeName != _nullValue
					            ? new Code(attrName0, codeName, codeValues[codeName])
					            : new Code(attrName0, codeValues[_nullValue]);

				result[i - 3] = code;
			}

			return result;
		}

		[NotNull]
		private string AdaptCode([NotNull] string attrName,
		                         [NotNull] string constraint,
		                         [NotNull] SortedDictionary<string, object> codeValues)
		{
			// TODO make configurable
			switch (constraint)
			{
				case "kein Wert":
					constraint = NonApplicableValueName;
					break;

				case "unbekannt":
					constraint = UnknownValueName;
					break;

				case "nicht erfasst":
					constraint = _nullValue;
					break;
			}

			// Assert existing code
			var exists = false;

			foreach (string code in codeValues.Keys)
			{
				if (! code.Equals(constraint, StringComparison.InvariantCultureIgnoreCase))
				{
					continue;
				}

				constraint = code;
				exists = true;
				break;
			}

			if (! exists)
			{
				double constraintValue;
				// TODO InvariantCulture?
				bool isNumeric = double.TryParse(constraint, out constraintValue);

				if (isNumeric)
				{
					foreach (string code in codeValues.Keys)
					{
						// TODO InvariantCulture?
						if (double.TryParse(code, out double codeValue) &&
						    Math.Abs(codeValue - constraintValue) < double.Epsilon)
						{
							constraint = code;
							Assert.False(
								exists, "Non unique condition {0} for attribute {1}, domain {2}",
								constraint, attrName, GetDomainName(attrName));
							exists = true;
						}
					}
				}
			}

			Assert.True(exists || constraint == _nullValue,
			            "Invalid Condition {0} for attribute {1}, domain {2}",
			            constraint, attrName, GetDomainName(attrName));
			return constraint;
		}

		[NotNull]
		private string GetDomainName([NotNull] string fieldName)
		{
			IObjectClass objectClass = ConfiguratorUtils.OpenFromDefaultDatabase(Dataset);

			int fieldIndex = objectClass.FindField(fieldName);
			IField field = objectClass.Fields.Field[fieldIndex];

			IDomain domain = field.Domain;

			return domain == null
				       ? "No Domain"
				       : domain.Name;
		}

		[NotNull]
		private SortedDictionary<string, object> GetCodeValues(
			[NotNull] string fieldName, [NotNull] IObjectClass objectClass)
		{
			int fieldIndex = objectClass.FindField(fieldName);

			Assert.True(fieldIndex >= 0, "Unknown field {0}", fieldName);

			IField field = objectClass.Fields.Field[fieldIndex];

			return GetCodeValuesCore(field, objectClass);
		}

		[NotNull]
		protected virtual SortedDictionary<string, object> GetCodeValuesCore(
			[NotNull] IField field, [NotNull] IObjectClass table)
		{
			_msg.VerboseDebug(() => $"Reading values for field {field.Name}");

			SortedDictionary<string, object> codeValues;
			var codedValueDomain = field.Domain as ICodedValueDomain;

			if (codedValueDomain == null)
			{
				int fieldIndex = table.FindField(field.Name);

				if (table is ISubtypes subtypesTbl &&
				    subtypesTbl.SubtypeFieldIndex == fieldIndex)
				{
					IList<Subtype> subtypes = DatasetUtils.GetSubtypes(table);
					codeValues = new SortedDictionary<string, object>();
					foreach (Subtype subtype in subtypes)
					{
						codeValues.Add(subtype.Name, subtype.Code);
					}

					return codeValues;
				}
			}

			Assert.True(codedValueDomain != null, "Field '{0}' not a coded value domain",
			            field.Name);

			SortedDictionary<object, string> valueCodes
				= DomainUtils.GetCodedValueMap<object>(codedValueDomain);

			codeValues = new SortedDictionary<string, object>();

			foreach (KeyValuePair<object, string> pair in valueCodes)
			{
				_msg.VerboseDebug(
					() => $"Adding key / value pair {pair.Value} / {pair.Key} to result");

				codeValues.Add(pair.Value, pair.Key);
			}

			Assert.True(codeValues.ContainsKey(_nullValue) == false,
			            _nullValue + " exists in domain of field " + field.Name);

			// add <Null> Value
			object nullObject = GetNullObject(field, valueCodes);
			codeValues.Add(_nullValue, nullObject);

			return codeValues;
		}

		private static object GetNullObject([NotNull] IField field,
		                                    [NotNull] IDictionary<object, string> valueCodes)
		{
			object nullObject = null;
			object testObject = null;
			while (nullObject == null)
			{
				testObject = GetTestObject(field.Type, testObject);

				if (testObject != null && ! valueCodes.TryGetValue(testObject, out _))
				{
					nullObject = testObject;
				}
			}

			return nullObject;
		}

		[CanBeNull]
		private static object GetTestObject(esriFieldType fieldType,
		                                    [CanBeNull] object testObject)
		{
			switch (fieldType)
			{
				case esriFieldType.esriFieldTypeString:
					return testObject == null
						       ? "<NULL>"
						       : (string) testObject + "_";

				case esriFieldType.esriFieldTypeDouble:
					return testObject == null
						       ? -1.0
						       : (double) testObject - 1.0;

				case esriFieldType.esriFieldTypeSingle:
					return testObject == null
						       ? -1.0f
						       : (float) ((float) testObject - 1.0);

				case esriFieldType.esriFieldTypeInteger:
					return testObject == null
						       ? -1
						       : (int) testObject - 1;

				case esriFieldType.esriFieldTypeSmallInteger:
					return testObject == null
						       ? (short) -1
						       : (short) ((short) testObject - 1);

				case esriFieldType.esriFieldTypeDate:
					return testObject == null
						       ? DateTime.MinValue
						       : (DateTime) testObject + new TimeSpan(1, 0, 0, 0);

				default:
					return testObject;
			}
		}

		[NotNull]
		protected static IList<string> Parse([NotNull] string expression)
		{
			string parse = expression.Replace(_nullValue, " #NULL# ");

			IList<string> terms = Parse(parse,
			                            new[] {'\''},
			                            new char[] { },
			                            new[] {',', ':', '(', ')', '=', '<', '>'});
			int termsCount = terms.Count;

			for (var termIndex = 0; termIndex < termsCount; termIndex++)
			{
				if (terms[termIndex] == "#NULL#")
				{
					terms[termIndex] = _nullValue;
				}
			}

			return terms;
		}

		[NotNull]
		private static IList<string> Parse([NotNull] string expression,
		                                   [NotNull] ICollection<char> stringDelimiters,
		                                   [NotNull] ICollection<char> escapeChars,
		                                   [NotNull] ICollection<char> specialChars)
		{
			var iStart = 0;
			int nPos = expression.Length;
			var iPos = 0;
			var terms = new List<string>();

			while (iPos < nPos)
			{
				char c = expression[iPos];
				if (stringDelimiters.Contains(c))
				{
					if (iStart != iPos)
					{
						string error = GetError(expression, iPos);
						throw new InvalidExpressionException(error);
					}

					iPos++;
					while (iPos < nPos && c != expression[iPos])
					{
						if (escapeChars.Contains(expression[iPos]))
						{
							iPos++;
						}

						iPos++;
					}

					if (iPos >= nPos)
					{
						string error = GetError(expression, iStart);
						throw new InvalidExpressionException(error);
					}

					terms.Add(expression.Substring(iStart, iPos + 1 - iStart));
					iStart = iPos + 1;

					if (iStart >= nPos ||
					    char.IsWhiteSpace(expression[iStart]) ||
					    specialChars.Contains(expression[iStart])) { }
					else
					{
						string error = GetError(expression, iPos);
						throw new InvalidExpressionException(error);
					}
				}
				else if (char.IsWhiteSpace(c))
				{
					if (iPos != iStart)
					{
						terms.Add(expression.Substring(iStart, iPos - iStart));
					}

					iStart = iPos + 1;
				}
				else if (specialChars.Contains(c))
				{
					if (iPos > iStart)
					{
						terms.Add(expression.Substring(iStart, iPos - iStart));
					}

					terms.Add(expression.Substring(iPos, 1));
					iStart = iPos + 1;
				}

				iPos++;
			}

			if (iPos > iStart)
			{
				terms.Add(expression.Substring(iStart, iPos - iStart));
			}

			return terms;
		}

		[NotNull]
		private static string GetError([NotNull] string expression, int iStart)
		{
			var sb = new StringBuilder();

			for (var i = 0; i < Math.Min(3, iStart); i++)
			{
				sb.Append(".");
			}

			var complete = false;
			var n = 10;

			if (expression.Length - iStart < 13)
			{
				n = expression.Length - iStart;
				complete = true;
			}

			for (int i = iStart; i < iStart + n; i++)
			{
				sb.Append(expression[i]);
			}

			if (complete == false)
			{
				sb.Append("...");
			}

			string error = string.Format("{0}" + Environment.NewLine +
			                             "Invalid expression near position {1}" +
			                             Environment.NewLine +
			                             "{2}", expression, iStart, sb);
			return error;
		}

		#endregion

		#region nested classes

		protected class FieldValues
		{
			[NotNull] private readonly IField _field;
			private IList<string> _codeNames;
			private IList<object> _codeValues;
			private readonly string _nullVal;

			public FieldValues([NotNull] IField field,
			                   int fieldIndex,
			                   [NotNull] string nullValue)
			{
				Assert.ArgumentNotNull(field, nameof(field));
				Assert.ArgumentNotNullOrEmpty(nullValue, nameof(nullValue));

				_field = field;
				FieldIndex = fieldIndex;
				_nullVal = nullValue;
			}

			public FieldValues([NotNull] IField field, int fieldIndex,
			                   [NotNull] string nullValue,
			                   IList<string> codeNames, IList<object> codeValues)
				: this(field, fieldIndex, nullValue)
			{
				_codeNames = codeNames;
				_codeValues = codeValues;
			}

			[NotNull]
			public string Name => _field.Name;

			public int FieldIndex { get; }

			[NotNull]
			public IList<string> CodeNames
			{
				get
				{
					if (_codeNames == null)
					{
						GetNameValues();
					}

					return Assert.NotNull(_codeNames, "_codeNames");
				}
			}

			[NotNull]
			public IList<object> CodeValues
			{
				get
				{
					if (_codeValues == null)
					{
						GetNameValues();
					}

					return Assert.NotNull(_codeValues, "_codeValues");
				}
			}

			public string CodeName(string codeValue)
			{
				int idx = IndexOfCodeValueString(codeValue);
				return idx >= 0
					       ? CodeNames[idx]
					       : null;
			}

			public int IndexOfCodeValueString(string codeValue)
			{
				for (var i = 0; i < CodeValues.Count; i++)
				{
					object value = CodeValues[i];
					string sVal = string.Format("{0}", value);

					if (sVal == codeValue)
					{
						return i;
					}

					if (value == DBNull.Value && codeValue == _nullVal)
					{
						return i;
					}
				}

				return -1;
			}

			private void GetNameValues()
			{
				_codeNames = new List<string>();
				_codeValues = new List<object>();

				var cdDomain = (ICodedValueDomain) _field.Domain;
				Assert.True(cdDomain != null, "Invalid field type for " + Name);

				int codesCount = cdDomain.CodeCount;
				for (var codeIndex = 0; codeIndex < codesCount; codeIndex++)
				{
					_codeNames.Add(cdDomain.Name[codeIndex]);
					_codeValues.Add(cdDomain.Value[codeIndex]);
				}

				_codeNames.Add(_nullVal);
				_codeValues.Add(DBNull.Value);
			}
		}

		private class Code
		{
			// TODO revise: invariant culture? current culture?
			private static readonly CultureInfo _culture = new CultureInfo("", false);

			private readonly object _codeId;

			public static int SortField(Code x, Code y)
			{
				return string.Compare(x.Field, y.Field, StringComparison.OrdinalIgnoreCase);
			}

			public Code([NotNull] string fieldName,
			            [CanBeNull] string codeName,
			            [CanBeNull] object codeId)
			{
				Field = fieldName;
				CodeName = codeName;
				_codeId = codeId;

				IsNull = false;
			}

			public Code([NotNull] string fieldName, [CanBeNull] object nonCodeValue)
			{
				Field = fieldName;
				_codeId = nonCodeValue;

				IsNull = true;
			}

			[NotNull]
			public string Field { get; }

			[CanBeNull]
			public string CodeName { get; }

			public string CodeIdString()
			{
				string val;
				if (_codeId is string)
				{
					val = string.Format("'{0}'", _codeId);
				}
				else
				{
					val = string.Format(_culture, "{0}", _codeId);

					// TODO use _culture as FormatProvider?
					if (! double.TryParse(val, out _))
					{
						val = string.Format("'{0}'", _codeId);
					}
				}

				return val;
			}

			public bool IsNull { get; }
		}

		#endregion
	}
}

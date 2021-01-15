using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Core;

namespace ProSuite.QA.TestFactories
{
	public class LineConnectionConfigurator
	{
		private const string _dummy = "dummy";
		private const string _false = "false";
		private const string _importedQualityCondition = "<imported_quality_condition>";
		private const string _importedTestDescriptor = "<imported_test_descriptor>";
		private const string _separator = ";";
		private const string _true = "true";

		public static readonly string FeatureClassesParamName = "featureClasses";
		public static readonly string RulesParamName = "rules";

		[NotNull]
		public QualityCondition Convert([NotNull] Matrix matrix,
		                                [NotNull] IList<Dataset> datasets)
		{
			Assert.ArgumentNotNull(matrix, "matrix");
			Assert.ArgumentNotNull(datasets, "datasets");

			var classDescriptor = new ClassDescriptor(typeof(QaLineConnection));

			var testDescriptor = new TestDescriptor(
				_importedTestDescriptor, classDescriptor);

			var qualityCondition = new QualityCondition(
				_importedQualityCondition, testDescriptor);

			Dictionary<string, VectorDataset> lineClasses = GetLineClasses(matrix, datasets);
			Dictionary<string, VectorDataset> nodeClasses = GetNodeClasses(matrix, datasets);

			foreach (KeyValuePair<string, VectorDataset> pair in lineClasses)
			{
				QualityCondition_Utils.AddParameterValue(qualityCondition, FeatureClassesParamName,
				                                         pair.Value);
			}

			foreach (KeyValuePair<string, VectorDataset> pair in nodeClasses)
			{
				QualityCondition_Utils.AddParameterValue(qualityCondition, FeatureClassesParamName,
				                                         pair.Value);
			}

			foreach (KeyValuePair<string, VectorDataset> pair in lineClasses)
			{
				string field;
				IList<Subtype> subtypes;
				GetSubtypes(pair.Value, out field, out subtypes);

				AssignSubtypes(matrix.LineTypes, pair.Value.Name, field, subtypes);
			}

			foreach (KeyValuePair<string, VectorDataset> pair in nodeClasses)
			{
				string field;
				IList<Subtype> subtypes;
				GetSubtypes(pair.Value, out field, out subtypes);

				foreach (IList<ConnectionType> nodeType in matrix.Nodes.Keys)
				{
					AssignSubtypes(nodeType, pair.Value.Name, field, subtypes);
				}
			}

			var rulesList = new List<RuleCount>();
			var lineName = new List<string>(lineClasses.Keys);
			foreach (KeyValuePair<IList<ConnectionType>, int[,]> node in matrix.Nodes)
			{
				IEnumerable<RuleCount> rules = Rules(matrix.LineTypes, node, lineName, nodeClasses);
				rulesList.AddRange(rules);
			}

			foreach (RuleCount ruleCount in rulesList)
			{
				ruleCount.Count(lineClasses, nodeClasses);
			}

			rulesList.Sort();

			foreach (RuleCount rules in rulesList)
			{
				foreach (string rule in rules.Rules)
				{
					QualityCondition_Utils.AddParameterValue(
						qualityCondition, RulesParamName, rule);
				}
			}

			return qualityCondition;
		}

		[NotNull]
		public Matrix Convert([NotNull] QualityCondition qualityCondition)
		{
			Assert.ArgumentNotNull(qualityCondition, "qualityCondition");

			IList<TestParameterValue> featureClassParameters =
				qualityCondition.GetParameterValues(FeatureClassesParamName);
			int featureClassCount = featureClassParameters.Count;

			IList<TestParameterValue> rules =
				qualityCondition.GetParameterValues(RulesParamName);

			Assert.True(rules.Count % featureClassCount == 0,
			            "Number of Rules {0} does not correspond to number of feature classes {1}",
			            rules.Count, featureClassCount);

			int lineFeatureClassCount;
			List<VectorDataset> vectorDatasets = GetVectorDatasets(
				featureClassParameters, out lineFeatureClassCount);

			if (rules.Count == 0)
			{
				int nMatrix = Math.Max(1, featureClassCount - lineFeatureClassCount);

				for (int iMatrix = 0; iMatrix < nMatrix; iMatrix++)
				{
					var rule = new TestParameter(RulesParamName, typeof(string));

					for (int lineFeatureClassIndex = 0;
					     lineFeatureClassIndex < lineFeatureClassCount;
					     lineFeatureClassIndex++)
					{
						TestParameterValue dummyLine = new ScalarTestParameterValue(rule, _dummy);
						rules.Add(dummyLine);
					}

					for (int iNodeLayer = lineFeatureClassCount;
					     iNodeLayer < featureClassCount;
					     iNodeLayer++)
					{
						string b = (iNodeLayer == iMatrix + lineFeatureClassCount)
							           ? _true
							           : _false;

						TestParameterValue dummyNode = new ScalarTestParameterValue(rule, b);
						rules.Add(dummyNode);
					}
				}
			}

			Matrix matrix = InitMatrix(vectorDatasets, lineFeatureClassCount, rules);

			// Interpret rules

			// Helper variable to handle equal node rules
			var nodeRules = new Dictionary<string, int[,]>();

			for (int ruleIndex = 0; ruleIndex < rules.Count; ruleIndex += featureClassCount)
			{
				var groupRule = new List<TestParameterValue>(featureClassCount);
				for (int iLayer = 0; iLayer < featureClassCount; iLayer++)
				{
					groupRule.Add(rules[ruleIndex + iLayer]);
				}

				InterpretRule(groupRule, vectorDatasets, lineFeatureClassCount, matrix, nodeRules);
			}

			// Complete Subtype information for mat.Nodes.Connections
			for (int lineFeatureClassIndex = lineFeatureClassCount;
			     lineFeatureClassIndex < featureClassCount;
			     lineFeatureClassIndex++)
			{
				IObjectDataset ds = vectorDatasets[lineFeatureClassIndex];
				string field;
				IList<Subtype> subtypes;
				GetSubtypes(ds, out field, out subtypes);

				foreach (KeyValuePair<IList<ConnectionType>, int[,]> pair in matrix.Nodes)
				{
					AssignSubtypes(pair.Key, ds.Name, field, subtypes);
				}
			}

			return matrix;
		}

		#region Non-public

		private static void GetSubtypes([NotNull] IObjectDataset objectDataset,
		                                [CanBeNull] out string field,
		                                [CanBeNull] out IList<Subtype> subtypes)
		{
			IObjectClass objectClass = ConfiguratorUtils.OpenFromDefaultDatabase(objectDataset);
			var s = (ISubtypes) objectClass;
			field = s.SubtypeFieldName;

			subtypes = string.IsNullOrEmpty(field)
				           ? null
				           : DatasetUtils.GetSubtypes(objectClass);
		}

		private static void AssignSubtypes(
			[NotNull] IEnumerable<ConnectionType> connectionTypes,
			[NotNull] string featureClassName,
			[CanBeNull] string field,
			[CanBeNull] IList<Subtype> subtypes)
		{
			if (subtypes == null)
			{
				return;
			}

			Assert.ArgumentNotNullOrEmpty(field, "field is null but subtype list is specified");

			foreach (ConnectionType connectionType in connectionTypes)
			{
				if (! Equals(connectionType.FeatureClassName, featureClassName))
				{
					continue;
				}

				connectionType.AssignSubtype(field, subtypes);
			}
		}

		[NotNull]
		private static IEnumerable<RuleCount> Rules(
			[NotNull] IList<ConnectionType> lineTypes,
			[NotNull] KeyValuePair<IList<ConnectionType>, int[,]> node,
			[NotNull] IList<string> lineClasses,
			[NotNull] Dictionary<string, VectorDataset> nodeClasses)
		{
			int[,] mat = node.Value;

			Debug.Assert(lineTypes.Count == mat.GetLength(0));
			Debug.Assert(lineTypes.Count == mat.GetLength(1));

			var nodeRules = new List<string>();

			foreach (KeyValuePair<string, VectorDataset> pair in nodeClasses)
			{
				string nodeRule = GetRule(node.Key, pair.Key);
				nodeRules.Add(nodeRule);
			}

			List<List<int>> connectionsList = GetConnectionsList(mat);

			var rules = new List<RuleCount>(connectionsList.Count);

			foreach (List<int> connections in connectionsList)
			{
				var rule = new RuleCount(GetRules(connections, mat, lineTypes,
				                                  lineClasses, nodeRules));
				rules.Add(rule);
			}

			return rules;
		}

		[NotNull]
		private static string GetRule([NotNull] IEnumerable<ConnectionType> connections,
		                              [NotNull] string featureClassName)
		{
			string rule = _false;

			// prepare line selection expressions
			foreach (ConnectionType connection in connections)
			{
				if (! connection.FeatureClassName.Equals(
					    featureClassName,
					    StringComparison.CurrentCultureIgnoreCase))
				{
					continue;
				}

				rule = AdaptRule(rule, connection);
			}

			if (rule.Contains(" in (") && ! rule.Contains(")"))
			{
				rule += ")";
			}

			return rule;
		}

		[NotNull]
		private static string AdaptRule([NotNull] string rule,
		                                [NotNull] ConnectionType connection)
		{
			if (string.IsNullOrEmpty(connection.SubtypeName))
			{
				rule = _true;
			}
			else
			{
				if (Equals(rule, _false))
				{
					rule = string.Format("{0} in ({1}",
					                     connection.SubtypeField,
					                     connection.SubtypeCode);
				}
				else
				{
					rule += ", " + connection.SubtypeCode;
				}
			}

			return rule;
		}

		[NotNull]
		private static List<string> GetRules([NotNull] IEnumerable<int> connections,
		                                     [NotNull] int[,] mat,
		                                     [NotNull] IList<ConnectionType> lineTypes,
		                                     [NotNull] IList<string> lineFeatureClassNames,
		                                     [NotNull] ICollection<string> nodeRules)
		{
			int lineFeatureClassCount = lineFeatureClassNames.Count;
			int nClasses = lineFeatureClassCount + nodeRules.Count;

			var rules = new List<string>(nClasses);

			for (int i = 0; i < lineFeatureClassCount; i++)
			{
				rules.Add(_false);
			}

			foreach (string nodeRule in nodeRules)
			{
				rules.Add(nodeRule);
			}

			// prepare line selection expressions
			foreach (int connection in connections)
			{
				ConnectionType lineType = lineTypes[connection];
				int iClass = lineFeatureClassNames.IndexOf(lineType.FeatureClassName);

				rules[iClass] = AdaptRule(rules[iClass], lineType);
			}

			// complete line selection expressions
			for (int i = 0; i < nClasses; i++)
			{
				string rule = rules[i];
				if (rule.Contains(" in (") && rule.Contains(")") == false)
				{
					rules[i] += ")";
				}
			}

			// handle types that must not connect to itself
			var maxConnect = new List<int>();
			int nMaxConstraint = 0;
			foreach (int connection in connections)
			{
				int max = mat[connection, connection];
				if (max < 0)
				{
					continue;
				}

				ConnectionType lineType = lineTypes[connection];
				int iClass = lineFeatureClassNames.IndexOf(lineType.FeatureClassName);

				string constraint;
				if (lineType.SubtypeName == null)
				{
					constraint = _true;
				}
				else
				{
					constraint = string.Format("{0} = {1}",
					                           lineType.SubtypeField,
					                           lineType.SubtypeCode);
				}

				rules[iClass] += string.Format("; m{0}: {1}", nMaxConstraint, constraint);

				if (max == 0)
				{
					max = 1;
				}

				maxConnect.Add(max);
				nMaxConstraint++;
			}

			if (nMaxConstraint > 0)
			{
				var constraint = new StringBuilder();
				int iMaxConstraint = 0;
				foreach (int max in maxConnect)
				{
					if (constraint.Length != 0)
					{
						constraint.Append(" AND ");
					}

					constraint.AppendFormat("m{0} < {1}", iMaxConstraint, max + 1);
					iMaxConstraint++;
				}

				rules[0] += _separator + constraint;
			}

			return rules;
		}

		[NotNull]
		private static List<List<int>> GetConnectionsList([NotNull] int[,] mat)
		{
			int nLineType = mat.GetLength(0);
			var cmpr = new ListComparer();
			var connectionsList = new List<List<int>>();

			for (int iLineType = 0; iLineType < nLineType; iLineType++)
			{
				var newLists = new List<List<int>>();

				foreach (List<int> connections in connectionsList)
				{
					List<int> newList = CanAdd(connections, iLineType, mat);

					if (newList == null)
					{
						continue;
					}

					Debug.Assert(newList[newList.Count - 1] == iLineType,
					             "Error in software design assumption");

					List<int> subList = null;
					List<int> exList = null;
					foreach (List<int> list in newLists)
					{
						subList = ListComparer.SubGroup(list, newList);
						if (subList != null)
						{
							exList = list;
							break;
						}
					}

					if (subList != newList)
					{
						if (subList != null)
						{
							Debug.Assert(exList == subList);
							newLists.Remove(subList);
						}

						newLists.Add(newList);
					}
				}

				foreach (List<int> list in newLists)
				{
					int pos = connectionsList.BinarySearch(list, cmpr);
					if (pos < 0)
					{
						connectionsList.Insert(-pos - 1, list);
					}
				}

				if (newLists.Count == 0)
				{
					connectionsList.Add(new List<int>(new[] {iLineType}));
				}
			}

			// remove "empty" Lists
			for (int iList = connectionsList.Count - 1; iList >= 0; iList--)
			{
				List<int> lst = connectionsList[iList];
				if (lst.Count != 1)
				{
					continue;
				}

				int iLineType = lst[0];
				if (mat[iLineType, iLineType] == 0)
				{
					connectionsList.RemoveAt(iList);
				}
			}

			return connectionsList;
		}

		[CanBeNull]
		private static List<int> CanAdd([NotNull] List<int> connections,
		                                int tryAdd,
		                                [NotNull] int[,] matrix)
		{
			bool all = true;

			List<int> newList = null;

			foreach (int iLineType in connections)
			{
				if (matrix[Math.Min(iLineType, tryAdd), Math.Max(iLineType, tryAdd)] == 0)
				{
					all = false;
				}
				else
				{
					if (newList == null)
					{
						newList = new List<int>();
					}

					newList.Add(iLineType);
				}
			}

			if (all)
			{
				connections.Add(tryAdd);
				return connections;
			}

			if (newList == null)
			{
				return newList;
			}

			newList.Add(tryAdd);

			return newList;
		}

		private static void InterpretRule([NotNull] IList<TestParameterValue> groupRule,
		                                  [NotNull] IList<VectorDataset> vectorDatasets,
		                                  int lineFeatureClassCount,
		                                  [NotNull] Matrix mat,
		                                  [NotNull] IDictionary<string, int[,]> nodeRules)
		{
			// create "nodeCondition"
			int vectorDatasetCount = vectorDatasets.Count;
			string nodeConditions = string.Empty;
			var nodeConditionList = new string[vectorDatasetCount];
			for (int iNodeLayer = lineFeatureClassCount;
			     iNodeLayer < vectorDatasetCount;
			     iNodeLayer++)
			{
				string condition = groupRule[iNodeLayer].StringValue;
				nodeConditions += condition + _separator;
				nodeConditionList[iNodeLayer] = condition;
			}

			// create new mat.Node if needed
			int[,] combs;
			if (nodeRules.TryGetValue(nodeConditions, out combs) == false)
			{
				IList<ConnectionType> nodeConnections =
					NodeConnections(nodeConditionList, vectorDatasets);

				combs = mat.AddNode(nodeConnections);
				nodeRules.Add(nodeConditions, combs);
			}

			// interpret line conditions
			var connections = new List<int>();
			var maxConnections = new Dictionary<int, int>();

			string maxCondition = null;
			foreach (TestParameterValue ruleParam in groupRule)
			{
				string rule = ruleParam.StringValue;
				string ruleCondition = GetMaxCondition(rule);
				if (ruleCondition != null)
				{
					Assert.Null(maxCondition,
					            string.Format("Max condition already defined: {0} <--> {1}",
					                          maxCondition, ruleCondition));
					maxCondition = ruleCondition;
				}
			}

			Dictionary<string, int> maxVars = null;
			if (maxCondition != null)
			{
				maxVars = GetConditionVariables(maxCondition);
			}

			for (int iLineLayer = 0; iLineLayer < lineFeatureClassCount; iLineLayer++)
			{
				string rule = groupRule[iLineLayer].StringValue;
				string layer = vectorDatasets[iLineLayer].Name;

				IList<int> subtypes = Subtypes(rule);

				if (subtypes == null)
				{
					continue;
				}

				Dictionary<int, int> subtypeMax = null;
				if (maxVars != null)
				{
					subtypeMax = GetSubtypesMax(groupRule[iLineLayer].StringValue, maxVars);
				}

				if (subtypes.Count == 0)
				{
					int idx = mat.LineTypesIndexOf(layer);
					connections.Add(idx);

					if (subtypeMax != null)
					{
						Assert.True(subtypeMax.Count == 1 && subtypeMax.ContainsKey(int.MinValue),
						            "unexpected rule " + rule);
						maxConnections.Add(idx, subtypeMax[int.MinValue]);
					}
				}
				else
				{
					foreach (int subtype in subtypes)
					{
						connections.Add(mat.LineTypesIndexOf(layer, subtype));
					}

					if (subtypeMax != null)
					{
						Assert.True(subtypeMax.Count > 0, "unexpected rule " + rule);
						foreach (KeyValuePair<int, int> pair in subtypeMax)
						{
							int subtype = pair.Key;
							int count = pair.Value;

							int idx = mat.LineTypesIndexOf(layer, subtype);
							Assert.True(idx >= 0,
							            string.Format(
								            "Unknown subtype {0} of layer {1} in rule {2}",
								            subtype, layer, rule));
							int e;
							if (maxConnections.TryGetValue(idx, out e))
							{
								Assert.True(count == e,
								            string.Format(
									            "Differing max counts ({0}, {1})for subtype {2} of layer {3}",
									            e, count, subtype, layer));
							}
							else
							{
								maxConnections.Add(idx, count);
							}
						}
					}
				}
			}

			if (maxVars != null && maxVars.Count > 0)
			{
				var missing = new StringBuilder("Following variables are not defined: ");
				bool first = true;
				foreach (string var in maxVars.Keys)
				{
					if (! first)
					{
						missing.Append(", ");
					}

					first = false;
					missing.Append(var);
				}

				Assert.Fail(missing.ToString());
			}

			int dim = connections.Count;
			for (int i0 = 0; i0 < dim; i0++)
			{
				int c0 = connections[i0];

				for (int i1 = i0; i1 < dim; i1++)
				{
					int c1 = connections[i1];
					combs[c0, c1] = -1;
					combs[c1, c0] = -1;
				}
			}

			foreach (KeyValuePair<int, int> pair in maxConnections)
			{
				int idx = pair.Key;
				int count = pair.Value;
				combs[idx, idx] = count - 1;
			}
		}

		[NotNull]
		private static List<VectorDataset> GetVectorDatasets(
			[NotNull] IList<TestParameterValue> featureClassParameters,
			out int lineFeatureClassCount)
		{
			int featureClassCount = featureClassParameters.Count;

			lineFeatureClassCount = 0;
			bool firstNode = true;

			var vectorDatasets = new List<VectorDataset>(featureClassCount);

			for (int featureClassIndex = 0;
			     featureClassIndex < featureClassCount;
			     featureClassIndex++)
			{
				var datasetParameterValue =
					(DatasetTestParameterValue) featureClassParameters[featureClassIndex];
				var dataset = (VectorDataset) datasetParameterValue.DatasetValue;

				Assert.NotNull(dataset, "Dataset parameter {0} does not refer to a dataset",
				               datasetParameterValue.TestParameterName);

				var featureClass = ConfiguratorUtils.OpenFromDefaultDatabase(dataset);

				if (featureClass.ShapeType != esriGeometryType.esriGeometryPoint)
				{
					Assert.True(firstNode, "Cannot convert condition");
					lineFeatureClassCount++;
				}
				else
				{
					firstNode = false;
				}

				vectorDatasets.Add(dataset);
			}

			return vectorDatasets;
		}

		[NotNull]
		private static Matrix InitMatrix([NotNull] IList<VectorDataset> vectorDatasets,
		                                 int lineFeatureClassCount,
		                                 [NotNull] IList<TestParameterValue> rules)
		{
			int nClasses = vectorDatasets.Count;

			var featureClassSubtypes =
				new List<ConnectionType>[lineFeatureClassCount];
			for (int iRule = 0; iRule < rules.Count; iRule += nClasses)
			{
				for (int lineFeatureClassIndex = 0;
				     lineFeatureClassIndex < lineFeatureClassCount;
				     lineFeatureClassIndex++)
				{
					string rule = rules[iRule + lineFeatureClassIndex].StringValue.Split(';')[0];

					bool boolValue;
					if (bool.TryParse(rule, out boolValue)) // rule == "false" / "true"
					{
						continue;
					}

					if (featureClassSubtypes[lineFeatureClassIndex] == null)
					{
						featureClassSubtypes[lineFeatureClassIndex] =
							GetConnectionTypes(vectorDatasets[lineFeatureClassIndex]);
					}
				}
			}

			for (int lineFeatureClassIndex = 0;
			     lineFeatureClassIndex < lineFeatureClassCount;
			     lineFeatureClassIndex++)
			{
				if (featureClassSubtypes[lineFeatureClassIndex] != null)
				{
					continue;
				}

				var type = new ConnectionType(vectorDatasets[lineFeatureClassIndex].Name,
				                              null);
				var lst = new List<ConnectionType>();
				lst.Add(type);
				featureClassSubtypes[lineFeatureClassIndex] = lst;
			}

			return new Matrix(featureClassSubtypes);
		}

		[NotNull]
		private static IList<ConnectionType> NodeConnections(
			[NotNull] string[] nodeConditions,
			[NotNull] IList<VectorDataset> datasets)
		{
			int n = nodeConditions.Length;
			var nodeTypes = new List<ConnectionType>();
			for (int i = 0; i < n; i++)
			{
				string condition = nodeConditions[i];
				if (condition == null)
				{
					continue;
				}

				condition = condition.Trim();
				string layer = datasets[i].Name;
				bool b;
				if (bool.TryParse(condition, out b))
				{
					if (b)
					{
						nodeTypes.Add(new ConnectionType(layer, null));
					}
				}
				else
				{
					int s = condition.IndexOf('(');
					if (s >= 0)
					{
						string[] sTypeList =
							condition.Substring(s + 1, condition.Length - s - 2).Split(',');
						foreach (string sType in sTypeList)
						{
							nodeTypes.Add(new ConnectionType(layer, int.Parse(sType)));
						}
					}
					else
					{
						s = condition.IndexOf('=');
						string sType = condition.Substring(s + 1);
						nodeTypes.Add(new ConnectionType(layer, int.Parse(sType)));
					}
				}
			}

			return nodeTypes;
		}

		[NotNull]
		private static List<ConnectionType> GetConnectionTypes(
			[NotNull] VectorDataset vectorDataset)
		{
			var s = (ISubtypes) ConfiguratorUtils.OpenFromDefaultDatabase(vectorDataset);
			IEnumSubtype es = s.Subtypes;

			var result = new List<ConnectionType>();
			string field = s.SubtypeFieldName;
			int code;

			for (string subtype = es.Next(out code);
			     ! string.IsNullOrEmpty(subtype);
			     subtype = es.Next(out code))
			{
				var ct = new ConnectionType(vectorDataset.Name, field, subtype, code);
				result.Add(ct);
			}

			result.Sort((x, y) => x.SubtypeCode - y.SubtypeCode);

			return result;
		}

		[CanBeNull]
		private static IList<int> Subtypes([NotNull] string rule)
		{
			string[] statements = rule.Split(';');
			string condition = statements[0];

			condition = condition.Trim();
			// find start of list, if no start --> no subtypes
			int s = condition.IndexOf('(');

			if (s > 0)
			{
				string subtypes = condition.Substring(s + 1, condition.Length - s - 2);
				if (string.IsNullOrEmpty(subtypes))
				{
					return new List<int>();
				}

				var subtypeList = new List<int>();
				foreach (string subtype in subtypes.Split(','))
				{
					subtypeList.Add(int.Parse(subtype));
				}

				return subtypeList;
			}

			if (condition == _dummy)
			{
				return null;
			}

			return bool.Parse(condition)
				       ? new List<int>()
				       : null;
		}

		private static Dictionary<string, int> GetConditionVariables(
			[NotNull] string allConditions)
		{
			var varMaxs = new Dictionary<string, int>();
			// condition for all variables
			const string and = " AND ";
			string uStatement = allConditions.ToUpper();
			int iStart = 0;
			while (iStart >= 0)
			{
				int iEnd = uStatement.IndexOf(and, iStart);
				string condition;
				if (iEnd > 0)
				{
					condition = allConditions.Substring(iStart, iEnd - iStart);
					iStart = iEnd + and.Length;
				}
				else
				{
					condition = allConditions.Substring(iStart);
					iStart = -1;
				}

				string[] terms = condition.Split('<');
				Assert.True(terms.Length == 2,
				            string.Format("Unexpected condition '{0}' in statement '{1}'",
				                          condition,
				                          allConditions));
				string var = terms[0].Trim();
				Assert.False(varMaxs.ContainsKey(var),
				             string.Format("multiple conditions for variable '{0}' in '{1}'", var,
				                           allConditions));
				int count;
				bool b = int.TryParse(terms[1], out count);
				Assert.True(b,
				            string.Format("Unexpected term {0} in condition {1}", terms[1],
				                          condition));

				varMaxs.Add(var, count);
			}

			return varMaxs;
		}

		[CanBeNull]
		private static string GetMaxCondition([NotNull] string rule)
		{
			string maxCondition = null;
			string[] statements = rule.Split(';');
			int n = statements.Length;
			for (int i = 1; i < n; i++)
			{
				string statement = statements[i];
				int sd = statement.IndexOf(":"); // Statement to assign variable --> sd > 0
				if (sd < 0)
				{
					Assert.Null(maxCondition,
					            string.Format("Max condition already defined: {0} <--> {1}",
					                          maxCondition, statement));
					maxCondition = statement;
				}
			}

			return maxCondition;
		}

		[CanBeNull]
		private static Dictionary<int, int> GetSubtypesMax([NotNull] string rule,
		                                                   Dictionary<string, int> maxVars)
		{
			Dictionary<string, int> varSubtypes = null;
			Dictionary<int, int> subtypesMax = null;

			string[] statements = rule.Split(';');
			int n = statements.Length;
			for (int i = 1; i < n; i++)
			{
				string statement = statements[i];
				int sd = statement.IndexOf(":"); // Statement to assign variable --> sd > 0
				if (sd < 0)
				{
					continue;
				}

				int se = statement.IndexOf("=");
				if (se < 0)
				{
					bool b;
					bool.TryParse(statement.Substring(sd + 1), out b);
					Assert.True(b, "unexpected statement " + statement);
					Assert.True(varSubtypes == null, "unexpected rule " + rule);
					varSubtypes = new Dictionary<string, int>();
					varSubtypes.Add(statement.Substring(0, sd).Trim(), int.MinValue);
				}
				else
				{
					if (varSubtypes == null)
					{
						varSubtypes = new Dictionary<string, int>();
					}
					else
					{
						Assert.True(varSubtypes.Count > 0 &&
						            varSubtypes.ContainsValue(int.MinValue) == false,
						            "unexpected rule " + rule);
					}

					int subtype;
					bool b = int.TryParse(statement.Substring(se + 1), out subtype);
					Assert.True(b, "unexpected statement " + statement);

					varSubtypes.Add(statement.Substring(0, sd).Trim(), subtype);
				}
			}

			if (varSubtypes != null)
			{
				subtypesMax = new Dictionary<int, int>(varSubtypes.Count);
				foreach (KeyValuePair<string, int> varSubtype in varSubtypes)
				{
					string var = varSubtype.Key;
					int subtype = varSubtype.Value;

					int count;
					if (maxVars.TryGetValue(var, out count) == false)
					{
						Assert.Fail(string.Format(
							            "Count for {0} not defined", var));
					}

					subtypesMax.Add(subtype, count);
					maxVars.Remove(var);
				}
			}

			return subtypesMax;
		}

		[NotNull]
		private static Dictionary<string, VectorDataset> GetLineClasses(
			[NotNull] Matrix matrix,
			[NotNull] IEnumerable<Dataset> datasets)
		{
			var result = new Dictionary<string, VectorDataset>();

			foreach (ConnectionType lineType in matrix.LineTypes)
			{
				string lineClass = lineType.FeatureClassName;
				if (result.ContainsKey(lineClass))
				{
					continue;
				}

				result.Add(lineClass, GetDataset(lineClass, datasets));
			}

			return result;
		}

		[NotNull]
		private static VectorDataset GetDataset(
			[NotNull] string datasetName,
			[NotNull] IEnumerable<Dataset> datasets)
		{
			var dataset = ConfiguratorUtils.GetDataset<VectorDataset>(datasetName,
			                                                          datasets);
			Assert.NotNull(dataset, "Vector dataset not found: {0}", datasetName);
			return dataset;
		}

		[NotNull]
		private static Dictionary<string, VectorDataset> GetNodeClasses(
			[NotNull] Matrix matrix,
			[NotNull] IEnumerable<Dataset> datasets)
		{
			var result = new Dictionary<string, VectorDataset>();

			foreach (IList<ConnectionType> nodeTypes in matrix.Nodes.Keys)
			{
				foreach (ConnectionType nodeType in nodeTypes)
				{
					string nodeClass = nodeType.FeatureClassName;
					if (string.IsNullOrEmpty(nodeClass))
					{
						break;
					}

					if (result.ContainsKey(nodeClass))
					{
						continue;
					}

					result.Add(nodeClass, GetDataset(nodeClass, datasets));
				}
			}

			return result;
		}

		#endregion

		#region nested classes

		#region Nested type: ConnectionType

		public class ConnectionType
		{
			private readonly string _featureClassName;
			private int _subtypeCode = -1;
			private string _subtypeField;
			private string _subtypeName;

			public ConnectionType([NotNull] string featureClassName, string subtypeName)
			{
				_featureClassName = featureClassName;
				_subtypeName = subtypeName;
			}

			public ConnectionType([NotNull] string featureClassName, int code)
			{
				_featureClassName = featureClassName;
				_subtypeCode = code;
			}

			public ConnectionType([NotNull] string featureClassName, string field,
			                      string subtype,
			                      int code)
			{
				_featureClassName = featureClassName;
				_subtypeCode = code;

				_subtypeField = field;
				_subtypeName = subtype;
			}

			[NotNull]
			public string FeatureClassName
			{
				get { return _featureClassName; }
			}

			public string SubtypeField
			{
				get { return _subtypeField; }
			}

			public string SubtypeName
			{
				get { return _subtypeName; }
			}

			public int SubtypeCode
			{
				get { return _subtypeCode; }
			}

			public int AssignSubtype([NotNull] string subtypeField,
			                         [NotNull] IList<Subtype> subtypes)
			{
				if (string.IsNullOrEmpty(_subtypeName) && _subtypeCode < 0)
				{
					return -1;
				}

				_subtypeField = subtypeField;

				if (_subtypeCode < 0)
				{
					foreach (Subtype subtype in subtypes)
					{
						if (! Equals(subtype.Name, _subtypeName))
						{
							continue;
						}

						_subtypeCode = subtype.Code;
						return _subtypeCode;
					}

					throw new InvalidDataException(
						string.Format("'{0}' is no subtype of {1}", _subtypeName,
						              _featureClassName));
				}

				foreach (Subtype subtype in subtypes)
				{
					if (! Equals(subtype.Code, _subtypeCode))
					{
						continue;
					}

					_subtypeName = subtype.Name;
					return _subtypeCode;
				}

				throw new InvalidDataException(
					string.Format("{0} no subtype of {1}", _subtypeCode,
					              _featureClassName));
			}
		}

		#endregion

		#region Nested type: ListComparer

		private class ListComparer : IComparer<List<int>>
		{
			#region IComparer<List<int>> Members

			public int Compare(List<int> x, List<int> y)
			{
				int nx = x.Count;
				int ny = y.Count;
				int n = Math.Max(nx, ny);

				for (int i = 0; i < n; i++)
				{
					int d = Comparer<int>.Default.Compare(x[i], y[i]);
					if (d != 0)
					{
						return d;
					}
				}

				return Comparer<int>.Default.Compare(nx, ny);
			}

			#endregion

			public static List<int> SubGroup(List<int> x, List<int> y)
			{
				int nx = x.Count;
				int ny = y.Count;
				if (nx < ny)
				{
					return SubGroup(y, x);
				}

				for (int i = 0; i < ny; i++)
				{
					if (x.Contains(y[i]) == false)
					{
						return null;
					}
				}

				return y;
			}
		}

		#endregion

		#region Nested type: Matrix

		public class Matrix
		{
			private const int _matStart = 2;
			private List<ConnectionType> _lineTypes;
			private Dictionary<IList<ConnectionType>, int[,]> _nodes;

			private Matrix() { }

			internal Matrix([NotNull] IEnumerable<IList<ConnectionType>> connectionTypes)
			{
				_lineTypes = new List<ConnectionType>();
				foreach (IList<ConnectionType> connectionType in connectionTypes)
				{
					_lineTypes.AddRange(connectionType);
				}
			}

			public IList<ConnectionType> LineTypes
			{
				get { return _lineTypes; }
			}

			public Dictionary<IList<ConnectionType>, int[,]> Nodes
			{
				get { return _nodes; }
			}

			[NotNull]
			public static Matrix Create([NotNull] TextReader textReader)
			{
				var m = new Matrix();

				string classesLine = textReader.ReadLine();
				string subtypesLine = textReader.ReadLine();

				Assert.NotNull(classesLine, "classesLine");
				Assert.NotNull(subtypesLine, "subtypesLine");

				m._lineTypes = new List<ConnectionType>();

				string[] featureClassNames = classesLine.Split(';');
				string[] subTypeStrings = subtypesLine.Split(';');
				string lastClassString = null;

				for (int featureClassIndex = _matStart;
				     featureClassIndex < featureClassNames.Length;
				     featureClassIndex++)
				{
					string featureClassName = featureClassNames[featureClassIndex].Trim();
					string subType = subTypeStrings[featureClassIndex].Trim();

					if (string.IsNullOrEmpty(featureClassName) &&
					    string.IsNullOrEmpty(subType)
					    && featureClassIndex == featureClassNames.Length - 1)
					{
						// ; at end of line
						break;
					}

					if (string.IsNullOrEmpty(featureClassName))
					{
						Assert.NotNullOrEmpty(subType, "Undefined line type");

						featureClassName = lastClassString;
					}

					Assert.NotNullOrEmpty(featureClassName, "featureClassName");

					m._lineTypes.Add(new ConnectionType(featureClassName, subType));

					lastClassString = featureClassName;
				}

				m._nodes = new Dictionary<IList<ConnectionType>, int[,]>();

				// TODO: repeat over different node types
				while (GetNodes(textReader, m)) { }

				return m;
			}

			[NotNull]
			private static string GetTrimmedValue(IList<string> matStrings, int index)
			{
				string value = matStrings.Count > index
					               ? matStrings[index].Trim()
					               : "";
				return value;
			}

			private static bool GetNodes([NotNull] TextReader textReader,
			                             [NotNull] Matrix matrix)
			{
				int dim = matrix._lineTypes.Count;
				var mat = new int[dim, dim];

				bool nodeConnection = true;
				string lastClassString = null;

				var nodeTypes = new List<ConnectionType>();
				int iRow = 0;
				while (iRow < dim)
				{
					string matLine = textReader.ReadLine();
					if (string.IsNullOrEmpty(matLine))
					{
						return false;
					}

					string[] matStrings = matLine.Split(';');

					if (nodeConnection &&
					    string.IsNullOrEmpty(GetTrimmedValue(matStrings, _matStart)))
					{
						string nodeClass = GetTrimmedValue(matStrings, 0);
						string nodeSubtype = GetTrimmedValue(matStrings, 1);

						if (string.IsNullOrEmpty(nodeClass))
						{
							nodeClass = lastClassString;
						}

						Assert.NotNullOrEmpty(nodeClass, "Undefine node type");
						lastClassString = nodeClass;

						var nodeType =
							new ConnectionType(nodeClass, nodeSubtype);
						nodeTypes.Add(nodeType);

						continue;
					}

					nodeConnection = false;
					for (int iCol = iRow; iCol < dim; iCol++)
					{
						string s = matStrings[iCol + _matStart].Trim();
						int c;
						if (s == "x")
						{
							c = -1;
						}
						else
						{
							c = int.Parse(s);
							if (iRow != iCol && c != 0)
							{
								throw new InvalidOperationException(
									"Can handle non-(0 or x) values only on the diagonal");
							}
						}

						mat[iRow, iCol] = c;
						mat[iCol, iRow] = c;
					}

					iRow++;
				}

				matrix._nodes.Add(nodeTypes, mat);
				return true;
			}

			public int LineTypesIndexOf([NotNull] string featureClassName)
			{
				for (int i = 0; i < _lineTypes.Count; i++)
				{
					ConnectionType lineType = _lineTypes[i];

					if (Equals(lineType.FeatureClassName, featureClassName))
					{
						Assert.True(lineType.SubtypeName == null, "Invalid connection type");
						return i;
					}
				}

				return -1;
			}

			public int LineTypesIndexOf([NotNull] string featureClassName, int subtypeCode)
			{
				for (int i = 0; i < _lineTypes.Count; i++)
				{
					ConnectionType lineType = _lineTypes[i];
					if (Equals(lineType.SubtypeCode, subtypeCode) &&
					    Equals(lineType.FeatureClassName, featureClassName))
					{
						return i;
					}
				}

				return -1;
			}

			[NotNull]
			public int[,] AddNode([NotNull] IList<ConnectionType> nodeTypes)
			{
				//TODO
				if (_nodes == null)
				{
					_nodes = new Dictionary<IList<ConnectionType>, int[,]>();
				}

				var mat = new int[_lineTypes.Count, _lineTypes.Count];

				_nodes.Add(nodeTypes, mat);
				return mat;
			}

			[NotNull]
			public string ToCsv()
			{
				var sb = new StringBuilder();

				// line feature classes
				for (int i = 0; i < _matStart; i++)
				{
					sb.Append(_separator);
				}

				foreach (ConnectionType lineType in _lineTypes)
				{
					sb.AppendFormat("{0};", lineType.FeatureClassName);
				}

				sb.AppendLine();
				// line subtypes
				for (int i = 0; i < _matStart; i++)
				{
					sb.Append(_separator);
				}

				foreach (ConnectionType lineType in _lineTypes)
				{
					sb.AppendFormat("{0};", lineType.SubtypeName);
				}

				sb.AppendLine();

				foreach (KeyValuePair<IList<ConnectionType>, int[,]> node in _nodes)
				{
					AppendCsv(sb, node);
				}

				return sb.ToString();
			}

			private void AppendCsv([NotNull] StringBuilder sb,
			                       [NotNull] KeyValuePair<IList<ConnectionType>, int[,]> node)
			{
				int n = _lineTypes.Count;
				foreach (ConnectionType nodeType in node.Key)
				{
					sb.AppendFormat("{0}{2}{1}{2}",
					                nodeType.FeatureClassName,
					                nodeType.SubtypeName,
					                _separator);
					for (int i = 0; i < n; i++)
					{
						sb.Append(_separator);
					}

					sb.AppendLine();
				}

				for (int iRow = 0; iRow < n; iRow++)
				{
					sb.AppendFormat("{0}{2}{1}{2}",
					                _lineTypes[iRow].FeatureClassName,
					                _lineTypes[iRow].SubtypeName,
					                _separator);

					for (int iCol = 0; iCol < iRow; iCol++)
					{
						sb.Append(_separator);
					}

					for (int iCol = iRow; iCol < n; iCol++)
					{
						int val = node.Value[iRow, iCol];
						string sVal = val.ToString();
						if (val == -1)
						{
							sVal = "x";
						}

						sb.AppendFormat("{0}{1}", sVal, _separator);
					}

					sb.AppendLine();
				}
			}
		}

		#endregion

		#region Nested type: RuleCount

		private class RuleCount : IComparable<RuleCount>
		{
			private readonly List<string> _rules;
			private int _count = -1;

			public RuleCount([NotNull] List<string> rules)
			{
				_rules = rules;
			}

			[NotNull]
			public IEnumerable<string> Rules
			{
				get { return _rules; }
			}

			#region IComparable<RuleCount> Members

			public int CompareTo(RuleCount other)
			{
				if (other._count < 0 || _count < 0)
				{
					throw new InvalidOperationException("Count not initialized (use Count(...))");
				}

				return other._count - _count;
			}

			#endregion

			public void Count([NotNull] Dictionary<string, VectorDataset> lineDatasets,
			                  [NotNull] Dictionary<string, VectorDataset> nodeDatasets)
			{
				int n = 0;
				int i = 0;

				foreach (KeyValuePair<string, VectorDataset> pair in lineDatasets)
				{
					var table = (ITable) ConfiguratorUtils.OpenFromDefaultDatabase(pair.Value);
					n += RowCount(table, _rules[i].Split(';')[0]);

					i++;
				}

				foreach (KeyValuePair<string, VectorDataset> pair in nodeDatasets)
				{
					var table = (ITable) ConfiguratorUtils.OpenFromDefaultDatabase(pair.Value);
					n += RowCount(table, _rules[i]);

					i++;
				}

				_count = n;
			}

			private static int RowCount([NotNull] ITable table, [NotNull] string rule)
			{
				IQueryFilter filter = new QueryFilterClass();
				bool boolValue;
				int n;

				if (bool.TryParse(rule, out boolValue))
				{
					n = boolValue
						    ? table.RowCount(filter)
						    : 0;
				}
				else
				{
					filter.WhereClause = rule;
					n = table.RowCount(filter);
				}

				return n;
			}
		}

		#endregion

		#endregion
	}
}

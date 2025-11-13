using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;

namespace ProSuite.QA.Tests.ParameterTypes
{
	public class QaConnectionRule
	{
		private readonly IList<string> _selectionExpressions;
		private readonly IList<ITableSchemaDef> _tables;
		private string _constraint;
		private Dictionary<string, QaConnectionCountRule> _countRulesByVariableName;

		public QaConnectionRule([NotNull] IList<ITableSchemaDef> tables,
		                        [NotNull] IList<string> tableRuleStrings)
		{
			Assert.ArgumentCondition(tableRuleStrings.Count == tables.Count,
			                         string.Format(
				                         "The number of tables ({0}) must be equal to the number of rules ({1})",
				                         tables.Count, tableRuleStrings.Count));

			_tables = tables;
			_selectionExpressions = new List<string>(tableRuleStrings.Count);

			int tableIndex = 0;
			foreach (string tableRuleString in tableRuleStrings)
			{
				// examples for tableRuleString: 
				// "false"
				// "ObjektArt in (1, 2)" 
				// "ObjektArt in (1, 2, 5);m5:ObjektArt = 5;m5 < 2"

				// the first token in that string is always the selectionExpression 
				// subsequent tokens are optional count selection expressions, which may contain
				string[] ruleTokens = tableRuleString.Split(';');

				string selectionExpression = ruleTokens[0];

				_selectionExpressions.Add(selectionExpression);

				// process concatenated (;) rule strings
				for (int i = 1; i < ruleTokens.Length; i++)
				{
					int separatorIndex = ruleTokens[i].IndexOf(':');

					if (separatorIndex > 0)
					{
						// the token is a variable declaration 
						// (m5:ObjektArt = 5 ==> m5 is assigned the result count of "ObjektArt = 5" within the result of selectionExpression)

						// example case: m5:ObjektArt = 5

						string variableName = ruleTokens[i].Substring(0, separatorIndex).Trim();
						string countSelectionExpression =
							ruleTokens[i].Substring(separatorIndex + 1);

						AddVariableDeclaration(variableName,
						                       new QaConnectionCountRule(_tables[tableIndex],
							                       countSelectionExpression));
					}
					else
					{
						// not a variable declaration. This is the single constraint which makes use 
						// of the declared variables

						// example: m5 < 2

						Assert.Null(_constraint, "Duplicate rule constraint");

						_constraint = ruleTokens[i];
					}
				}

				tableIndex++;
			}
		}

		public IList<ITableSchemaDef> TableList => _tables;

		public IList<string> SelectionExpressions => _selectionExpressions;

		public Dictionary<string, QaConnectionCountRule> CountRulesByVariableName =>
			_countRulesByVariableName;

		public string Constraint
		{
			get { return _constraint; }
			set { _constraint = value; }
		}

		public void AddVariableDeclaration(string variableName, QaConnectionCountRule rule)
		{
			if (_countRulesByVariableName == null)
			{
				_countRulesByVariableName = new Dictionary<string, QaConnectionCountRule>();
			}

			_countRulesByVariableName.Add(variableName, rule);
		}
	}
}

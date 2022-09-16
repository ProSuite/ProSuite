using System.Collections.Generic;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.QA.Container;

namespace ProSuite.QA.Tests.Test.Transformer
{
	[UsedImplicitly]
	public class TrRule : ITableTransformer<string>
	{
		private readonly string _rule;

		public TrRule(string rule)
		{
			_rule = rule;
		}

		string ITableTransformer.TransformerName { get; set; }

		object ITableTransformer.GetTransformed() => GetTransformed();

		public string GetTransformed() => _rule;

		IList<IReadOnlyTable> IInvolvesTables.InvolvedTables => new IReadOnlyTable[] { };

		void IInvolvesTables.SetConstraint(int tableIndex, string constraint) { }

		void IInvolvesTables.SetSqlCaseSensitivity(int tableIndex, bool sensitiv) { }
	}

	[UsedImplicitly]
	public class TrRules : ITableTransformer<IList<string[]>>
	{
		private readonly IList<string[]> _rules;

		public TrRules(IList<string> rules)
		{
			_rules = new string[rules.Count][];
			int iRule = 0;
			foreach (string rule in rules)
			{
				_rules[iRule] = new[] { rule };
				iRule++;
			}
		}

		string ITableTransformer.TransformerName { get; set; }

		object ITableTransformer.GetTransformed() => GetTransformed();

		public IList<string[]> GetTransformed() => _rules;

		IList<IReadOnlyTable> IInvolvesTables.InvolvedTables => new IReadOnlyTable[] { };

		void IInvolvesTables.SetConstraint(int tableIndex, string constraint) { }

		void IInvolvesTables.SetSqlCaseSensitivity(int tableIndex, bool sensitiv) { }
	}
}

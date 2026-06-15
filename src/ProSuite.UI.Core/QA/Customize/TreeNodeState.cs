using System.Collections.Generic;

namespace ProSuite.UI.Core.QA.Customize
{
	internal class TreeNodeState
	{
		public string Id { get; set; }
		public bool Expanded { get; set; }
		public List<TreeNodeState> ChildrenStates { get; } = new List<TreeNodeState>();

		public override string ToString()
		{
			string exp = Expanded ? "+" : "-";
			return $"{exp}:{Id}";
		}
	}
}

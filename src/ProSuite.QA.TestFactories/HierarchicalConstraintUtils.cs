using System.Collections.Generic;
using ProSuite.QA.Tests.Constraints;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.TestFactories
{
	public static class HierarchicalConstraintUtils
	{
		[NotNull]
		public static IList<ConstraintNode> GetConstraintHierarchy(
			[NotNull] IList<string> constraints)
		{
			Assert.ArgumentNotNull(constraints, nameof(constraints));

			var constraintNodes = new List<ConstraintNode>();
			var nodeHierarchy = new List<IList<ConstraintNode>> {constraintNodes};

			foreach (string constraint in constraints)
			{
				string trimmedConstraint = constraint.Trim();
				int nPlus = 0;

				while (trimmedConstraint.StartsWith("+"))
				{
					trimmedConstraint = trimmedConstraint.Substring(1).Trim();
					nPlus++;
				}

				while (nodeHierarchy.Count > nPlus + 1)
				{
					nodeHierarchy.RemoveAt(nPlus + 1);
				}

				var node = new ConstraintNode(trimmedConstraint);

				Assert.True(nodeHierarchy.Count == nPlus + 1,
				            "Too many '+' in constraint " + constraint);

				nodeHierarchy[nPlus].Add(node);
				nodeHierarchy.Add(node.Nodes);
			}

			return constraintNodes;
		}
	}
}

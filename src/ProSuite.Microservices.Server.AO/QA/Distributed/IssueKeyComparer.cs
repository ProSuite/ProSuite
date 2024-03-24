using System.Collections.Generic;
using ProSuite.Commons.AO.Geodatabase.TablesBased;
using ProSuite.QA.Container;

namespace ProSuite.Microservices.Server.AO.QA.Distributed
{
	internal class IssueKeyComparer : IEqualityComparer<IssueKey>
	{
		public bool Equals(IssueKey x, IssueKey y)
		{
			if (x == y)
				return true;

			if (x == null || y == null)
				return false;

			if (x.ConditionId != y.ConditionId)
				return false;

			if (TestUtils.CompareSortedInvolvedRows(x.InvolvedRows, y.InvolvedRows,
			                                        validateRowCount: true) != 0)
			{
				return false;
			}

			if (TestUtils.CompareQaErrors(x.QaError, y.QaError,
			                              compareIndividualInvolvedRows: true) != 0)
			{
				return false;
			}

			return true;
		}

		public int GetHashCode(IssueKey obj)
		{
			unchecked
			{
				int hashCode = obj.ConditionId;
				hashCode = (hashCode * 397) ^ obj.Description.GetHashCode();

				foreach (InvolvedRow involvedRow in obj.InvolvedRows)
				{
					hashCode = (hashCode * 397) ^ involvedRow.OID.GetHashCode();
				}

				return hashCode;
			}
		}
	}
}

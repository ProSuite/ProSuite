using System;
using System.Collections.Generic;
using System.Linq;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container
{
	public class InvolvedNested : Involved, IEquatable<InvolvedNested>
	{
		public InvolvedNested(string tableName, IReadOnlyList<Involved> baseRows)
		{
			TableName = tableName;
			BaseRows = baseRows;
		}

		[NotNull]
		public string TableName { get; }

		[NotNull]
		public IReadOnlyList<Involved> BaseRows { get; }

		public override int GetHashCode()
		{
			return (BaseRows[0].GetHashCode() * BaseRows.Count * 29) ^ TableName.GetHashCode();
		}

		public override IEnumerable<InvolvedRow> EnumInvolvedRows()
		{
			foreach (Involved involved in BaseRows)
			{
				foreach (InvolvedRow involvedRow in involved.EnumInvolvedRows())
				{
					yield return involvedRow;
				}
			}
		}

		public override bool Equals(object obj)
		{
			return Equals(obj as InvolvedNested);
		}

		public bool Equals(InvolvedNested other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			if (! Equals(other.TableName, TableName))
			{
				return false;
			}

			if (! Equals(other.BaseRows.Count, BaseRows.Count))
			{
				return false;
			}

			HashSet<Involved> thisBaseRows = BaseRows.ToHashSet();
			HashSet<Involved> otherBaseRows = other.BaseRows.ToHashSet();
			if (thisBaseRows.Count != otherBaseRows.Count)
			{
				return false;
			}

			foreach (Involved involved in thisBaseRows)
			{
				if (! otherBaseRows.Contains(involved))
				{
					return false;
				}
			}

			return true;
		}
	}
}
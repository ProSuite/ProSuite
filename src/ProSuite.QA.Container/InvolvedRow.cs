using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Container
{
	public class InvolvedRow : Involved, IComparable<InvolvedRow>, IEquatable<InvolvedRow>
	{
		private const int _oidForEntireTable = -1;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="InvolvedRow"/> class.
		/// </summary>
		/// <param name="row">The row.</param>
		public InvolvedRow([NotNull] IReadOnlyRow row)
			: this(row.Table.Name,
			       row.Table.HasOID
				       ? row.OID
				       : _oidForEntireTable) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="InvolvedRow"/> class.
		/// </summary>
		/// <param name="tableName">Name of the table.</param>
		/// <param name="oid">The oid.</param>
		public InvolvedRow([NotNull] string tableName, int oid = _oidForEntireTable)
		{
			Assert.ArgumentNotNullOrEmpty(tableName, nameof(tableName));

			TableName = tableName;
			OID = oid;
		}

		[NotNull]
		public static IList<InvolvedRow> CreateList([NotNull] IList<IReadOnlyRow> rows)
		{
			int count = rows.Count;
			var result = new InvolvedRow[count];

			for (var i = 0; i < count; i++)
			{
				result[i] = new InvolvedRow(rows[i]);
			}

			return result;
		}

		#endregion

		public bool RepresentsEntireTable => OID == _oidForEntireTable;

		public int OID { get; }

		[NotNull]
		public string TableName { get; }

		public override IEnumerable<InvolvedRow> EnumInvolvedRows()
		{
			yield return this;
		}

		#region IComparable<InvolvedRow> Members

		public int CompareTo(InvolvedRow other)
		{
			return OID != other.OID
				       ? Comparer<int>.Default.Compare(OID, other.OID)
				       : Comparer<string>.Default.Compare(TableName, other.TableName);
		}

		#endregion

		#region IEquatable<InvolvedRow> Members

		public bool Equals(InvolvedRow other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			return other.OID == OID && Equals(other.TableName, TableName);
		}

		#endregion

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}

			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			if (obj.GetType() != typeof(InvolvedRow))
			{
				return false;
			}

			return Equals((InvolvedRow) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return (OID * 397) ^ TableName.GetHashCode();
			}
		}

		public override string ToString()
		{
			return TableName + ", " + OID;
		}
	}
}

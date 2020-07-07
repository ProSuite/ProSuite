using System;
using System.ComponentModel;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.UI.WinForms.Controls
{
	public class ColumnSortState : IEquatable<ColumnSortState>
	{
		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="ColumnSortState"/> class.
		/// </summary>
		/// <remarks>
		/// Required for xml serialization
		/// </remarks>
		public ColumnSortState() { }

		/// <summary>
		/// Initializes a new instance of the <see cref="ColumnSortState"/> class.
		/// </summary>
		/// <param name="sortedColumnName">Name of the sorted column.</param>
		/// <param name="listSortDirection">The list sort direction.</param>
		public ColumnSortState(
			[CanBeNull] string sortedColumnName,
			ListSortDirection listSortDirection = ListSortDirection.Ascending)
			: this(sortedColumnName, GetSortOrder(listSortDirection)) { }

		/// <summary>
		/// Initializes a new instance of the <see cref="ColumnSortState"/> class.
		/// </summary>
		/// <param name="sortedColumnName">Name of the sorted column.</param>
		/// <param name="sortOrder">The sort order.</param>
		public ColumnSortState([CanBeNull] string sortedColumnName, SortOrder sortOrder)
		{
			SortedColumnName = sortedColumnName;
			SortOrder = sortOrder;
		}

		#endregion

		[CanBeNull]
		public string SortedColumnName { get; set; }

		public SortOrder SortOrder { get; set; }

		public ListSortDirection? GetListSortDirection()
		{
			return GetListSortDirection(SortOrder);
		}

		public override string ToString()
		{
			return $"SortedColumnName: {SortedColumnName}, SortOrder: {SortOrder}";
		}

		public bool Equals(ColumnSortState other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			return Equals(other.SortedColumnName, SortedColumnName) &&
			       Equals(other.SortOrder, SortOrder);
		}

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

			if (obj.GetType() != typeof(ColumnSortState))
			{
				return false;
			}

			return Equals((ColumnSortState) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return ((SortedColumnName?.GetHashCode() ?? 0) * 397) ^ SortOrder.GetHashCode();
			}
		}

		private static SortOrder GetSortOrder(ListSortDirection listSortDirection)
		{
			switch (listSortDirection)
			{
				case ListSortDirection.Ascending:
					return SortOrder.Ascending;

				case ListSortDirection.Descending:
					return SortOrder.Descending;

				default:
					throw new ArgumentOutOfRangeException(nameof(listSortDirection));
			}
		}

		private static ListSortDirection? GetListSortDirection(SortOrder sortOrder)
		{
			switch (sortOrder)
			{
				case SortOrder.Ascending:
					return ListSortDirection.Ascending;

				case SortOrder.Descending:
					return ListSortDirection.Descending;

				default:
					return null;
			}
		}
	}
}

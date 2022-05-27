using System;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Core.IssueCodes
{
	public class IssueCode : IEquatable<IssueCode>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="IssueCode"/> class.
		/// </summary>
		/// <param name="id">The id.</param>
		/// <param name="description">The description.</param>
		public IssueCode([NotNull] string id, [CanBeNull] string description = null)
		{
			Assert.ArgumentNotNullOrEmpty(id, nameof(id));

			ID = id;
			Description = description;
		}

		/// <summary>
		/// Gets the ID for the issue code
		/// </summary>
		[NotNull]
		public string ID { get; }

		/// <summary>
		/// Gets the description for the issue code
		/// </summary>
		[CanBeNull]
		public string Description { get; }

		public override string ToString()
		{
			return ID;
		}

		public bool Equals(IssueCode other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			return string.Equals(other.ID, ID, StringComparison.OrdinalIgnoreCase);
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

			if (obj.GetType() != typeof(IssueCode))
			{
				return false;
			}

			return Equals((IssueCode) obj);
		}

		public override int GetHashCode()
		{
			return ID.GetHashCode();
		}
	}
}

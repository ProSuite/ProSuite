using System;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;

namespace ProSuite.DomainServices.AO.QA.Exceptions
{
	public class ExceptionCategory : IEquatable<ExceptionCategory>,
	                                 IComparable<ExceptionCategory>, IComparable
	{
		public ExceptionCategory([CanBeNull] string name, int listOrder = 0)
		{
			ListOrder = listOrder;
			if (StringUtils.IsNullOrEmptyOrBlank(name))
			{
				Name = null;
				Key = null;
			}
			else
			{
				Name = name.Trim();
				Key = Name.ToUpper();
			}
		}

		[CanBeNull]
		public string Key { get; }

		[CanBeNull]
		public string Name { get; }

		private int ListOrder { get; }

		public override string ToString()
		{
			return $"{nameof(Key)}: {Key}, " +
			       $"{nameof(Name)}: {Name}, " +
			       $"{nameof(ListOrder)}: {ListOrder}";
		}

		public bool Equals(ExceptionCategory other)
		{
			if (ReferenceEquals(null, other))
				return false;

			if (ReferenceEquals(this, other))
				return true;

			return string.Equals(Key, other.Key);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
				return false;
			if (ReferenceEquals(this, obj))
				return true;
			if (obj.GetType() != GetType())
				return false;
			return Equals((ExceptionCategory) obj);
		}

		public override int GetHashCode()
		{
			return Key != null ? Key.GetHashCode() : 0;
		}

		public int CompareTo(ExceptionCategory other)
		{
			if (ReferenceEquals(this, other))
				return 0;

			if (ReferenceEquals(null, other))
				return 1;

			int listOrderComparison = ListOrder.CompareTo(other.ListOrder);
			if (listOrderComparison != 0)
				return listOrderComparison;

			if (Key == null && other.Key != null)
			{
				// Key=null comes last
				return 1;
			}

			return string.Compare(Key, other.Key,
			                      StringComparison.Ordinal);
		}

		public int CompareTo(object obj)
		{
			if (ReferenceEquals(null, obj))
				return 1;

			if (ReferenceEquals(this, obj))
				return 0;

			if (! (obj is ExceptionCategory))
				throw new ArgumentException(
					$"Object must be of type {nameof(ExceptionCategory)}");

			return CompareTo((ExceptionCategory) obj);
		}
	}
}

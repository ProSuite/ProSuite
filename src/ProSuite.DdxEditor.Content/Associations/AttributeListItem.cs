using System;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DdxEditor.Content.Associations
{
	public class AttributeListItem : IComparable, IEquatable<AttributeListItem>
	{
		public AttributeListItem(string name, AssociationAttribute attribute)
		{
			_name = name;
			_listAttribute = attribute;
		}

		private readonly string _name;
		private readonly AssociationAttribute _listAttribute;

		[UsedImplicitly]
		public AssociationAttribute ListAttribute => _listAttribute;

		[UsedImplicitly]
		public string Name => _name;

		public override string ToString()
		{
			return _listAttribute.ToString();
		}

		#region IComparable Members

		public int CompareTo(object other)
		{
			if (other is AttributeListItem)
			{
				return string.CompareOrdinal(ToString(), other.ToString());
			}

			return -1;
		}

		#endregion

		#region IEquatable<SnapTypeListItem> Members

		public bool Equals(AttributeListItem other)
		{
			return _listAttribute.Equals(other);
		}

		#endregion
	}
}

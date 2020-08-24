using ArcGIS.Core.Data;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.AGP.Gdb;

namespace ProSuite.AGP.WorkList
{
	public class SelectionSourceClass : ISourceClass
	{
		private readonly GdbTableIdentity _identity;

		public SelectionSourceClass(GdbTableIdentity identity,
		                            IAttributeReader attributeReader)
		{
			_identity = identity;
			AttributeReader = attributeReader;
		}

		public string Name => _identity.Name;
		public IAttributeReader AttributeReader { get; }
		public long Id => _identity.Id;

		public bool Uses(Table table)
		{
			return _identity.Equals(new GdbTableIdentity(table));
		}
	}
}

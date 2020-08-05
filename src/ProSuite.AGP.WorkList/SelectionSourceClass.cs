using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.AGP.Gdb;

namespace ProSuite.AGP.WorkList
{
	public class SelectionSourceClass : ISourceClass
	{
		public SelectionSourceClass(GdbTableIdentity identity, IAttributeReader attributeReader)
		{
			Identity = identity;
			AttributeReader = attributeReader;
		}

		public string Name => Identity.Name;
		public GdbTableIdentity Identity { get; }
		public IAttributeReader AttributeReader { get; set; }
	}
}

using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList
{
	public class SelectionSourceClass : SourceClass
	{
		public SelectionSourceClass(GdbTableIdentity identity,
		                            [CanBeNull] IAttributeReader attributeReader = null) :
			base(identity, attributeReader) { }
	}
}

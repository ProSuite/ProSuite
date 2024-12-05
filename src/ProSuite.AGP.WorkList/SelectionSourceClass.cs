using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList
{
	public class SelectionSourceClass : SourceClass
	{
		public SelectionSourceClass(GdbTableIdentity tableIdentity,
		                            [CanBeNull] IAttributeReader attributeReader = null)
			: base(tableIdentity, attributeReader) { }

		#region Overrides of SourceClass

		public override long GetUniqueTableId()
		{
			// NOTE: We want to support
			// - un-registered tables, such as shape files
			// - tables from different geodatabases

			return WorkListUtils.GetUniqueTableIdAcrossWorkspaces(TableIdentity);
		}

		#endregion
	}
}

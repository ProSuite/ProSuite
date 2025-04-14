using System.Collections.Generic;
using ArcGIS.Core.Data;
using ProSuite.AGP.WorkList.Contracts;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.AGP.WorkList
{
	public class SelectionSourceClass : SourceClass
	{
		public List<long> Oids { get; }

		public SelectionSourceClass(GdbTableIdentity tableIdentity,
		                            Datastore datastore, SourceClassSchema schema,
		                            List<long> oids,
		                            [CanBeNull] IAttributeReader attributeReader = null)
			: base(tableIdentity, schema, attributeReader)
		{
			Oids = oids;
		}

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

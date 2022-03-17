using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase
{
	public class ForeignKeyAssociationDescription : AssociationDescription
	{
		private readonly string _referencingKeyName;
		private readonly string _referencedKeyName;

		public ForeignKeyAssociationDescription([NotNull] ITable referencingTable,
		                                        [NotNull] string referencingKeyName,
		                                        [NotNull] ITable referencedTable,
		                                        [NotNull] string referencedKeyName)
			: base(referencingTable, referencedTable)
		{
			_referencingKeyName = referencingKeyName;
			_referencedKeyName = referencedKeyName;
		}

		[NotNull]
		public string ReferencingKeyName => _referencingKeyName;

		[NotNull]
		public string ReferencedKeyName => _referencedKeyName;

		public ITable ReferencingTable => Table1;

		public ITable ReferencedTable => Table2;
	}
}

using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase
{
	public class ForeignKeyAssociationDescription : AssociationDescription
	{
		private readonly string _referencingKeyName;
		private readonly string _referencedKeyName;

		[CLSCompliant(false)]
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
	}
}

using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.DataModel
{
	public interface IFieldIndexCache
	{
		int GetFieldIndex([NotNull] IObjectClass objectClass,
		                  [NotNull] string fieldName,
		                  [CanBeNull] AttributeRole role);

		int GetFieldIndex([NotNull] ITable table,
		                  [NotNull] string fieldName,
		                  [CanBeNull] AttributeRole role);

		int GetSubtypeFieldIndex([NotNull] IObjectClass objectClass);

		int GetSubtypeFieldIndex([NotNull] ITable table);
	}
}

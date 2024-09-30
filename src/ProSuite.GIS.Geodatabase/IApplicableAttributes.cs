using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.GIS.Geodatabase.API;

namespace ProSuite.GIS.Geodatabase
{
	public interface IApplicableAttributes
	{
		bool HasNonApplicableAttributes([NotNull] IObjectClass objectClass);

		bool IsApplicable([NotNull] IObjectClass objectClass, int fieldIndex, int? subtype);

		bool IsNonApplicableForAnySubtype([NotNull] IObjectClass objectClass, int fieldIndex);

		object GetNonApplicableValue([NotNull] IObjectClass objectClass, int fieldIndex);

		bool IsNonApplicableValue([NotNull] IObjectClass objectClass, int fieldIndex,
		                          object value);

		void ClearCache();
	}
}

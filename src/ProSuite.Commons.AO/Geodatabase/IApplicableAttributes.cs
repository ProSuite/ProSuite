using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geodatabase
{
	[CLSCompliant(false)]
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

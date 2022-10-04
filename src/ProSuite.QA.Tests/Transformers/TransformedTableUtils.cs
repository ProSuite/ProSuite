using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.QA.Tests.Transformers
{
	public static class TransformedTableUtils
	{
		public static int GetClassId([NotNull] IReadOnlyTable baseTable)
		{
			int classId = -1;
			if (baseTable is IObjectClass objectClass)
			{
				classId = objectClass.ObjectClassID;
			}
			else if (baseTable is ReadOnlyTable roTable)
			{
				// Consider adding this to IReadOnly interface
				classId = ((IObjectClass) roTable.BaseTable).ObjectClassID;
			}

			return classId;
		}

		public static string GetAliasName([NotNull] IReadOnlyTable baseClass)
		{
			string aliasName = null;
			if (baseClass is IObjectClass objectClass)
			{
				aliasName = objectClass.AliasName;
			}
			else if (baseClass is ReadOnlyTable roTable)
			{
				// Consider adding this to IReadOnly interface
				aliasName = ((IObjectClass) roTable.BaseTable).AliasName;
			}

			return aliasName;
		}
	}
}

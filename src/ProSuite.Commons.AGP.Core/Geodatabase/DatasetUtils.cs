using System;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AGP.Core.Geodatabase
{
	public static class DatasetUtils
	{
		[NotNull]
		public static string GetTableDisplayName([NotNull] Table table)
		{
			TableDefinition definition = table.GetDefinition();
			string name = definition.GetName();
			string alias = definition.GetAliasName();

			if (! string.Equals(name, alias, StringComparison.CurrentCultureIgnoreCase)
			) // TODO really CurrentCulture?
			{
				return alias;
			}

			using (var datastore = table.GetDatastore())
			{
				var sqlSyntax = datastore.GetSQLSyntax();
				// TODO why using alias here and not name?
				if (sqlSyntax == null) return alias;
				var parts = sqlSyntax.ParseTableName(alias);
				return parts.Item3;
			}
		}

		public static bool IsSameClass(Table featureClass1, Table featureClass2)
		{
			return featureClass1.Handle == featureClass2.Handle;
		}

		public static GeometryType GetShapeType([NotNull] FeatureClass featureClass)
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));

			return featureClass.GetDefinition().GetShapeType();
		}
	}
}

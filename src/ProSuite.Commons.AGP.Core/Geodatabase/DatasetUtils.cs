using System;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;

namespace ProSuite.Commons.AGP.Core.Geodatabase
{
	public static class DatasetUtils
	{
		[NotNull]
		public static string GetTableDisplayName([NotNull] Table table)
		{
			TableDefinition definition = table.GetDefinition();
			string name = definition.GetName();
			string alias = GetAliasName(definition);

			if (string.Equals(name, alias, StringComparison.CurrentCultureIgnoreCase))
			{
				// the alias name is equal to the name (but may have different case)
				// un-qualify the alias name to preserve it's case.
				using (var datastore = table.GetDatastore())
				{
					var sqlSyntax = datastore.GetSQLSyntax();
					// TODO why using alias here and not name?
					if (sqlSyntax == null) return alias;
					var parts = sqlSyntax.ParseTableName(alias);
					return parts.Item3;
				}
			}

			return alias;
		}

		public static string GetName(Table table)
		{
			if (table is null) return null;
			return table.GetDefinition()?.GetName();
		}

		public static string GetAliasName([NotNull] Table table)
		{
			Assert.ArgumentNotNull(table, nameof(table));

			return GetAliasName(table.GetDefinition());
		}

		[NotNull]
		public static string GetAliasName([NotNull] TableDefinition definition)
		{
			Assert.ArgumentNotNull(definition, nameof(definition));

			try
			{
				string aliasName = definition.GetAliasName();

				return StringUtils.IsNotEmpty(aliasName)
					       ? aliasName
					       : definition.GetName();
			}
			catch (NotImplementedException)
			{
				return definition.GetName();
			}
		}

		[CanBeNull]
		public static SpatialReference GetSpatialReference([NotNull] Feature feature)
		{
			Assert.ArgumentNotNull(feature, nameof(feature));

			return GetSpatialReference(feature.GetTable());
		}

		[CanBeNull]
		public static SpatialReference GetSpatialReference([NotNull] this FeatureClass featureClass)
		{
			Assert.ArgumentNotNull(featureClass, nameof(featureClass));

			return featureClass.GetDefinition()?.GetSpatialReference();
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

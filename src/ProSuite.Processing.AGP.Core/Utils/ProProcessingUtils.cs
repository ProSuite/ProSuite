using System;
using System.Linq;
using ArcGIS.Core.Data;
using ArcGIS.Core.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Gdb;
using ProSuite.Processing.AGP.Core.Domain;

namespace ProSuite.Processing.AGP.Core.Utils
{
	public static class ProProcessingUtils
	{
		[NotNull]
		public static ICartoProcess CreateCartoProcess(Type type, CartoProcessRepo repo = null)
		{
			if (type == null || ! typeof(ICartoProcess).IsAssignableFrom(type))
			{
				throw new CartoConfigException($"Type {type?.Name ?? "(null)"} is not a carto process type");
			}

			object instance;
			if (repo != null && type.GetConstructor(new[] { typeof(CartoProcessRepo) }) != null)
			{
				instance = Activator.CreateInstance(type, repo);
			}
			else
			{
				// by default, use the parameter-less constructor:
				instance = Activator.CreateInstance(type);
			}

			if (instance is null)
			{
				throw new AssertionException($"Activator returned null for type {type.Name}");
			}

			if (instance is not ICartoProcess process)
			{
				throw new AssertionException(
					$"Activator returned a {instance.GetType().Name} instance for type {type.Name}");
			}

			return process;
		}

		/// <summary>
		/// Create a string from the given object. Format:
		/// &quot;OID=123 Class=AliasNameOrDatasetName&quot;
		/// </summary>
		public static string Format(Feature feature)
		{
			if (feature is null) return string.Empty;

			var oid = feature.GetObjectID();
			string className;

			using (var table = feature.GetTable())
			{
				className = table?.GetName() ?? "UnknownTable";
			}

			return FormattableString.Invariant($"OID={oid} Class={className}");
		}

		public static T GetBaseTable<T>(T table) where T : Table
		{
			if (table == null)
				return null;

			while (table.IsJoinedTable())
			{
				var join = table.GetJoin();
				var destination = join.GetDestinationTable();
				table = (T) destination;
			}

			return table;

			//if (!layerTable.IsJoinedTable())
			//	return layerTable;

			//var join = layerTable.GetJoin();
			//var baseTable = join.GetDestinationTable();

			//return (T) baseTable;
		}

		[NotNull]
		public static QueryFilter CreateFilter(string whereClause, Geometry extent)
		{
			QueryFilter filter;

			if (extent != null)
			{
				filter = new SpatialQueryFilter
				         {
					         FilterGeometry = extent,
					         SpatialRelationship = SpatialRelationship.Intersects
				         };
			}
			else
			{
				filter = new QueryFilter();
			}

			if (! string.IsNullOrEmpty(whereClause))
			{
				filter.WhereClause = whereClause;
			}

			return filter;
		}

		/// <summary>
		/// Find the subtype, if any, of the given row values and for the
		/// given table (or feature class) definition; return null if no subtype.
		/// </summary>
		[CanBeNull]
		public static Subtype FindSubtype([NotNull] TableDefinition definition, [NotNull] IRowValues values)
		{
			if (definition is null)
				throw new ArgumentNullException(nameof(definition));
			if (values is null)
				throw new ArgumentNullException(nameof(values));

			string subtypeFieldName = definition.GetSubtypeField();
			if (string.IsNullOrEmpty(subtypeFieldName))
			{
				return null;
			}

			object value = values.GetValue(subtypeFieldName);
			var subtypes = definition.GetSubtypes();

			if (value is int code)
			{
				return subtypes?.FirstOrDefault(s => s.GetCode() == code);
			}

			if (value is string name)
			{
				return subtypes?.FirstOrDefault(s => string.Equals(s.GetName(), name)); // TODO ignore case?
			}

			// First subtype is default subtype (empirical)
			return subtypes?.FirstOrDefault();
		}
	}
}

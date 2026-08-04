using System;
using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.DataModel.LegacyTypes;
using ProSuite.QA.Core;

namespace ProSuite.DomainModel.Core.QA
{
	/// <summary>
	/// AO-free (ArcObjects-independent) mapping between .NET <see cref="Type"/>s used for test,
	/// transformer and issue filter parameters and <see cref="TestParameterType"/>, plus
	/// dataset-parameter validation and empty-value creation.
	/// </summary>
	/// <remarks>
	/// This is the platform-independent core of what
	/// <c>ProSuite.DomainModel.AO.QA.TestParameterTypeUtils</c> exposes. Platform-specific
	/// (e.g. ArcObjects legacy interface) type mappings that cannot live here are registered
	/// via <see cref="RegisterTypeMapping"/>.
	/// </remarks>
	public static class TestParameterTypes
	{
		private static readonly List<Func<Type, TestParameterType?>> _typeMappingResolvers =
			new List<Func<Type, TestParameterType?>>();

		/// <summary>
		/// Registers a fallback resolver, invoked by <see cref="GetParameterType"/> for types not
		/// covered by the platform-independent mapping (scalars, enums, and the schema-definition
		/// interfaces in <see cref="ProSuite.Commons.GeoDb"/>). Resolvers are tried in
		/// registration order; the first non-null result wins.
		/// </summary>
		/// <param name="resolver">A function returning the <see cref="TestParameterType"/> for a
		/// given data type, or <c>null</c> if it does not recognize the type.</param>
		public static void RegisterTypeMapping(
			[NotNull] Func<Type, TestParameterType?> resolver)
		{
			Assert.ArgumentNotNull(resolver, nameof(resolver));

			// Idempotent: registering the same resolver twice (e.g. via both the static
			// constructor and an explicit EnsureRegistered call) must not create duplicate
			// entries. Delegates over the same static method compare equal.
			if (! _typeMappingResolvers.Contains(resolver))
			{
				_typeMappingResolvers.Add(resolver);
			}
		}

		public static TestParameterType GetParameterType([NotNull] Type dataType)
		{
			Assert.ArgumentNotNull(dataType, nameof(dataType));

			// NOTE: test more specific types first, base types last

			// Platform independent definition Types:
			if (typeof(IFeatureClassSchemaDef).IsAssignableFrom(dataType))
				return TestParameterType.VectorDataset;
			if (typeof(ITableSchemaDef).IsAssignableFrom(dataType))
				return TestParameterType.ObjectDataset;
			if (typeof(IMosaicRasterDatasetDef).IsAssignableFrom(dataType))
				return TestParameterType.RasterMosaicDataset;
			if (typeof(IRasterDatasetDef).IsAssignableFrom(dataType))
				return TestParameterType.RasterDataset;
			if (typeof(ITerrainDef).IsAssignableFrom(dataType))
				return TestParameterType.TerrainDataset;
			if (typeof(ITopologyDef).IsAssignableFrom(dataType))
				return TestParameterType.TopologyDataset;

			// Platform-specific fallback (e.g. legacy ArcObjects types), registered by the
			// respective platform assembly (see RegisterTypeMapping):
			foreach (Func<Type, TestParameterType?> resolver in _typeMappingResolvers)
			{
				TestParameterType? resolved = resolver(dataType);

				if (resolved != null)
				{
					return resolved.Value;
				}
			}

			if (dataType == typeof(double))
				return TestParameterType.Double;
			if (dataType == typeof(int))
				return TestParameterType.Integer;
			if (dataType == typeof(bool))
				return TestParameterType.Boolean;
			if (dataType == typeof(string))
				return TestParameterType.String;
			if (dataType == typeof(DateTime))
				return TestParameterType.DateTime;
			if (dataType.IsEnum)
				return TestParameterType.Integer;

			return TestParameterType.CustomScalar;
		}

		public static bool IsDatasetType([NotNull] Type type)
		{
			Assert.ArgumentNotNull(type, nameof(type));

			if (type.IsValueType)
			{
				return false;
			}

			// Derive from the (resolver-aware) parameter type instead of a separate interface
			// list, so IsDatasetType and GetParameterType can never disagree: a legacy
			// ArcObjects feature class resolves to VectorDataset here just as it does in
			// GetParameterType, and therefore CreateEmptyParameterValue produces a
			// DatasetTestParameterValue for it.
			return IsDatasetParameterType(GetParameterType(type));
		}

		/// <summary>
		/// Returns whether the given <see cref="TestParameterType"/> denotes a dataset
		/// (as opposed to a scalar) parameter.
		/// </summary>
		public static bool IsDatasetParameterType(TestParameterType parameterType)
		{
			switch (parameterType)
			{
				case TestParameterType.Unknown:
				case TestParameterType.CustomScalar:
				case TestParameterType.String:
				case TestParameterType.Integer:
				case TestParameterType.Double:
				case TestParameterType.DateTime:
				case TestParameterType.Boolean:
					return false;

				default:
					return true;
			}
		}

		public static bool IsValidDataset(TestParameterType parameterType,
		                                  [NotNull] Dataset dataset)
		{
			switch (parameterType)
			{
				case TestParameterType.Dataset:
					return true;

				case TestParameterType.ObjectDataset:
					return dataset is ObjectDataset;

				case TestParameterType.VectorDataset:
					return dataset is VectorDataset;

				case TestParameterType.TableDataset:
					return dataset is TableDataset;

				case TestParameterType.TopologyDataset:
					return dataset is TopologyDataset;

				case TestParameterType.TerrainDataset:
					return dataset is ISimpleTerrainDataset;

				case TestParameterType.GeometricNetworkDataset:
					return dataset is IGeometricNetworkDataset;

				case TestParameterType.RasterMosaicDataset:
					return dataset is IRasterMosaicDataset;

				case TestParameterType.RasterDataset:
					return dataset is RasterDataset;

				default:
					throw new ArgumentException(
						string.Format("Unsupported parameter type: {0}",
						              Enum.GetName(typeof(TestParameterType), parameterType)));
			}
		}

		/// <summary>
		/// Creates an empty (default-valued) parameter value instance for the given parameter:
		/// a <see cref="DatasetTestParameterValue"/> for dataset parameter types, otherwise a
		/// <see cref="ScalarTestParameterValue"/> initialized with the parameter's default value.
		/// </summary>
		[NotNull]
		public static TestParameterValue CreateEmptyParameterValue(
			[NotNull] TestParameter parameter)
		{
			Assert.ArgumentNotNull(parameter, nameof(parameter));

			if (IsDatasetType(parameter.Type))
			{
				return new DatasetTestParameterValue(parameter);
			}

			if (parameter.DefaultValue == null && parameter.Type == typeof(DateTime))
			{
				// The type default of DateTime (01.01.0001) is never a meaningful date
				// parameter value, unlike 0 / false / the first enum member. Leave the
				// value empty instead, so the user has to enter a date and validation
				// reports the parameter as not set until then.
				return new ScalarTestParameterValue(parameter, (object) null);
			}

			return new ScalarTestParameterValue(
				parameter, $"{parameter.DefaultValue ?? GetDefault(parameter.Type)}");
		}

		[CanBeNull]
		private static object GetDefault([NotNull] Type type)
		{
			if (! type.IsValueType)
			{
				return null;
			}

			object defaultValue = Activator.CreateInstance(type);

			if (type.IsEnum)
			{
				// Ensure valid value for enums: if default value (0) is not in list, return the
				// first enum item value
				if (! Enum.IsDefined(type, defaultValue))
				{
					string[] values = Enum.GetNames(type);
					if (values.Length > 0)
					{
						return values[0];
					}
				}
			}

			return defaultValue;
		}
	}
}

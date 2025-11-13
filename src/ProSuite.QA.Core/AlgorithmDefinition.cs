using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Geom.EsriShape;
using ProSuite.Commons.Reflection;
using ProSuite.QA.Core.ParameterTypes;

namespace ProSuite.QA.Core
{
	/// <summary>
	/// Base class for instance definitions. The definitions have minimal dependencies and can be
	/// instantiated in every environment in order to get the metadata.
	/// The actual test implementation should have a last constructor taking the corresponding
	/// definition class.
	/// </summary>
	public abstract class AlgorithmDefinition
	{
		protected const AngleUnit DefaultAngleUnit = AngleUnit.Radiant;

		public IList<ITableSchemaDef> InvolvedTables { get; }


		protected AlgorithmDefinition([NotNull] ITableSchemaDef involvedTable) : this(
			new[] { involvedTable }) { }

		protected AlgorithmDefinition([NotNull] IEnumerable<ITableSchemaDef> involvedTables)
		{
			Assert.ArgumentNotNull(involvedTables, nameof(involvedTables));

			InvolvedTables = new List<ITableSchemaDef>(involvedTables);
		}

		public object CreateInstance(AlgorithmDefinition definition)
		{
			AssemblyName assemblyName = GetType().Assembly.GetName();

			string assembly = GetType().Assembly.FullName;

			string implementationAssemblyName =
				InstanceUtils.GetImplementationAssemblyName(assemblyName.Name);

			assemblyName.Name = implementationAssemblyName;

			string typeName = Assert.NotNull(definition.GetType().FullName);

			string instanceTypeName = Assert.NotNull(InstanceUtils.TryGetAlgorithmName(typeName));

			Type instanceType =
				PrivateAssemblyUtils.LoadType(assemblyName.FullName, instanceTypeName);

			// Get the last constructor, which is the one that takes the definition
			ConstructorInfo[] constructors = instanceType.GetConstructors();

			int constructorIndex = constructors.Length - 1;
			ConstructorInfo constructor = constructors[constructorIndex];

			return constructor.Invoke(new object[] { definition });
		}
		protected static IList<ITableSchemaDef> CastToTables(
			params IList<IFeatureClassSchemaDef>[] featureClasses)
		{
			int totalCount = featureClasses.Sum(list => list.Count);

			var union = new List<ITableSchemaDef>(totalCount);

			foreach (IList<IFeatureClassSchemaDef> list in featureClasses)
			{
				foreach (IFeatureClassSchemaDef featureClass in list)
				{
					Assert.NotNull(featureClass, "list entry is null");

					union.Add(featureClass);
				}
			}

			return union;
		}
		[NotNull]
		protected static IList<IFeatureClassSchemaDef> Union(
			[NotNull] IList<IFeatureClassSchemaDef> featureClasses0,
			[NotNull] IList<IFeatureClassSchemaDef> featureClasses1)
		{
			Assert.ArgumentNotNull(featureClasses0, nameof(featureClasses0));
			Assert.ArgumentNotNull(featureClasses1, nameof(featureClasses1));

			var union =
				new List<IFeatureClassSchemaDef>(featureClasses0.Count + featureClasses1.Count);

			union.AddRange(featureClasses0);
			union.AddRange(featureClasses1);

			return union;
		}
	}
}

using System;
using System.Collections.Generic;
using System.Reflection;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Reflection;

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
			string assembly = GetType().Assembly.FullName;

			assembly = InstanceUtils.GetImplementationAssemblyName(assembly);

			string typeName = Assert.NotNull(definition.GetType().FullName);

			string instanceTypeName = Assert.NotNull(InstanceUtils.TryGetAlgorithmName(typeName));

			Type instanceType = PrivateAssemblyUtils.LoadType(assembly, instanceTypeName);

			// Get the last constructor, which is the one that takes the definition
			ConstructorInfo[] constructors = instanceType.GetConstructors();

			int constructorIndex = constructors.Length - 1;
			ConstructorInfo constructor = constructors[constructorIndex];

			return constructor.Invoke(new object[] { definition });
		}
	}
}

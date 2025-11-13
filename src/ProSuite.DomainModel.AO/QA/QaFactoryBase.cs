using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.GeoDb;
using ProSuite.DomainModel.Core;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Core;
using System.Collections.Generic;
using System.Linq;

namespace ProSuite.DomainModel.AO.QA
{
	public abstract class QaFactoryBase : TestFactory
	{
		private TestFactoryDefinition _factoryDefinition;

		public TestFactoryDefinition FactoryDefinition
		{
			get
			{
				if (_factoryDefinition == null)
				{
					// Should probably be set by factory creator, but unit tests might not want to...
					ClassDescriptor classDescriptor = new ClassDescriptor(GetType());
					_factoryDefinition =
						InstanceDescriptorUtils.GetTestFactoryDefinition(classDescriptor);
				}
				return _factoryDefinition;
			}
			set => _factoryDefinition = value;
		}

		public override string TestDescription => Assert.NotNull(FactoryDefinition).TestDescription;

		protected override IList<TestParameter> CreateParameters()
		{
			return Assert.NotNull(FactoryDefinition).Parameters;
		}

		public override string GetTestTypeDescription()
		{
			return Assert.NotNull(FactoryDefinition).GetTestTypeDescription();
		}

		protected static List<T> ToReadOnlyTableList<T>(object tableDefListValue) where T : IReadOnlyTable
		{
			var tableSchemaDefList = (IList<ITableSchemaDef>)tableDefListValue;

			List<T> tables = tableSchemaDefList.Cast<T>().ToList();

			return tables;
		}
	}
}

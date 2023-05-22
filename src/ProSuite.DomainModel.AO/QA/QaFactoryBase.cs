using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.QA.Core;

namespace ProSuite.DomainModel.AO.QA
{
	public abstract class QaFactoryBase : TestFactory
	{
		public TestFactoryDefinition FactoryDefinition { get; set; }

		public override string TestDescription => Assert.NotNull(FactoryDefinition).TestDescription;

		protected override IList<TestParameter> CreateParameters()
		{
			return Assert.NotNull(FactoryDefinition).Parameters;
		}

		public override string GetTestTypeDescription()
		{
			return Assert.NotNull(FactoryDefinition).GetTestTypeDescription();
		}
	}
}

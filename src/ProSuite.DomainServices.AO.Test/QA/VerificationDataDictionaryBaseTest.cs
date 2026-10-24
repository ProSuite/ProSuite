using System;
using System.Collections.Generic;
using NUnit.Framework;
using ProSuite.Commons.DomainModels;
using ProSuite.DomainModel.AO.Workflow;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.DataModel.Repositories;
using ProSuite.DomainModel.Core.QA.Repositories;
using ProSuite.DomainServices.AO.QA;

namespace ProSuite.DomainServices.AO.Test.QA
{
	[TestFixture]
	public class VerificationDataDictionaryBaseTest
	{
		[Test]
		public void CanActivateNamedEnvironment()
		{
			var activatedEnvironments = new List<string>();

			var dataDictionary = new TestDataDictionary();
			dataDictionary.EnableMultipleEnvironments(activatedEnvironments.Add);

			dataDictionary.ActivateForCurrentThread("TOPGISP_DDX");

			Assert.AreEqual(new[] { "TOPGISP_DDX" }, activatedEnvironments);
		}

		[Test]
		public void CanUseGlobalEnvironmentWithoutEnvironmentName()
		{
			// Only one environment is served: the global container is used, no name is needed.
			var dataDictionary = new TestDataDictionary();

			Assert.DoesNotThrow(() => dataDictionary.ActivateForCurrentThread(null));
			Assert.DoesNotThrow(() => dataDictionary.ActivateForCurrentThread(string.Empty));
		}

		[TestCase(null)]
		[TestCase("")]
		public void CannotActivateWithoutEnvironmentNameIfSeveralEnvironmentsAreServed(
			string environmentName)
		{
			// Issue A (qa-server-issues.md): the server serves several environments and the
			// request names none. Previously the call returned silently. The thread then used
			// the container of the environment that the previous request on the same thread
			// had activated, or failed with 'IoCContainer has not been set up for current
			// thread' when the thread had never served a request before.
			var activatedEnvironments = new List<string>();

			var dataDictionary = new TestDataDictionary();
			dataDictionary.EnableMultipleEnvironments(activatedEnvironments.Add);

			var exception =
				Assert.Throws<InvalidOperationException>(() => dataDictionary
					                                         .ActivateForCurrentThread(
						                                         environmentName));

			Assert.NotNull(exception);
			StringAssert.Contains("environment", exception.Message);
			Assert.IsEmpty(activatedEnvironments);
		}

		private class TestDataDictionary : VerificationDataDictionaryBase<ProductionModel>
		{
			public override IDomainTransactionManager DomainTransactions =>
				throw new NotImplementedException();

			public override IAssociationRepository Associations =>
				throw new NotImplementedException();

			public override IDatasetRepository Datasets => throw new NotImplementedException();

			public override IProjectRepository<ProductionModel> Projects =>
				throw new NotImplementedException();

			public override IQualityConditionRepository QualityConditions =>
				throw new NotImplementedException();

			public override IQualitySpecificationRepository QualitySpecifications =>
				throw new NotImplementedException();
		}
	}
}

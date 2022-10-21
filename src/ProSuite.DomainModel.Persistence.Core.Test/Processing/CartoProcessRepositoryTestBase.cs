using NUnit.Framework;
using ProSuite.DomainModel.Core;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.Processing;
using ProSuite.DomainModel.Core.Processing.Repositories;

namespace ProSuite.DomainModel.Persistence.Core.Test.Processing
{
	public abstract class CartoProcessRepositoryTestBase
		: RepositoryTestBase<ICartoProcessRepository>
	{
		protected abstract DdxModel CreateModel();

		[Test]
		public void CanGetCartoProcess()
		{
			DdxModel model = CreateModel();

			CartoProcessType processType = new CartoProcessType(
				"typeName", "typeDesc",
				new ClassDescriptor("processTypeName", "processAssemblyName"));

			const string cpName = "cpName";

			CartoProcess cp = new CartoProcess(cpName, null, model, processType);
			cp.AddParameter(new CartoProcessParameter("paramName", "paramValue", cp));

			CreateSchema(model, processType, cp);

			UnitOfWork.NewTransaction(
				delegate
				{
					AssertUnitOfWorkHasNoChanges();

					CartoProcess foundProcess = Repository.Get(cp.Id);

					Assert.IsNotNull(foundProcess);
					Assert.AreNotSame(foundProcess, cp);
					Assert.AreEqual(cpName, foundProcess.Name);
					Assert.AreEqual(processType.Name, foundProcess.CartoProcessType.Name);
					Assert.AreEqual(1, foundProcess.Parameters.Count);

					var param = foundProcess.Parameters[0];
					Assert.IsNotNull(param);

					Assert.AreEqual("paramName", param.Name);
					Assert.AreEqual("paramValue", param.Value);
				});
		}
	}
}

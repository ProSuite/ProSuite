using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.AO.Workflow;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.DataModel.Repositories;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Repositories;

namespace ProSuite.DomainServices.AO.QA
{
	public class VerificationDataDictionary<TModel>
		: VerificationDataDictionaryBase<TModel>, IVerificationDataDictionary<TModel> where TModel : ProductionModel
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull] private readonly IQualitySpecificationRepository _qualitySpecifications;
		[NotNull] private readonly IQualityConditionRepository _qualityConditions;
		[NotNull] private readonly IProjectRepository<TModel> _projects;
		[NotNull] private readonly IDatasetRepository _datasets;
		[NotNull] private readonly IAssociationRepository _associations;

		public override IDomainTransactionManager DomainTransactions { get; }

		public override IQualitySpecificationRepository QualitySpecifications => _qualitySpecifications;

		public override IQualityConditionRepository QualityConditions => _qualityConditions;

		public override IProjectRepository<TModel> Projects => _projects;

		public override IDatasetRepository Datasets => _datasets;

		public override IAssociationRepository Associations => _associations;

		public VerificationDataDictionary(
			[NotNull] IDomainTransactionManager domainTransactions,
			[NotNull] IQualitySpecificationRepository qualitySpecifications,
			[NotNull] IQualityConditionRepository qualityConditions,
			[NotNull] IProjectRepository<TModel> projects,
			[NotNull] IDatasetRepository datasets,
			[NotNull] IAssociationRepository associations)
		{
			DomainTransactions = domainTransactions;

			_qualitySpecifications = qualitySpecifications;
			_qualityConditions = qualityConditions;
			_projects = projects;
			_datasets = datasets;
			_associations = associations;
		}
	}
}

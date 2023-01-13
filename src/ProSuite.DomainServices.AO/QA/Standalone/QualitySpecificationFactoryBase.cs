using System.Collections.Generic;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Xml;
using ProSuite.DomainServices.AO.QA.Standalone.XmlBased;
using ProSuite.DomainServices.AO.QA.VerifiedDataModel;

namespace ProSuite.DomainServices.AO.QA.Standalone
{
	public abstract class QualitySpecificationFactoryBase
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		protected IVerifiedModelFactory ModelFactory { get; }

		/// <summary>
		/// Initializes a new instance of the <see cref="XmlBasedQualitySpecificationFactory"/> class.
		/// </summary>
		/// <param name="modelFactory">The model builder.</param>
		protected QualitySpecificationFactoryBase(
			[NotNull] IVerifiedModelFactory modelFactory)
		{
			Assert.ArgumentNotNull(modelFactory, nameof(modelFactory));

			ModelFactory = modelFactory;
		}

		protected static void HandleNoConditionCreated(
			[CanBeNull] string conditionName,
			[NotNull] IDictionary<string, DdxModel> modelsByWorkspaceId,
			bool ignoreConditionsForUnknownDatasets,
			[NotNull] ICollection<DatasetTestParameterRecord> unknownDatasetParameters)
		{
			Assert.True(ignoreConditionsForUnknownDatasets,
			            "ignoreConditionsForUnknownDatasets");
			Assert.True(unknownDatasetParameters.Count > 0,
			            "Unexpected number of unknown datasets");

			_msg.WarnFormat(
				unknownDatasetParameters.Count == 1
					? "Quality condition '{0}' is ignored because the following dataset is not found: {1}"
					: "Quality condition '{0}' is ignored because the following datasets are not found: {1}",
				conditionName,
				XmlDataQualityUtils.ConcatenateUnknownDatasetNames(
					unknownDatasetParameters,
					modelsByWorkspaceId,
					DataSource.AnonymousId));
		}
	}
}

using System.Collections.Generic;
using NHibernate;
using ProSuite.Commons.Collections;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Orm.NHibernate;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainModel.Core.QA.Repositories;

namespace ProSuite.DomainModel.Persistence.Core.QA
{
	[UsedImplicitly]
	public class QualityVerificationRepository :
		NHibernateRepository<QualityVerification>,
		IQualityVerificationRepository
	{
		#region IQualityVerificationRepository Members

		public IList<QualityVerification> Get(QualityCondition qualityCondition)
		{
			Assert.ArgumentNotNull(qualityCondition, nameof(qualityCondition));

			if (! qualityCondition.IsPersistent)
			{
				return new List<QualityVerification>();
			}

			using (ISession session = OpenSession(true))
			{
				return session.CreateQuery(
					              "select distinct qveri " +
					              "  from QualityVerification qveri " +
					              "  join qveri.ConditionVerifications elem " +
					              " where elem.QualityCondition = :qualityCondition or " +
					              "elem.StopCondition = :qualityCondition")
				              .SetEntity("qualityCondition", qualityCondition)
				              .List<QualityVerification>();
			}
		}

		public IEnumerable<QualityVerification> Get(DdxModel model)
		{
			Assert.ArgumentNotNull(model, nameof(model));

			if (! model.IsPersistent)
			{
				return new List<QualityVerification>();
			}

			using (ISession session = OpenSession(true))
			{
				IList<int> datasetIds = GetEntityIds(model.Datasets);

				if (datasetIds.Count == 0)
				{
					return new List<QualityVerification>();
				}

				const int maxSublistLength = 1000;
				if (datasetIds.Count <= maxSublistLength)
				{
					return GetQualityVerifications(session, datasetIds);
				}

				// more than 1000 datasets; split the lists
				var distinctVerifications = new HashSet<QualityVerification>();

				var first = true;
				foreach (IList<int> datasetIdSublist in
					CollectionUtils.Split(datasetIds, maxSublistLength))
				{
					foreach (QualityVerification verification in
						GetQualityVerifications(session, datasetIdSublist))
					{
						// avoid the cost of unnecessary Contains() call for first sub list
						// (verification should be unique *within* each sub list)
						if (first || ! distinctVerifications.Contains(verification))
						{
							distinctVerifications.Add(verification);
						}
					}

					first = false;
				}

				return distinctVerifications;
			}
		}

		[NotNull]
		private static IEnumerable<QualityVerification> GetQualityVerifications(
			[NotNull] ISession session,
			[NotNull] IEnumerable<int> datasetIds)
		{
			Assert.ArgumentNotNull(session, nameof(session));
			Assert.ArgumentNotNull(datasetIds, nameof(datasetIds));

			return session.CreateQuery(
				              "select distinct qveri " +
				              "  from QualityVerification qveri" +
				              "  join qveri.VerificationDatasets verifiedDataset " +
				              " where verifiedDataset.Dataset.Id in (:datasetIds)")
			              .SetParameterList("datasetIds", datasetIds)
			              .List<QualityVerification>();
		}

		#endregion
	}
}

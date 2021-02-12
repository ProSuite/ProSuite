using System;
using System.Collections.Generic;
using System.Reflection;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainServices.AO.QA.IssuePersistence;
using ProSuite.QA.Container;

namespace ProSuite.DomainServices.AO.QA
{
	public class AllowedErrorFactory
	{
		[NotNull] private readonly IDictionary<int, QualityCondition> _qualityConditionsById;

		[NotNull] private readonly IQualityConditionObjectDatasetResolver _datasetResolver;

		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// Initializes a new instance of the <see cref="AllowedErrorFactory"/> class.
		/// </summary>
		/// <param name="qualityConditionsById">The quality conditions by id.</param>
		/// <param name="datasetResolver">The quality condition-based dataset resolver.</param>
		public AllowedErrorFactory(
			[NotNull] IDictionary<int, QualityCondition> qualityConditionsById,
			[NotNull] IQualityConditionObjectDatasetResolver datasetResolver)
		{
			Assert.ArgumentNotNull(qualityConditionsById, nameof(qualityConditionsById));
			Assert.ArgumentNotNull(datasetResolver, nameof(datasetResolver));

			_qualityConditionsById = qualityConditionsById;
			_datasetResolver = datasetResolver;
		}

		[CanBeNull]
		public AllowedError CreateAllowedError(
			[NotNull] IssueDatasetWriter issueWriter,
			[NotNull] IRow errorRow)
		{
			Assert.ArgumentNotNull(issueWriter, nameof(issueWriter));
			Assert.ArgumentNotNull(errorRow, nameof(errorRow));

			int? qualityConditionId = issueWriter.Get<int>(
				errorRow, AttributeRole.ErrorConditionId);

			if (qualityConditionId == null)
			{
				_msg.WarnFormat(
					"Error object <oid> {0} in {1} has no quality condition id",
					errorRow.OID, issueWriter.DatasetName);
				return null;
			}

			QualityCondition qualityCondition;
			if (! _qualityConditionsById.TryGetValue((int) qualityConditionId,
			                                         out qualityCondition))
			{
				return null;
			}

			IGeometry geometry = null;
			var errorFeature = errorRow as IFeature;
			if (errorFeature != null)
			{
				geometry = errorFeature.Shape.Envelope;
				geometry.SnapToSpatialReference();
			}

			string involvedObjectsString = issueWriter.GetString(
				errorRow, AttributeRole.ErrorObjects);

			IList<InvolvedRow> involvedRows = RowParser.Parse(involvedObjectsString);

			string description =
				issueWriter.GetString(errorRow, AttributeRole.ErrorDescription);

			// explicit storage of ConditionVersion for later comparison (context change determination)
			// because condition.Version is read-only (VersionedEntity)
			int? conditionVersion = null;

			if (issueWriter.HasAttribute(AttributeRole.ErrorQualityConditionVersion))
			{
				conditionVersion =
					issueWriter.Get<int>(errorRow,
					                     AttributeRole.ErrorQualityConditionVersion);
			}

			// Date of creation attribute is expected
			DateTime? dateOfCreation = issueWriter.Get<DateTime>(
				errorRow, AttributeRole.DateOfCreation);

			if (dateOfCreation == null)
			{
				_msg.WarnFormat(
					"Date of creation field is null for allowed error row <oid> {0} in {1}. It will be disregarded.",
					errorRow.OID, issueWriter.DatasetName);
				return null;
			}

			return new AllowedError(qualityCondition, conditionVersion, geometry, description,
			                        involvedRows, issueWriter.Table, errorRow.OID,
			                        (DateTime) dateOfCreation, _datasetResolver);
		}
	}
}

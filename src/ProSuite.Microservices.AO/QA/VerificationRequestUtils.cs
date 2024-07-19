using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.Microservices.Definitions.QA;
using ProSuite.Microservices.Definitions.Shared.Gdb;

namespace ProSuite.Microservices.AO.QA
{
	public static class VerificationRequestUtils
	{
		/// <summary>
		/// Creates the verification request using the specified work context message.
		/// </summary>
		/// <param name="workContextMsg">The pre-assembled work context message.</param>
		/// <param name="qualitySpecificationMsg">The pre-assembled quality specification message.</param>
		/// <param name="perimeter">The verification perimeter or null to verify the entire work
		/// context.</param>
		/// <param name="objectsToVerify">The specific objects to be verified.</param>
		/// <param name="datasetLookup">The dataset lookup which must be provided if objectsToVerify is
		/// specified.</param>
		/// <param name="ddxEnvironmentName">The DDX environment for single-DDX applications to be
		/// provided with each request.
		/// This property is only relevant for enterprise server verification setups where
		/// one server shall serve requests from multiple DDX environments.</param>
		/// <returns></returns>
		public static VerificationRequest CreateRequest(
			[NotNull] WorkContextMsg workContextMsg,
			[NotNull] QualitySpecificationMsg qualitySpecificationMsg,
			[CanBeNull] IGeometry perimeter,
			[CanBeNull] IList<IObject> objectsToVerify = null,
			[CanBeNull] IDatasetLookup datasetLookup = null,
			[CanBeNull] string ddxEnvironmentName = null)
		{
			var request = new VerificationRequest();

			request.WorkContext = workContextMsg;

			request.Specification = qualitySpecificationMsg;

			request.Parameters = new VerificationParametersMsg();

			if (perimeter != null && ! perimeter.IsEmpty)
			{
				ShapeMsg areaOfInterest = ProtobufGeometryUtils.ToShapeMsg(
					perimeter, ShapeMsg.FormatOneofCase.EsriShape,
					SpatialReferenceMsg.FormatOneofCase.SpatialReferenceEsriXml);

				request.Parameters.Perimeter = areaOfInterest;
			}

			if (objectsToVerify != null)
			{
				Assert.NotNull(datasetLookup, nameof(datasetLookup));

				foreach (IObject objToVerify in objectsToVerify)
				{
					IGeometry geometry = null;
					if (objToVerify is IFeature feature)
					{
						geometry = feature.Shape;
					}

					ObjectDataset objectDatset = datasetLookup.GetDataset(objToVerify);

					if (objectDatset != null)
					{
						request.Features.Add(
							ProtobufGdbUtils.ToGdbObjectMsg(
								objToVerify, geometry, objectDatset.Id,
								SpatialReferenceMsg.FormatOneofCase.SpatialReferenceEsriXml));
					}
				}
			}

			request.UserName = EnvironmentUtils.UserDisplayName;
			request.Environment = ddxEnvironmentName;

			return request;
		}

		public static void SetVerificationParameters(
			[NotNull] VerificationRequest request,
			double tileSize,
			bool saveVerification,
			bool filterTableRowsUsingRelatedGeometry,
			bool invalidateExceptionsIfAnyInvolvedObjectChanged,
			bool invalidateExceptionsIfConditionWasUpdated = false)
		{
			request.Parameters.TileSize = tileSize;

			// Save verification if it's the full work unit / release cycle
			request.Parameters.SaveVerificationStatistics = saveVerification;

			// ErrorCreation.UseReferenceGeometries (not for WU verification!)
			request.Parameters.FilterTableRowsUsingRelatedGeometry =
				filterTableRowsUsingRelatedGeometry;

			// ErrorCreation.IgnoreAllowedErrors translates to 
			request.Parameters.OverrideAllowedErrors = false;

			// Always report unused (and it has no performance impact)
			request.Parameters.ReportUnusedExceptions = true;

			// Invalid could be
			// - Involvd row has been deleted (always determined)
			// - InvalidateAllowedErrorsIfAnyInvolvedObjectChanged (see below)
			// - InvalidateAllowedErrorsIfQualityConditionWasUpdated (see below)
			request.Parameters.ReportInvalidExceptions = true;

			request.Parameters.InvalidateExceptionsIfConditionWasUpdated =
				invalidateExceptionsIfConditionWasUpdated;

			request.Parameters.InvalidateExceptionsIfAnyInvolvedObjectChanged =
				invalidateExceptionsIfAnyInvolvedObjectChanged;
		}
	}
}

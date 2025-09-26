using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.Microservices.Definitions.QA;
using ProSuite.Microservices.Definitions.Shared.Gdb;

namespace ProSuite.Microservices.AO.QA
{
	public static class VerificationRequestUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

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

		public static ITableFilter CreateFilter([NotNull] IReadOnlyTable objectClass,
		                                        [CanBeNull] string subFields,
		                                        [CanBeNull] string whereClause,
		                                        [CanBeNull] ShapeMsg searchGeometryMsg)
		{
			subFields = EnsureOIDFieldName(subFields, objectClass);

			if (! (objectClass is IReadOnlyFeatureClass featureClass))
			{
				return CreateFilter(subFields, whereClause);
			}

			IGeometry searchGeometry = ProtobufGeometryUtils.FromShapeMsg(
				searchGeometryMsg, featureClass.SpatialReference);

			if (searchGeometry == null)
			{
				return CreateFilter(subFields, whereClause);
			}

			IFeatureClassFilter result = GdbQueryUtils.CreateFeatureClassFilter(searchGeometry);

			SetSubfieldsAndWhereClause(result, subFields, whereClause);

			return result;
		}

		public static string GetSubFieldForCounting(ITable objectClass,
		                                            bool isRelQueryTable)
		{
			if (isRelQueryTable && objectClass is IFeatureClass featureClass)
			{
				// Workaround for TOP-4975: crash for certain joins/extents if OID field 
				// (which was incorrectly changed by IName.Open()!) is used as only subfields field
				// Note: when not crashing, the resulting row count was incorrect when that OID field was used.
				return featureClass.ShapeFieldName;
			}

			return objectClass.OIDFieldName;
		}

		// TODO: Make obsolete, use other overload instead
		public static GdbData ReadGdbData([NotNull] IReadOnlyTable roTable,
		                                  [CanBeNull] ITableFilter filter,
		                                  string subFields,
		                                  long resultClassHandle)
		{
			GdbData featureData = new GdbData();

			if (roTable is IReadOnlyFeatureClass fc)
			{
				_msg.VerboseDebug(() => $"{fc.Name} shape field is {fc.ShapeFieldName}");
				_msg.VerboseDebug(() => $"{fc.Name} object id field is {fc.OIDFieldName}");
			}

			foreach (IReadOnlyRow row in roTable.EnumRows(filter, true))
			{
				try
				{
					GdbObjectMsg objectMsg =
						ProtobufGdbUtils.ToGdbObjectMsg(row, false, true, subFields);

					objectMsg.ClassHandle = resultClassHandle;

					featureData.GdbObjects.Add(objectMsg);
				}
				catch (Exception e)
				{
					_msg.Debug($"Error converting {GdbObjectUtils.ToString(row)} to object message",
					           e);
					throw;
				}
			}

			// Later, we could break up into several messages, if the total size gets too large
			return featureData;
		}

		public static IEnumerable<GdbData> ReadGdbData([NotNull] IReadOnlyTable roTable,
		                                               [CanBeNull] ITableFilter filter,
		                                               string subFields,
		                                               long resultClassHandle,
		                                               int maxRowCount,
		                                               bool countOnly)
		{
			GdbData featureData = new GdbData();

			if (roTable is IReadOnlyFeatureClass fc)
			{
				_msg.VerboseDebug(() => $"{fc.Name} shape field is {fc.ShapeFieldName}");
				_msg.VerboseDebug(() => $"{fc.Name} object id field is {fc.OIDFieldName}");
			}

			if (countOnly)
			{
				featureData.GdbObjectCount = roTable.RowCount(filter);

				yield return featureData;
				yield break;
			}

			int rowCount = 0;

			foreach (IReadOnlyRow row in roTable.EnumRows(filter, true))
			{
				try
				{
					GdbObjectMsg objectMsg =
						ProtobufGdbUtils.ToGdbObjectMsg(row, false, true, subFields);

					objectMsg.ClassHandle = resultClassHandle;

					featureData.GdbObjects.Add(objectMsg);

					rowCount++;
				}
				catch (Exception e)
				{
					_msg.Debug($"Error converting {GdbObjectUtils.ToString(row)} to object message",
					           e);
					throw;
				}

				if (rowCount % maxRowCount == 0)
				{
					_msg.VerboseDebug(
						() => $"Read {rowCount} rows from {roTable.Name} (max {maxRowCount})");
					featureData.HasMoreData = true;

					yield return featureData;

					featureData = new GdbData(); // reset for next batch
				}
			}

			// Later, we could break up into several messages, if the total size gets too large
			yield return featureData;
		}

		private static string EnsureOIDFieldName(string subFields, IReadOnlyTable objectClass)
		{
			if (string.IsNullOrEmpty(subFields))
			{
				return subFields;
			}

			if (subFields.Contains("*"))
			{
				return subFields;
			}

			if (objectClass.HasOID && ! subFields.Contains(objectClass.OIDFieldName))
			{
				return objectClass.OIDFieldName + "," + subFields;
			}

			return subFields;
		}

		private static ITableFilter CreateFilter([CanBeNull] string subFields,
		                                         [CanBeNull] string whereClause)
		{
			ITableFilter result = new AoTableFilter();

			SetSubfieldsAndWhereClause(result, subFields, whereClause);

			return result;
		}

		private static void SetSubfieldsAndWhereClause([NotNull] ITableFilter result,
		                                               [CanBeNull] string subFields,
		                                               [CanBeNull] string whereClause)
		{
			if (! string.IsNullOrEmpty(subFields))
			{
				result.SubFields = subFields;
			}

			if (! string.IsNullOrEmpty(whereClause))
			{
				result.WhereClause = whereClause;
			}
		}
	}
}

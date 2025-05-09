using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.Callbacks;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;
using ProSuite.DomainModel.AO.DataModel;
using ProSuite.DomainModel.AO.QA;
using ProSuite.DomainModel.Core.QA;
using ProSuite.DomainServices.AO.QA.IssuePersistence;
using ProSuite.DomainServices.AO.QA.Issues;
using ProSuite.DomainServices.AO.QA.Standalone.XmlBased;
using ProSuite.Microservices.AO;
using ProSuite.Microservices.Definitions.QA;
using ProSuite.Microservices.Definitions.Shared.Gdb;
using ProSuite.QA.Core.IssueCodes;

namespace ProSuite.Microservices.Server.AO.QA
{
	public static class ProtobufQaUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public static List<DataSource> GetDataSources(
			[NotNull] IEnumerable<DataSourceMsg> dataSourceMsgs)
		{
			var dataSources = dataSourceMsgs.Select(
				dsMsg => new DataSource(dsMsg.ModelName, dsMsg.Id, dsMsg.CatalogPath,
				                        dsMsg.Database, dsMsg.SchemaOwner)).ToList();

			_msg.DebugFormat("{0} data sources provided:{1} {2}",
			                 dataSources.Count, Environment.NewLine,
			                 StringUtils.Concatenate(dataSources, Environment.NewLine));

			return dataSources;
		}

		public static IssueMsg CreateIssueProto(
			[NotNull] IssueFoundEventArgs args,
			[NotNull] IVerificationContext verificationContext)
		{
			var supportedGeometryTypes =
				GetSupportedErrorRepoGeometryTypes(verificationContext).ToList();

			ISpatialReference spatialReference =
				verificationContext.SpatialReferenceDescriptor.GetSpatialReference();

			return CreateIssueProto(args, spatialReference, supportedGeometryTypes);
		}

		public static IssueMsg CreateIssueProto(IssueFoundEventArgs args,
		                                        ISpatialReference spatialReference,
		                                        List<esriGeometryType> supportedGeometryTypes)
		{
			QualityCondition qualityCondition =
				args.QualitySpecificationElement.QualityCondition;

			IssueMsg issueProto = new IssueMsg();

			issueProto.ConditionId = qualityCondition.Id;
			issueProto.Allowable = args.IsAllowable;
			issueProto.StopCondition = args.Issue.StopCondition;

			CallbackUtils.DoWithNonNull(
				args.Issue.Description, s => issueProto.Description = s);

			IssueCode issueCode = args.Issue.IssueCode;

			if (issueCode != null)
			{
				CallbackUtils.DoWithNonNull(
					issueCode.ID, s => issueProto.IssueCodeId = s);

				CallbackUtils.DoWithNonNull(
					issueCode.Description, s => issueProto.IssueCodeDescription = s);
			}

			CallbackUtils.DoWithNonNull(
				args.Issue.AffectedComponent,
				(value) => issueProto.AffectedComponent = value);

			issueProto.InvolvedTables.AddRange(GetInvolvedTableMessages(args.Issue.InvolvedTables));

			CallbackUtils.DoWithNonNull(
				args.LegacyInvolvedObjectsString,
				(value) => issueProto.LegacyInvolvedRows = value);

			// create valid Error geometry (geometry type, min dimensions) if possible
			IGeometry geometry = ErrorRepositoryUtils.GetGeometryToStore(
				args.ErrorGeometry, spatialReference, supportedGeometryTypes);

			issueProto.IssueGeometry =
				ProtobufGeometryUtils.ToShapeMsg(geometry);

			// NOTE: Multipatches are not restored from byte arrays in EsriShape (10.6.1)
			ShapeMsg.FormatOneofCase format =
				geometry?.GeometryType == esriGeometryType.esriGeometryMultiPatch
					? ShapeMsg.FormatOneofCase.Wkb
					: ShapeMsg.FormatOneofCase.EsriShape;

			issueProto.IssueGeometry =
				ProtobufGeometryUtils.ToShapeMsg(geometry, format);

			issueProto.CreationDateTimeTicks = DateTime.Now.Ticks;

			//issueProto.IsInvalidException = args.us;

			//if (args.IsAllowed)
			//{
			//	issueProto.ExceptedObjRef = new GdbObjRefMsg()
			//	                            {
			//		                            ClassHandle = args.AllowedErrorRef.ClassId,
			//		                            ObjectId = args.AllowedErrorRef.ObjectId
			//	                            };
			//}

			return issueProto;
		}

		private static IEnumerable<esriGeometryType> GetSupportedErrorRepoGeometryTypes(
			IVerificationContext verificationContext)
		{
			if (verificationContext.NoGeometryIssueDataset != null)
				yield return esriGeometryType.esriGeometryNull;

			if (verificationContext.MultipointIssueDataset != null)
				yield return esriGeometryType.esriGeometryMultipoint;

			if (verificationContext.LineIssueDataset != null)
				yield return esriGeometryType.esriGeometryPolyline;

			if (verificationContext.PolygonIssueDataset != null)
				yield return esriGeometryType.esriGeometryPolygon;

			if (verificationContext.MultiPatchIssueDataset != null)
				yield return esriGeometryType.esriGeometryMultiPatch;
		}

		private static IEnumerable<InvolvedTableMsg> GetInvolvedTableMessages(
			IEnumerable<InvolvedTable> involvedTables)
		{
			foreach (InvolvedTable involvedTable in involvedTables)
			{
				var involvedTableMsg = new InvolvedTableMsg();
				involvedTableMsg.TableName = involvedTable.TableName;

				foreach (RowReference rowRef in involvedTable.RowReferences)
				{
					involvedTableMsg.ObjectIds.Add(rowRef.OID);
				}

				yield return involvedTableMsg;
			}
		}
	}
}

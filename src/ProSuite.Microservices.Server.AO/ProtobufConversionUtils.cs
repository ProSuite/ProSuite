using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.IO;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;
using ProSuite.DomainModel.Core.QA.Xml;
using ProSuite.DomainServices.AO.QA.Standalone.XmlBased;
using ProSuite.Microservices.AO;
using ProSuite.Microservices.Client;
using ProSuite.Microservices.Client.QA;
using ProSuite.Microservices.Definitions.QA;
using ProSuite.Microservices.Definitions.Shared.Gdb;
using ProSuite.Microservices.Server.AO.Geodatabase;
using ProSuite.Microservices.Server.AO.QA;

namespace ProSuite.Microservices.Server.AO
{
	public static class ProtobufConversionUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		/// <summary>
		/// Converts the specified messages into a list of features that reference
		/// a GdbTable or GdbFeatureClass which, however have null workspaces.
		/// </summary>
		/// <param name="gdbObjectMessages"></param>
		/// <param name="objectClassMessages"></param>
		/// <returns></returns>
		[NotNull]
		public static IList<IFeature> FromGdbObjectMsgList(
			[NotNull] ICollection<GdbObjectMsg> gdbObjectMessages,
			[NotNull] ICollection<ObjectClassMsg> objectClassMessages)
		{
			IDictionary<long, GdbFeatureClass> classesByHandle =
				CreateGdbClassByHandleDictionary(objectClassMessages);

			var result = new List<IFeature>();

			foreach (GdbObjectMsg gdbObjectMsg in gdbObjectMessages)
			{
				GdbFeature remoteFeature = FromGdbFeatureMsg(
					gdbObjectMsg, () => classesByHandle[gdbObjectMsg.ClassHandle]);

				result.Add(remoteFeature);
			}

			return result;
		}

		private static IDictionary<long, GdbFeatureClass> CreateGdbClassByHandleDictionary(
			[NotNull] ICollection<ObjectClassMsg> objectClassMsgs)
		{
			var result = new Dictionary<long, GdbFeatureClass>();

			// Create minimal workspace (for equality comparisons)
			var workspaces = new Dictionary<long, GdbWorkspace>();

			foreach (ObjectClassMsg classMsg in objectClassMsgs)
			{
				if (! workspaces.TryGetValue(classMsg.WorkspaceHandle, out GdbWorkspace workspace))
				{
					workspace = GdbWorkspace.CreateEmptyWorkspace(classMsg.WorkspaceHandle);
					workspaces.Add(classMsg.WorkspaceHandle, workspace);
				}

				if (! result.ContainsKey(classMsg.ClassHandle))
				{
					GdbFeatureClass gdbTable =
						(GdbFeatureClass) FromObjectClassMsg(classMsg, workspace);

					result.Add(classMsg.ClassHandle, gdbTable);
				}
			}

			return result;
		}

		public static IList<IFeature> FromGdbObjectMsgList(
			[NotNull] ICollection<GdbObjectMsg> gdbObjectMessages,
			[NotNull] GdbTableContainer container)
		{
			var result = new List<IFeature>(gdbObjectMessages.Count);

			Assert.NotNull(container, "No object class provided");

			foreach (GdbObjectMsg gdbObjectMsg in gdbObjectMessages)
			{
				GdbFeature remoteFeature = FromGdbFeatureMsg(
					gdbObjectMsg,
					() => (GdbFeatureClass) container.GetByClassId((int) gdbObjectMsg.ClassHandle));

				result.Add(remoteFeature);
			}

			return result;
		}

		public static GdbTableContainer CreateGdbTableContainer(
			[NotNull] IEnumerable<ObjectClassMsg> objectClassMessages,
			[CanBeNull] Func<DataVerificationResponse, DataVerificationRequest> getRemoteDataFunc,
			out GdbWorkspace workspace)
		{
			GdbTableContainer container = null;
			long? workspaceHandle = null;
			workspace = null;

			foreach (ObjectClassMsg objectClassMsg in objectClassMessages)
			{
				if (workspaceHandle == null)
				{
					workspaceHandle = objectClassMsg.WorkspaceHandle;

					container = new GdbTableContainer();

					workspace = new GdbWorkspace(container, workspaceHandle);
				}
				else
				{
					Assert.AreEqual(workspaceHandle, objectClassMsg.WorkspaceHandle,
					                "Not all features are from the same workspace");
				}

				if (objectClassMsg.WorkspaceHandle == -1)
				{
					workspace = null;
				}

				Func<ITable, BackingDataset> createBackingDataset = null;

				if (getRemoteDataFunc != null)
				{
					createBackingDataset = (t) =>
						new RemoteDataset(t, getRemoteDataFunc,
						                  new ClassDef
						                  {
							                  ClassHandle = objectClassMsg.ClassHandle,
							                  WorkspaceHandle = objectClassMsg.WorkspaceHandle
						                  });
				}

				GdbTable gdbTable =
					FromObjectClassMsg(objectClassMsg, workspace, createBackingDataset);

				container.TryAdd(gdbTable);
			}

			return container;
		}

		public static GdbWorkspace CreateGdbWorkspace(
			[NotNull] WorkspaceMsg workspaceMessage,
			[NotNull] IEnumerable<ObjectClassMsg> objectClassMessages)
		{
			var container = new GdbTableContainer();

			DateTime? defaultCreationDate =
				FromTicks(workspaceMessage.DefaultVersionCreationTicks);

			DateTime? defaultModificationDate =
				FromTicks(workspaceMessage.DefaultVersionModificationTicks);

			string path = FileSystemUtils.FromPathUri(workspaceMessage.Path);

			var gdbWorkspace = new GdbWorkspace(container, workspaceMessage.WorkspaceHandle,
			                                    (WorkspaceDbType) workspaceMessage.WorkspaceDbType,
			                                    path,
			                                    EmptyToNull(workspaceMessage.VersionName),
			                                    EmptyToNull(workspaceMessage.DefaultVersionName),
			                                    defaultCreationDate,
			                                    defaultModificationDate);

			foreach (ObjectClassMsg objectClassMsg in objectClassMessages)
			{
				Assert.AreEqual(gdbWorkspace.WorkspaceHandle, objectClassMsg.WorkspaceHandle,
				                "Not all object classes are from the provided workspace");

				GdbTable gdbTable = FromObjectClassMsg(objectClassMsg, gdbWorkspace);

				container.TryAdd(gdbTable);
			}

			return gdbWorkspace;
		}

		public static IList<GdbWorkspace> CreateSchema(
			[NotNull] IEnumerable<ObjectClassMsg> objectClassMessages,
			[CanBeNull] ICollection<ObjectClassMsg> relClassMessages = null,
			Func<DataVerificationResponse, DataVerificationRequest> moreDataRequest = null)
		{
			var result = new List<GdbWorkspace>();
			foreach (IGrouping<long, ObjectClassMsg> classGroup in objectClassMessages.GroupBy(
				         c => c.WorkspaceHandle))
			{
				GdbTableContainer gdbTableContainer =
					CreateGdbTableContainer(classGroup, moreDataRequest,
					                        out GdbWorkspace gdbWorkspace);

				result.Add(gdbWorkspace);

				if (relClassMessages == null)
				{
					continue;
				}

				foreach (ObjectClassMsg relTableMsg
				         in relClassMessages.Where(
					         r => r.WorkspaceHandle == gdbWorkspace.WorkspaceHandle))
				{
					GdbTable relClassTable = FromObjectClassMsg(relTableMsg, gdbWorkspace);

					gdbTableContainer.TryAddRelationshipClass(relClassTable);
				}
			}

			return result;
		}

		public static IList<GdbWorkspace> CreateSchema(
			[NotNull] IEnumerable<ObjectClassMsg> objectClassMessages,
			[NotNull] ICollection<WorkspaceMsg> workspaceMessages)
		{
			var result = new List<GdbWorkspace>();

			foreach (IGrouping<long, ObjectClassMsg> classGroup in objectClassMessages.GroupBy(
				         c => c.WorkspaceHandle))
			{
				long workspaceHandle = classGroup.Key;

				WorkspaceMsg workspaceMsg =
					workspaceMessages.Single(wm => wm.WorkspaceHandle == workspaceHandle);

				GdbWorkspace gdbWorkspace = CreateGdbWorkspace(workspaceMsg, classGroup);

				result.Add(gdbWorkspace);
			}

			return result;
		}

		public static GdbTable FromObjectClassMsg(
			[NotNull] ObjectClassMsg objectClassMsg,
			[CanBeNull] IWorkspace workspace,
			[CanBeNull] Func<ITable, BackingDataset> createBackingDataset = null)
		{
			esriGeometryType geometryType = (esriGeometryType) objectClassMsg.GeometryType;

			GdbTable result;
			if (geometryType == esriGeometryType.esriGeometryNull)
			{
				result = new GdbTable((int) objectClassMsg.ClassHandle,
				                      objectClassMsg.Name, objectClassMsg.Alias,
				                      createBackingDataset, workspace);
			}
			else
			{
				result = new GdbFeatureClass(
					         (int) objectClassMsg.ClassHandle,
					         objectClassMsg.Name,
					         (esriGeometryType) objectClassMsg.GeometryType,
					         objectClassMsg.Alias,
					         createBackingDataset, workspace)
				         {
					         SpatialReference =
						         ProtobufGeometryUtils.FromSpatialReferenceMsg(
							         objectClassMsg.SpatialReference)
				         };

				if (objectClassMsg.Fields == null || objectClassMsg.Fields.Count == 0)
				{
					// The shape field can be important to determine Z/M awareness, etc:
					bool hasZ = result.SpatialReference.HasZPrecision();
					bool hasM = result.SpatialReference.HasMPrecision();
					IField shapeField = FieldUtils.CreateShapeField(
						result.ShapeFieldName, geometryType, result.SpatialReference,
						0d, hasZ, hasM);

					result.AddField(shapeField);
				}
			}

			AddFields(objectClassMsg, result, geometryType);

			return result;
		}

		public static GdbTable FromQueryTableMsg(
			[NotNull] ObjectClassMsg objectClassMsg,
			[NotNull] IWorkspace workspace,
			[NotNull] Func<ITable, BackingDataset> createBackingDataset,
			IList<IReadOnlyTable> involvedTables)
		{
			esriGeometryType geometryType = (esriGeometryType) objectClassMsg.GeometryType;

			GdbTable result;
			if (geometryType == esriGeometryType.esriGeometryNull)
			{
				result = new RemoteQueryTable(
					(int) objectClassMsg.ClassHandle, objectClassMsg.Name, objectClassMsg.Alias,
					createBackingDataset, workspace, involvedTables);
			}
			else
			{
				result = new RemoteQueryFeatureClass(
					         (int) objectClassMsg.ClassHandle, objectClassMsg.Name,
					         (esriGeometryType) objectClassMsg.GeometryType,
					         objectClassMsg.Alias,
					         createBackingDataset, workspace, involvedTables)
				         {
					         SpatialReference =
						         ProtobufGeometryUtils.FromSpatialReferenceMsg(
							         objectClassMsg.SpatialReference)
				         };
			}

			AddFields(objectClassMsg, result, geometryType);

			return result;
		}

		public static GdbFeature FromGdbFeatureMsg(
			[NotNull] GdbObjectMsg gdbObjectMsg,
			[NotNull] Func<GdbFeatureClass> getClass)
		{
			GdbFeatureClass featureClass = getClass();
			//(IFeatureClass) tableContainer.GetByClassId((int) gdbObjectMsg.ClassHandle);

			GdbFeature result = CreateGdbFeature(gdbObjectMsg, featureClass);

			return result;
		}

		public static GdbRow FromGdbObjectMsg(
			[NotNull] GdbObjectMsg gdbObjectMsg,
			[NotNull] GdbTable table)
		{
			GdbRow result;
			if (table is GdbFeatureClass featureClass)
			{
				result = CreateGdbFeature(gdbObjectMsg, featureClass);
			}
			else
			{
				result = new GdbRow((int) gdbObjectMsg.ObjectId, table);
			}

			ReadMsgValues(gdbObjectMsg, result, table);

			return result;
		}

		public static SpecificationElement CreateXmlConditionElement(
			[NotNull] QualitySpecificationElementMsg specificationElementMsg)
		{
			QualityConditionMsg conditionMsg = specificationElementMsg.Condition;

			var parameterList = new List<XmlTestParameterValue>();

			foreach (ParameterMsg parameterMsg in conditionMsg.Parameters)
			{
				XmlTestParameterValue xmlParameter;
				if (StringUtils.IsNotEmpty(parameterMsg.WorkspaceId))
				{
					xmlParameter = new XmlDatasetTestParameterValue()
					               {
						               TestParameterName = parameterMsg.Name,
						               Value = ProtobufGeomUtils.EmptyToNull(parameterMsg.Value),
						               WorkspaceId = parameterMsg.WorkspaceId,
						               WhereClause = parameterMsg.WhereClause
					               };
				}
				else
				{
					xmlParameter = new XmlScalarTestParameterValue()
					               {
						               TestParameterName = parameterMsg.Name,
						               Value = ProtobufGeomUtils.EmptyToNull(parameterMsg.Value)
					               };
				}

				parameterList.Add(xmlParameter);
			}

			XmlQualityCondition xmlCondition =
				new XmlQualityCondition
				{
					Name = conditionMsg.Name,
					TestDescriptorName = conditionMsg.TestDescriptorName,
					Description = conditionMsg.Description,
					Url = conditionMsg.Url
				};

			xmlCondition.ParameterValues.AddRange(parameterList);

			var specificationElement =
				new SpecificationElement(xmlCondition,
				                         specificationElementMsg.CategoryName)
				{
					AllowErrors = specificationElementMsg.AllowErrors,
					StopOnError = specificationElementMsg.StopOnError
				};
			return specificationElement;
		}

		private static GdbFeature CreateGdbFeature(GdbObjectMsg gdbObjectMsg,
		                                           GdbFeatureClass featureClass)
		{
			GdbFeature result = GdbFeature.Create((int) gdbObjectMsg.ObjectId, featureClass);

			ShapeMsg shapeBuffer = gdbObjectMsg.Shape;

			if (shapeBuffer != null)
			{
				ISpatialReference classSpatialRef = DatasetUtils.GetSpatialReference(featureClass);

				// NOTE: Setting the shape can be slow due to the Property-Set work-arounds
				IGeometry shape =
					ProtobufGeometryUtils.FromShapeMsg(shapeBuffer, classSpatialRef);

				result.Shape = shape;
			}

			return result;
		}

		private static void AddFields([NotNull] ObjectClassMsg objectClassMsg,
		                              [NotNull] VirtualTable toResultTable,
		                              esriGeometryType geometryType)
		{
			if (objectClassMsg.Fields == null || objectClassMsg.Fields.Count <= 0)
			{
				return;
			}

			foreach (FieldMsg fieldMsg in objectClassMsg.Fields)
			{
				IField field = FieldUtils.CreateField(fieldMsg.Name,
				                                      (esriFieldType) fieldMsg.Type,
				                                      fieldMsg.AliasName);

				if (field.Type == esriFieldType.esriFieldTypeString)
				{
					((IFieldEdit) field).Length_2 = fieldMsg.Length;
				}
				else if (field.Type == esriFieldType.esriFieldTypeGeometry)
				{
					var sr = ProtobufGeometryUtils.FromSpatialReferenceMsg(
						objectClassMsg.SpatialReference);
					field = FieldUtils.CreateShapeField(geometryType, sr, 1000, true, false);
				}

				if (toResultTable.Fields.FindField(field.Name) < 0)
				{
					toResultTable.AddField(field);
				}
				else
				{
					_msg.DebugFormat("Field {0} is duplicate or has been added previously",
					                 field.Name);
				}

				if (fieldMsg.DomainName == ProtobufGdbUtils.SubtypeDomainName)
				{
					toResultTable.SubtypeFieldName = field.Name;
				}
			}
		}

		private static void ReadMsgValues(GdbObjectMsg gdbObjectMsg, GdbRow intoResult,
		                                  ITable table)
		{
			if (gdbObjectMsg.Values.Count == 0)
			{
				return;
			}

			Assert.AreEqual(table.Fields.FieldCount, gdbObjectMsg.Values.Count,
			                "GdbObject message values do not correspond to table schema");

			for (var index = 0; index < gdbObjectMsg.Values.Count; index++)
			{
				AttributeValue attributeValue = gdbObjectMsg.Values[index];

				object valueObj = ProtoDataQualityUtils.FromAttributeValue(attributeValue);

				// Special case:
				if (valueObj is Guid guid)
				{
					valueObj = UIDUtils.CreateUID(guid);
				}

				if (valueObj != null)
				{
					intoResult.set_Value(index, valueObj);
				}
			}
		}

		public static DateTime? FromTicks(long ticks)
		{
			DateTime? defaultCreationDate =
				ticks <= 0
					? (DateTime?) null
					: new DateTime(ticks);
			return defaultCreationDate;
		}

		public static string EmptyToNull(string value)
		{
			return string.IsNullOrEmpty(value) ? null : value;
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ProSuite.Commons.AO;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.AO.Geodatabase.GdbSchema;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Gdb;
using ProSuite.Commons.Logging;
using ProSuite.Commons.Text;
using ProSuite.DomainModel.AO.QA.Xml;
using ProSuite.DomainServices.AO.QA.Standalone.XmlBased;
using ProSuite.Microservices.AO;
using ProSuite.Microservices.Definitions.QA;
using ProSuite.Microservices.Definitions.Shared;
using ProSuite.Microservices.Server.AO.Geodatabase;

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
			IDictionary<long, IFeatureClass> classesByHandle =
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

		private static IDictionary<long, IFeatureClass> CreateGdbClassByHandleDictionary(
			[NotNull] ICollection<ObjectClassMsg> objectClassMsgs)
		{
			var result = new Dictionary<long, IFeatureClass>();

			foreach (ObjectClassMsg classMsg in objectClassMsgs)
			{
				if (! result.ContainsKey(classMsg.ClassHandle))
				{
					IFeatureClass gdbTable = (IFeatureClass) FromObjectClassMsg(classMsg, null);

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
					() => (IFeatureClass) container.GetByClassId((int) gdbObjectMsg.ClassHandle));

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
				workspaceMessage.DefaultVersionCreationTicks == 0
					? (DateTime?) null
					: new DateTime(workspaceMessage.DefaultVersionCreationTicks);

			var gdbWorkspace = new GdbWorkspace(container, workspaceMessage.WorkspaceHandle,
			                                    (WorkspaceDbType) workspaceMessage.WorkspaceDbType,
			                                    EmptyToNull(workspaceMessage.Path),
			                                    EmptyToNull(workspaceMessage.VersionName),
			                                    EmptyToNull(workspaceMessage.DefaultVersionName),
			                                    defaultCreationDate);

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
			}

			if (objectClassMsg.Fields == null || objectClassMsg.Fields.Count <= 0)
			{
				return result;
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

				if (result.Fields.FindField(field.Name) < 0)
				{
					result.AddField(field);
				}
				else
				{
					_msg.DebugFormat("Field {0} is duplicate or has been added previously",
					                 field.Name);
				}
			}

			return result;
		}

		public static GdbFeature FromGdbFeatureMsg(
			[NotNull] GdbObjectMsg gdbObjectMsg,
			[NotNull] Func<IFeatureClass> getClass)
		{
			IFeatureClass featureClass = getClass();
			//(IFeatureClass) tableContainer.GetByClassId((int) gdbObjectMsg.ClassHandle);

			GdbFeature result = CreateGdbFeature(gdbObjectMsg, featureClass);

			return result;
		}

		public static GdbRow FromGdbObjectMsg(
			[NotNull] GdbObjectMsg gdbObjectMsg,
			[NotNull] ITable table)
		{
			GdbRow result;
			if (table is IFeatureClass featureClass)
			{
				result = CreateGdbFeature(gdbObjectMsg, featureClass);
			}
			else
			{
				result = new GdbRow((int) gdbObjectMsg.ObjectId, (IObjectClass) table);
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
						               Value = parameterMsg.Value,
						               WorkspaceId = parameterMsg.WorkspaceId,
						               WhereClause = parameterMsg.WhereClause
					               };
				}
				else
				{
					xmlParameter = new XmlScalarTestParameterValue()
					               {
						               TestParameterName = parameterMsg.Name,
						               Value = parameterMsg.Value
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
		                                           IFeatureClass featureClass)
		{
			ISpatialReference classSpatialRef = DatasetUtils.GetSpatialReference(featureClass);

			IGeometry shape =
				ProtobufGeometryUtils.FromShapeMsg(gdbObjectMsg.Shape, classSpatialRef);

			var result = new GdbFeature((int) gdbObjectMsg.ObjectId, featureClass)
			             {
				             Shape = shape
			             };

			return result;
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

				switch (attributeValue.ValueCase)
				{
					case AttributeValue.ValueOneofCase.None:
						break;
					case AttributeValue.ValueOneofCase.DbNull:
						intoResult.set_Value(index, DBNull.Value);
						break;
					case AttributeValue.ValueOneofCase.ShortIntValue:
						intoResult.set_Value(index, attributeValue.ShortIntValue);
						break;
					case AttributeValue.ValueOneofCase.LongIntValue:
						intoResult.set_Value(index, attributeValue.LongIntValue);
						break;
					case AttributeValue.ValueOneofCase.FloatValue:
						intoResult.set_Value(index, attributeValue.FloatValue);
						break;
					case AttributeValue.ValueOneofCase.DoubleValue:
						intoResult.set_Value(index, attributeValue.DoubleValue);
						break;
					case AttributeValue.ValueOneofCase.StringValue:
						intoResult.set_Value(index, attributeValue.StringValue);
						break;
					case AttributeValue.ValueOneofCase.DateTimeTicksValue:
						intoResult.set_Value(
							index, new DateTime(attributeValue.DateTimeTicksValue));
						break;
					case AttributeValue.ValueOneofCase.UuidValue:
						var guid = new Guid(attributeValue.UuidValue.Value.ToByteArray());
						IUID uid = UIDUtils.CreateUID(guid);
						intoResult.set_Value(index, uid);
						break;
					case AttributeValue.ValueOneofCase.BlobValue:
						intoResult.set_Value(index, attributeValue.BlobValue);
						break;
					default:
						if (table.Fields.Field[index].Type == esriFieldType.esriFieldTypeGeometry)
						{
							// Leave empty, it is already assigned to the Shape property
							break;
						}

						throw new ArgumentOutOfRangeException();
				}
			}
		}

		public static string EmptyToNull(string value)
		{
			return string.IsNullOrEmpty(value) ? null : value;
		}
	}
}

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Google.Protobuf;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;
using ProSuite.Commons.Geom.EsriShape;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.Core;
using ProSuite.DomainModel.Core.DataModel;
using ProSuite.DomainModel.Core.QA;
using ProSuite.Microservices.Definitions.QA;
using ProSuite.Microservices.Definitions.Shared.Ddx;
using ProSuite.Microservices.Definitions.Shared.Gdb;

namespace ProSuite.Microservices.Client.QA
{
	public static class ProtoDataQualityUtils
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		private static int _currentModelId = -100;

		private static readonly IList<GeometryType> _geometryTypes =
			GeometryTypeFactory.CreateGeometryTypes().ToList();

		/// <summary>
		/// Creates a specification message containing the the protobuf based conditions.
		/// </summary>
		/// <param name="specification"></param>
		/// <param name="supportedInstanceDescriptors">If the optional supported instance descriptors
		/// are specified, the instance descriptor names are checked if they can be resolved and,
		/// potentially translated to the canonical name.</param>
		/// <param name="usedModelsById">The data models that are referenced by the specification.
		/// In case the stand-alone verification is used, make sure to augment the result's DataSourceMsg
		/// list with the respective connection information.</param>
		/// <returns></returns>
		public static ConditionListSpecificationMsg CreateConditionListSpecificationMsg(
			[NotNull] QualitySpecification specification,
			[CanBeNull] ISupportedInstanceDescriptors supportedInstanceDescriptors,
			out IDictionary<int, DdxModel> usedModelsById)
		{
			var result = new ConditionListSpecificationMsg
			             {
				             Name = specification.Name,
				             Description = specification.Description ?? string.Empty
			             };

			// The datasource ID will correspond with the model id. The model id must not be -1
			// (the non-persistent value) to avoid two non-persistent but different models with the
			// same id.
			usedModelsById = new Dictionary<int, DdxModel>();

			// The parameters must be initialized!
			InstanceConfigurationUtils.InitializeParameterValues(specification);

			foreach (QualitySpecificationElement element in specification.Elements)
			{
				if (! element.Enabled)
				{
					continue;
				}

				QualityCondition condition = element.QualityCondition;

				string categoryName = condition.Category?.Name ?? string.Empty;

				var elementMsg = new QualitySpecificationElementMsg
				                 {
					                 AllowErrors = element.AllowErrors,
					                 StopOnError = element.StopOnError,
					                 CategoryName = categoryName
				                 };

				QualityConditionMsg conditionMsg =
					CreateQualityConditionMsg(condition, supportedInstanceDescriptors,
					                          usedModelsById);

				elementMsg.Condition = conditionMsg;

				result.Elements.Add(elementMsg);
			}

			// NOTE: The added data sources list does not contain connection information.
			//       The caller must assign the catalog path or connection props, if necessary.

			result.DataSources.AddRange(
				usedModelsById.Select(
					kvp => new DataSourceMsg
					       {
						       Id = kvp.Key.ToString(CultureInfo.InvariantCulture),
						       ModelName = kvp.Value.Name
					       }));

			return result;
		}

		public static QualityConditionMsg CreateQualityConditionMsg(
			[NotNull] QualityCondition condition,
			[CanBeNull] ISupportedInstanceDescriptors supportedInstanceDescriptors,
			IDictionary<int, DdxModel> usedModelsById)
		{
			string descriptorName =
				GetDescriptorName(condition.TestDescriptor, supportedInstanceDescriptors);

			var conditionMsg = new QualityConditionMsg
			                   {
				                   ConditionId = condition.Id,
				                   TestDescriptorName = descriptorName,
				                   Name = Assert.NotNull(
					                   condition.Name, "Condition name is null"),
				                   Description = condition.Description ?? string.Empty,
				                   Url = condition.Url ?? string.Empty,
				                   IssueFilterExpression =
					                   condition.IssueFilterExpression ?? string.Empty
			                   };

			AddParameterMessages(condition.GetDefinedParameterValues(), conditionMsg.Parameters,
			                     supportedInstanceDescriptors, usedModelsById);

			foreach (IssueFilterConfiguration filterConfiguration in condition
				         .IssueFilterConfigurations)
			{
				conditionMsg.ConditionIssueFilters.Add(
					CreateInstanceConfigMsg<IssueFilterDescriptor>(
						filterConfiguration, supportedInstanceDescriptors, usedModelsById));
			}

			return conditionMsg;
		}

		private static string GetDescriptorName<T>(
			[NotNull] T instanceDescriptor,
			[CanBeNull] ISupportedInstanceDescriptors supportedInstanceDescriptors)
			where T : InstanceDescriptor
		{
			if (supportedInstanceDescriptors == null)
			{
				// Cannot check. Let's hope the server knows it.
				return instanceDescriptor.Name;
			}

			string descriptorName = instanceDescriptor.Name;

			if (supportedInstanceDescriptors.GetInstanceDescriptor<T>(descriptorName) != null)
			{
				return descriptorName;
			}

			// The instance descriptor name is not known. Try the canonical name:
			string canonicalName = instanceDescriptor.GetCanonicalName();

			// TODO: Automatically add the canonical name as fall-back
			if (supportedInstanceDescriptors.GetInstanceDescriptor<T>(canonicalName) != null)
			{
				return canonicalName;
			}

			// Fall-back: Use AsssemblyQualified type name with constructor
			if (InstanceDescriptorUtils.TryExtractClassInfo(descriptorName, out _, out _))
			{
				// It's in the fully qualified form, good to go
				return descriptorName;
			}

			// It will probably fail on the server, unless it's supported there...
			_msg.DebugFormat(
				"Descriptor name {0} could not be resolved. Let's hope it can be resolved on the server!",
				descriptorName);

			return descriptorName;
		}

		private static void AddParameterMessages(
			[NotNull] IEnumerable<TestParameterValue> parameterValues,
			[NotNull] ICollection<ParameterMsg> parameterMsgs,
			[CanBeNull] ISupportedInstanceDescriptors supportedInstanceDescriptors,
			IDictionary<int, DdxModel> usedModelsById)
		{
			foreach (TestParameterValue parameterValue in parameterValues)
			{
				ParameterMsg parameterMsg = new ParameterMsg
				                            {
					                            Name = parameterValue.TestParameterName
				                            };

				if (parameterValue is DatasetTestParameterValue datasetParamValue)
				{
					if (datasetParamValue.DatasetValue != null)
					{
						parameterMsg.Value = datasetParamValue.DatasetValue.Name;

						DdxModel model = datasetParamValue.DatasetValue.Model;

						int modelId = -1;
						if (model.Id == -1)
						{
							// Find by reference
							foreach (var kvp in usedModelsById)
							{
								if (kvp.Value != model)
								{
									continue;
								}

								modelId = kvp.Key;
								break;
							}

							if (modelId == -1)
							{
								modelId = _currentModelId--;
							}
						}
						else
						{
							modelId = model.Id;
						}

						// NOTE: Fake values (negative, but not -1) are allowed. But they must be unique per model!
						Assert.False(modelId == -1,
						             "Invalid model id (not persistent and no CloneId has been set)");

						if (! usedModelsById.ContainsKey(modelId))
						{
							usedModelsById.Add(modelId, model);
						}

						parameterMsg.WorkspaceId = modelId.ToString(CultureInfo.InvariantCulture);
					}

					if (datasetParamValue.ValueSource != null)
					{
						TransformerConfiguration transformerConfiguration =
							Assert.NotNull(datasetParamValue.ValueSource);

						parameterMsg.Transformer =
							CreateInstanceConfigMsg<TransformerDescriptor>(
								transformerConfiguration, supportedInstanceDescriptors,
								usedModelsById);
					}

					parameterMsg.WhereClause = datasetParamValue.FilterExpression ?? string.Empty;
					parameterMsg.UsedAsReferenceData = datasetParamValue.UsedAsReferenceData;
				}
				else if (parameterValue is ScalarTestParameterValue scalarParamValue)
				{
					// Transport in invariant culture, it will be formatted on the client
					if (scalarParamValue.PersistedStringValue != null)
					{
						parameterMsg.Value = scalarParamValue.PersistedStringValue;
					}
				}
				else
				{
					// Does this ever happen?
					parameterMsg.Value = parameterValue.StringValue;
				}

				parameterMsgs.Add(parameterMsg);
			}
		}

		private static InstanceConfigurationMsg CreateInstanceConfigMsg<T>(
			[NotNull] InstanceConfiguration instanceConfiguration,
			[CanBeNull] ISupportedInstanceDescriptors supportedInstanceDescriptors,
			[NotNull] IDictionary<int, DdxModel> usedModelsById)
			where T : InstanceDescriptor
		{
			var result = new InstanceConfigurationMsg
			             {
				             Id = instanceConfiguration.Id,
				             Name = instanceConfiguration.Name,
				             Url = instanceConfiguration.Url ?? string.Empty,
				             Description = instanceConfiguration.Description ?? string.Empty
			             };

			string descriptorName =
				GetDescriptorName((T) instanceConfiguration.InstanceDescriptor,
				                  supportedInstanceDescriptors);

			result.InstanceDescriptorName = descriptorName;

			AddParameterMessages(instanceConfiguration.GetDefinedParameterValues(),
			                     result.Parameters, supportedInstanceDescriptors, usedModelsById);

			return result;
		}

		public static IEnumerable<InstanceDescriptorMsg> GetInstanceDescriptorMsgs(
			[NotNull] QualitySpecification specification)
		{
			IEnumerable<QualityCondition> qualityConditions =
				specification.Elements.Select(e => e.QualityCondition);

			foreach (InstanceDescriptorMsg instanceDescriptorMsg in GetInstanceDescriptorMsgs(
				         qualityConditions))
			{
				yield return instanceDescriptorMsg;
			}
		}

		public static IEnumerable<InstanceDescriptorMsg> GetInstanceDescriptorMsgs(
			[NotNull] IEnumerable<QualityCondition> qualityConditions)
		{
			ICollection<InstanceDescriptor> referencedDescriptors =
				GetReferencedDescriptors(qualityConditions);

			foreach (InstanceDescriptor instanceDescriptor in referencedDescriptors)
			{
				yield return ToInstanceDescriptorMsg(instanceDescriptor);
			}
		}

		[NotNull]
		private static ICollection<InstanceDescriptor> GetReferencedDescriptors(
			[NotNull] IEnumerable<QualityCondition> qualityConditions)
		{
			var descriptors = new HashSet<InstanceDescriptor>();
			var allTransformerConfigurations = new HashSet<TransformerConfiguration>();

			foreach (QualityCondition qualityCondition in qualityConditions)
			{
				descriptors.Add(qualityCondition.TestDescriptor);

				CollectTransformers(qualityCondition, allTransformerConfigurations);

				foreach (IssueFilterConfiguration filterConfiguration in
				         qualityCondition.IssueFilterConfigurations)
				{
					descriptors.Add(filterConfiguration.IssueFilterDescriptor);

					CollectTransformers(filterConfiguration, allTransformerConfigurations);
				}
			}

			foreach (TransformerConfiguration transformerConfiguration in
			         allTransformerConfigurations)
			{
				descriptors.Add(transformerConfiguration.TransformerDescriptor);
			}

			return descriptors;
		}

		private static void CollectTransformers(
			[NotNull] InstanceConfiguration configuration,
			[NotNull] HashSet<TransformerConfiguration> allTransformers)
		{
			foreach (TestParameterValue parameterValue in configuration.ParameterValues)
			{
				TransformerConfiguration transformer = parameterValue.ValueSource;
				if (transformer != null)
				{
					if (allTransformers.Add(transformer))
					{
						CollectTransformers(transformer, allTransformers);
					}
				}
			}
		}

		public static InstanceDescriptorMsg ToInstanceDescriptorMsg(
			[NotNull] InstanceDescriptor instanceDescriptor)
		{
			var result = new InstanceDescriptorMsg
			             {
				             Id = instanceDescriptor.Id,
				             Name = instanceDescriptor.Name,
				             Type = (int) GetInstanceType(instanceDescriptor)
			             };

			result.ClassDescriptor = ToClassDescriptorMsg(instanceDescriptor);

			// -1 in case of TestFactory! It can be 0 in the instanceDescriptor.ConstructorId, see TestFactoryDescriptor setter of TestDescriptor (!)
			if (instanceDescriptor is TestDescriptor testDescriptor &&
			    testDescriptor.TestFactoryDescriptor != null)
			{
				result.Constructor = -1;
			}
			else
			{
				result.Constructor = instanceDescriptor.ConstructorId;
			}

			return result;
		}

		[NotNull]
		private static ClassDescriptorMsg ToClassDescriptorMsg(
			[NotNull] InstanceDescriptor instanceDescriptor)
		{
			Type instanceType = instanceDescriptor.InstanceInfo?.InstanceType;

			if (instanceType != null)
			{
				// Typically redirected to ProSuite.Qa.Tests/TestFactories
				ClassDescriptor actualClassDescriptor = new ClassDescriptor(instanceType);
				return ToClassDescriptorMsg(actualClassDescriptor);
			}

			ClassDescriptor classDescriptor = instanceDescriptor.Class;

			if (classDescriptor == null &&
			    instanceDescriptor is TestDescriptor testDescriptor)
			{
				classDescriptor = testDescriptor.TestFactoryDescriptor;
			}

			Assert.NotNull(classDescriptor, $"No class descriptor for {instanceDescriptor.Name}");

			return ToClassDescriptorMsg(classDescriptor);
		}

		private static ClassDescriptorMsg ToClassDescriptorMsg(
			[NotNull] ClassDescriptor classDescriptor)
		{
			var result = new ClassDescriptorMsg
			             {
				             TypeName = classDescriptor.TypeName,
				             AssemblyName = classDescriptor.AssemblyName
			             };

			return result;
		}

		private static InstanceType GetInstanceType([NotNull] InstanceDescriptor instanceDescriptor)
		{
			if (instanceDescriptor is TestDescriptor)
			{
				return InstanceType.Test;
			}

			if (instanceDescriptor is TransformerDescriptor)
			{
				return InstanceType.Transformer;
			}

			if (instanceDescriptor is IssueFilterDescriptor)
			{
				return InstanceType.IssueFilter;
			}

			throw new ArgumentOutOfRangeException(
				$"Unsupported type: {instanceDescriptor.GetType()}");
		}

		public static T FromInstanceDescriptorMsg<T>(
			[NotNull] InstanceDescriptorMsg instanceDescriptorMsg) where T : InstanceDescriptor
		{
			ClassDescriptorMsg classDescriptorMsg = instanceDescriptorMsg.ClassDescriptor;

			var classDescriptor =
				new ClassDescriptor(classDescriptorMsg.TypeName, classDescriptorMsg.AssemblyName);

			string name = instanceDescriptorMsg.Name;

			int constructorId = instanceDescriptorMsg.Constructor;

			InstanceDescriptor result;

			if (typeof(T) == typeof(TestDescriptor))
			{
				result =
					constructorId < 0
						? new TestDescriptor(name, classDescriptor)
						: new TestDescriptor(name, classDescriptor,
						                     constructorId);
			}
			else if (typeof(T) == typeof(TransformerDescriptor))
			{
				result = new TransformerDescriptor(name, classDescriptor, constructorId);
			}
			else if (typeof(T) == typeof(IssueFilterDescriptor))
			{
				result = new IssueFilterDescriptor(name, classDescriptor, constructorId);
			}
			else
			{
				throw new ArgumentOutOfRangeException($"Unknown descriptor type: {typeof(T)}");
			}

			result.SetCloneId(instanceDescriptorMsg.Id);

			return (T) result;
		}

		#region ProtoModelUtils

		public static ModelMsg ToDdxModelMsg(DdxModel productionModel, SpatialReferenceMsg srWkId,
		                                     ICollection<DatasetMsg> referencedDatasetMsgs)
		{
			var modelMsg =
				new ModelMsg
				{
					ModelId = productionModel.Id,
					Name = productionModel.Name,
					SpatialReference = srWkId
				};

			foreach (Dataset dataset in productionModel.GetDatasets())
			{
				modelMsg.DatasetIds.Add(dataset.Id);

				if (dataset is IErrorDataset)
				{
					modelMsg.ErrorDatasetIds.Add(dataset.Id);
				}

				DatasetMsg datasetMsg = ToDatasetMsg(dataset);

				referencedDatasetMsgs.Add(datasetMsg);
			}

			modelMsg.SqlCaseSensitivity = (int) productionModel.SqlCaseSensitivity;

			modelMsg.ElementNamesAreQualified = productionModel.ElementNamesAreQualified;

			modelMsg.DefaultDatabaseName =
				ProtobufGeomUtils.NullToEmpty(productionModel.DefaultDatabaseName);
			modelMsg.DefaultDatabaseSchemaOwner =
				ProtobufGeomUtils.NullToEmpty(productionModel.DefaultDatabaseSchemaOwner);

			return modelMsg;
		}

		public static DatasetMsg ToDatasetMsg([NotNull] Dataset dataset,
		                                      bool includeDetails = false)
		{
			int geometryType = (int) GetGeometryType(dataset);

			var datasetMsg =
				new DatasetMsg
				{
					DatasetId = dataset.Id,
					Name = ProtobufGeomUtils.NullToEmpty(dataset.Name),
					AliasName = ProtobufGeomUtils.NullToEmpty(dataset.AliasName),
					GeometryType = geometryType,
					DatasetType = (int) dataset.DatasetType
				};

			if (includeDetails)
			{
				if (dataset is ObjectDataset objectDataset)
				{
					foreach (ObjectAttribute attribute in objectDataset.GetAttributes())
					{
						AttributeMsg attributeMsg = ToAttributeMsg(attribute);

						datasetMsg.Attributes.Add(attributeMsg);
					}

					foreach (ObjectType objectType in objectDataset.GetObjectTypes())
					{
						datasetMsg.ObjectCategories.AddRange(ToObjectCategoryMsg(objectType));
					}
				}
			}

			return datasetMsg;
		}

		public static Dataset FromDatasetMsg(DatasetMsg datasetMsg,
		                                     Func<DatasetMsg, Dataset> factoryMethod)
		{
			Dataset dataset = factoryMethod(datasetMsg);

			dataset.SetCloneId(datasetMsg.DatasetId);

			dataset.AliasName = datasetMsg.AliasName;

			dataset.GeometryType =
				GetGeometryType((ProSuiteGeometryType) datasetMsg.GeometryType);

			return dataset;
		}

		public static ObjectDataset CreateErrorDataset(int datasetId, string name,
		                                               ProSuiteGeometryType geometryType)
		{
			ObjectDataset result;
			switch (geometryType)
			{
				case ProSuiteGeometryType.Multipoint:
					result = new ErrorMultipointDataset(name);
					break;
				case ProSuiteGeometryType.Polyline:
					result = new ErrorLineDataset(name);
					break;
				case ProSuiteGeometryType.Polygon:
					result = new ErrorPolygonDataset(name);
					break;
				case ProSuiteGeometryType.MultiPatch:
					result = new ErrorMultiPatchDataset(name);
					break;
				case ProSuiteGeometryType.Null:
					result = new ErrorTableDataset(name);
					break;
				default:
					throw new ArgumentOutOfRangeException(
						nameof(geometryType), $"Unsupported geometry type: {geometryType}");
			}

			result.SetCloneId(datasetId);

			return result;
		}

		public static void AddDetailsToDataset(ObjectDataset dataset, DatasetMsg detailedDatasetMsg)
		{
			// Enrich the original dataset with the details:
			foreach (AttributeMsg attributeMsg in detailedDatasetMsg.Attributes)
			{
				ObjectAttribute attribute = FromAttributeMsg(attributeMsg);

				dataset.AddAttribute(attribute);
			}

			var objectTypeBySubtypeCode = new Dictionary<int, ObjectType>();
			foreach (ObjectCategoryMsg objectTypeMsg in detailedDatasetMsg.ObjectCategories.Where(
				         m => m.ObjectSubtypeCriterion.Count == 0))
			{
				ObjectType objectType =
					dataset.AddObjectType(objectTypeMsg.SubtypeCode, objectTypeMsg.Name);

				objectType.SetCloneId(objectTypeMsg.ObjectCategoryId);

				objectTypeBySubtypeCode.Add(objectType.SubtypeCode, objectType);
			}

			// ObjectSubtypes:
			foreach (ObjectCategoryMsg subTypeMsg in detailedDatasetMsg.ObjectCategories.Where(
				         m => m.ObjectSubtypeCriterion.Count > 0))
			{
				ObjectType objectType = objectTypeBySubtypeCode[subTypeMsg.SubtypeCode];

				ObjectSubtype objectSubtype = objectType.AddObjectSubType(subTypeMsg.Name);

				foreach (ObjectSubtypeCriterionMsg criterionMsg in
				         subTypeMsg.ObjectSubtypeCriterion)
				{
					object attributeValue =
						FromAttributeValue(criterionMsg.AttributeValue);
					objectSubtype.AddCriterion(criterionMsg.AttributeName, attributeValue);
				}
			}
		}

		private static IEnumerable<ObjectCategoryMsg> ToObjectCategoryMsg(ObjectType objectType)
		{
			int subTypeCode = objectType.SubtypeCode;

			var objTypeMsg = new ObjectCategoryMsg
			                 {
				                 ObjectCategoryId = objectType.Id,
				                 Name = objectType.Name,
				                 SubtypeCode = subTypeCode
			                 };

			yield return objTypeMsg;

			foreach (ObjectSubtype objectSubtype in objectType.ObjectSubtypes)
			{
				var subTypeMsg = new ObjectCategoryMsg
				                 {
					                 ObjectCategoryId = objectSubtype.Id,
					                 Name = objectSubtype.Name,
					                 SubtypeCode = subTypeCode
				                 };

				subTypeMsg.ObjectSubtypeCriterion.AddRange(
					ToSubtypeCriteriaMessages(objectSubtype));

				yield return subTypeMsg;
			}
		}

		private static IEnumerable<ObjectSubtypeCriterionMsg> ToSubtypeCriteriaMessages(
			ObjectSubtype objectSubtype)
		{
			foreach (ObjectSubtypeCriterion criterion in objectSubtype.Criteria)
			{
				ObjectAttribute attribute = Assert.NotNull(criterion.Attribute);

				var criterionMsg = new ObjectSubtypeCriterionMsg()
				                   {
					                   AttributeName = attribute.Name,
					                   AttributeValue = ToAttributeValue(
						                   attribute.FieldType, criterion.AttributeValue)
				                   };

				yield return criterionMsg;
			}
		}

		public static AttributeMsg ToAttributeMsg(ObjectAttribute attribute)
		{
			FieldType fieldType = attribute.FieldType;
			var valueObject = attribute.NonApplicableValue;

			AttributeValue attributeValue = null;
			if (valueObject != null && valueObject != DBNull.Value)
			{
				try
				{
					attributeValue = ToAttributeValue(fieldType, valueObject);
				}
				catch (Exception e)
				{
					_msg.Debug($"Error encoding value {valueObject} for field type {fieldType} " +
					           $"for attribute {attribute.Name} of {attribute.Dataset?.Name}", e);
					throw;
				}
			}

			var attributeMsg = new AttributeMsg
			                   {
				                   AttributeId = attribute.Id,
				                   Name = attribute.Name ?? string.Empty,
				                   Type = (int) attribute.FieldType,
				                   IsReadonly = attribute.ReadOnly,
				                   IsObjectDefining = attribute.IsObjectDefining,
				                   AttributeRole = attribute.Role?.Id ?? -1,
				                   NonApplicableValue = attributeValue
			                   };
			return attributeMsg;
		}

		public static ObjectAttribute FromAttributeMsg([NotNull] AttributeMsg attributeMsg)
		{
			ObjectAttributeType attributeType = null;

			// TODO: Remove hard coded field names
			if (string.Equals("SHAPE", attributeMsg.Name, StringComparison.OrdinalIgnoreCase))
			{
				attributeType =
					new ObjectAttributeType(AttributeRole.Shape);
			}
			else if (string.Equals("SHAPE.LEN", attributeMsg.Name,
			                       StringComparison.OrdinalIgnoreCase))
			{
				attributeType =
					new ObjectAttributeType(AttributeRole.ShapeLength);
			}
			// TODO daro correct name?
			else if (string.Equals("SHAPE.AREA", attributeMsg.Name,
			                       StringComparison.OrdinalIgnoreCase))
			{
				attributeType =
					new ObjectAttributeType(AttributeRole.ShapeArea);
			}
			else if (attributeMsg.AttributeRole > 0)
			{
				// Probably not worth caching, treat it as value type:
				attributeType =
					new ObjectAttributeType(new AttributeRole(attributeMsg.AttributeRole));
			}

			ObjectAttribute attribute = new ObjectAttribute(attributeMsg.Name,
			                                                (FieldType) attributeMsg.Type,
			                                                attributeType)
			                            {
				                            IsObjectDefining = attributeMsg.IsObjectDefining,
				                            ReadOnly = attributeMsg.IsReadonly
			                            };

			attribute.SetCloneId(attributeMsg.AttributeId);

			object nonApplicableValue =
				FromAttributeValue(attributeMsg.NonApplicableValue);

			if (nonApplicableValue != null && nonApplicableValue != DBNull.Value)
			{
				attribute.NonApplicableValue = nonApplicableValue;
			}

			return attribute;
		}

		private static AttributeValue ToAttributeValue(FieldType fieldType,
		                                               object valueObject)
		{
			var attributeValue = new AttributeValue();

			if (valueObject == DBNull.Value || valueObject == null)
			{
				attributeValue.DbNull = true;
			}
			else
			{
				switch (fieldType)
				{
					case FieldType.ShortInteger:
						attributeValue.ShortIntValue = (int) valueObject;
						break;
					case FieldType.LongInteger:
						attributeValue.LongIntValue = (int) valueObject;
						break;
					case FieldType.Double:
						attributeValue.DoubleValue = (double) valueObject;
						break;
					case FieldType.Text:
						attributeValue.StringValue = (string) valueObject;
						break;
					case FieldType.Date:
						attributeValue.DateTimeTicksValue = ((DateTime) valueObject).Ticks;
						break;
					case FieldType.ObjectID:
						attributeValue.ShortIntValue = (int) valueObject;
						break;
					case FieldType.Geometry:
						// Leave empty, it is sent through Shape property
						break;
					case FieldType.Blob:
						// TODO: Test and make this work
						attributeValue.BlobValue =
							ByteString.CopyFrom((byte[]) valueObject);
						break;
					case FieldType.Raster: // Not supported, ignore
						break;
					case FieldType.Guid:
						byte[] asBytes = new Guid((string) valueObject).ToByteArray();
						attributeValue.UuidValue =
							new UUID { Value = ByteString.CopyFrom(asBytes) };
						break;
					case FieldType.GlobalID:
						asBytes = new Guid((string) valueObject).ToByteArray();
						attributeValue.UuidValue =
							new UUID { Value = ByteString.CopyFrom(asBytes) };
						break;
					case FieldType.Xml:
						// Not supported, ignore
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}

			return attributeValue;
		}

		/// <summary>
		/// Converts an attribute value to an object (that can be stored in the database).
		/// </summary>
		/// <param name="attributeValue"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public static object FromAttributeValue([CanBeNull] AttributeValue attributeValue)
		{
			if (attributeValue == null)
			{
				return null;
			}

			object result;

			switch (attributeValue.ValueCase)
			{
				case AttributeValue.ValueOneofCase.None:
					result = null;
					break;
				case AttributeValue.ValueOneofCase.DbNull:
					result = DBNull.Value;
					break;
				case AttributeValue.ValueOneofCase.ShortIntValue:
					result = attributeValue.ShortIntValue;
					break;
				case AttributeValue.ValueOneofCase.LongIntValue:
					result = attributeValue.LongIntValue;
					break;
				case AttributeValue.ValueOneofCase.FloatValue:
					result = attributeValue.FloatValue;
					break;
				case AttributeValue.ValueOneofCase.DoubleValue:
					result = attributeValue.DoubleValue;
					break;
				case AttributeValue.ValueOneofCase.StringValue:
					result = attributeValue.StringValue;
					break;
				case AttributeValue.ValueOneofCase.DateTimeTicksValue:
					result = new DateTime(attributeValue.DateTimeTicksValue);
					break;
				case AttributeValue.ValueOneofCase.UuidValue:
					var guid = new Guid(attributeValue.UuidValue.Value.ToByteArray());
					result = guid;
					break;
				case AttributeValue.ValueOneofCase.BlobValue:
					result = attributeValue.BlobValue;
					break;

				default:

					throw new ArgumentOutOfRangeException();
			}

			return result;
		}

		public static ProSuiteGeometryType GetGeometryType(Dataset dataset)
		{
			ProSuiteGeometryType geometryType;

			GeometryType datasetGeometryType = dataset.GeometryType;

			if (datasetGeometryType == null)
			{
				// DPS-91: The geometry type can be null
				return ProSuiteGeometryType.Unknown;
			}

			switch (datasetGeometryType)
			{
				case GeometryTypeShape shape:
				{
					geometryType = shape.ShapeType;
					break;
				}
				case GeometryTypeNoGeometry _:
					geometryType = ProSuiteGeometryType.Null;
					break;
				case GeometryTypeTopology _:
					geometryType = ProSuiteGeometryType.Topology;
					break;
				case GeometryTypeRasterDataset _:
					geometryType = ProSuiteGeometryType.Raster;
					break;
				case GeometryTypeRasterMosaic _:
					geometryType = ProSuiteGeometryType.RasterMosaic;
					break;
				case GeometryTypeTerrain _:
					geometryType = ProSuiteGeometryType.Terrain;
					break;
				default:
					throw new ArgumentOutOfRangeException(
						$"Unsupported dataset type: {dataset.Name}");
			}

			return geometryType;
		}

		[CanBeNull]
		private static GeometryType GetGeometryType(ProSuiteGeometryType proSuiteGeometryType)
		{
			if (proSuiteGeometryType == ProSuiteGeometryType.Unknown)
			{
				return null;
			}

			if (proSuiteGeometryType == ProSuiteGeometryType.Null)
			{
				return _geometryTypes.OfType<GeometryTypeNoGeometry>()
				                     .FirstOrDefault();
			}

			if (proSuiteGeometryType == ProSuiteGeometryType.Topology)
			{
				return _geometryTypes.OfType<GeometryTypeTopology>()
				                     .FirstOrDefault();
			}

			if (proSuiteGeometryType == ProSuiteGeometryType.Raster)
			{
				return _geometryTypes.OfType<GeometryTypeRasterDataset>()
				                     .FirstOrDefault();
			}

			if (proSuiteGeometryType == ProSuiteGeometryType.RasterMosaic)
			{
				return _geometryTypes.OfType<GeometryTypeRasterMosaic>()
				                     .FirstOrDefault();
			}

			if (proSuiteGeometryType == ProSuiteGeometryType.Terrain)
			{
				return _geometryTypes.OfType<GeometryTypeTerrain>()
				                     .FirstOrDefault();
			}

			return _geometryTypes.OfType<GeometryTypeShape>()
			                     .FirstOrDefault(gt => gt.ShapeType == proSuiteGeometryType);
		}

		#endregion
	}
}

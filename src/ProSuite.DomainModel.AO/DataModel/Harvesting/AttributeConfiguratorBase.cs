using System;
using System.Collections.Generic;
using System.Linq;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.DataModel.Harvesting
{
	public abstract class AttributeConfiguratorBase<T> : IAttributeConfigurator
		where T : ObjectAttributeType
	{
		private static readonly IMsg _msg = Msg.ForCurrentClass();

		[NotNull] private readonly Dictionary<AttributeRole, ObjectAttributeType> _attributeTypeMap
			= new Dictionary<AttributeRole, ObjectAttributeType>();

		[NotNull] private readonly List<ObjectAttributeType> _attributeTypes =
			new List<ObjectAttributeType>();

		/// <summary>
		/// The list of attribute roles for dataset types. It is ordered by descending
		/// specialization, i.e. the most specialized type comes first (serialized inheritance tree).
		/// </summary>
		[NotNull] private readonly List<DatasetAttributeTypes> _datasetAttributeTypesList =
			new List<DatasetAttributeTypes>();

		[NotNull] private readonly Dictionary<Type, DatasetAttributeTypes> _datasetAttributeTypesMap
			= new Dictionary<Type, DatasetAttributeTypes>();

		[NotNull] private readonly Dictionary<AttributeRole, T>
			_existingAttributeTypeMap = new Dictionary<AttributeRole, T>();

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="AttributeConfiguratorBase&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="existingAttributeTypes">The existing attribute types (optional).</param>
		protected AttributeConfiguratorBase(
			[CanBeNull] IEnumerable<AttributeType> existingAttributeTypes)
		{
			if (existingAttributeTypes == null)
			{
				return;
			}

			foreach (AttributeType attributeType in existingAttributeTypes)
			{
				var oat = attributeType as T;

				if (oat?.AttributeRole != null)
				{
					_existingAttributeTypeMap.Add(oat.AttributeRole, oat);
				}
			}
		}

		#endregion

		#region IAttributeConfigurator Members

		IList<ObjectAttributeType> IAttributeConfigurator.DefineAttributeTypes()
		{
			_attributeTypes.Clear();
			_attributeTypeMap.Clear();
			_datasetAttributeTypesMap.Clear();
			_datasetAttributeTypesList.Clear();

			ConfigureAttributeTypes();

			foreach (ObjectAttributeType attributeType in _attributeTypes)
			{
				AttributeRole role = attributeType.AttributeRole;

				if (_attributeTypeMap.ContainsKey(role))
				{
					throw new InvalidOperationException(
						$"An attribute type for role {role} has already been added");
				}

				_attributeTypeMap.Add(role, attributeType);
			}

			MapAttributeRoles();

			_datasetAttributeTypesList.AddRange(_datasetAttributeTypesMap.Values);

			// sort by type assignability (more spezialized types first)
			_datasetAttributeTypesList.Sort(CompareDatasetAttributeTypes);

			return _attributeTypes;
		}

		public void Configure(IObjectDataset dataset, IObjectClass objectClass)
		{
			Assert.ArgumentNotNull(dataset, nameof(dataset));
			Assert.ArgumentNotNull(objectClass, nameof(objectClass));

			AssignObjectIdRole(dataset, objectClass);

			// configure individual fields
			foreach (IField field in DatasetUtils.GetFields(objectClass))
			{
				ObjectAttribute attribute = dataset.GetAttribute(field.Name);
				if (attribute == null)
				{
					_msg.WarnFormat("Attribute not found for field {0} ({1})",
					                field.Name,
					                DatasetUtils.GetName(objectClass));
					continue;
				}

				ObjectAttributeType assignedAttributeType;
				Configure(attribute, field, out assignedAttributeType);

				if (attribute.Role != null && assignedAttributeType != null)
				{
					// the attribute has a role and it was assigned based on this configurator
					ClearOtherTypeAssignments(dataset, attribute, assignedAttributeType);
				}
			}
		}

		public void Configure(ObjectAttribute attribute, IField field,
		                      out ObjectAttributeType assignedAttributeType)
		{
			Assert.ArgumentNotNull(attribute, nameof(attribute));
			Assert.ArgumentNotNull(field, nameof(field));
			Assert.NotNull(attribute.Dataset, "attribute not assigned to dataset");

			string fieldName = field.Name.ToUpper();

			// keep existing attribute type assignments EXCEPT if one has 
			// the same role as a new assigment -> then delete all existing

			assignedAttributeType = null;

			foreach (DatasetAttributeTypes attributeTypes in _datasetAttributeTypesList)
			{
				if (! attributeTypes.AreApplicableFor(attribute))
				{
					continue;
				}

				if (! attributeTypes.HasSpecialRole(fieldName))
				{
					continue;
				}

				// special role of most specialized type wins
				assignedAttributeType = attributeTypes.GetAttributeType(fieldName);
				attribute.ObjectAttributeType = assignedAttributeType;

				break;
			}

			ConfigureAttributeCore(attribute, field);
		}

		#endregion

		/// <summary>
		/// Determines whether the field at a given index has coded values (coded value domain or subtype).
		/// </summary>
		/// <param name="field">The field.</param>
		/// <param name="objectClass">The object class.</param>
		/// <returns>
		/// 	<c>true</c> if the field has either a coded value domain or is the subtype; otherwise, <c>false</c>.
		/// </returns>
		/// <remarks>If the class has subtypes, the domain from the default subtype code is checked. Fields
		/// which use both coded value domains and other/no domains depending on subtype are <c>not</c> supported.</remarks>
		protected static bool HasCodedValues([NotNull] IField field,
		                                     [NotNull] IObjectClass objectClass)
		{
			Assert.ArgumentNotNull(field, nameof(field));
			Assert.ArgumentNotNull(objectClass, nameof(objectClass));

			bool hasCodedValues;
			var subtypes = objectClass as ISubtypes;
			if (subtypes != null && subtypes.HasSubtype)
			{
				int fieldIndex = objectClass.FindField(field.Name);

				if (subtypes.SubtypeFieldIndex == fieldIndex)
				{
					hasCodedValues = true;
				}
				else
				{
					IDomain domain = subtypes.get_Domain(subtypes.DefaultSubtypeCode,
					                                     field.Name);

					hasCodedValues = domain is ICodedValueDomain;
				}
			}
			else
			{
				hasCodedValues = field.Domain is ICodedValueDomain;
			}

			return hasCodedValues;
		}

		protected virtual void ConfigureAttributeCore(
			[NotNull] ObjectAttribute objectAttribute,
			[NotNull] IField field) { }

		protected abstract void ConfigureAttributeTypes();

		protected abstract void MapAttributeRoles();

		protected void Map<D>([NotNull] string fieldName, [NotNull] AttributeRole role)
			where D : IDdxDataset
		{
			Assert.ArgumentNotNullOrEmpty(fieldName, nameof(fieldName));
			Assert.ArgumentNotNull(role, nameof(role));

			ObjectAttributeType attributeType = GetAttributeType(role);

			DatasetAttributeTypes datasetAttributeTypes;
			Type datasetType = typeof(D);

			if (! _datasetAttributeTypesMap.TryGetValue(datasetType,
			                                            out datasetAttributeTypes))
			{
				datasetAttributeTypes = new DatasetAttributeTypes(datasetType);

				_datasetAttributeTypesMap.Add(datasetType, datasetAttributeTypes);
			}

			datasetAttributeTypes.Add(fieldName, attributeType);
		}

		[CanBeNull]
		protected T TryGetExistingType([NotNull] AttributeRole role)
		{
			T existingType;
			return _existingAttributeTypeMap.TryGetValue(role, out existingType)
				       ? existingType
				       : null;
		}

		protected void AddAttributeType([NotNull] T attributeType)
		{
			_attributeTypes.Add(attributeType);
		}

		/// <summary>
		/// Maps the error dataset attribute roles based on a given error dataset schema.
		/// </summary>
		/// <param name="schema">The error dataset schema.</param>
		protected void MapErrorDatasetAttributeRoles([NotNull] ErrorDatasetSchema schema)
		{
			Assert.ArgumentNotNull(schema, nameof(schema));

			Map<IErrorDataset>(schema.StatusFieldName,
			                   AttributeRole.ErrorCorrectionStatus);
			Map<IErrorDataset>(schema.QualityConditionNameFieldName,
			                   AttributeRole.ErrorConditionName);
			Map<IErrorDataset>(schema.QualityConditionParametersFieldName,
			                   AttributeRole.ErrorConditionParameters);
			Map<IErrorDataset>(schema.QualityConditionIDFieldName,
			                   AttributeRole.ErrorConditionId);
			Map<IErrorDataset>(schema.ErrorObjectsFieldName,
			                   AttributeRole.ErrorObjects);
			Map<IErrorDataset>(schema.ErrorDescriptionFieldName,
			                   AttributeRole.ErrorDescription);
			Map<IErrorDataset>(schema.ErrorTypeFieldName,
			                   AttributeRole.ErrorErrorType);
			Map<IErrorDataset>(schema.QualityConditionVersionFieldName,
			                   AttributeRole.ErrorQualityConditionVersion);
			Map<IErrorDataset>(schema.OperatorFieldName,
			                   AttributeRole.Operator);
			Map<IErrorDataset>(schema.DateOfCreationFieldName,
			                   AttributeRole.DateOfCreation);
			Map<IErrorDataset>(schema.DateOfChangeFieldName,
			                   AttributeRole.DateOfChange);

			// optional fields (may or may not be defined per solution implementation)

			if (schema.ErrorAffectedComponentFieldName != null)
			{
				Map<IErrorDataset>(schema.ErrorAffectedComponentFieldName,
				                   AttributeRole.ErrorAffectedComponent);
			}
		}

		private void AssignObjectIdRole([NotNull] IObjectDataset dataset,
		                                [NotNull] IObjectClass objectClass)
		{
			IField uniqueIntegerField = DatasetUtils.GetUniqueIntegerField(objectClass);

			if (uniqueIntegerField == null)
			{
				return;
			}

			ObjectAttribute uniqueIntegerAttribute =
				dataset.GetAttribute(uniqueIntegerField.Name);

			if (uniqueIntegerAttribute == null)
			{
				_msg.WarnFormat("Attribute not found for unique integer field {0} ({1})",
				                uniqueIntegerField.Name,
				                DatasetUtils.GetName(objectClass));
				return;
			}

			ObjectAttributeType oidAttributeType = GetAttributeType(AttributeRole.ObjectID);

			if (Equals(oidAttributeType, uniqueIntegerAttribute.ObjectAttributeType))
			{
				// attribute type is already assigned
				return;
			}

			// assign the attribute type
			uniqueIntegerAttribute.ObjectAttributeType = oidAttributeType;

			ClearOtherTypeAssignments(dataset, uniqueIntegerAttribute, oidAttributeType);
		}

		private static void ClearOtherTypeAssignments(
			[NotNull] IObjectDataset dataset,
			[NotNull] ObjectAttribute assignedAttribute,
			[NotNull] ObjectAttributeType assignedAttributeType)
		{
			foreach (ObjectAttribute otherAttribute in
			         dataset.GetAttributes(includeDeleted: true)
			                .Where(a => ! Equals(a, assignedAttribute)))
			{
				if (! Equals(otherAttribute.Role, assignedAttribute.Role))
				{
					// different role, ok
					continue;
				}

				// another attribute has the same role already --> the current assignment wins
				_msg.WarnFormat(
					"Removing attribute type from {0} due to role collision " +
					"with new assignment of attribute type {1} to {2}",
					otherAttribute.Name, assignedAttributeType.Name, assignedAttribute.Name);

				otherAttribute.ObjectAttributeType = null;
			}
		}

		[NotNull]
		private ObjectAttributeType GetAttributeType([NotNull] AttributeRole role)
		{
			ObjectAttributeType result;
			if (_attributeTypeMap.TryGetValue(role, out result))
			{
				return result;
			}

			throw new ArgumentException($@"No attribute type defined for role {role}",
			                            nameof(role));
		}

		private static int CompareDatasetAttributeTypes(
			[NotNull] DatasetAttributeTypes datasetAttributeTypes1,
			[NotNull] DatasetAttributeTypes datasetAttributeTypes2)
		{
			Assert.ArgumentNotNull(datasetAttributeTypes1, nameof(datasetAttributeTypes1));
			Assert.ArgumentNotNull(datasetAttributeTypes2, nameof(datasetAttributeTypes2));

			return datasetAttributeTypes1.CompareTo(datasetAttributeTypes2);
		}

		#region Nested types

		private class DatasetAttributeTypes : IComparable<DatasetAttributeTypes>
		{
			[NotNull] private readonly Dictionary<string, ObjectAttributeType> _attributeTypes =
				new Dictionary<string, ObjectAttributeType>(
					50, StringComparer.OrdinalIgnoreCase);

			[NotNull] private readonly Type _type;

			/// <summary>
			/// Initializes a new instance of the <see cref="DatasetAttributeTypes"/> class.
			/// </summary>
			/// <param name="type">The type.</param>
			public DatasetAttributeTypes([NotNull] Type type)
			{
				Assert.ArgumentNotNull(type, nameof(type));

				_type = type;
			}

			#region IComparable<AttributeConfiguratorBase<T>.DatasetAttributeTypes> Members

			public int CompareTo(DatasetAttributeTypes other)
			{
				Assert.ArgumentNotNull(other, nameof(other));

				if (_type == other._type)
				{
					return 0;
				}

				if (_type.IsAssignableFrom(other._type))
				{
					return 1;
				}

				return -1;
			}

			#endregion

			public bool AreApplicableFor([NotNull] ObjectAttribute attribute)
			{
				return _type.IsInstanceOfType(attribute.Dataset);
			}

			public void Add([NotNull] string fieldName, [NotNull] ObjectAttributeType role)
			{
				_attributeTypes.Add(fieldName, role);
			}

			public bool HasSpecialRole([NotNull] string fieldName)
			{
				return _attributeTypes.ContainsKey(fieldName);
			}

			[NotNull]
			public ObjectAttributeType GetAttributeType([NotNull] string fieldName)
			{
				return _attributeTypes[fieldName];
			}

			public override string ToString()
			{
				return _type.ToString();
			}
		}

		#endregion
	}
}

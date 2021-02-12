using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.DataModel
{
	public abstract class ObjectObject
	{
		private readonly IObject _object;
		private readonly ObjectDataset _dataset;
		private readonly IFieldIndexCache _fieldIndexCache;

		protected ObjectObject([NotNull] IObject obj,
		                       [NotNull] ObjectDataset dataset,
		                       [CanBeNull] IFieldIndexCache fieldIndexCache)
		{
			_object = obj;
			_dataset = dataset;
			_fieldIndexCache = fieldIndexCache;
		}

		protected object GetValue([NotNull] AttributeRole role)
		{
			const bool roleIsOptional = false;
			return GetValue(role, roleIsOptional);
		}

		[CanBeNull]
		protected object GetValue([NotNull] AttributeRole role, bool roleIsOptional)
		{
			int? index = GetFieldIndex(role, _fieldIndexCache, roleIsOptional);

			return index == null
				       ? null
				       : _object.Value[index.Value];
		}

		protected void SetValue([NotNull] AttributeRole role, [CanBeNull] object value)
		{
			int index = GetFieldIndex(role, _fieldIndexCache);
			_object.set_Value(index, value ?? DBNull.Value);
		}

		[CanBeNull]
		protected string GetDisplayValue([NotNull] AttributeRole role)
		{
			// don't call in tight loops
			int index = GetFieldIndex(role, _fieldIndexCache);
			object value = GdbObjectUtils.GetDisplayValue(_object, index);

			// NOTE: explicit cast to string results in exception if the domain code could not be resolved
			return Convert.ToString(value);
		}

		[CanBeNull]
		protected bool? GetBoolean([NotNull] AttributeRole role)
		{
			object value = GetValue(role);

			if (value == null || value is DBNull)
			{
				return null;
			}

			const int trueValue = 1;
			return Equals(value, trueValue);
		}

		[CanBeNull]
		protected string GetString([NotNull] AttributeRole role)
		{
			object value = GetValue(role);

			return value is DBNull
				       ? null
				       : (string) value;
		}

		private int GetFieldIndex([NotNull] AttributeRole role,
		                          [CanBeNull] IFieldIndexCache fieldIndexCache)
		{
			const bool roleIsOptional = false;
			int? result = GetFieldIndex(role, fieldIndexCache, roleIsOptional);

			if (result == null)
			{
				throw new InvalidOperationException("field index expected");
			}

			return result.Value;
		}

		private int? GetFieldIndex([NotNull] AttributeRole role,
		                           [CanBeNull] IFieldIndexCache fieldIndexCache,
		                           bool roleIsOptional)
		{
			ObjectAttribute attribute = _dataset.GetAttribute(role);

			if (attribute == null)
			{
				if (roleIsOptional)
				{
					return null;
				}

				throw new ArgumentException(
					string.Format("Dataset [{0}] has no attribute role [{1}]",
					              _dataset.Name, role), nameof(role));
			}

			return AttributeUtils.GetFieldIndex(_object.Class, attribute, fieldIndexCache);
		}
	}
}

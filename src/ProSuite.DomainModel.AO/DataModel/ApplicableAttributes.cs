using System;
using System.Collections.Generic;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.AO.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.AO.DataModel
{
	/// <summary>
	/// Provides fast access to the applicable attributes definition for object classes
	/// (based on ObjectCategoryAttributeConstraints defined in the data dictionary)
	/// </summary>
	public class ApplicableAttributes : IApplicableAttributes
	{
		[NotNull] private readonly Func<IObjectClass, IList<ObjectCategoryNonApplicableAttribute>>
			_getNonApplicableAttributes;

		[NotNull] private readonly Dictionary<int, DatasetMatrix> _matrixMap =
			new Dictionary<int, DatasetMatrix>();

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="ApplicableAttributes"/> class.
		/// </summary>
		/// <param name="getNonApplicableAttributes">The function to get the non applicable attributes
		/// for an object class.</param>
		public ApplicableAttributes(
			[NotNull] Func<IObjectClass, IList<ObjectCategoryNonApplicableAttribute>>
				getNonApplicableAttributes)
		{
			Assert.ArgumentNotNull(getNonApplicableAttributes,
			                       nameof(getNonApplicableAttributes));

			_getNonApplicableAttributes = getNonApplicableAttributes;
		}

		#endregion

		#region IApplicableAttributes

		bool IApplicableAttributes.HasNonApplicableAttributes(IObjectClass objectClass)
		{
			Assert.ArgumentNotNull(objectClass, nameof(objectClass));

			DatasetMatrix matrix = GetMatrix(objectClass);

			return matrix.HasNonApplicableAttributes;
		}

		bool IApplicableAttributes.IsApplicable(IObjectClass objectClass, int fieldIndex,
		                                        int? subtype)
		{
			Assert.ArgumentNotNull(objectClass, nameof(objectClass));

			DatasetMatrix matrix = GetMatrix(objectClass);

			return matrix.IsApplicable(fieldIndex, subtype);
		}

		bool IApplicableAttributes.IsNonApplicableForAnySubtype(IObjectClass objectClass,
		                                                        int fieldIndex)
		{
			Assert.ArgumentNotNull(objectClass, nameof(objectClass));

			DatasetMatrix matrix = GetMatrix(objectClass);

			return matrix.IsNonApplicableForAnySubtype(fieldIndex);
		}

		object IApplicableAttributes.GetNonApplicableValue(IObjectClass objectClass,
		                                                   int fieldIndex)
		{
			Assert.ArgumentNotNull(objectClass, nameof(objectClass));

			DatasetMatrix matrix = GetMatrix(objectClass);

			return matrix.GetNonApplicableValue(fieldIndex);
		}

		bool IApplicableAttributes.IsNonApplicableValue(IObjectClass objectClass,
		                                                int fieldIndex,
		                                                object value)
		{
			Assert.ArgumentNotNull(objectClass, nameof(objectClass));

			DatasetMatrix matrix = GetMatrix(objectClass);

			return matrix.IsNonApplicableValue(fieldIndex, value);
		}

		void IApplicableAttributes.ClearCache()
		{
			_matrixMap.Clear();
		}

		#endregion

		#region Non-public members

		private DatasetMatrix GetMatrix([NotNull] IObjectClass objectClass)
		{
			int classId = objectClass.ObjectClassID;

			DatasetMatrix matrix;
			if (! _matrixMap.TryGetValue(classId, out matrix))
			{
				matrix = CreateMatrix(objectClass);
				_matrixMap.Add(classId, matrix);
			}

			return matrix;
		}

		[NotNull]
		private DatasetMatrix CreateMatrix([NotNull] IObjectClass objectClass)
		{
			Assert.ArgumentNotNull(objectClass, nameof(objectClass));

			var result = new DatasetMatrix(objectClass);

			IList<ObjectCategoryNonApplicableAttribute> constraints =
				_getNonApplicableAttributes(objectClass);

			foreach (ObjectCategoryNonApplicableAttribute constraint in constraints)
			{
				result.Add(constraint, objectClass);
			}

			return result;
		}

		#endregion

		#region Nested types

		private class DatasetMatrix
		{
			[NotNull] private readonly Dictionary<int, List<int>> _invalidSubtypesByFieldIndex =
				new Dictionary<int, List<int>>();

			[NotNull] private readonly Dictionary<int, object> _nonApplicableValueByFieldIndex =
				new Dictionary<int, object>();

			[NotNull] private readonly Dictionary<int, bool> _subtypeCodes =
				new Dictionary<int, bool>();

			public DatasetMatrix([NotNull] IObjectClass objectClass)
			{
				Assert.ArgumentNotNull(objectClass, nameof(objectClass));

				IList<Subtype> subtypes = DatasetUtils.GetSubtypes(objectClass);
				foreach (Subtype subtype in subtypes)
				{
					_subtypeCodes.Add(subtype.Code, true);
				}
			}

			public bool HasNonApplicableAttributes => _invalidSubtypesByFieldIndex.Count > 0;

			public void Add([NotNull] ObjectCategoryNonApplicableAttribute constraint,
			                [NotNull] IObjectClass objectClass)
			{
				if (constraint.ObjectAttribute.Deleted)
				{
					return;
				}

				int fieldIndex =
					AttributeUtils.GetFieldIndex(objectClass, constraint.ObjectAttribute);

				if (fieldIndex < 0)
				{
					return;
				}

				int subtype = constraint.ObjectCategory.SubtypeCode;

				// add to list of non-applicable subtypes per field index
				List<int> sortedInvalidSubtypes;
				if (! _invalidSubtypesByFieldIndex.TryGetValue(fieldIndex,
				                                               out sortedInvalidSubtypes))
				{
					sortedInvalidSubtypes = new List<int>();
					_invalidSubtypesByFieldIndex.Add(fieldIndex, sortedInvalidSubtypes);
				}

				if (! sortedInvalidSubtypes.Contains(subtype))
				{
					sortedInvalidSubtypes.Add(subtype);
					sortedInvalidSubtypes.Sort();
				}

				if (! _nonApplicableValueByFieldIndex.ContainsKey(fieldIndex))
				{
					_nonApplicableValueByFieldIndex.Add(fieldIndex,
					                                    constraint.GetNonApplicableValue());
				}
			}

			public bool IsApplicable(int fieldIndex, int? subtype)
			{
				List<int> sortedInvalidSubtypes;
				if (! _invalidSubtypesByFieldIndex.TryGetValue(fieldIndex,
				                                               out sortedInvalidSubtypes))
				{
					// not invalid for any subtype --> applicable
					return true;
				}

				if (subtype.HasValue)
				{
					if (_subtypeCodes.ContainsKey(subtype.Value))
					{
						// valid subtype
						// invalid for some subtypes --> check if for this one also:
						// - applicable if not in list
						return sortedInvalidSubtypes.BinarySearch(subtype.Value) < 0;
					}

					// invalid subtype
					return false;
				}

				// If the field is invalid for some subtypes, and the subtype value
				// is NULL, then assume non-applicability (safe side)
				return false;
			}

			public bool IsNonApplicableForAnySubtype(int fieldIndex)
			{
				List<int> sortedInvalidSubtypes;
				if (! _invalidSubtypesByFieldIndex.TryGetValue(fieldIndex,
				                                               out sortedInvalidSubtypes))
				{
					// not invalid for any subtype --> applicable
					return false;
				}

				return sortedInvalidSubtypes.Count > 0;
			}

			public object GetNonApplicableValue(int fieldIndex)
			{
				object nonApplicableValue;
				return _nonApplicableValueByFieldIndex.TryGetValue(fieldIndex,
					       out nonApplicableValue)
					       ? nonApplicableValue
					       : null;
			}

			public bool IsNonApplicableValue(int fieldIndex, object value)
			{
				object nonApplicableValue = GetNonApplicableValue(fieldIndex);

				return Equals(value, nonApplicableValue);
			}
		}

		#endregion
	}
}

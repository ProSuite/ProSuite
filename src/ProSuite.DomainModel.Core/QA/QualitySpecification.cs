using System;
using System.Collections.Generic;
using System.Reflection;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Validation;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.Core.QA
{
	public class QualitySpecification : VersionedEntityWithMetadata, INamed, IAnnotated
	{
		[UsedImplicitly] [Obfuscation(Exclude = true)]
		private string _name;

		[UsedImplicitly] [Obfuscation(Exclude = true)]
		private string _description;

		private bool _isUnion;

		[UsedImplicitly] [Obfuscation(Exclude = true)]
		private int _listOrder;

		[UsedImplicitly] [Obfuscation(Exclude = true)]
		private readonly
			IList<QualitySpecificationElement> _elements =
				new List<QualitySpecificationElement>();

		[UsedImplicitly] [Obfuscation(Exclude = true)]
		private double? _tileSize;

		[UsedImplicitly] [Obfuscation(Exclude = true)]
		private string _url;

		[UsedImplicitly] [Obfuscation(Exclude = true)]
		private string _notes;

		[UsedImplicitly] [Obfuscation(Exclude = true)]
		private bool _hidden;

		[UsedImplicitly] [Obfuscation(Exclude = true)]
		private DataQualityCategory _category;

		[UsedImplicitly] [Obfuscation(Exclude = true)]
		private string _uuid;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="QualitySpecification"/> class.
		/// </summary>
		/// <remarks>Required for nhibernate, and public to allow adding a new unnamed
		/// quality specification in the ddx editor.</remarks>
		public QualitySpecification() : this(assignUuid: false) { }

		public QualitySpecification(bool assignUuid)
		{
			if (assignUuid)
			{
				_uuid = GenerateUuid();
			}
		}

		public QualitySpecification(string name,
		                            bool assignUuid = true) : this(assignUuid)
		{
			_name = name;
		}

		#endregion

		[Required]
		[UsedImplicitly]
		[MaximumStringLength(200)]
		public string Name
		{
			get { return _name; }
			set { _name = value; }
		}

		[Required]
		public string Uuid
		{
			get { return _uuid; }
			set
			{
				Assert.ArgumentNotNull(value, nameof(value));

				_uuid = GetUuid(value);
			}
		}

		[UsedImplicitly]
		[MaximumStringLength(2000)]
		public string Description
		{
			get { return _description; }
			set { _description = value; }
		}

		[UsedImplicitly]
		public int ListOrder
		{
			get { return _listOrder; }
			set { _listOrder = value; }
		}

		[UsedImplicitly]
		public double? TileSize
		{
			get { return _tileSize; }
			set { _tileSize = value; }
		}

		[CanBeNull]
		[UsedImplicitly]
		public string Url
		{
			get { return _url; }
			set { _url = value; }
		}

		[UsedImplicitly]
		public bool Hidden
		{
			get { return _hidden; }
			set { _hidden = value; }
		}

		[CanBeNull]
		[UsedImplicitly]
		[MaximumStringLength(2000)]
		public string Notes
		{
			get { return _notes; }
			set { _notes = value; }
		}

		[CanBeNull]
		public DataQualityCategory Category
		{
			get { return _category; }
			set { _category = value; }
		}

		public bool IsUnion => _isUnion;

		public bool IsCustom => IsCustomCore;

		protected virtual bool IsCustomCore => false;

		[NotNull]
		public virtual QualitySpecification BaseSpecification => this;

		// ReSharper disable once VirtualMemberNeverOverridden.Global
		public virtual bool CanCustomize => true;

		[NotNull]
		public IList<QualitySpecificationElement> Elements
			=> new ReadOnlyList<QualitySpecificationElement>(_elements);

		[NotNull]
		public string GetQualifiedName(string pathSeparator = "/")
		{
			return _category == null
				       ? Name
				       : string.Format("{0}{1}{2}",
				                       _category.GetQualifiedName(pathSeparator),
				                       pathSeparator,
				                       Name);
		}

		[NotNull]
		public QualitySpecificationElement AddElement(
			[NotNull] QualityCondition qualityCondition,
			bool? stopOnErrorOverride = null, bool? allowErrorsOverride = null)
		{
			// add to end of list
			return AddElement(qualityCondition, stopOnErrorOverride,
			                  allowErrorsOverride,
			                  _elements.Count);
		}

		[NotNull]
		public QualitySpecificationElement AddElement(
			[NotNull] QualityCondition qualityCondition,
			bool? stopOnErrorOverride, bool? allowErrorsOverride, bool disabled)
		{
			// add to end of list
			return AddElement(qualityCondition,
			                  stopOnErrorOverride, allowErrorsOverride, _elements.Count,
			                  disabled);
		}

		[NotNull]
		public QualitySpecificationElement AddElement(
			[NotNull] QualityCondition qualityCondition, int insertIndex)
		{
			return AddElement(qualityCondition, null, null, insertIndex);
		}

		[NotNull]
		public QualitySpecificationElement AddElement(
			[NotNull] QualityCondition qualityCondition, bool? stopOnErrorOverride,
			bool? allowErrorsOverride, int insertIndex, bool disabled = false)
		{
			Assert.ArgumentNotNull(qualityCondition, nameof(qualityCondition));

			var element = new QualitySpecificationElement(
				qualityCondition, stopOnErrorOverride, allowErrorsOverride, disabled);

			_elements.Insert(insertIndex, element);

			return element;
		}

		public bool RemoveAllElements()
		{
			if (_elements.Count == 0)
			{
				return false; // nothing to remove
			}

			_elements.Clear();
			return true;
		}

		public bool RemoveElement([NotNull] QualitySpecificationElement element)
		{
			Assert.ArgumentNotNull(element, nameof(element));

			return _elements.Remove(element);
		}

		public bool RemoveElement([NotNull] QualityCondition qualityCondition)
		{
			Assert.ArgumentNotNull(qualityCondition, nameof(qualityCondition));

			QualitySpecificationElement element = GetElement(qualityCondition);

			return element != null && _elements.Remove(element);
		}

		[CanBeNull]
		public QualitySpecificationElement GetElement(
			[NotNull] QualityCondition qualityCondition)
		{
			Assert.ArgumentNotNull(qualityCondition, nameof(qualityCondition));

			foreach (QualitySpecificationElement element in _elements)
			{
				if (qualityCondition.Equals(element.QualityCondition))
				{
					return element;
				}
			}

			return null;
		}

		public void MoveElementTo(int fromIndex, int toIndex)
		{
			QualitySpecificationElement element = _elements[fromIndex];
			MoveElementTo(element, toIndex);
		}

		private void MoveElementTo([NotNull] QualitySpecificationElement element,
		                           int toIndex)
		{
			Assert.ArgumentNotNull(element, nameof(element));

			_elements.Remove(element);
			_elements.Insert(toIndex, element);
		}

		/// <summary>
		/// Compares the quality specification to another quality specification.
		/// </summary>
		/// <param name="other">The other.</param>
		/// <returns>bitwise result: 
		/// 1 : this has specification not existing in other;
		/// 2 : other has specifications not existing in this
		/// </returns>
		public int Compare([NotNull] QualitySpecification other)
		{
			int equal;
			Union(other, out equal);

			return equal;
		}

		/// <summary>
		/// Create union of this specification and other specification
		/// </summary>
		/// <param name="other"></param>
		/// <returns>united specification</returns>
		[NotNull]
		public QualitySpecification Union([NotNull] QualitySpecification other)
		{
			return Union(other, out int _);
		}

		/// <summary>
		/// Create union of this specification and other specification
		/// </summary>
		/// <param name="other"></param>
		/// <param name="equal">bitwise result: 
		/// 1 : this has specification not existing in other;
		/// 2 : other has specifications not existing in this</param>
		/// <returns>united specification</returns>
		[NotNull]
		public QualitySpecification Union([NotNull] QualitySpecification other,
		                                  out int equal)
		{
			Assert.ArgumentNotNull(other, nameof(other));

			var result = new QualitySpecification(Name + "*" + other.Name) {_isUnion = true};

			equal = 0;
			var minOther = 0;
			// index des minimalen other elements, das noch nicht geprueft wurde, ob es in die union gehoert
			foreach (QualitySpecificationElement thisElement in _elements)
			{
				bool stopOnError = thisElement.StopOnError;
				bool allowErrors = thisElement.AllowErrors;
				bool enabled = thisElement.Enabled;

				int otherIndex = other.IndexOf(thisElement.QualityCondition);
				if (otherIndex >= 0)
				{
					QualitySpecificationElement otherElement;
					for (int idxOther = minOther; idxOther < otherIndex; idxOther++)
					{
						otherElement = other._elements[idxOther];
						QualityCondition otherCond = otherElement.QualityCondition;

						if (IndexOf(otherCond) >= 0 || result.IndexOf(otherCond) >= 0)
						{
							continue;
						}

						result.AddElement(otherElement.QualityCondition,
						                  otherElement.StopOnError,
						                  otherElement.AllowErrors,
						                  ! otherElement.Enabled);
						equal |= 2;
						// == equal = equal | 2; Beispiel equal = 1 --> nachher equal = 1 | 2 = 3
					}

					minOther = otherIndex;

					otherElement = other._elements[otherIndex];
					if (stopOnError != otherElement.StopOnError)
					{
						equal |= stopOnError
							         ? 1
							         : 2;
						stopOnError = true;
					}

					if (allowErrors != otherElement.AllowErrors)
					{
						equal |= allowErrors
							         ? 1
							         : 2;
						allowErrors = true;
					}
				}
				else
				{
					equal |= 1;
				}

				result.AddElement(thisElement.QualityCondition, stopOnError,
				                  allowErrors, ! enabled);
			}

			foreach (QualitySpecificationElement otherElement in other._elements)
			{
				int unionIndex = result.IndexOf(otherElement.QualityCondition);
				if (unionIndex >= 0)
				{
					continue;
				}

				equal |= 2;
				result.AddElement(otherElement.QualityCondition,
				                  otherElement.StopOnError,
				                  otherElement.AllowErrors,
				                  ! otherElement.Enabled);
			}

			return result;
		}

		private int IndexOf([NotNull] QualityCondition condition)
		{
			for (var i = 0; i < _elements.Count; i++)
			{
				QualitySpecificationElement element = _elements[i];
				if (element.QualityCondition.Equals(condition))
				{
					return i;
				}
			}

			return -1;
		}

		public int IndexOf(int conditionId)
		{
			for (var i = 0; i < _elements.Count; i++)
			{
				if (_elements[i].QualityCondition.Id == conditionId)
				{
					return i;
				}
			}

			return -1;
		}

		[NotNull]
		public CustomQualitySpecification GetCustomizable()
		{
			return GetCustomizableCore();
		}

		/// <summary>
		/// Creates a deep clone of the quality specification
		/// </summary>
		/// <returns></returns>
		[NotNull]
		protected virtual CustomQualitySpecification GetCustomizableCore()
		{
			CustomQualitySpecification clone =
				InitCustomSpecification($"{Name} (Clone)",
				                        copyUuid: true);

			foreach (QualitySpecificationElement element in _elements)
			{
				clone.AddElement(element.QualityCondition.Clone(),
				                 element.StopOnError,
				                 element.AllowErrors,
				                 ! element.Enabled);
			}

			return clone;
		}

		/// <summary>
		/// Creates a new quality specification with references 
		/// to the same instances of quality conditions as the calling quality specification
		/// </summary>
		/// <returns></returns>
		[NotNull]
		public QualitySpecification CreateCopy()
		{
			QualitySpecification result = CreateEmptyCopy(string.Format("Copy of {0}", Name));

			foreach (QualitySpecificationElement element in _elements)
			{
				result.AddElement(element.QualityCondition,
				                  element.StopOnErrorOverride,
				                  element.AllowErrorsOverride);
			}

			return result;
		}

		[NotNull]
		public CustomQualitySpecification Customize(
			[NotNull] IEnumerable<Dataset> verifiedDatasets)
		{
			Assert.ArgumentNotNull(verifiedDatasets, nameof(verifiedDatasets));
			Assert.True(CanCustomize, "cannot customize quality specification '{0}'", Name);
			Assert.False(IsCustom, "quality specification '{0}' is already customized", Name);

			string name = $"{Name} (Customized)";

			CustomQualitySpecification result =
				InitCustomSpecification(name, copyUuid: true);

			var datasets = new HashSet<Dataset>(verifiedDatasets);
			foreach (QualitySpecificationElement element in _elements)
			{
				if (IsQualityConditionApplicable(element.QualityCondition, datasets))
				{
					result.AddElement(element.QualityCondition.Clone(),
					                  element.StopOnError,
					                  element.AllowErrors,
					                  ! element.Enabled);
				}
			}

			return result;
		}

		/// <summary>
		/// Disables all specification elements that have no dataset in editableDatasets
		/// </summary>
		/// <param name="verifiedDatasets"></param>
		/// <returns>List of newly disabled datasets</returns>
		[NotNull]
		public IList<QualitySpecificationElement> DisableNonApplicableElements(
			[NotNull] ICollection<Dataset> verifiedDatasets)
		{
			var result = new List<QualitySpecificationElement>();

			var datasets = new HashSet<Dataset>(verifiedDatasets);

			foreach (QualitySpecificationElement element in Elements)
			{
				if (! element.Enabled)
				{
					// already disabled, ignore
					continue;
				}

				if (IsQualityConditionApplicable(element.QualityCondition, datasets))
				{
					// the condition is applicable
					continue;
				}

				// The condition is not applicable for the list of datasets; disable this element
				result.Add(element);
				element.Enabled = false;
			}

			return result;
		}

		public void DisableNoErrorElements([CanBeNull] IList<Dataset> uneditedDatasets)
		{
			if (uneditedDatasets == null || uneditedDatasets.Count == 0)
			{
				return;
			}

			foreach (QualitySpecificationElement element in Elements)
			{
				QualityCondition condition = element.QualityCondition;
				var enable = false;
				foreach (TestParameterValue testParameterValue in condition.ParameterValues)
				{
					if (! (testParameterValue is DatasetTestParameterValue))
					{
						continue;
					}

					Dataset ds = ((DatasetTestParameterValue) testParameterValue).DatasetValue;
					if (ds != null && uneditedDatasets.Contains(ds))
					{
						continue;
					}

					enable = true;
					break;
				}

				if (! enable)
				{
					element.Enabled = false;
				}
			}
		}

		public override string ToString()
		{
			return Name;
		}

		public bool Contains([NotNull] QualityCondition qualityCondition)
		{
			Assert.ArgumentNotNull(qualityCondition, nameof(qualityCondition));

			foreach (QualitySpecificationElement element in _elements)
			{
				if (element.QualityCondition.Equals(qualityCondition))
				{
					return true;
				}
			}

			return false;
		}

		public void Clear()
		{
			_elements.Clear();
		}

		[NotNull]
		private static string GetUuid([NotNull] string value)
		{
			// this fails if the string is not a valid guid:
			var guid = new Guid(value);

			return FormatUuid(guid);
		}

		[NotNull]
		private static string GenerateUuid()
		{
			return FormatUuid(Guid.NewGuid());
		}

		[NotNull]
		private static string FormatUuid(Guid guid)
		{
			// default format (no curly braces)
			return guid.ToString().ToUpper();
		}

		protected static bool IsQualityConditionApplicable(
			[NotNull] QualityCondition condition,
			[NotNull] HashSet<Dataset> verifiedDatasets)
		{
			Assert.ArgumentNotNull(condition, nameof(condition));
			Assert.ArgumentNotNull(verifiedDatasets, nameof(verifiedDatasets));

			IList<TestParameterValue> deleted = condition.GetDeletedParameterValues();
			if (deleted.Count > 0)
			{
				// there are deleted parameter values
				return false;
			}

			return condition.IsApplicableFor(verifiedDatasets,
			                                 onlyIfNotUsedAsReferenceData: true);
		}

		[NotNull]
		private CustomQualitySpecification InitCustomSpecification(
			[NotNull] string name,
			bool copyUuid = false)
		{
			var result = new CustomQualitySpecification(BaseSpecification,
			                                            name,
			                                            assignUuid: ! copyUuid);

			CopyPropertiesTo(result);

			if (copyUuid)
			{
				result.Uuid = Uuid;
			}

			return result;
		}

		[NotNull]
		private QualitySpecification CreateEmptyCopy(
			[CanBeNull] string name)
		{
			var result = new QualitySpecification(name);
			CopyPropertiesTo(result);

			return result;
		}

		protected internal void CopyPropertiesTo([NotNull] QualitySpecification target)
		{
			target._listOrder = _listOrder;
			target._description = _description;
			target._tileSize = _tileSize;
			target._url = _url;
			target._notes = _notes;
			target._hidden = _hidden;
			target._category = _category;
		}
	}
}

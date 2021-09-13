using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.Commons.Validation;
using ProSuite.DomainModel.Core.DataModel;

namespace ProSuite.DomainModel.Core.QA
{
	public class DataQualityCategory : VersionedEntityWithMetadata,
	                                   INamed, IAnnotated,
	                                   IEquatable<DataQualityCategory>
	{
		[UsedImplicitly] [Obfuscation(Exclude = true)]
		private string _uuid;

		[UsedImplicitly] [Obfuscation(Exclude = true)]
		private string _name;

		[UsedImplicitly] [Obfuscation(Exclude = true)]
		private string _description;

		[UsedImplicitly] [Obfuscation(Exclude = true)]
		private string _abbreviation;

		[UsedImplicitly] [Obfuscation(Exclude = true)]
		private int _listOrder;

		[UsedImplicitly] [Obfuscation(Exclude = true)]
		private DdxModel _defaultModel;

		[UsedImplicitly] [Obfuscation(Exclude = true)]
		private bool
			_canContainQualityConditions = true;

		[UsedImplicitly] [Obfuscation(Exclude = true)]
		private bool
			_canContainQualitySpecifications = true;

		[UsedImplicitly] [Obfuscation(Exclude = true)]
		private bool _canContainSubCategories
			= true;

		[UsedImplicitly] [Obfuscation(Exclude = true)]
		private
			IList<DataQualityCategory> _subCategories =
				new List<DataQualityCategory>();

		[UsedImplicitly] [Obfuscation(Exclude = true)]
		private DataQualityCategory
			_parentCategory;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="DataQualityCategory"/> class.
		/// </summary>
		/// <remarks>Required for NHibernate</remarks>
		[UsedImplicitly]
		protected DataQualityCategory() : this(assignUuid : false) { }

		public DataQualityCategory(bool assignUuid)
		{
			if (assignUuid)
			{
				_uuid = GenerateUuid();
			}
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="DataQualityCategory"/> class.
		/// </summary>
		/// <param name="name">The category name.</param>
		/// <param name="abbreviation">The abbreviation for the category.</param>
		/// <param name="description">The category description.</param>
		/// <param name="assignUuid">Indicates if a UUID value should be assigned</param>
		public DataQualityCategory([NotNull] string name,
		                           [CanBeNull] string abbreviation = null,
		                           [CanBeNull] string description = null,
		                           bool assignUuid = true) : this(assignUuid)
		{
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));

			_name = name;
			_abbreviation = abbreviation; // TODO unique index does not yet ignore NULL values
			_description = description;
		}

		#endregion

		[Required]
		public string Name
		{
			get { return _name; }
			set { _name = value; }
		}

		public string Description
		{
			get { return _description; }
			set { _description = value; }
		}

		[UsedImplicitly]
		public string Abbreviation
		{
			get { return _abbreviation; }
			set { _abbreviation = value; }
		}

		[NotNull]
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
		public bool CanContainQualityConditions
		{
			get { return _canContainQualityConditions; }
			set { _canContainQualityConditions = value; }
		}

		[UsedImplicitly]
		public bool CanContainQualitySpecifications
		{
			get { return _canContainQualitySpecifications; }
			set { _canContainQualitySpecifications = value; }
		}

		[UsedImplicitly]
		public bool CanContainSubCategories
		{
			get { return _canContainSubCategories; }
			set { _canContainSubCategories = value; }
		}

		public int ListOrder
		{
			get { return _listOrder; }
			set { _listOrder = value; }
		}

		[CanBeNull]
		public DdxModel GetDefaultModel()
		{
			return _defaultModel ?? _parentCategory?.GetDefaultModel();
		}

		[CanBeNull]
		public DdxModel DefaultModel
		{
			get { return _defaultModel; }
			set { _defaultModel = value; }
		}

		public bool CanContainOnlyQualityConditions => _canContainQualityConditions &&
		                                               ! _canContainQualitySpecifications &&
		                                               ! _canContainSubCategories;

		public bool CanContainOnlyQualitySpecifications => _canContainQualitySpecifications &&
		                                                   ! _canContainQualityConditions &&
		                                                   ! _canContainSubCategories;

		[ContractAnnotation("getDisplayName:notnull => canbenull")]
		public string GetQualifiedName(
			[CanBeNull] string pathSeparator = "/",
			[CanBeNull] Func<DataQualityCategory, string> getDisplayName = null)
		{
			if (_parentCategory == null)
			{
				return GetDisplayName(this, getDisplayName);
			}

			var sb = new StringBuilder();

			AppendParentCategoryName(sb, _parentCategory, pathSeparator, getDisplayName);

			sb.Append(GetDisplayName(this, getDisplayName));

			return sb.ToString();
		}

		public bool IsSubCategoryOf([NotNull] DataQualityCategory category)
		{
			Assert.ArgumentNotNull(category, nameof(category));

			if (_parentCategory == null)
			{
				return false;
			}

			if (_parentCategory.Equals(category))
			{
				return true;
			}

			// recursion
			return _parentCategory.IsSubCategoryOf(category);
		}

		public void AddSubCategory([NotNull] DataQualityCategory category)
		{
			Assert.ArgumentNotNull(category, nameof(category));
			Assert.ArgumentCondition(category.ParentCategory == null,
			                         "category is already assigned to parent");
			Assert.ArgumentCondition(! Equals(category, this),
			                         "Cannot add category as subcategory of itself");
			Assert.ArgumentCondition(! IsSubCategoryOf(category),
			                         "Category {0} is a parent of {1}, cannot assign as sub-category",
			                         category.GetQualifiedName(), GetQualifiedName());

			if (! _canContainSubCategories)
			{
				throw new InvalidOperationException(
					string.Format("Category {0} does not allow sub-categories",
					              GetQualifiedName()));
			}

			category.ParentCategory = this;
			_subCategories.Add(category);
		}

		public void RemoveSubCategory([NotNull] DataQualityCategory category)
		{
			Assert.ArgumentNotNull(category, nameof(category));
			Assert.ArgumentCondition(Equals(category.ParentCategory, this),
			                         "not a sub-category of this category");

			int index = _subCategories.IndexOf(category);
			Assert.ArgumentCondition(index >= 0, "Category not found in sub-categories");

			_subCategories.RemoveAt(index);
			category.ParentCategory = null;
		}

		[NotNull]
		public IList<DataQualityCategory> SubCategories
		{
			get { return _subCategories; }
		}

		[CanBeNull]
		public DataQualityCategory ParentCategory
		{
			get { return _parentCategory; }
			private set { _parentCategory = value; }
		}

		public override string ToString()
		{
			return string.Format("Name: {0}, Abbreviation: {1} Id: {2}",
			                     _name, _abbreviation, Id);
		}

		public bool Equals(DataQualityCategory other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			return string.Equals(_uuid, other._uuid) &&
			       string.Equals(_name, other._name) &&
			       Equals(_parentCategory, other._parentCategory);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}

			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			if (obj.GetType() != GetType())
			{
				return false;
			}

			return Equals((DataQualityCategory) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				int hashCode = _uuid != null ? _uuid.GetHashCode() : 0;
				hashCode = (hashCode * 397) ^ (_name != null ? _name.GetHashCode() : 0);
				hashCode = (hashCode * 397) ^
				           (_parentCategory != null ? _parentCategory.GetHashCode() : 0);
				return hashCode;
			}
		}

		[CanBeNull]
		private static string GetDisplayName(
			[NotNull] DataQualityCategory category,
			[CanBeNull] Func<DataQualityCategory, string> getDisplayName)
		{
			if (getDisplayName == null)
			{
				return category.Name;
			}

			string displayName = getDisplayName(category);
			return StringUtils.IsNotEmpty(displayName)
				       ? displayName
				       : null;
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

		private static void AppendParentCategoryName(
			[NotNull] StringBuilder sb,
			[CanBeNull] DataQualityCategory category, [CanBeNull] string pathSeparator = "/",
			[CanBeNull] Func<DataQualityCategory, string> getDisplayName = null)
		{
			if (category == null)
			{
				return;
			}

			// stack overflow in case of cycles!!
			// TODO detect cycles

			AppendParentCategoryName(sb, category.ParentCategory);

			sb.AppendFormat("{0}{1}",
			                GetDisplayName(category, getDisplayName),
			                pathSeparator);
		}
	}
}

using System;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Validation;

namespace ProSuite.DomainModel.Core.DataModel
{
	/// <summary>
	/// Abstract object category.
	/// </summary>
	public abstract class ObjectCategory : VersionedEntityWithMetadata,
	                                       INamed, IAnnotated,
	                                       IRegisteredGdbObject,
	                                       IEquatable<ObjectCategory>
	{
		private int _cloneId = -1;

		[UsedImplicitly] private string _name;
		[UsedImplicitly] private string _description;
		[UsedImplicitly] private readonly ObjectDataset _objectDataset;
		[UsedImplicitly] private bool _allowOrphanDeletion;
		[UsedImplicitly] private double? _minimumSegmentLength;
		[UsedImplicitly] private int _sortOrder;
		[UsedImplicitly] private bool _deleted;
		[UsedImplicitly] private DateTime? _deletionRegisteredDate;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="ObjectCategory"/> class.
		/// </summary>
		/// <remarks>Required for NHibernate</remarks>
		protected ObjectCategory() { }

		/// <summary>
		/// Initializes a new instance of the <see cref="ObjectCategory"/> class.
		/// </summary>
		/// <param name="objectDataset">The object dataset.</param>
		/// <param name="name">The name.</param>
		protected ObjectCategory([NotNull] ObjectDataset objectDataset, string name)
		{
			Assert.ArgumentNotNull(objectDataset, nameof(objectDataset));

			_name = name;
			_objectDataset = objectDataset;
		}

		#endregion

		/// <summary>
		/// The clone Id can be set if this instance is a (remote) clone of a persistent DdxModel.
		/// </summary>
		/// <param name="id"></param>
		public void SetCloneId(int id)
		{
			Assert.True(base.Id < 0, "Persistent entity or already initialized clone.");
			_cloneId = id;
		}

		public new int Id
		{
			get
			{
				if (base.Id < 0 && _cloneId != -1)
				{
					return _cloneId;
				}

				return base.Id;
			}
		}

		/// <summary>
		/// Gets the name.
		/// </summary>
		/// <value>The name.</value>
		[Required]
		[UsedImplicitly]
		public string Name
		{
			get { return _name; }
			set
			{
				if (Equals(_name, value))
				{
					return;
				}

				Assert.True(CanChangeName, "Cannot change name");
				_name = value;
			}
		}

		protected void SetName(string name)
		{
			_name = name;
		}

		public abstract bool CanChangeName { get; }

		/// <summary>
		/// Gets or sets the description of the object category.
		/// </summary>
		/// <value>The description.</value>
		[UsedImplicitly]
		public string Description
		{
			get { return _description; }
			set { _description = value; }
		}

		/// <summary>
		/// Gets the object dataset that this category belongs to.
		/// </summary>
		/// <value>The object dataset.</value>
		public ObjectDataset ObjectDataset => _objectDataset;

		/// <summary>
		/// Gets a value indicating whether the objects of this category have a geometry 
		/// (i.e. are features).
		/// </summary>
		/// <value>
		/// 	<c>true</c> if objects of this category have geometry; otherwise, <c>false</c>.
		/// </value>
		public bool HasGeometry => _objectDataset.HasGeometry;

		/// <summary>
		/// Gets a value indicating whether the automatic deletion of unreferenced 
		/// objects (orphans) is allowed for objects of this category.
		/// </summary>
		/// <value><c>true</c> if orphan deletion is allowed; otherwise, <c>false</c>.</value>
		public bool AllowOrphanDeletion
		{
			get { return _allowOrphanDeletion; }
			set { _allowOrphanDeletion = value; }
		}

		public double MinimumSegmentLength
		{
			get
			{
				if (_minimumSegmentLength.HasValue)
				{
					return _minimumSegmentLength.Value;
				}

				var dataset = ObjectDataset as IVectorDataset;

				return dataset?.MinimumSegmentLength ??
				       ObjectDataset.Model.DefaultMinimumSegmentLength;
			}
			set { _minimumSegmentLength = value; }
		}

		[UsedImplicitly]
		public double? MinimumSegmentLengthOverride
		{
			get { return _minimumSegmentLength; }
			set { _minimumSegmentLength = value; }
		}

		public abstract int SubtypeCode { get; }

		/// <summary>
		/// Gets or sets the sort order (the subtype position index when it was last refreshed
		/// from the geodatabase).
		/// </summary>
		/// <value>The sort order.</value>
		/// <remarks>Initially this corresponds to the geodatabase position index,
		/// but after the deletion of subtypes, this may no longer be the case.</remarks>
		public int SortOrder
		{
			get { return _sortOrder; }
			set { _sortOrder = value; }
		}

		#region IRegisteredGdbObject Members

		public bool Deleted => _deleted;

		public DateTime? DeletionRegisteredDate => _deletionRegisteredDate;

		public void RegisterDeleted()
		{
			_deleted = true;
			_deletionRegisteredDate = DateTime.Now;
		}

		public void RegisterExisting()
		{
			_deleted = false;
			_deletionRegisteredDate = null;
		}

		#endregion

		#region Object overrides

		public override string ToString()
		{
			return string.Format("{0} (subtype={1} id={2}{3})",
			                     _name, SubtypeCode, Id,
			                     Deleted
				                     ? " DELETED"
				                     : string.Empty);
		}

		public bool Equals(ObjectCategory objectCategory)
		{
			if (objectCategory == null)
			{
				return false;
			}

			return Equals(_name, objectCategory._name) &&
			       Equals(_objectDataset, objectCategory._objectDataset);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			return Equals(obj as ObjectCategory);
		}

		public override int GetHashCode()
		{
			return
				(_name != null
					 ? _name.GetHashCode()
					 : 0) +
				29 * _objectDataset.GetHashCode();
		}

		#endregion
	}
}

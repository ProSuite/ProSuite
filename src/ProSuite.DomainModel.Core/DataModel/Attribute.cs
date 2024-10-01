using System;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.GeoDb;

namespace ProSuite.DomainModel.Core.DataModel
{
	public abstract class Attribute : EntityWithMetadata, INamed, IAnnotated,
	                                  IRegisteredGdbObject
	{
		private int _cloneId = -1;

		[UsedImplicitly] private readonly string _name;
		[UsedImplicitly] private string _description;
		[UsedImplicitly] private int _sortOrder;
		[UsedImplicitly] private FieldType _fieldType;
		[UsedImplicitly] private int _fieldLength;

		[UsedImplicitly] private bool _deleted;
		[UsedImplicitly] private DateTime? _deletionRegisteredDate;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="Attribute"/> class.
		/// </summary>
		/// <remarks>Required for NHibernate</remarks>
		protected Attribute() { }

		protected Attribute([NotNull] string name, FieldType fieldType)
		{
			Assert.ArgumentNotNullOrEmpty(name, nameof(name));

			_name = name;
			_fieldType = fieldType;
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

		[UsedImplicitly]
		public string Name => _name;

		// Consider harvesting as well:
		[CanBeNull]
		public string AliasName { get; set; }

		[UsedImplicitly]
		public string Description
		{
			get { return _description; }
			set { _description = value; }
		}

		public bool Deleted => _deleted || IsTableDeleted;

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

		[UsedImplicitly]
		public FieldType FieldType
		{
			get { return _fieldType; }
			set { _fieldType = value; }
		}

		[UsedImplicitly]
		public int FieldLength
		{
			get { return _fieldLength; }
			set { _fieldLength = value; }
		}

		/// <summary>
		/// Gets or sets the sort order (the field index when it was last refreshed
		/// from the geodatabase).
		/// </summary>
		/// <value>The sort order.</value>
		/// <remarks>Initially this corresponds to the geodatabase field index,
		/// but after the deletion of fields, this may no longer be the case.</remarks>
		public int SortOrder
		{
			get { return _sortOrder; }
			set { _sortOrder = value; }
		}

		[CanBeNull]
		public abstract DdxModel Model { get; }

		[NotNull]
		public static string GetTypeName(FieldType fieldType)
		{
			return fieldType.ToString();
		}

		#region Non-public members

		protected abstract bool IsTableDeleted { get; }

		#endregion
	}
}

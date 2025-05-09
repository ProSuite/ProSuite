using System;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.DataModel
{
	public abstract class AssociationEnd : EntityWithMetadata, INamed
	{
		[UsedImplicitly] private readonly Association _association;
		[UsedImplicitly] private readonly Association _end1Association;
		[UsedImplicitly] private readonly Association _end2Association;

		[UsedImplicitly] private ObjectDataset _objectDataset;

		[UsedImplicitly] private bool _documentAssociationEdit;
		[UsedImplicitly] private bool _cascadeDeletion;
		[UsedImplicitly] private bool _cascadeDeleteOrphans;
		[UsedImplicitly] private string _name;
		[UsedImplicitly] private CopyPolicy _copyPolicy = CopyPolicy.DuplicateAssociation;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="AssociationEnd"/> class.
		/// </summary>
		/// <remarks>Required for NHibernate</remarks>
		protected AssociationEnd() { }

		/// <summary>
		/// Initializes a new instance of the <see cref="AssociationEnd"/> class.
		/// </summary>
		/// <param name="association">The association.</param>
		/// <param name="objectDataset">The object dataset.</param>
		/// <param name="documentAssociationEdit"></param>
		protected AssociationEnd([NotNull] Association association,
		                         [NotNull] ObjectDataset objectDataset,
		                         bool documentAssociationEdit)
		{
			Assert.ArgumentNotNull(association, nameof(association));
			Assert.ArgumentNotNull(objectDataset, nameof(objectDataset));

			_association = association;
			_end1Association = null;
			_end2Association = null;

			_objectDataset = objectDataset;
			_documentAssociationEdit = documentAssociationEdit;

			// TODO: revise
			_objectDataset.AddAssociationEnd(this);

			_name = GetEndName(association, objectDataset);
		}

		#endregion

		private int _cloneId = -1;

		/// <summary>
		/// The clone ID can be set if this instance is a (remote) clone of a persistent AssociationEnd.
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

		protected void Redirect([NotNull] ObjectDataset objectDataset)
		{
			Assert.ArgumentNotNull(objectDataset, nameof(objectDataset));

			_objectDataset.RemoveAssociationEnd(this);

			_objectDataset = objectDataset;
			_objectDataset.AddAssociationEnd(this);

			_name = GetEndName(Association, objectDataset);
		}

		[NotNull]
		public Association Association
		{
			get
			{
				if (_association != null)
				{
					return _association;
				}

				if (_end1Association != null)
				{
					return _end1Association;
				}

				Assert.NotNull(_end2Association, "end2 association is null");
				return _end2Association;
			}
		}

		/// <summary>
		/// Gets or sets a value indicating whether edits to the association are documented on this end.
		/// </summary>
		/// <value>
		/// 	<c>true</c> if edits to the association are documented on this end; otherwise, <c>false</c>.
		/// </value>
		public bool DocumentAssociationEdit
		{
			get { return _documentAssociationEdit; }
			set
			{
				if (value == _documentAssociationEdit)
				{
					return;
				}

				if (CanChangeDocumentAssociationEditCore)
				{
					_documentAssociationEdit = value;
				}
				else
				{
					throw new NotSupportedException(
						"Changing the documentation of association edits not supported");
				}
			}
		}

		public bool CanChangeDocumentAssociationEdit =>
			CanChangeDocumentAssociationEditCore;

		public bool CanChangeCascadeDeletion => CanChangeCascadeDeletionCore;

		public bool Deleted => Association.Deleted;

		public abstract bool HasForeignKey { get; }

		public abstract bool HasPrimaryKey { get; }

		public ObjectDataset ObjectDataset => _objectDataset;

		public string Name
		{
			get { return _name; }
			set { _name = value; }
		}

		public bool CascadeDeletion
		{
			get { return _cascadeDeletion; }
			set
			{
				if (_cascadeDeletion == value)
				{
					return;
				}

				if (value && ! CanChangeCascadeDeletion)
				{
					throw new NotSupportedException("Cascade Deletion not supported");
				}

				_cascadeDeletion = value;
			}
		}

		/// <summary>
		/// 
		/// </summary>
		/// <remarks>Not used currently.</remarks>
		public bool CascadeDeleteOrphans
		{
			get { return _cascadeDeleteOrphans; }
			set { _cascadeDeleteOrphans = value; }
		}

		public abstract Attribute ForeignKey { get; }

		public abstract ObjectAttribute PrimaryKey { get; }

		[NotNull]
		public AssociationEnd OppositeEnd
		{
			get
			{
				if (Association.End1.Equals(this))
				{
					return Association.End2;
				}

				if (Association.End2.Equals(this))
				{
					return Association.End1;
				}

				throw new InvalidOperationException(
					"Invalid association assignment. End does not belong to the assigned association");
			}
		}

		public AssociationEndType AssociationEndType
		{
			get
			{
				switch (Association.Cardinality)
				{
					case AssociationCardinality.Unknown:
						return AssociationEndType.Unknown;

					case AssociationCardinality.OneToOne:
						if (Association.IsAttributed)
						{
							return Association.OriginEnd.Equals(this)
								       ? AssociationEndType.OneToOnePK
								       : AssociationEndType.OneToOneFK;
						}

						// not an attributed association
						if (HasForeignKey)
						{
							return AssociationEndType.OneToOneFK;
						}

						return HasPrimaryKey
							       ? AssociationEndType.OneToOnePK
							       : AssociationEndType.Unknown;

					case AssociationCardinality.OneToMany:
						if (Association.IsAttributed)
						{
							return Association.OriginEnd.Equals(this)
								       ? AssociationEndType.OneToMany
								       : AssociationEndType.ManyToOne;
						}

						// not an attributed association
						if (HasForeignKey)
						{
							return AssociationEndType.ManyToOne;
						}

						return HasPrimaryKey
							       ? AssociationEndType.OneToMany
							       : AssociationEndType.Unknown;

					case AssociationCardinality.ManyToMany:
						return Association.End1.Equals(this)
							       ? AssociationEndType.ManyToManyEnd1
							       : AssociationEndType.ManyToManyEnd2;

					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}

		public ObjectDataset OppositeDataset => OppositeEnd.ObjectDataset;

		public CopyPolicy CopyPolicy
		{
			get { return _copyPolicy; }
			set { _copyPolicy = value; }
		}

		[UsedImplicitly]
		public Association End1Association => _end1Association;

		[UsedImplicitly]
		public Association End2Association => _end2Association;

		public void CopyPropertiesTo([NotNull] AssociationEnd other)
		{
			Assert.ArgumentNotNull(other, nameof(other));

			other.CopyPolicy = _copyPolicy;

			if (other.CanChangeDocumentAssociationEditCore)
			{
				other.DocumentAssociationEdit = _documentAssociationEdit;
			}

			other.CascadeDeleteOrphans = _cascadeDeleteOrphans;
			other.CascadeDeletion = _cascadeDeletion;
		}

		public override string ToString()
		{
			return string.Format("{0} -> {1}",
			                     _association.Name ?? "null",
			                     _objectDataset.Name ?? "null");
		}

		public override bool Equals(object obj)
		{
			if (this == obj)
			{
				return true;
			}

			var associationEnd = obj as AssociationEnd;
			if (associationEnd == null)
			{
				return false;
			}

			return
				Equals(_objectDataset, associationEnd.ObjectDataset) &&
				Equals(Association, associationEnd.Association);
		}

		public override int GetHashCode()
		{
			return _objectDataset.GetHashCode() + 29 * Association.GetHashCode();
		}

		protected abstract bool CanChangeDocumentAssociationEditCore { get; }

		protected abstract bool CanChangeCascadeDeletionCore { get; }

		private static string GetEndName(IModelElement association,
		                                 IModelElement objectDataset)
		{
			return string.Format("{0}[{1}]",
			                     association.UnqualifiedName,
			                     objectDataset.UnqualifiedName);
		}

		[NotNull]
		public string CardinalityText
		{
			get
			{
				switch (AssociationEndType)
				{
					case AssociationEndType.Unknown:
						return "Unknown";

					case AssociationEndType.OneToMany:
						return "1 : n";

					case AssociationEndType.ManyToOne:
						return "n : 1";

					case AssociationEndType.OneToOneFK:
					case AssociationEndType.OneToOnePK:
						return "1 : 1";

					case AssociationEndType.ManyToManyEnd1:
					case AssociationEndType.ManyToManyEnd2:
						return "n : m";

					default:
						throw new ArgumentOutOfRangeException();
				}
			}
		}
	}
}

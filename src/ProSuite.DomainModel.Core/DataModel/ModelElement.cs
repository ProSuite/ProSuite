using System;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.DomainModel.Core.DataModel
{
	public abstract class ModelElement : VersionedEntityWithMetadata, IModelElement,
	                                     IEquatable<ModelElement>, IAnnotated, INamed
	{
		[UsedImplicitly] private string _description;
		[UsedImplicitly] private DdxModel _model;
		[UsedImplicitly] private string _name;

		[UsedImplicitly] private bool _deleted;
		[UsedImplicitly] private DateTime? _deletionRegisteredDate;

		private string _unqualifiedName;

		private int _cloneId = -1;

		/// <summary>
		/// The clone Id can be set if this instance is a (remote) clone of a persistent ModelElement.
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

		#region IModelElement Members

		public string Description
		{
			get { return _description; }
			set { _description = value; }
		}

		public virtual string DisplayName => Name;

		public virtual DdxModel Model
		{
			get { return _model; }
			set { _model = value; }
		}

		public string Name
		{
			get { return _name; }
			set
			{
				Assert.ArgumentNotNullOrEmpty(value, nameof(value));

				_name = value;
				_unqualifiedName = ModelElementNameUtils.GetUnqualifiedName(_name);
			}
		}

		public string UnqualifiedName => _unqualifiedName ??
		                                 (_unqualifiedName =
			                                  ModelElementNameUtils.GetUnqualifiedName(_name));

		public string GetNameWithoutCatalog()
		{
			return ModelElementNameUtils.GetNameWithoutCatalogPart(_name);
		}

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

		public bool Equals(ModelElement modelElement)
		{
			if (modelElement == null)
			{
				return false;
			}

			return Equals(_name, modelElement._name) &&
			       Equals(_model, modelElement._model);
		}

		public override string ToString()
		{
			return string.Format("{0} [{1}]", Name, _model == null
				                                        ? "<model not assigned>"
				                                        : _model.Name);
		}

		public override bool Equals(object obj)
		{
			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			return Equals(obj as ModelElement);
		}

		public override int GetHashCode()
		{
			return (_name != null
				        ? _name.GetHashCode()
				        : 0) +
			       29 * (_model != null
				             ? _model.GetHashCode()
				             : 0);
		}

		#endregion
	}
}

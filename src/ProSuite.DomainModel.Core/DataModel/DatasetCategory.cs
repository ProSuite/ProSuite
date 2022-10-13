using System;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Validation;

namespace ProSuite.DomainModel.Core.DataModel
{
	public class DatasetCategory : EntityWithMetadata, INamed, IAnnotated,
	                               IEquatable<DatasetCategory>
	{
		[UsedImplicitly] private string _name;
		[UsedImplicitly] private string _description;
		[UsedImplicitly] private string _abbreviation;

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="DatasetCategory"/> class.
		/// </summary>
		/// <remarks>Required for NHibernate</remarks>
		public DatasetCategory() { }

		/// <summary>
		/// Initializes a new instance of the <see cref="DatasetCategory"/> class.
		/// </summary>
		/// <param name="name">The name.</param>
		/// <param name="abbreviation">The abbreviation.</param>
		public DatasetCategory([NotNull] string name,
		                       [NotNull] string abbreviation)
		{
			Name = name;
			Abbreviation = abbreviation;
		}

		#endregion

		[Required]
		public string Name
		{
			get { return _name; }
			set { _name = value; }
		}

		[Required]
		[UsedImplicitly]
		public string Abbreviation
		{
			get { return _abbreviation; }
			set { _abbreviation = value; }
		}

		[UsedImplicitly]
		public string Description
		{
			get { return _description; }
			set { _description = value; }
		}

		public override string ToString()
		{
			return Name;
		}

		public void Add([NotNull] IDdxDataset dataset)
		{
			dataset.DatasetCategory = this;
		}

		[NotNull]
		public DatasetCategory Clone()
		{
			return new DatasetCategory(_name, _abbreviation) {Description = _description};
		}

		public bool Equals(DatasetCategory other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			return Equals(other._name, _name) && Equals(other._abbreviation, _abbreviation);
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

			if (obj.GetType() != typeof(DatasetCategory))
			{
				return false;
			}

			return Equals((DatasetCategory) obj);
		}

		public override int GetHashCode()
		{
			unchecked
			{
				return ((_name != null
					         ? _name.GetHashCode()
					         : 0) * 397) ^ (_abbreviation != null
						                        ? _abbreviation.GetHashCode()
						                        : 0);
			}
		}
	}
}

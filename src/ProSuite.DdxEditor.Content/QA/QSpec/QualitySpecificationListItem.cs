using System;
using System.Drawing;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.DomainModel.Core.QA;
using ProSuite.UI.Core.QA.ResourceLookup;

namespace ProSuite.DdxEditor.Content.QA.QSpec
{
	public class QualitySpecificationListItem : IComparable<QualitySpecificationListItem>,
	                                            IEquatable<QualitySpecificationListItem>
	{
		[NotNull] private readonly QualitySpecification _qualitySpecification;

		/// <summary>
		/// Initializes a new instance of the <see cref="QualitySpecificationListItem"/> class.
		/// </summary>
		/// <param name="qualitySpecification">The quality specification.</param>
		public QualitySpecificationListItem(
			[NotNull] QualitySpecification qualitySpecification)
		{
			Assert.ArgumentNotNull(qualitySpecification, nameof(qualitySpecification));

			_qualitySpecification = qualitySpecification;

			Name = qualitySpecification.Name;
			Description = qualitySpecification.Description;

			Image = QualitySpecificationImageLookup.GetImage(qualitySpecification);

			if (qualitySpecification.Category != null)
			{
				Category = qualitySpecification.Category.GetQualifiedName();
			}
		}

		[UsedImplicitly]
		public bool Selected { get; set; }

		[UsedImplicitly]
		public Image Image { get; private set; }

		[UsedImplicitly]
		public string Name { get; private set; }

		[UsedImplicitly]
		public string Category { get; private set; }

		[UsedImplicitly]
		public string Description { get; private set; }

		[UsedImplicitly]
		public DateTime? CreatedDate => _qualitySpecification.CreatedDate;

		[UsedImplicitly]
		public string CreatedByUser => _qualitySpecification.CreatedByUser;

		[UsedImplicitly]
		public DateTime? LastChangedDate => _qualitySpecification.LastChangedDate;

		[UsedImplicitly]
		public string LastChangedByUser => _qualitySpecification.LastChangedByUser;

		public int CompareTo(QualitySpecificationListItem other)
		{
			return string.Compare(Name, other.Name, StringComparison.CurrentCulture);
		}

		public override string ToString()
		{
			return _qualitySpecification.Name;
		}

		[NotNull]
		public QualitySpecification QualitySpecification => _qualitySpecification;

		public bool Equals(QualitySpecificationListItem other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			return Equals(other._qualitySpecification, _qualitySpecification);
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

			if (obj.GetType() != typeof(QualitySpecificationListItem))
			{
				return false;
			}

			return Equals((QualitySpecificationListItem) obj);
		}

		public override int GetHashCode()
		{
			return _qualitySpecification.GetHashCode();
		}
	}
}

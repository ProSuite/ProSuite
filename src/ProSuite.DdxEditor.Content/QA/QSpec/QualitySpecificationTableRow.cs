using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.DdxEditor.Framework.TableRows;
using ProSuite.DomainModel.Core.QA;
using ProSuite.UI.Core.QA.ResourceLookup;

namespace ProSuite.DdxEditor.Content.QA.QSpec
{
	public class QualitySpecificationTableRow : SelectableTableRow,
	                                            IEntityRow,
	                                            IEntityRow<QualitySpecification>,
	                                            IEquatable<QualitySpecificationTableRow>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="QualitySpecificationTableRow"/> class.
		/// </summary>
		/// <param name="entity">The quality specification.</param>
		public QualitySpecificationTableRow([NotNull] QualitySpecification entity)
		{
			Assert.ArgumentNotNull(entity, nameof(entity));

			QualitySpecification = entity;
			Hidden = entity.Hidden
				         ? "Yes"
				         : "No";
			Image = QualitySpecificationImageLookup.GetImage(entity);
			Category = entity.Category?.GetQualifiedName();
		}

		[DisplayName("")]
		[NotNull]
		[UsedImplicitly]
		[ColumnConfiguration(AutoSizeColumnMode = DataGridViewAutoSizeColumnMode.AllCells)]
		public Image Image { get; }

		[UsedImplicitly]
		[ColumnConfiguration(AutoSizeColumnMode = DataGridViewAutoSizeColumnMode.AllCells)]
		public string Name => QualitySpecification.Name;

		[CanBeNull]
		[ColumnConfiguration(Width = 200)]
		[UsedImplicitly]
		public string Category { get; }

		[CanBeNull]
		[ColumnConfiguration(MinimumWidth = 100,
		                     AutoSizeColumnMode = DataGridViewAutoSizeColumnMode.Fill)]
		[UsedImplicitly]
		public string Description => QualitySpecification.Description;

		[DisplayName("Tile Size")]
		[UsedImplicitly]
		public double? TileSize => QualitySpecification.TileSize;

		[DisplayName("Display List Order")]
		[UsedImplicitly]
		public int ListOrder => QualitySpecification.ListOrder;

		[UsedImplicitly]
		[NotNull]
		public string Hidden { get; }

		[DisplayName("Created")]
		[ColumnConfiguration(Width = 100)]
		[UsedImplicitly]
		public DateTime? CreatedDate => QualitySpecification.CreatedDate;

		[DisplayName("Created By")]
		[ColumnConfiguration(Width = 80)]
		[UsedImplicitly]
		public string CreatedByUser => QualitySpecification.CreatedByUser;

		[DisplayName("Last Changed")]
		[ColumnConfiguration(Width = 100)]
		[UsedImplicitly]
		public DateTime? LastChangedDate => QualitySpecification.LastChangedDate;

		[DisplayName("Last Changed By")]
		[ColumnConfiguration(MinimumWidth = 90)]
		[UsedImplicitly]
		public string LastChangedByUser => QualitySpecification.LastChangedByUser;

		[Browsable(false)]
		[NotNull]
		public QualitySpecification QualitySpecification { get; }

		Entity IEntityRow.Entity => QualitySpecification;

		QualitySpecification IEntityRow<QualitySpecification>.Entity => QualitySpecification;

		public bool Equals(QualitySpecificationTableRow other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			return QualitySpecification.Equals(other.QualitySpecification);
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

			return Equals((QualitySpecificationTableRow) obj);
		}

		public override int GetHashCode()
		{
			return QualitySpecification.GetHashCode();
		}
	}
}

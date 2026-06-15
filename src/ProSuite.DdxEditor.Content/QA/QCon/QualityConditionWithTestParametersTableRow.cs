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

namespace ProSuite.DdxEditor.Content.QA.QCon
{
	public class QualityConditionWithTestParametersTableRow :
		SelectableTableRow,
		IEntityRow,
		IEquatable<QualityConditionWithTestParametersTableRow>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="QualityConditionWithTestParametersTableRow"/> class.
		/// </summary>
		/// <param name="entity">The quality condition.</param>
		/// <param name="testParameters"></param>
		/// <param name="testDescription"></param>
		/// <param name="qualitySpecificationRefCount"></param>
		public QualityConditionWithTestParametersTableRow([NotNull] QualityCondition entity,
		                                                  [NotNull] string testParameters,
		                                                  [NotNull] string testDescription,
		                                                  int qualitySpecificationRefCount)
		{
			Assert.ArgumentNotNull(entity, nameof(entity));

			QualityCondition = entity;
			TestParameters = testParameters;
			TestDescription = testDescription;
			QualitySpecificationRefCount = qualitySpecificationRefCount;

			Image = TestTypeImageLookup.GetImage(entity);
			Image.Tag = TestTypeImageLookup.GetDefaultSortIndex(entity);
			Category = entity.Category?.GetQualifiedName();
		}

		[DisplayName("")]
		[NotNull]
		[UsedImplicitly]
		public Image Image { get; }

		[ColumnConfiguration(Width = 400)]
		[UsedImplicitly]
		public string Name => QualityCondition.Name;

		[CanBeNull]
		[ColumnConfiguration(Width = 200)]
		[UsedImplicitly]
		public string Category { get; }

		[ColumnConfiguration(MinimumWidth = 100,
		                     AutoSizeColumnMode = DataGridViewAutoSizeColumnMode.Fill)]
		[UsedImplicitly]
		public string Description => QualityCondition.Description;

		[DisplayName("Usage Count")]
		[ColumnConfiguration(Width = 70)]
		[UsedImplicitly]
		public int QualitySpecificationRefCount { get; }

		[DisplayName("Test")]
		[ColumnConfiguration(Width = 200)]
		[UsedImplicitly]
		public string TestDescriptor => QualityCondition.TestDescriptor.Name;

		[DisplayName("Test Parameters")]
		[ColumnConfiguration(Width = 200)]
		[UsedImplicitly]
		[NotNull]
		public string TestParameters { get; }

		[DisplayName("Test Documentation")]
		[ColumnConfiguration(Width = 300)]
		[UsedImplicitly]
		[NotNull]
		public string TestDescription { get; }

		[DisplayName("Url")]
		[ColumnConfiguration(Width = 100)]
		[UsedImplicitly]
		public string Url => QualityCondition.Url;

		[DisplayName("Created")]
		[ColumnConfiguration(Width = 100)]
		[UsedImplicitly]
		public DateTime? CreatedDate => QualityCondition.CreatedDate;

		[DisplayName("Created By")]
		[ColumnConfiguration(Width = 80)]
		[UsedImplicitly]
		public string CreatedByUser => QualityCondition.CreatedByUser;

		[DisplayName("Last Changed")]
		[ColumnConfiguration(Width = 100)]
		[UsedImplicitly]
		public DateTime? LastChangedDate => QualityCondition.LastChangedDate;

		[DisplayName("Last Changed By")]
		[ColumnConfiguration(MinimumWidth = 90)]
		[UsedImplicitly]
		public string LastChangedByUser => QualityCondition.LastChangedByUser;

		[Browsable(false)]
		[NotNull]
		public QualityCondition QualityCondition { get; }

		#region IEntityRow Members

		Entity IEntityRow.Entity => QualityCondition;

		#endregion

		public bool Equals(QualityConditionWithTestParametersTableRow other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			return QualityCondition.Equals(other.QualityCondition);
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

			return Equals((QualityConditionWithTestParametersTableRow) obj);
		}

		public override int GetHashCode()
		{
			return QualityCondition.GetHashCode();
		}
	}
}

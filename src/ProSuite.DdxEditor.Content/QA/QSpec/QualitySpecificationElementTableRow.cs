using System.ComponentModel;
using System.Drawing;
using ProSuite.Commons.DomainModels;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.WinForms.Controls;
using ProSuite.DdxEditor.Framework.TableRows;
using ProSuite.DomainModel.Core.QA;
using ProSuite.UI.Core.QA.ResourceLookup;

namespace ProSuite.DdxEditor.Content.QA.QSpec
{
	public class QualitySpecificationElementTableRow : SelectableTableRow, IEntityRow
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="QualitySpecificationElementTableRow"/> class.
		/// </summary>
		/// <param name="element">The element.</param>
		/// <param name="involvesDeletedDatasets">Indicates if the quality condition involves deleted datasets</param>
		public QualitySpecificationElementTableRow(
			[NotNull] QualitySpecificationElement element,
			bool involvesDeletedDatasets = false)
		{
			Assert.ArgumentNotNull(element, nameof(element));

			Element = element;
			InvolvesDeletedDatasets = involvesDeletedDatasets;

			UpdateCategory();
		}

		[DisplayName("Condition Name")]
		[UsedImplicitly]
		public string QualityConditionName => Element.QualityCondition.Name;

		[UsedImplicitly]
		public string Category { get; private set; }

		public void UpdateCategory()
		{
			Category = Element.QualityCondition.Category?.GetQualifiedName();
		}

		[UsedImplicitly]
		public string Test => Element.QualityCondition.TestDescriptor.Name;

		[UsedImplicitly]
		public Image TestTypeImage => TestTypeImageLookup.GetImage(Element);

		[DisplayName("Issue Type (Condition Default)")]
		[UsedImplicitly]
		public string AllowErrors => Element.QualityCondition.AllowErrors
			                             ? "Warning"
			                             : "Error";

		[DisplayName("Issue Type (Override)")]
		[UsedImplicitly]
		public BooleanOverride AllowErrorsOverride
		{
			get
			{
				return NullableBooleanItems.GetBooleanOverride(
					Element.AllowErrorsOverride);
			}
			set
			{
				Element.AllowErrorsOverride =
					NullableBooleanItems.GetNullableBoolean(value);
			}
		}

		[DisplayName("Stop On Error (Condition Default)")]
		[UsedImplicitly]
		public bool StopOnError => Element.QualityCondition.StopOnError;

		[DisplayName("Stop On Error (Override")]
		[UsedImplicitly]
		public BooleanOverride StopOnErrorOverride
		{
			get
			{
				return NullableBooleanItems.GetBooleanOverride(
					Element.StopOnErrorOverride);
			}
			set
			{
				Element.StopOnErrorOverride =
					NullableBooleanItems.GetNullableBoolean(value);
			}
		}

		[DisplayName("Condition Description")]
		[UsedImplicitly]
		public string QualityConditionDescription => Element.QualityCondition.Description;

		[DisplayName("Url")]
		[ColumnConfiguration(Width = 100)]
		[UsedImplicitly]
		public string Url => Element.QualityCondition.Url;

		[Browsable(false)]
		[NotNull]
		public QualitySpecificationElement Element { get; }

		public bool InvolvesDeletedDatasets { get; }

		Entity IEntityRow.Entity => Element.QualityCondition;
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.Commons.UI.Dialogs;
using ProSuite.DomainModel.Core.QA;

namespace ProSuite.DdxEditor.Content.QA.QCon
{
	public partial class FindQualityConditionByNameForm : Form
	{
		[NotNull] private readonly Func<string, QualityCondition> _getQualityCondition;
		[NotNull] private readonly HashSet<string> _names;

		public FindQualityConditionByNameForm(
			[NotNull] IEnumerable<string> qualityConditionNames,
			[NotNull] Func<string, QualityCondition> getQualityCondition)
		{
			Assert.ArgumentNotNull(qualityConditionNames, nameof(qualityConditionNames));
			Assert.ArgumentNotNull(getQualityCondition, nameof(getQualityCondition));

			_names = new HashSet<string>(qualityConditionNames,
			                             StringComparer.OrdinalIgnoreCase);
			_getQualityCondition = getQualityCondition;

			InitializeComponent();

			_textBoxName.AutoCompleteCustomSource = GetAutoCompleteCustomSource(_names);
			_buttonOK.Enabled = false;
		}

		[CanBeNull]
		public QualityCondition QualityCondition { get; private set; }

		[NotNull]
		private static AutoCompleteStringCollection GetAutoCompleteCustomSource(
			[NotNull] IEnumerable<string> names)
		{
			var result = new AutoCompleteStringCollection();

			result.AddRange(names.ToArray());

			return result;
		}

		[CanBeNull]
		private string GetSearchText()
		{
			return _textBoxName.Text?.Trim();
		}

		[CanBeNull]
		private static QualityCondition FindQualityCondition(
			[NotNull] Func<string, QualityCondition> getQualityCondition,
			[CanBeNull] string name)
		{
			return StringUtils.IsNullOrEmptyOrBlank(name)
				       ? null
				       : getQualityCondition(name.Trim());
		}

		private void _buttonOK_Click(object sender, EventArgs e)
		{
			string searchText = GetSearchText();

			try
			{
				QualityCondition = FindQualityCondition(_getQualityCondition, searchText);
			}
			catch (Exception ex)
			{
				ErrorHandler.HandleError($"Error getting quality condition: {ex.Message}",
				                         ex, null, this, Text);
			}

			if (QualityCondition != null)
			{
				DialogResult = DialogResult.OK;
				Close();
			}
			else
			{
				Dialog.Warning(this, Text, $"Quality condition not found: {searchText}");
			}
		}

		private void _buttonCancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void _textBoxName_TextChanged(object sender, EventArgs e)
		{
			_buttonOK.Enabled = _names.Contains(GetSearchText());
		}
	}
}

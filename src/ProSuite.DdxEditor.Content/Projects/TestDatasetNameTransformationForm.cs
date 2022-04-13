using System;
using System.Drawing;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.UI.Persistence.WinForms;
using ProSuite.DomainModel.AO.Workflow;

namespace ProSuite.DdxEditor.Content.Projects
{
	public partial class TestDatasetNameTransformationForm : Form
	{
		private DatasetNameTransformer _datasetNameTransformer;

		private enum TransformationStatus
		{
			Undefined,
			NoChange,
			Changed,
			CaseOnlyChanged,
			InvalidPatterns
		}

		#region Constructors

		public TestDatasetNameTransformationForm([CanBeNull] string patterns)
		{
			InitializeComponent();

			var formStateManager = new BasicFormStateManager(this);
			formStateManager.RestoreState();

			_textBoxTransformationPatterns.Text = patterns;

			FormClosed += delegate { formStateManager.SaveState(); };
		}

		#endregion

		public string TransformationPatterns => _textBoxTransformationPatterns.Text;

		#region Non-public

		private void RenderTransformationStatus(TransformationStatus status)
		{
			string labelText;
			Color backColor = SystemColors.Control;

			switch (status)
			{
				case TransformationStatus.Undefined:
					labelText = null;
					break;

				case TransformationStatus.NoChange:
					labelText = string.IsNullOrEmpty(_textBoxTransformedDatasetName.Text)
						            ? null
						            : "no change";
					break;

				case TransformationStatus.Changed:
					labelText = "changed";
					backColor = Color.Yellow;
					break;

				case TransformationStatus.CaseOnlyChanged:
					labelText = "only case changed";
					break;

				case TransformationStatus.InvalidPatterns:
					labelText = "pattern error";
					break;

				default:
					throw new ArgumentOutOfRangeException(nameof(status));
			}

			_labelTransformationStatus.Text = labelText;
			_textBoxTransformedDatasetName.BackColor = backColor;
		}

		private bool ValidateTransformation()
		{
			DatasetNameTransformer transformer;
			try
			{
				transformer =
					new DatasetNameTransformer(_textBoxTransformationPatterns.Text ?? string.Empty);
			}
			catch (Exception ex)
			{
				_errorProvider.SetError(_textBoxTransformationPatterns, ex.Message);
				_textBoxTransformedDatasetName.Text = string.Empty;

				_labelTransformationStatus.Text = null;
				RenderTransformationStatus(TransformationStatus.InvalidPatterns);
				return false;
			}

			_datasetNameTransformer = transformer;

			_errorProvider.SetError(_textBoxTransformationPatterns, null);

			TransformDatasetName();

			return true;
		}

		private void TransformDatasetName()
		{
			Assert.NotNull(_datasetNameTransformer, "transformer not defined");

			TransformationStatus status;
			_textBoxTransformedDatasetName.Text = GetTransformedName(_datasetNameTransformer,
			                                                         _textBoxDatasetName.Text,
			                                                         out status);

			RenderTransformationStatus(status);
		}

		[NotNull]
		private static string GetTransformedName(
			[NotNull] IDatasetNameTransformer datasetNameTransformer,
			[NotNull] string datasetName,
			out TransformationStatus status)
		{
			string result = datasetNameTransformer.TransformName(datasetName);

			if (string.Equals(result, datasetName))
			{
				status = TransformationStatus.NoChange;
			}
			else
			{
				status = string.Equals(result, datasetName,
				                       StringComparison.OrdinalIgnoreCase)
					         ? TransformationStatus.CaseOnlyChanged
					         : TransformationStatus.Changed;
			}

			return result;
		}

		#region Event handlers

		private void TestDatasetNameTransformationForm_Load(object sender, EventArgs e)
		{
			_buttonOK.Enabled = ValidateTransformation();
		}

		private void _buttonOK_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.OK;
			Close();
		}

		private void _buttonCancel_Click(object sender, EventArgs e)
		{
			DialogResult = DialogResult.Cancel;
			Close();
		}

		private void _textBoxTransformationPatterns_TextChanged(object sender, EventArgs e)
		{
			_buttonOK.Enabled = ValidateTransformation();
		}

		private void _textBoxDatasetName_TextChanged(object sender, EventArgs e)
		{
			if (_datasetNameTransformer == null)
			{
				_textBoxTransformedDatasetName.Clear();
			}
			else
			{
				TransformDatasetName();
			}
		}

		#endregion

		#endregion
	}
}

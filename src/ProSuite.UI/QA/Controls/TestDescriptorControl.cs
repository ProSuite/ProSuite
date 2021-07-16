using System;
using System.ComponentModel;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Text;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Core;

namespace ProSuite.UI.QA.Controls
{
	public partial class TestDescriptorControl : UserControl
	{
		private TestDescriptor _testDescriptor;

		/// <summary>
		/// Initializes a new instance of the <see cref="TestDescriptorControl"/> class.
		/// </summary>
		public TestDescriptorControl()
		{
			InitializeComponent();
		}

		[CanBeNull]
		[Browsable(false)]
		public TestDescriptor TestDescriptor
		{
			set
			{
				if (value == null)
				{
					Clear();
					_testDescriptor = value;
					return;
				}

				if (_testDescriptor == value)
				{
					return;
				}

				_testDescriptor = value;

				BindTo(value);
			}
			get { return _testDescriptor; }
		}

		private void BindTo([NotNull] TestDescriptor testDescriptor)
		{
			_textBoxName.Text = testDescriptor.Name;
			_textBoxImplementation.Text = GetImplementation(testDescriptor);

			var testImplementationInfo =
				TestDescriptorUtils.GetTestImplementationInfo(testDescriptor);

			_textBoxTestDescription.Text = testImplementationInfo == null
				                               ? string.Empty
				                               : testImplementationInfo.GetTestDescription();

			_textBoxCategories.Text = testImplementationInfo == null
				                          ? string.Empty
				                          : StringUtils.ConcatenateSorted(
					                          testImplementationInfo.TestCategories,
					                          ", ");
			try
			{
				_textBoxSignature.Text = testImplementationInfo == null
					                         ? "Unable to create test signature"
					                         : InstanceUtils.GetTestSignature(
						                         testImplementationInfo);
			}
			catch (Exception e)
			{
				_textBoxSignature.Text = string.Format("Unable to get test signature ({0})",
				                                       e.Message);
			}
		}

		[NotNull]
		private static string GetImplementation([NotNull] TestDescriptor testDescriptor)
		{
			if (testDescriptor.TestClass != null)
			{
				return string.Format("{0} (constructor {1})",
				                     testDescriptor.TestClass.TypeName,
				                     testDescriptor.TestConstructorId);
			}

			return testDescriptor.TestFactoryDescriptor != null
				       ? testDescriptor.TestFactoryDescriptor.TypeName
				       : string.Empty;
		}

		public virtual void Clear()
		{
			_textBoxName.Clear();
			_textBoxImplementation.Clear();
			_textBoxTestDescription.Clear();
			_textBoxSignature.Clear();
			_textBoxCategories.Clear();
		}
	}
}

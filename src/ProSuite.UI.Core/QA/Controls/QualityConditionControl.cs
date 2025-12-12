using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Essentials.System;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI.Dialogs;
using ProSuite.DomainModel.Core.QA;
using ProSuite.QA.Core;

namespace ProSuite.UI.Core.QA.Controls
{
	public partial class QualityConditionControl : UserControl
	{
		private QualityCondition _qualityCondition;

		private readonly Color _readOnlyBackColor = SystemColors.Control;

		private readonly Color _writableBackColor = SystemColors.Window;

		private static readonly IMsg _msg = Msg.ForCurrentClass();

		public QualityConditionControl()
		{
			InitializeComponent();

			_textBoxTest.BackColor = _readOnlyBackColor;
		}

		public bool ReadOnly
		{
			get { return _textBoxName.ReadOnly; }
			set
			{
				_textBoxName.ReadOnly = value;
				_textBoxTestDescription.ReadOnly = value;

				Color backColor = value
					                  ? _readOnlyBackColor
					                  : _writableBackColor;

				_textBoxName.BackColor = backColor;
				_textBoxTestDescription.BackColor = backColor;
			}
		}

		[CanBeNull]
		[Browsable(false)]
		public QualityCondition QualityCondition
		{
			set
			{
				if (value == null)
				{
					Clear();
					_qualityCondition = value;
					return;
				}

				if (_qualityCondition == value)
				{
					return;
				}

				_qualityCondition = value;

				BindTo(value);
			}
			get { return _qualityCondition; }
		}

		public virtual void Clear()
		{
			_textBoxName.Clear();
			_textBoxTestDescription.Clear();
			_textBoxCategory.Clear();
			_textBoxTest.Clear();

			ClearUrl();
		}

		private void BindTo([NotNull] QualityCondition qualityCondition)
		{
			_textBoxName.Text = qualityCondition.Name;
			_textBoxCategory.Text = qualityCondition.Category?.GetQualifiedName();

			_textBoxTest.Text = qualityCondition.TestDescriptor == null
				                    ? "<not defined>"
				                    : qualityCondition.TestDescriptor.Name;

			_textBoxTestDescription.Text = GetDescription(qualityCondition);

			if (string.IsNullOrEmpty(qualityCondition.Url))
			{
				ClearUrl();
			}
			else
			{
				_linkLabelUrl.Text = qualityCondition.Url;
				_linkLabelUrl.LinkArea = new LinkArea(0, _linkLabelUrl.Text.Length);
			}
		}

		private void ClearUrl()
		{
			_linkLabelUrl.Text = @"<no url>";
			_linkLabelUrl.LinkArea = new LinkArea(0, 0);
		}

		[NotNull]
		private static string GetDescription([NotNull] QualityCondition condition)
		{
			string description;
			if (string.IsNullOrEmpty(condition.Description))
			{
				TestDescriptor testDescriptor = condition.TestDescriptor;

				IInstanceInfo instanceInfo =
					testDescriptor == null
						? null
						: InstanceDescriptorUtils.GetInstanceInfo(testDescriptor);

				description = instanceInfo == null
					              ? string.Empty
					              : instanceInfo.TestDescription;
			}
			else
			{
				description = condition.Description;
			}

			if (description == null)
			{
				return string.Empty;
			}

			description = description.Replace("\r", string.Empty);
			description = description.Replace("\n", Environment.NewLine);

			return description;
		}

		private void _linkLabelUrl_LinkClicked(object sender,
		                                       LinkLabelLinkClickedEventArgs e)
		{
			try
			{
				// the link should not be clickable if there's no url
				Assert.NotNullOrEmpty(_qualityCondition.Url, "url is not defined");

				ProcessUtils.StartProcess(_qualityCondition.Url);
			}
			catch (Exception ex)
			{
				ErrorHandler.HandleError(
					string.Format("Unable to open link: {0}", ex.Message),
					ex, _msg, this);
			}
		}
	}
}

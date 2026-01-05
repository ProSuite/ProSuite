using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ArcGIS.Desktop.Framework;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Exceptions;
using ProSuite.Commons.Logging;
using ProSuite.Commons.UI.Dialogs;
using ProSuite.Commons.UI.Drawing;
using Color = System.Drawing.Color;

namespace ProSuite.Commons.AGP.Framework.Controls;

/// <summary>
/// Tool strip button acting as wrapper for a given command type implementing ICommand
/// </summary>
public partial class ToolStripCommandWrapperButton : ToolStripButton, ICommandWrapper
{
	private readonly string _damlId;

	private static readonly IMsg _msg = Msg.ForCurrentClass();

	private IPlugInWrapper _pluginWrapper;

	private string _lastCommandToolTip;
	private const string _mnemonicCharacter = "&";

	#region Constructors

	/// <summary>
	/// Initializes a new instance of the <see cref="ToolStripCommandWrapperButton"/> class.
	/// </summary>
	/// <param name="damlId"></param>
	/// <param name="delayInitialization"></param>
	public ToolStripCommandWrapperButton(string damlId,
	                                     bool delayInitialization = false)
		: this(FrameworkApplication.GetPlugInWrapper(damlId, ! delayInitialization))
	{
		_damlId = damlId;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ToolStripCommandWrapperButton"/> class.
	/// </summary>
	/// <param name="plugInWrapper">The ArcGIS Pro plugin wrapper.</param>
	public ToolStripCommandWrapperButton(IPlugInWrapper plugInWrapper)
	{
		SetPluginWrapper(plugInWrapper);

		InitializeComponent();
	}

	private void SetPluginWrapper(IPlugInWrapper plugInWrapper)
	{
		_pluginWrapper = plugInWrapper;

		if (_pluginWrapper is ICommand command)
		{
			// Set the base.Command instance of the ToolStripButton to the wrapper's command instance:
			Command = command;

			// And get notified when something changes (seems to get fired also on Checked changed):
			WireEvents();
		}
	}

	private void _pluginWrapper_CanExecuteChanged(object sender, EventArgs args)
	{
		if (Checked != _pluginWrapper.Checked)
		{
			Checked = _pluginWrapper.Checked;
		}

		if (Enabled != PlugInWrapper.Enabled)
		{
			Enabled = PlugInWrapper.Enabled;
		}
	}

	#endregion

	/// <summary>
	/// Gets or sets a value indicating whether the tooltip text assigned to the control 
	/// should override the tool tip from the command.
	/// </summary>
	/// <value>
	/// 	<c>true</c> if the button tooltip should override the tooltip of the wrapped command; otherwise, <c>false</c>.
	/// </value>
	[DefaultValue(true)]
	public bool OverrideCommandToolTip { get; set; }

	[NotNull]
	protected IPlugInWrapper PlugInWrapper
	{
		get
		{
			if (_pluginWrapper == null)
			{
				SetPluginWrapper(FrameworkApplication.GetPlugInWrapper(_damlId));
			}

			return Assert.NotNull(_pluginWrapper, $"Cannot create tool {_damlId}");
		}
	}

	#region ICommandWrapper Members

	public string CommandID => _damlId;

	//public ICommand Command => PlugInWrapper as RelayCommand;

	/// <summary>
	/// Updates the appearance of the button based on the state of the
	/// wrapped command.
	/// </summary>
	public virtual void UpdateAppearance(bool force = true)
	{
		try
		{
			// PROBLEM: When the Enabled value is false, the Image is not rendered.
			// IDEA: Draw a greyed version of the Image as BackgroundImage, when Enabled is false.

			// update Enabled
			Enabled = PlugInWrapper.Enabled;

			// update Checked
			Checked = PlugInWrapper.Checked;

			// update Text
			Text = PlugInWrapper.Caption;

			// update ToolTip
			if (! OverrideCommandToolTip)
			{
				string toolTip = PlugInWrapper.Tooltip;

				if (! Equals(toolTip, _lastCommandToolTip))
				{
					_lastCommandToolTip = toolTip;
					ToolTipText =
						toolTip?.Replace(_mnemonicCharacter, string.Empty) ??
						string.Empty;
				}
			}

			if (Image == null || force)
			{
				// update Image
				Bitmap bitmap = null;
				if (PlugInWrapper.SmallImage is BitmapImage bitmapImage)
				{
					bitmap = BitmapUtils.CreateBitmap(bitmapImage);
				}

				if (PlugInWrapper.SmallImage is DrawingImage drawingImage)
				{
					bitmap = BitmapUtils.CreateBitmap(drawingImage);
				}

				if (bitmap != null)
				{
					Image = bitmap;

					Color firstPixelValue = bitmap.GetPixel(0, 0);
					if (firstPixelValue.A != 0)
					{
						ImageTransparentColor = bitmap.GetPixel(0, 0);
					}
				}
			}
		}
		catch (Exception e)
		{
			_msg.Warn(
				$"Error updating appearance of wrapper control: {ExceptionUtils.FormatMessage(e)}",
				e);
		}
	}

	public bool Initialized => _pluginWrapper != null;

	#endregion

	public event CancelEventHandler Clicking;

	/// <summary>
	/// Gets or sets a value indicating whether the button is forced to 
	/// appear as disabled, regardless of the state of the wrapped command.
	/// </summary>
	/// <value><c>true</c> if the button is to be forced to appear as disabled; otherwise, <c>false</c>.</value>
	public bool ForceDisabled { set; get; }

	public override bool Enabled
	{
		get { return ! ForceDisabled && base.Enabled; }
		set { base.Enabled = value; }
	}

	/// <summary>
	/// Clean up any resources being used.
	/// </summary>
	/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			if (components != null)
			{
				components.Dispose();
			}

			UnwireEvents();

			if (PlugInWrapper is IDisposable disposablePlugin)
			{
				disposablePlugin.Dispose();
			}
		}

		base.Dispose(disposing);
	}

	protected override void OnClick(EventArgs e)
	{
		try
		{
			var eventArgs = new CancelEventArgs();
			OnClicking(eventArgs);
			if (eventArgs.Cancel)
			{
				if (eventArgs.Cancel)
				{
					_msg.Debug("OnClick was cancelled in Clicking event.");
					return;
				}
			}

			UpdateAppearance();

			base.OnClick(e);
		}
		catch (Exception ex)
		{
			ErrorHandler.HandleError(ex, _msg);
		}
	}

	public override string ToString()
	{
		return string.Format("{0} [Command Id: {1}]",
		                     base.ToString(),
		                     _pluginWrapper != null ? PlugInWrapper.Caption : "<no adapter>");
	}

	#region Non-public members

	protected virtual void OnClicking(CancelEventArgs eventArgs)
	{
		Clicking?.Invoke(this, eventArgs);
	}

	// Still necessary if we set the command?

	protected virtual void WireEvents()
	{
		if (_msg.IsVerboseDebugEnabled)
		{
			_msg.Debug("ToolStripCommandWrapperButton.WireEvents");
		}

		Assert.NotNull(Command).CanExecuteChanged += _pluginWrapper_CanExecuteChanged;
	}

	protected virtual void UnwireEvents()
	{
		if (_msg.IsVerboseDebugEnabled)
		{
			_msg.Debug("ToolStripCommandWrapperButton.UnwireEvents");
		}

		if (Command != null)
		{
			Command.CanExecuteChanged -= _pluginWrapper_CanExecuteChanged;
		}
	}

	#endregion
}

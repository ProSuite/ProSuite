using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
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

	/// <summary>
	/// The image source the current <see cref="ToolStripItem.Image"/> was created from.
	/// Rasterizing is expensive (and, in some environments, unreliable), hence it is done
	/// only when the wrapped command actually provides a different image source.
	/// </summary>
	private ImageSource _currentImageSource;

	/// <summary>
	/// The bitmap assigned to <see cref="ToolStripItem.Image"/>. It is owned (and must be
	/// disposed) by this control: the tool strip item does not dispose its image.
	/// </summary>
	private Bitmap _currentImage;

	/// <summary>
	/// The size (in device pixels) the current <see cref="_currentImage"/> was rendered at.
	/// </summary>
	private int _currentImagePixelSize;

	private int _rasterizeFailureCount;

	/// <summary>
	/// Maximum number of attempts to create the image for a given image source. Creating
	/// the image can fail for reasons outside our control (no render target available in
	/// some remote desktop / virtualized graphics environments); retrying on every single
	/// appearance update would just burn resources and spam the log.
	/// </summary>
	private const int _maxRasterizeFailures = 3;

	/// <summary>
	/// The image size in device-independent pixels. This is the size at which the ArcGIS
	/// Pro ribbon renders the small image of a command; rendering at the same logical size
	/// makes the wrapper buttons match the ribbon buttons on any display scaling.
	/// </summary>
	private const int _logicalImageSize = 16;

	private const int _defaultDpi = 96;

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

	/// <summary>
	/// The command caption (tool name), shown as the bold header line of the tooltip.
	/// </summary>
	public string ToolTipHeader { get; private set; } = string.Empty;

	/// <summary>
	/// The command description, shown as the body of the tooltip.
	/// </summary>
	public string ToolTipBody { get; private set; } = string.Empty;

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
				string header = CleanToolTipText(PlugInWrapper.Caption);
				string body = CleanToolTipText(PlugInWrapper.Tooltip);
				string combined = JoinToolTip(header, body);

				if (! Equals(combined, _lastCommandToolTip))
				{
					_lastCommandToolTip = combined;

					ToolTipHeader = header;
					ToolTipBody = body;

					// plain multi-line fallback for consumers that show the
					// built-in tooltip (the tool palette owner-draws instead)
					ToolTipText = combined;
				}
			}

			if (Image == null || force)
			{
				// update Image
				UpdateImage(PlugInWrapper.SmallImage as ImageSource);
			}
		}
		catch (Exception e)
		{
			_msg.Warn(
				$"Error updating appearance of wrapper control: {ExceptionUtils.FormatMessage(e)}",
				e);
		}
	}

	/// <summary>
	/// Creates the button image from the given image source, unless the current image was
	/// already created from it. A failure to create the image is not propagated: the button
	/// keeps whatever image it has (possibly none) and remains fully functional.
	/// </summary>
	private void UpdateImage([CanBeNull] ImageSource imageSource)
	{
		if (imageSource == null)
		{
			// No image (yet) - keep the current one, if any
			return;
		}

		int pixelSize = GetImagePixelSize();

		if (ReferenceEquals(imageSource, _currentImageSource) &&
		    pixelSize == _currentImagePixelSize && Image != null)
		{
			// Unchanged: no need to rasterize again
			return;
		}

		if (! ReferenceEquals(imageSource, _currentImageSource))
		{
			_rasterizeFailureCount = 0;
		}

		if (_rasterizeFailureCount >= _maxRasterizeFailures)
		{
			return;
		}

		Bitmap bitmap;
		try
		{
			bitmap = BitmapUtils.CreateBitmap(imageSource, pixelSize);
		}
		catch (Exception e)
		{
			_rasterizeFailureCount++;

			string message =
				$"Error creating the image of {_damlId ?? PlugInWrapper.Caption}: " +
				$"{ExceptionUtils.FormatMessage(e)}. The button is shown without image.";

			if (_rasterizeFailureCount == 1)
			{
				_msg.Warn(message, e);
			}
			else
			{
				_msg.Debug(message, e);
			}

			return;
		}

		if (bitmap == null)
		{
			return;
		}

		Bitmap previousImage = _currentImage;

		Image = bitmap;

		_currentImage = bitmap;
		_currentImageSource = imageSource;
		_currentImagePixelSize = pixelSize;
		_rasterizeFailureCount = 0;

		// The previous bitmap is no longer referenced by this item: dispose it right away
		// instead of leaving it to the finalizer (each one holds a GDI bitmap handle).
		previousImage?.Dispose();

		Color firstPixelValue = bitmap.GetPixel(0, 0);
		if (firstPixelValue.A != 0)
		{
			ImageTransparentColor = firstPixelValue;
		}
	}

	/// <summary>
	/// The size (in device pixels) at which the image is to be rendered, i.e. the logical
	/// image size scaled by the display scaling of the tool strip this item belongs to.
	/// </summary>
	private int GetImagePixelSize()
	{
		ToolStrip owner = Owner;

		if (owner != null && ImageScaling == ToolStripItemImageScaling.SizeToFit)
		{
			// The tool strip scales the image to its ImageScalingSize anyway: rendering
			// at any other size would just add another (lower quality) scaling step.
			return Math.Max(owner.ImageScalingSize.Width, owner.ImageScalingSize.Height);
		}

		return (int) Math.Round(
			_logicalImageSize * GetDeviceDpi(owner) / (double) _defaultDpi);
	}

	private static int GetDeviceDpi([CanBeNull] Control control)
	{
		if (control != null && control.DeviceDpi > 0)
		{
			return control.DeviceDpi;
		}

		// Not (yet) added to a tool strip: fall back to the desktop dpi
		using (Graphics graphics = Graphics.FromHwnd(IntPtr.Zero))
		{
			return (int) Math.Round(graphics.DpiX);
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
	/// The tool strip enforces the (dpi-scaled) default item width, but derives the item
	/// height from the image alone. The resulting box is wider than high - increasingly so
	/// on a high-dpi display - which makes the image-only buttons look squeezed. Make them
	/// square instead, matching the buttons of the ArcGIS Pro ribbon.
	/// </summary>
	public override Size GetPreferredSize(Size constrainingSize)
	{
		Size preferredSize = base.GetPreferredSize(constrainingSize);

		if (DisplayStyle == ToolStripItemDisplayStyle.Image)
		{
			preferredSize.Height = Math.Max(preferredSize.Height, preferredSize.Width);
		}

		return preferredSize;
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

			Image = null;

			_currentImage?.Dispose();
			_currentImage = null;
			_currentImageSource = null;
			_currentImagePixelSize = 0;

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

	/// <summary>
	/// Removes the mnemonic marker and surrounding whitespace from a caption or
	/// tooltip string.
	/// </summary>
	private static string CleanToolTipText([CanBeNull] string text)
	{
		return text?.Replace(_mnemonicCharacter, string.Empty).Trim() ?? string.Empty;
	}

	/// <summary>
	/// Joins the caption (header) and description (body) into a single multi-line
	/// tooltip string, placing the header on the first line and the description on
	/// the following line(s).
	/// </summary>
	private static string JoinToolTip([NotNull] string header, [NotNull] string body)
	{
		if (string.IsNullOrEmpty(header))
		{
			return body;
		}

		if (string.IsNullOrEmpty(body))
		{
			return header;
		}

		return $"{header}{Environment.NewLine}{body}";
	}

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

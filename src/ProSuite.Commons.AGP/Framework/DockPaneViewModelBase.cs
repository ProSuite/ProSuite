using System;
using System.Threading.Tasks;
using System.Windows.Controls;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Mapping;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AGP.Framework;

public abstract class DockPaneViewModelBase : DockPane
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	private readonly Control _contentControl;
	private Exception _viewCreationException;

	protected DockPaneViewModelBase()
	{
		_contentControl = TryCreateView();
	}

	[Obsolete("Use parameterless constructor instead and override CreateView() to provide the content control.")]
	protected DockPaneViewModelBase([NotNull] Control contentControl)
	{
		_contentControl =
			contentControl ?? throw new ArgumentNullException(nameof(contentControl));
	}

	protected override Control OnCreateContent()
	{
		if (_contentControl is null)
		{
			// View creation failed (or CreateView() is not overridden). Return a placeholder
			// instead of failing: Pro creates the pane not only on user action but also when
			// restoring a project/session in which the pane was open, and an exception here
			// surfaces to the user and breaks DockPaneManager.Find() for this pane ID.
			return CreateFallbackView();
		}

		_contentControl.DataContext = this;

		return _contentControl;
	}

	private Control CreateFallbackView()
	{
		string details = _viewCreationException is null
			                 ? "No view was created."
			                 : _viewCreationException.Message;

		var textBlock = new TextBlock
		                {
			                Text = $"The '{Caption}' pane could not be initialized:" +
			                       $"{Environment.NewLine}{Environment.NewLine}{details}" +
			                       $"{Environment.NewLine}{Environment.NewLine}See the log file for details.",
			                TextWrapping = System.Windows.TextWrapping.Wrap,
			                Margin = new System.Windows.Thickness(12)
		                };

		// Follow Pro's theme (readable in dark mode); harmless no-op if the resource is unknown.
		textBlock.SetResourceReference(TextBlock.ForegroundProperty, "Esri_TextStyleDefaultBrush");

		return new ScrollViewer { Content = textBlock };
	}

	/// <summary>
	/// Gets the OperationManager associated with the current map or null
	/// </summary>
	public override OperationManager OperationManager => MapView.Active?.Map.OperationManager;

	protected override async void OnShow(bool isVisible)
	{
		try
		{
			OnShowCore(isVisible);
			await OnShowCoreAsync(isVisible);
		}
		catch (Exception ex)
		{
			_msg.Error($"Error showing dock pane {Caption}: {ex.Message}", ex);
		}
	}

	//This method will become abstract when the all to subclasses implement it
	protected virtual Control CreateView()
	{
		_msg.Warn($"No view created for dock pane {Caption} because CreateView() was not overridden.");
		return null;
	}

	protected virtual void OnShowCore(bool isVisible) { }

	protected virtual Task OnShowCoreAsync(bool isVisible)
	{
		return Task.CompletedTask;
	}

	protected override void OnHidden()
	{
		try
		{
			OnHiddenCore();
		}
		catch (Exception ex)
		{
			_msg.Error($"Error hiding dock pane {Caption}: {ex.Message}", ex);
		}
	}

	protected virtual void OnHiddenCore() { }

	/// <summary>
	/// This method can be used to get notified when the application context has been
	/// initialized. Dock panes can be shown directly when ArcGIS Pro starts before
	/// the application has had a chance to initialize. Once the application context
	/// is ready, also the dock pane can initialize its application-specific state.
	/// It is the application's responsibility to call this method of all known
	/// dock-panes when it is initialized.
	/// </summary>
	public virtual void OnContextInitialized() { }

	private Control TryCreateView()
	{
		try
		{
			return CreateView();
		}
		catch (Exception e)
		{
			_viewCreationException = e;
			_msg.Error($"Error creating view for dock pane {Caption}: {e.Message}", e);
		}

		return null;
	}
}

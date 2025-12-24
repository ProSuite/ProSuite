using System;
using System.Reflection;
using System.Windows.Media.Imaging;
using ArcGIS.Desktop.Framework.Contracts;
using ArcGIS.Desktop.Framework.Threading.Tasks;
using ArcGIS.Desktop.Mapping;
using ArcGIS.Desktop.Mapping.Events;
using ProSuite.Commons.AGP.Framework;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AGP.Carto;

// TODO derive from ButtonCommandBase? --> review LogEntry etc.
public abstract class ToggleSymbolDisplayButtonBase : Button
{
	private bool? _toggleState; // initially unknown
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	protected virtual ISymbolDisplayManager Manager => SymbolDisplayManager.Instance;

	protected ToggleSymbolDisplayButtonBase()
	{
		MapViewInitializedEvent.Subscribe(OnMapViewInitialized);
		ActiveMapViewChangedEvent.Subscribe(OnActiveMapViewChanged);

		Initialize();
	}

	private void OnMapViewInitialized(MapViewEventArgs args)
	{
		Initialize(args.MapView?.Map);
	}

	private void OnActiveMapViewChanged(ActiveMapViewChangedEventArgs args)
	{
		// empirical: may get this twice, once for the old and once for the new active map
		Initialize(args.IncomingView?.Map);
	}

	private async void Initialize(Map map = null)
	{
		try
		{
			map ??= MapView.Active?.Map;
			if (map is null) return;

			_toggleState = await QueuedTask.Run(() => GetInitialState(map));
		}
		catch (Exception ex)
		{
			Gateway.LogError(ex, _msg);
		}
	}

	protected override async void OnClick()
	{
		Gateway.LogEntry(_msg);

		try
		{
			var map = MapView.Active?.Map;
			if (map is null) return;

			_toggleState = await QueuedTask.Run(() => Toggle(map));
		}
		catch (Exception ex)
		{
			Gateway.ShowError(ex, _msg);
		}
	}

	protected override void OnUpdate()
	{
		try
		{
			UpdateCore();
		}
		catch (Exception ex)
		{
			Gateway.LogError(ex, _msg);
		}
	}

	protected abstract void UpdateCore();

	/// <returns>The current toggle state (true=on, false=off)</returns>
	/// <remarks>Will be called on MCT</remarks>
	protected abstract bool GetInitialState(Map map);

	/// <returns>The toggled state (true=on, false=off)</returns>
	/// <remarks>Will be called on MCT</remarks>
	protected abstract bool Toggle(Map map);

	private protected static BitmapImage GetImage(string fileName)
	{
		return new BitmapImage(GetPackUri(fileName));
	}

	private static Uri GetPackUri(string fileName, string assemblyName = null)
	{
		if (string.IsNullOrEmpty(fileName))
			throw new ArgumentNullException(nameof(fileName));
		const string authority = "application:,,,";
		//		const string assemblyName = "ProSuite.Commons.AGP";
		assemblyName ??= Assembly.GetExecutingAssembly().GetName().Name;
		return new Uri($"pack://{authority}/{assemblyName};component/{fileName}");
	}
}

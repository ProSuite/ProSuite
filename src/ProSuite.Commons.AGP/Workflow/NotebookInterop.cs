using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ArcGIS.Desktop.Framework;
using ArcGIS.Desktop.Framework.Contracts;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.IO;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AGP.Workflow;

/// <remarks>
/// As of Pro 3.6 there are is no functionality in the SDK
/// to open and clone Jupyter Notebooks. The helper methods
/// here use reflection to invoke internal Pro commands and
/// therefore may not work in future versions of Pro!
/// </remarks>
public static class NotebookInterop
{
	private static readonly IMsg _msg = Msg.ForCurrentClass();

	public static bool TryOpen(string notebookPath, out string error)
	{
		const string methodName = "OnOpenNotebook";
		return TryInvoke(methodName, notebookPath, out error);
		// TODO Avoid opening more than one pane per notebook?!
	}

	public static bool TryClose1(string notebookPath, out string error)
	{
		// Method must have existed once but is gone at least since Pro 3.6.2
		const string methodName = "OnCloseNotebook";
		return TryInvoke(methodName, notebookPath, out error);
	}

	private static bool TryInvoke(string methodName, string notebookPath, out string error)
	{
		const string typeName = "ArcGIS.Desktop.GeoProcessing.Commands";
		const string assemblyName = "ArcGIS.Desktop.GeoProcessing";

		try
		{
			const string qualifiedTypeName = $"{typeName}, {assemblyName}";
			var commandsType = Type.GetType(qualifiedTypeName, throwOnError: false);
			if (commandsType is null)
			{
				error = "GeoProcessing commands type not found.";
				return false;
			}

			const BindingFlags flags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
			var parameterTypes = new[] { typeof(string) };
			var method = commandsType.GetMethod(methodName, flags, parameterTypes);
			if (method is null)
			{
				error = $"{typeName}.{methodName}(string) is not available in this Pro version";
				return false;
			}

			method.Invoke(null, new object[] { notebookPath });

			error = null;
			return true;
		}
		catch (TargetInvocationException ex)
		{
			error = ex.InnerException?.Message ?? ex.Message;
			return false;
		}
		catch (Exception ex)
		{
			error = ex.Message;
			return false;
		}
	}

	/// <param name="notebookPath">The notebook pane to close;
	/// if null, close all notebook panes</param>
	/// <returns>Number of panes that were closed</returns>
	public static int TryClose2(string notebookPath)
	{
		var panes = FindNotebookPanes(notebookPath, true);

		var instanceIds = panes.Select(pane => pane.InstanceID).ToList();

		foreach (var instanceId in instanceIds)
		{
			FrameworkApplication.Panes.ClosePane(instanceId);
		}

		return instanceIds.Count; // number of panes closed
	}

	private static IEnumerable<Pane> FindNotebookPanes([CanBeNull] string notebookPath, bool matchUnknown)
	{
		const string proNotebookPaneDamlID = "esri_geoprocessing_proNotebookPane";

		var panes = FrameworkApplication.Panes.Find(proNotebookPaneDamlID);

		return panes.Where(pane => IsNotebookPane(pane, notebookPath) ?? matchUnknown);
	}

	public static bool? IsNotebookPane(Pane pane, string notebookPath = null)
	{
		// Notebook panes are of type ProNotebookPaneViewModel, which is
		// an internal class, so resort to Reflection and hope that the
		// class and its relevant property will be there in future Pro versions...

		const string typeFullName = "ArcGIS.Desktop.GeoProcessing.ProNotebookPaneViewModel";
		const string propertyName = "NotebookPath";

		if (pane is null)
		{
			return false;
		}

		var type = pane.GetType();

		if (! string.Equals(type.FullName, typeFullName, StringComparison.Ordinal))
		{
			return false;
		}

		if (! string.IsNullOrEmpty(notebookPath))
		{
			const BindingFlags flags =
				BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
			var property = type.GetProperty(propertyName, flags);
			if (property is null)
			{
				_msg.WarnFormat("Property {0} not found on type {1}",
				                propertyName, typeFullName);
				return null;
			}

			var value = property.GetValue(pane);
			if (value is not string text)
			{
				_msg.WarnFormat("Value of property {0} on type {1} is not string",
				                propertyName, typeFullName);
				return null;
			}

			if (! FileSystemUtils.ArePathsEqual(notebookPath, text))
			{
				return false;
			}
		}

		return true;
	}
}

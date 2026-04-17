using System;
using System.Reflection;

namespace ProSuite.Commons.AGP.Workflow;

/// <remarks>
/// As of Pro 3.6 there are is no functionality in the SDK
/// to open and clone Jupyter Notebooks. The helper methods
/// here use reflection to invoke internal Pro commands and
/// therefore may not work in future versions of Pro!
/// </remarks>
public static class NotebookInterop
{
	public static bool TryOpen(string notebookPath, out string error)
	{
		const string methodName = "OnOpenNotebook";
		return TryInvoke(methodName, notebookPath, out error);
	}

	public static bool TryClose(string notebookPath, out string error)
	{
		// Method existed in Pro 3.6.2 but is gone in Pro 3.6.3
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
}

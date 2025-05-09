using System;
using System.Reflection;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.Essentials.System
{
	/// <summary>
	/// Helper methods for resolving / loading assemblies
	/// </summary>
	public static class AssemblyResolveUtils
	{
		[ThreadStatic] private static bool _loadingAssembly;

		/// <summary>
		/// Tries to load an assembly given the name (as reported by a <see cref="ResolveEventArgs"></see> instance
		/// when handling an <see cref="AppDomain.AssemblyResolve"></see> event) and a codebase path.
		/// </summary>
		/// <param name="name">The assembly name.</param>
		/// <param name="codeBase">The code base path from where to load the assembly.</param>
		/// <param name="logMethod">Optional procedure for logging the load attempt.</param>
		/// <returns></returns>
		[CanBeNull]
		public static Assembly TryLoadAssembly([NotNull] string name,
		                                       [NotNull] string codeBase,
		                                       [CanBeNull] LogMethod logMethod = null)
		{
			return TryLoadAssembly(name, codeBase, null, logMethod);
		}

		/// <summary>
		/// Tries to load an assembly given the name (as reported by a <see cref="ResolveEventArgs"></see> instance
		/// when handling an <see cref="AppDomain.AssemblyResolve"></see> event) and a codebase path.
		/// </summary>
		/// <param name="name">The assembly name.</param>
		/// <param name="codeBase">The code base path from where to load the assembly.</param>
		/// <param name="desiredVersion">The desired version that shall replace the version from the name</param>
		/// <param name="logMethod">Optional procedure for logging the load attempt.</param>
		/// <returns></returns>
		[CanBeNull]
		public static Assembly TryLoadAssembly([NotNull] string name,
		                                       [NotNull] string codeBase,
		                                       [CanBeNull] Version desiredVersion,
		                                       [CanBeNull] LogMethod logMethod = null)
		{
			if (_loadingAssembly)
			{
				TryLog(logMethod, "Recursive TryLoadAssembly() call ({0})", name);
				return null;
			}

			try
			{
				_loadingAssembly = true;

				TryLog(logMethod, "Attempting to load assembly '{0}' from codebase '{1}'",
				       name, codeBase);

				var assemblyName = new AssemblyName(name) { CodeBase = codeBase };

				if (desiredVersion != null)
				{
					assemblyName.Version = desiredVersion;
				}

				return Assembly.Load(assemblyName);
			}
			catch (Exception e)
			{
				TryLog(logMethod, "Error loading assembly name '{0}': {1}", name, e.Message);
				return null;
			}
			finally
			{
				_loadingAssembly = false;
			}
		}

		[StringFormatMethod("format")]
		private static void TryLog([CanBeNull] LogMethod logMethod, [NotNull] string format,
		                           params object[] args)
		{
			logMethod?.Invoke(string.Format(format, args));
		}
	}
}

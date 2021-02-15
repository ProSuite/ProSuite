using System;
using System.Reflection;
using System.Runtime.InteropServices;
using ESRI.ArcGIS.esriSystem;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AO
{
	/// <summary>
	/// Utilities for reading from and writing to variant streams.
	/// </summary>
	public static class StreamUtils
	{
		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// Reads from the specified stream.
		/// </summary>
		/// <param name="stream">The stream to read from.</param>
		/// <param name="currentVersion">The current version of the calling component
		/// (used to validate the stream version). Must be a non-negative integer.</param>
		/// <param name="versionedRead">Delegate to do the actual reading, based on the version of the stream.</param>
		/// <param name="releaseStream">if set to <c>true</c> the stream is released using Marshal.ReleaseComObject. This 
		/// should only be done in calls from direct com interface implementations.</param>
		public static void Load([NotNull] IVariantStream stream,
		                        int currentVersion,
		                        [NotNull] Action<int> versionedRead,
		                        bool releaseStream = false)
		{
			try
			{
				// assert inside try block to make sure assertion violations are visible
				Assert.ArgumentNotNull(stream, nameof(stream));
				Assert.ArgumentNotNull(versionedRead, nameof(versionedRead));
				Assert.True(currentVersion >= 0, "Invalid current version: {0}",
				            currentVersion);

				object versionObject = stream.Read();
				Assert.NotNull(versionObject, "Unable to read version from stream");

				var version = (int) versionObject;
				Assert.True(version >= 0 && version <= currentVersion,
				            "Unexpected version: {0}", version);

				versionedRead(version);
			}
			catch (Exception e)
			{
				var comEx = e as COMException;
				string message =
					comEx == null
						? $"Error reading from stream: {e.Message}"
						: $"Error reading from stream: {comEx.Message}; ErrorCode: {comEx.ErrorCode}";
				_msg.Error(message, e);
				throw;
			}
			finally
			{
				// ReSharper disable once ConditionIsAlwaysTrueOrFalse
				if (releaseStream && stream != null)
				{
					Marshal.ReleaseComObject(stream);
				}
			}
		}

		/// <summary>
		/// Writes to the specified stream.
		/// </summary>
		/// <param name="stream">The stream to write to.</param>
		/// <param name="currentVersion">The current version of the calling component
		/// (written to the stream). Must be a non-negative integer.</param>
		/// <param name="write">Delegate to do the actual writing.</param>
		/// <param name="releaseStream">if set to <c>true</c> the stream is released using Marshal.ReleaseComObject. This 
		/// should only be done in calls from direct com interface implementations.</param>
		public static void Save([NotNull] IVariantStream stream,
		                        int currentVersion,
		                        [NotNull] Action write,
		                        bool releaseStream = false)
		{
			try
			{
				// assert inside try block to make sure assertion violations are visible
				Assert.ArgumentNotNull(stream, nameof(stream));
				Assert.ArgumentNotNull(write, nameof(write));
				Assert.True(currentVersion >= 0, "Invalid current version: {0}",
				            currentVersion);

				stream.Write(currentVersion);

				write();
			}
			catch (Exception e)
			{
				var comEx = e as COMException;
				string message =
					comEx == null
						? $"Error reading from stream: {e.Message}"
						: $"Error reading from stream: {comEx.Message}; ErrorCode: {comEx.ErrorCode}";
				_msg.Error(message, e);
				throw;
			}
			finally
			{
				// ReSharper disable once ConditionIsAlwaysTrueOrFalse
				if (releaseStream && stream != null)
				{
					Marshal.ReleaseComObject(stream);
				}
			}
		}
	}
}

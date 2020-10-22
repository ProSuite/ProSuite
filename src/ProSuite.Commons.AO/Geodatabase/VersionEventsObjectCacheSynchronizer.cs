using System;
using System.Reflection;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.AO.Geodatabase
{
	public class VersionEventsObjectCacheSynchronizer : IDisposable
	{
		private readonly IVersionEvents_Event _versionEvents;
		private readonly IFeatureWorkspace _featureWorkspace;

		private readonly IObjectCache _objectCache;

		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="VersionEventsObjectCacheSynchronizer"/> class.
		/// </summary>
		/// <param name="objectCache">The object cache.</param>
		/// <param name="featureWorkspace">The feature workspace.</param>
		[CLSCompliant(false)]
		public VersionEventsObjectCacheSynchronizer(IObjectCache objectCache,
		                                            IFeatureWorkspace featureWorkspace)
		{
			Assert.ArgumentNotNull(objectCache, nameof(objectCache));
			Assert.ArgumentNotNull(featureWorkspace, nameof(featureWorkspace));

			_objectCache = objectCache;
			_featureWorkspace = featureWorkspace;

			_versionEvents = _featureWorkspace as IVersionEvents_Event;

			WireVersionEvents();
		}

		#endregion

		#region IDisposable

		public void Dispose()
		{
			UnwireVersionEvents();
		}

		#endregion

		#region Non-public members

		private void Invalidate()
		{
			try
			{
				_objectCache.Invalidate(_featureWorkspace);
			}
			catch (Exception e)
			{
				_msg.Error("Error invalidating object cache", e);
				throw;
			}
		}

		private void WireVersionEvents()
		{
			if (_versionEvents != null)
			{
				_versionEvents.OnReconcile += _versionEvents_OnReconcile;
				_versionEvents.OnRedefineVersion += _versionEvents_OnRedefineVersion;
				_versionEvents.OnRefreshVersion += _versionEvents_OnRefreshVersion;
			}
		}

		private void UnwireVersionEvents()
		{
			if (_versionEvents != null)
			{
				_versionEvents.OnReconcile -= _versionEvents_OnReconcile;
				_versionEvents.OnRedefineVersion -= _versionEvents_OnRedefineVersion;
				_versionEvents.OnRefreshVersion -= _versionEvents_OnRefreshVersion;
			}
		}

		#region Event handlers

		private void _versionEvents_OnReconcile(string targetVersionName,
		                                        bool HasConflicts)
		{
			Invalidate();
		}

		private void _versionEvents_OnRefreshVersion()
		{
			Invalidate();
		}

		private void _versionEvents_OnRedefineVersion(string oldVersionName,
		                                              string newVersionName)
		{
			Invalidate();
		}

		#endregion

		#endregion
	}
}

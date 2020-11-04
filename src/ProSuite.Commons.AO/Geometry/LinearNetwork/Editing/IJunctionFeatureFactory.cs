using System;
using ESRI.ArcGIS.Geodatabase;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO.Geometry.LinearNetwork.Editing
{
	/// <summary>
	/// Interface that allows implementors to determine the type and attributes of
	/// a junction to be created in the specified network if any but the default junction
	/// type defined in the linear network shall be created.
	/// </summary>
	[CLSCompliant(false)]
	public interface IJunctionFeatureFactory
	{
		/// <summary>
		/// A new feature of the type that is currently suitable. The feature can be pre-initialized
		/// with attribute values, e.g. according to a template specification.
		/// As a fallback, implementors can return null to let the caller create a default junction
		/// as defined by the linear network.
		/// </summary>
		/// <param name="forNetwork"></param>
		/// <returns></returns>
		[CanBeNull]
		IFeature CreateJunction([NotNull] LinearNetworkDef forNetwork);

		/// <summary>
		/// Allows the (temporary) enabling/disabling of the junction creation with the result that
		/// implementors can return null to signal to hte caller that a default junction should be
		/// created instead.
		/// </summary>
		bool Enabled { set; }
	}
}

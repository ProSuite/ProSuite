using System;

namespace ProSuite.DomainModel.AO.QA
{
	[Flags]
	public enum ErrorCreation
	{
		None = 0,
		Create = 1,
		AllowDisjointPerimeter = 2,
		IgnoreAllowedErrors = 4,

		/// <summary>
		/// for none spatial objects, check only those that reference features within the test area
		/// </summary>
		UseReferenceGeometries = 8,

		/// <summary>
		/// for none spatial errors : do not store any geometry. 
		/// If this flag is not set, it is tried to create geometries corresponding to involved features
		/// or features referencing involved objects
		/// </summary>
		NoStoreReferenceGeometries = 16

		// not used
		// DoNotCreateErrorsFromBorderMarkedFeatures = 32
	}
}

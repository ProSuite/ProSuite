using ESRI.ArcGIS.esriSystem;
using System;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO
{
	public static class SysUtils
	{
		/// <summary>
		/// Erzeugt einen Clone von orig. orig muss IClone implementieren
		/// </summary>
		[NotNull]
		public static T Clone<T>([NotNull] T orig)
		{
			object clone = ((IClone)orig).Clone();
			return (T)clone;
		}
	}
}

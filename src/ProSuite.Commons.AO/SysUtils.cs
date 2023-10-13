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

		/// <summary>
		/// Use this method to create singleton objects !
		/// multiple calls on :   new Singleton..()   will lead to errors!
		/// </summary>
		[NotNull]
		public static I Create<T, I>() where T : class, I
		{
			Type guidType = typeof(T);
			Type classType = Type.GetTypeFromCLSID(guidType.GUID);

			//string loc = System.Diagnostics.Debugger.IsAttached ? $"{typeof(T).Assembly.Location}" : "";
			//string locGuid = System.Diagnostics.Debugger.IsAttached ? $"{guidType.Assembly.Location}" : "";
			//string locClass = System.Diagnostics.Debugger.IsAttached ? $"{classType.Assembly.Location}" : "";

			I created = (I)Activator.CreateInstance(classType);
			return created;
		}
	}
}

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;
using ProSuite.Commons.Logging;

namespace ProSuite.Commons.Com
{
	public static class ComUtils
	{
		private static readonly IMsg _msg =
			new Msg(MethodBase.GetCurrentMethod().DeclaringType);

		/// <summary>
		/// Determines whether a given type can be activated via Com.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <returns>
		/// 	<c>true</c> if the type can be activated via Com; otherwise, <c>false</c>.
		/// </returns>
		public static bool IsCreatableComClass([NotNull] Type type)
		{
			Assert.ArgumentNotNull(type, nameof(type));

			return type.IsPublic &&
			       ! type.IsAbstract &&
			       HasPublicParameterlessConstructor(type) &&
			       IsComVisible(type);
		}

		/// <summary>
		/// Method to avoid Creation Problems of COM-Singletons
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <typeparam name="I"></typeparam>
		/// <returns></returns>
		public static I Create<T, I>() where T : class, I
		{
			Type guidType = typeof(T);
			Type classType = Type.GetTypeFromCLSID(guidType.GUID);
			I created = (I) Activator.CreateInstance(classType);
			return created;
		}

		public static string FormatGuid<T>()
		{
			return FormatGuid(typeof(T).GUID);
		}

		public static string FormatGuid(Guid guid)
		{
			return guid.ToString("B").ToUpper();
		}

		public static string GetRegistryKey([NotNull] Type type)
		{
			Assert.ArgumentNotNull(type, nameof(type));

			return string.Format("HKEY_CLASSES_ROOT\\CLSID\\{{{0}}}", type.GUID);
		}

		/// <summary>
		/// Creates an object based on a COM ProgId
		/// </summary>
		/// <param name="progId">The ProgId.</param>
		/// <returns></returns>
		[NotNull]
		public static object CreateObject([NotNull] string progId)
		{
			return CreateObject<object>(progId);
		}

		[NotNull]
		public static T CreateObject<T>([NotNull] string progId)
		{
			Assert.ArgumentNotNullOrEmpty(progId, nameof(progId));

			Type type = Type.GetTypeFromProgID(progId);
			Assert.NotNull(type, "Unable to get type for progId: {0}", progId);

			try
			{
				return (T) Activator.CreateInstance(type);
			}
			catch (Exception e)
			{
				_msg.ErrorFormat("Error instantiating object for {0}: {1}",
				                 progId, e.Message);
				throw;
			}
		}

		/// <summary>
		/// Decrements the reference count to zero of the supplied runtime callable wrapper. 
		/// </summary>
		/// <param name="o">The COM object to release.</param>
		public static void ReleaseComObject([CanBeNull] object o)
		{
			if (o == null)
			{
				return;
			}

			if (! Marshal.IsComObject(o))
			{
				return;
			}

			while (Marshal.ReleaseComObject(o) > 0) { }
		}

		public static bool IsComVisible([NotNull] Type type)
		{
			Assert.ArgumentNotNull(type, nameof(type));

			return Marshal.IsTypeVisibleFromCom(type);
		}

		/// <summary>
		/// Query interfaces for System.__ComObject Type e.g. StyleGalleryItem.Item
		/// interfaceForAssembly try Interfaces e.g. IBasicOverposterLayerProperties 
		/// </summary>
		/// <param name="comObject"></param>
		/// <param name="interfaceForAssembly"></param>
		/// <returns></returns>
		[NotNull]
		public static List<string> QueryInterfacesForUnknownObject(
			[NotNull] object comObject, [NotNull] Type interfaceForAssembly)
		{
			Assert.ArgumentNotNull(comObject, nameof(comObject));
			Assert.ArgumentNotNull(interfaceForAssembly, nameof(interfaceForAssembly));

			var result = new List<string>();

			IntPtr iunkwn = Marshal.GetIUnknownForObject(comObject);
			Assembly objAssembly = Assembly.GetAssembly(interfaceForAssembly);
			Type[] objTypes = objAssembly.GetTypes();
			// find the first implemented interop type
			foreach (Type currType in objTypes)
			{
				// get the iid of the current type
				Guid iid = currType.GUID;
				if (! currType.IsInterface || iid == Guid.Empty)
				{
					// com interop type must be an interface with valid iid
					continue;
				}

				// query supportability of current interface on object
				IntPtr ipointer;
				Marshal.QueryInterface(iunkwn, ref iid, out ipointer);
				if (ipointer != IntPtr.Zero)
				{
					result.Add(currType.FullName);
				}
			}

			return result;
		}

		private static bool HasPublicParameterlessConstructor([NotNull] Type type)
		{
			Assert.ArgumentNotNull(type, nameof(type));

			return type.GetConstructor(new Type[] { }) != null;
		}
	}
}

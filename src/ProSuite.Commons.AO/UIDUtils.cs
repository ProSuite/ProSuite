using System;
using ESRI.ArcGIS.esriSystem;
using ProSuite.Commons.Com;
using ProSuite.Commons.Essentials.Assertions;
using ProSuite.Commons.Essentials.CodeAnnotations;

namespace ProSuite.Commons.AO
{
	[CLSCompliant(false)]
	public static class UIDUtils
	{
		[NotNull]
		public static UID CreateUID()
		{
			UID uid = new UIDClass();

			uid.Generate();

			return uid;
		}

		[NotNull]
		public static UID CreateUID(Guid guid)
		{
			return new UIDClass {Value = ComUtils.FormatGuid(guid)};
		}

		[NotNull]
		public static UID CreateUID([NotNull] string stringID)
		{
			Assert.ArgumentNotNull(stringID, nameof(stringID));

			UID uid = new UIDClass();

			// deal with uid strings with subtypes
			// (e.g. "esriArcMapUI.MxEditMenuItem 1" -> Undo)
			int blankIndex = stringID.IndexOf(' ');
			if (blankIndex > 0 && blankIndex < stringID.Length - 1)
			{
				// there is a blank, not at the ends
				string[] tokens = stringID.Split(
					new[] {" "}, StringSplitOptions.RemoveEmptyEntries);

				if (tokens.Length == 2)
				{
					// more defensiveness...
					int subType;
					if (int.TryParse(tokens[1], out subType))
					{
						// create subtyped uid
						uid.Value = tokens[0];
						uid.SubType = subType;

						return uid;
					}
				}
			}

			// there is no subtype in the string, just assign it
			uid.Value = stringID;
			return uid;
		}

		[NotNull]
		public static UID CreateUID([NotNull] Type type)
		{
			Assert.ArgumentNotNull(type, nameof(type));

			return new UIDClass {Value = ComUtils.FormatGuid(type.GUID)};
		}

		[NotNull]
		public static UID CreateUID<T>() where T : class
		{
			return CreateUID(typeof(T));
		}
	}
}

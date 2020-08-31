//using Newtonsoft.Json;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.AGP.Storage;
using System;

namespace ProSuite.AGP.WorkList.Test
{
	public class IssuesStateJsonRepository : JsonRepository<GdbRowIdentity>
	{
		private readonly string _issueStatePath;

		//public IssuesStateJsonRepository(string filename) : base( filename, new JsonGdbIdentityConverter())
		public IssuesStateJsonRepository(string filename) : base(filename)
		{
			_issueStatePath = filename;
		}
	}

	//public class JsonGdbIdentityConverter : JsonConverter<GdbRowIdentity>
	//{
	//	public override void WriteJson(JsonWriter writer, GdbRowIdentity value, JsonSerializer serializer)
	//	{
	//		writer.WriteStartObject();
	//		writer.WritePropertyName("$" + value.ObjectId);
	//		writer.WriteStartObject();
	//		writer.WritePropertyName("Table");
	//		serializer.Serialize(writer, value.Table);
	//		writer.WriteEndObject();
	//		writer.WriteEndObject();
	//	}

	//	public override GdbRowIdentity ReadJson(JsonReader reader, Type objectType, GdbRowIdentity existingValue,
	//	                                        bool hasExistingValue, JsonSerializer serializer)
	//	{
	//		throw new NotImplementedException();
	//	}
	//}

}

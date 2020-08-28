using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ProSuite.Commons.AGP.Gdb;
using ProSuite.Commons.AGP.Storage;
using System;

namespace ProSuite.AGP.WorkList.Test
{
	public class IssuesStateJsonRepository : JsonRepository<GdbRowIdentity>
	{
		private readonly string _issueStatePath;

		public IssuesStateJsonRepository(string filename) : base(
			filename, new JsonGdbIdentityConverter())
		{
			_issueStatePath = filename;
		}
	}

	public abstract class JsonCreationConverter<T> : JsonConverter
	{
		protected abstract T Create(Type objectType, JObject jsonObject);

		public override bool CanConvert(Type objectType)
		{
			return typeof(T).IsAssignableFrom(objectType);
		}

		public override object ReadJson(JsonReader reader, Type objectType,
		                                object existingValue, JsonSerializer serializer)
		{
			var jsonObject = JObject.Load(reader);
			var target = Create(objectType, jsonObject);
			serializer.Populate(jsonObject.CreateReader(), target);
			return target;
		}

		public override void WriteJson(JsonWriter writer, object value,
		                               JsonSerializer serializer)
		{
			writer.WriteValue(value.ToString());
		}
	}

	public class JsonGdbIdentityConverter : JsonCreationConverter<GdbTableIdentity>
	{
		protected override GdbTableIdentity Create(Type objectType, JObject jsonObject)
		{
			//if (jsonObject["Table"] != null)
			//{
			//	return new GdbTableIdentity();
			//}

			//if (jsonObject["FullHD"] != null)
			//{
			//	return new TvSpecs();
			//}

			return new GdbTableIdentity();
		}
	}


}

using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace AXAXL.DbEntity.Types.NewtonsoftConverters
{
	public class RowVersionConverter : JsonConverter<RowVersion>
	{
		public override void WriteJson(JsonWriter writer, RowVersion value, JsonSerializer serializer)
		{
			writer.WriteValue((long)value);
		}
		public override RowVersion ReadJson(JsonReader reader, Type objectType, RowVersion existingValue, bool hasExistingValue, JsonSerializer serializer)
		{
			return (RowVersion)((long)reader.Value);
		}
	}
}

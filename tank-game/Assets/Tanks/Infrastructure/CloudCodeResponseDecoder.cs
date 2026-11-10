using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using Tanks.Core;

namespace Tanks.Infrastructure
{
    public static class CloudCodeResponseDecoder
    {
        public static ResultType Decode(string json)
        {
            JObject envelope = JObject.Parse(json);
            string error = Get(envelope, "errorCode")?.Value<string>();
            return error == null ? Result.Success() : Result.Failure(error);
        }

        public static ResultType<T> Decode<T>(string json)
        {
            JObject envelope = JObject.Parse(json);
            string error = Get(envelope, "errorCode")?.Value<string>();
            if (error != null) return Result.Failure<T>(error);
            JToken value = Get(envelope, "value");

            if (value == null || value.Type == JTokenType.Null)
            {
                return Result.Failure<T>("invalid-data");
            }

            return Result.Success(value.ToObject<T>());
        }

        private static JToken Get(JObject envelope, string name)
        {
            return envelope.GetValue(name, StringComparison.OrdinalIgnoreCase);
        }
    }
}

using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;

namespace Tools.Github.Extensions
{
    internal static class StringContentJsonExtensions
    {
        private static readonly JsonSerializerSettings JsonSerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new SnakeCaseNamingStrategy()
            },
            Converters =
            {
                new StringEnumConverter(true)
            }
        };

        private static readonly List<MediaTypeFormatter> MediaTypeFormatters = new List<MediaTypeFormatter>
        {
            new JsonMediaTypeFormatter
            {
                SerializerSettings = JsonSerializerSettings
            }
        };

        public static HttpContent AsSnakeCaseJsonContent<T>(this T content)
        {
            return new StringContent(JsonConvert.SerializeObject(content, JsonSerializerSettings))
            {
                Headers = {ContentType = MediaTypeHeaderValue.Parse("application/json; charset=utf-8")}
            };
        }

        public static async Task<T> AsSnakeCaseJsonAsync<T>(this HttpContent httpContent)
        {
            return await httpContent.ReadAsAsync<T>(MediaTypeFormatters);
        }
    }
}
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DocumentManagement.Specs._13_SimplerDeserialization
{
    internal static class HttpClientExtensions
    {
        public static async Task<T> GetJsonAs<T>(this HttpClient httpClient, string requestUri, T prototypeType)
        {
            HttpResponseMessage response = await httpClient.GetAsync(requestUri);

            string body = await response.Content.ReadAsStringAsync();

            return JsonConvert.DeserializeAnonymousType(body, prototypeType);
        }
    }
}
using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using System.Text.Json;

namespace DocumentManagement.Specs._14_SimplerDeserialization_SystemText
{
    internal static class HttpClientExtensions
    {
        public static HttpResponseMessageAssertions Should(this HttpResponseMessage response)
        {
            return new HttpResponseMessageAssertions(response);
        }
    }

    internal class HttpResponseMessageAssertions
    {
        private HttpResponseMessage response;

        public HttpResponseMessageAssertions(HttpResponseMessage response)
        {
            this.response = response;
        }

        public async Task BeEquivalentTo<T>(T expectation)
        {
            string body = await response.Content.ReadAsStringAsync();

            object actual = JsonSerializer.Deserialize(body, expectation.GetType(), new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            
            actual.Should().BeEquivalentTo(expectation);
        }
    }
}
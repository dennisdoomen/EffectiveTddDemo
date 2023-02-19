using System.Net.Http;
using System.Threading.Tasks;
using FluentAssertions;
using Newtonsoft.Json;

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

            T actual = JsonConvert.DeserializeAnonymousType(body, expectation);
            actual.Should().BeEquivalentTo(expectation);
        }
    }
}
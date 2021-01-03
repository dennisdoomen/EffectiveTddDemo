using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Raven.Client.Documents;

namespace DocumentManagement.Statistics
{
    
    [Produces("application/json")]
    [Route("metrics")]
    public class MetricsController : ControllerBase
    {
        private readonly GetRavenSession getRavenSession;

        public MetricsController(GetRavenSession getRavenSession)
        {
            this.getRavenSession = getRavenSession;
        }

        [Route("{CountsPerState}")]
        [HttpGet]
        public async Task<dynamic> GetCountsPerState(Guid country, string kind)
        {
            using (var session = getRavenSession())
            {
                var staticResults = await session
                    .Query<Documents_CountsByStaticState.Result, Documents_CountsByStaticState>()
                    .Customize(c => c.WaitForNonStaleResults())
                    .Where(x => x.Kind == kind && x.Country == country)
                    .ToListAsync();

                var stream = session
                    .Query<Documents_ByDynamicState.Result, Documents_ByDynamicState>()
                    .Where(x => x.Kind == kind && x.Country == country)
                    .As<DocumentCountProjection>();

                string countryName = (await session.LoadAsync<CountryLookup>($"CountryLookup/{country}")).Name;

                var evaluator = new RealtimeStateEvaluator();

                var iterator = await session.Advanced.StreamAsync(stream);
                while (await iterator.MoveNextAsync())
                {
                    DocumentCountProjection projection = iterator.Current.Document;
                    var actualState = evaluator.Evaluate(new RealtimeStateEvaluationContext
                    {
                        StaticState = projection.State,
                        Country = projection.Country,
                        NextReviewAt = projection.NextReviewAt,
                        PlannedPeriod = new ValidityPeriod(projection.StartDateTime, projection.EndDateTime),
                        ExpirationDateTime = projection.LifetimePeriodEnd
                    });

                    Documents_CountsByStaticState.Result result = staticResults.SingleOrDefault(r => r.State == actualState);
                    if (result == null)
                    {
                        result = new Documents_CountsByStaticState.Result
                        {
                            Kind = kind,
                            Country = country,
                            CountryName = countryName,
                            State = actualState,
                        };

                        staticResults.Add(result);
                    }

                    result.Count++;
                }

                return staticResults;
            }
        }
    }
}
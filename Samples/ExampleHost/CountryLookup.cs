using LiquidProjections.RavenDB;

namespace LiquidProjections.ExampleHost
{
    public class CountryLookup : IHaveIdentity
    {
        public string Id { get; set; }
        public string Name { get; set; }
    }
}
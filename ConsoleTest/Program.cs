using NPOI.Util.Collections;
using System.Linq.Expressions;
using Xyfy.Helper;

namespace ConsoleTest
{

    class TestPropertyClass
    {
        public int sample { get; set; }
    }
    internal class Program
    {
        static void Main(string[] args)
        {
            var algoliaOptions = new AlgoliaOptions();

            algoliaOptions.SetPropertyValue(x => x.AlgoliaApiKey, "abc");

            Console.Write(algoliaOptions.AlgoliaApiKey);
            TestPropertyClass obj = new TestPropertyClass();
            obj.sample = 40;
            obj.SetPropertyValue(nameof(obj.sample), 500);

        }
    }


    public class AlgoliaOptions
    {
        public const string Position = "Algolia";

        public string RootDocsPath { get; set; } = string.Empty;

        public int BatchSize { get; set; } = 2000;

        public string ApplicationId { get; set; } = string.Empty;

        public string IndexPrefix { get; set; } = "blazor-masastack_";

        public Dictionary<string, string>? Projects { get; set; } = null;

        public string DocDomain { get; set; } = string.Empty;

        public string? AlgoliaApiKey { get; set; } = null;

        public IEnumerable<string>? ExcludedUrls { get; set; } = null;
    }
}
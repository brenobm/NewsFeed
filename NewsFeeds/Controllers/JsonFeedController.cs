using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.Serialization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace NewsFeeds.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class JsonFeedController : ControllerBase
    {
        private const string _feedEMUrl = "https://www.em.com.br/rss/noticia/gerais/rss.xml";
        private const string _feedFolhaUrl = "https://feeds.folha.uol.com.br/emcimadahora/rss091.xml";

        private readonly ILogger<JsonFeedController> _logger;

        public JsonFeedController(ILogger<JsonFeedController> logger)
        {
            _logger = logger;
        }

        [HttpGet("em")]
        public async Task<IEnumerable<Entry>> GetEM()
        {
            var rssText = await GetResponse(_feedEMUrl);

            var list = ParseXmlForEM(rssText);

            return list;
        }

        [HttpGet("folha")]
        public async Task<IEnumerable<Entry>> GetFolha()
        {
            var rssText = await GetResponse(_feedFolhaUrl);

            var list = ParseXmlForFolha(rssText);

            return list;
        }

        private async Task<string> GetResponse(string url)
        {
            var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(15);

            using (var request = new HttpRequestMessage(HttpMethod.Get, new Uri(url)))
            {
                request.Headers.TryAddWithoutValidation("Accept", "text/html,application/xhtml+xml,application/xml");
                request.Headers.TryAddWithoutValidation("Accept-Encoding", "gzip, deflate");
                request.Headers.TryAddWithoutValidation("User-Agent", "Mozilla/5.0 (Windows NT 6.2; WOW64; rv:19.0) Gecko/20100101 Firefox/19.0");
                request.Headers.TryAddWithoutValidation("Accept-Charset", "ISO-8859-1");

                using (var response = await httpClient.SendAsync(request).ConfigureAwait(false))
                {
                    response.EnsureSuccessStatusCode();
                    using (var responseStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                    using (var decompressedStream = new GZipStream(responseStream, CompressionMode.Decompress))
                    using (var streamReader = new StreamReader(decompressedStream))
                    {
                        return await streamReader.ReadToEndAsync().ConfigureAwait(false);
                    }
                }
            }
        }

        private IEnumerable<Entry> ParseXmlForEM(string xmlText)
        {
            XDocument quotesDoc = XDocument.Parse(xmlText);


            IEnumerable<Entry> quotes = quotesDoc.Root
                .Element("channel").
                Elements("item")
                .Select(x => new Entry
                {
                    uid = x.Element("guid")?.Value ?? "vazio",
                    titleText = x.Element("title")?.Value ?? "vazio",
                    mainText = x.Element("subtitle")?.Value ?? "vazio",
                    updateDate = DateTime.Parse(x.Element("pubDate")?.Value).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:sszzz") ?? DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:sszzz"),
                    redirectionUrl = x.Element("link")?.Value ?? "vazio"
                });

            return quotes;
        }

        private IEnumerable<Entry> ParseXmlForFolha(string xmlText)
        {
            XDocument quotesDoc = XDocument.Parse(xmlText);


            IEnumerable<Entry> quotes = quotesDoc.Root
                .Element("channel").
                Elements("item")
                .Select(x => new Entry
                {
                    uid = Guid.NewGuid().ToString(),
                    titleText = x.Element("title")?.Value ?? "vazio",
                    mainText = x.Element("description")?.Value ?? "vazio",
                    updateDate = DateTime.Parse(x.Element("pubDate")?.Value).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:sszzz") ?? DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:sszzz"),
                    redirectionUrl = x.Element("link")?.Value ?? "vazio"
                });

            return quotes;
        }
    }

    public class Entry
    {
        [JsonPropertyName("uid")]
        public string uid { get; set; }
        [JsonPropertyName("updateDate")]
        public string updateDate { get; set; }
        [JsonPropertyName("mainText")]
        public string mainText { get; set; }
        [JsonPropertyName("titleText")]
        public string titleText { get; set; }
        [JsonPropertyName("redirectionUrl")]
        public string redirectionUrl { get; set; }
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text;
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

            var response = await httpClient.GetAsync(new Uri(url));

            var content = await response.Content.ReadAsByteArrayAsync();

            var text = Encoding.UTF8.GetString(content);

            if (text.Contains("ISO-8859-1"))
                text = Encoding
                    .GetEncoding("iso-8859-1")
                    .GetString(content);

            return text;
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
                    mainText = $"{(x.Element("title")?.Value ?? "")} - {x.Element("subtitle")?.Value ?? "vazio"}",
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
                    mainText = $"{(x.Element("title")?.Value ?? "")} - {x.Element("description")?.Value ?? "vazio"}",
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

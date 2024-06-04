using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace DictionaryApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DictionaryController : ControllerBase
    {
        private readonly HttpClient _httpClient;
        private readonly string _dictionaryApiUrl = "https://api.dictionaryapi.dev/api/v2/entries/en/";

        public DictionaryController()
        {
            _httpClient = new HttpClient();
        }

        [HttpGet("definitions/{word}")]
        public async Task<IActionResult> GetDefinitions(string word)
        {
            var dictionaryResponse = await _httpClient.GetStringAsync(_dictionaryApiUrl + word);
            var jsonObject = JArray.Parse(dictionaryResponse);
            var definitions = jsonObject.SelectTokens("$..definitions[*].definition").Select(d => d.ToString()).ToList();

            return Ok(definitions.Any() ? definitions : new List<string> { "No definitions found." });
        }

        [HttpGet("synonyms/{word}")]
        public async Task<IActionResult> GetSynonyms(string word)
        {
            var dictionaryResponse = await _httpClient.GetStringAsync(_dictionaryApiUrl + word);
            var jsonObject = JArray.Parse(dictionaryResponse);
            var synonyms = jsonObject.SelectTokens("$..synonyms[*]").Select(s => s.ToString()).Distinct().ToList();

            return Ok(synonyms.Any() ? synonyms : new List<string> { "No synonyms found." });
        }

        [HttpGet("antonyms/{word}")]
        public async Task<IActionResult> GetAntonyms(string word)
        {
            var dictionaryResponse = await _httpClient.GetStringAsync(_dictionaryApiUrl + word);
            var jsonObject = JArray.Parse(dictionaryResponse);
            var antonyms = jsonObject.SelectTokens("$..antonyms[*]").Select(a => a.ToString()).Distinct().ToList();

            return Ok(antonyms.Any() ? antonyms : new List<string> { "No antonyms found." });
        }

        [HttpGet("phonetics/{word}")]
        public async Task<IActionResult> GetPhonetics(string word)
        {
            var dictionaryResponse = await _httpClient.GetStringAsync(_dictionaryApiUrl + word);
            var jsonObject = JArray.Parse(dictionaryResponse);
            var phonetics = jsonObject.SelectTokens("$..phonetics[*].text").Select(p => p.ToString()).ToList();

            return Ok(phonetics.Any() ? phonetics : new List<string> { "No phonetics found." });
        }
    }
}

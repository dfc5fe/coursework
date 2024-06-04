using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace TelegramBot.Services
{
    public class DictionaryService
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiUrl = "http://";

        public DictionaryService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<List<string>> GetDefinitions(string word)
        {
            var response = await _httpClient.GetStringAsync(_apiUrl + "definitions/" + word);
            return JsonConvert.DeserializeObject<List<string>>(response);
        }

        public async Task<List<string>> GetSynonyms(string word)
        {
            var response = await _httpClient.GetStringAsync(_apiUrl + "synonyms/" + word);
            return JsonConvert.DeserializeObject<List<string>>(response);
        }

        public async Task<List<string>> GetAntonyms(string word)
        {
            var response = await _httpClient.GetStringAsync(_apiUrl + "antonyms/" + word);
            return JsonConvert.DeserializeObject<List<string>>(response);
        }

        public async Task<List<string>> GetPhonetics(string word)
        {
            var response = await _httpClient.GetStringAsync(_apiUrl + "phonetics/" + word);
            return JsonConvert.DeserializeObject<List<string>>(response);
        }
    }
}
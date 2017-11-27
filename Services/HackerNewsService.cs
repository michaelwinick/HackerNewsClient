using HackerNewsClient.Models;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace HackerNewsClient.Services
{
    public class HackerNewsService : IHackerNewsService
    {
        private readonly ILogger<HackerNewsService> _logger;
        private readonly IMemoryCache _memoryCache;

        private const double timeoutInSeconds = 10;
        private const string topStoryKey = "top-story";

        public HackerNewsService(
            ILogger<HackerNewsService> logger,
            IMemoryCache memoryCache)
        {
            _logger = logger;
            _memoryCache = memoryCache;
        }

        public async Task<StoryModel> TopStoryAsync()
        {
            var topStory = TopStoryFromCache();
            if (topStory == null)
            {
                _logger.LogInformation($"*** Top story NOT IN the cache ***");

                topStory = await TopStoryFromServiceAsync();
                SetTopStoryInCache(topStory);
            }
            else
            {
                _logger.LogInformation($"*** Top story IN the cache ***");
            }

            return topStory;
        }

        private async Task<StoryModel> TopStoryFromServiceAsync()
        {
            var topItemNumber = await TopItemNumberAsync();

            return await StoryByItemNumberAsync(topItemNumber);
        }

        private async Task<StoryModel> StoryByItemNumberAsync(string itemNumber)
        {
            var title = await TitleByItemNumberAsync(itemNumber);
            var author = await AuthorByItemNumberAsync(itemNumber);

            var topStory = new StoryModel()
            {
                Title = title,
                Author = author
            };

            return topStory;
        }

        private void SetTopStoryInCache(StoryModel topStoryModel)
        {
            _memoryCache.Set<StoryModel>(
                topStoryKey,
                topStoryModel,
                new MemoryCacheEntryOptions().SetAbsoluteExpiration(
                    TimeSpan.FromSeconds(timeoutInSeconds)));
        }

        private StoryModel TopStoryFromCache()
        {
            StoryModel topStoryFromCache = null;
            _memoryCache.TryGetValue(topStoryKey, out topStoryFromCache);

            return topStoryFromCache;
        }

        private async Task<string> TopItemNumberAsync()
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var response = await client.GetAsync("https://hacker-news.firebaseio.com/v0/topstories.json");
            if (!response.IsSuccessStatusCode)
            {
                throw new Exception("hacker-news error");
            }

            var allEntries = JsonConvert.DeserializeObject<List<string>>(await response.Content.ReadAsStringAsync());

            if (allEntries.Count <= 0)
            {
                throw new Exception("No items found...");
            }
            
            var topEntry = allEntries[0];

            return topEntry;
        }

        private async Task<string> TitleByItemNumberAsync(string topEntry)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var titleResponse = await client.GetAsync($"https://hacker-news.firebaseio.com/v0/item/{topEntry}/title.json");
            if (!titleResponse.IsSuccessStatusCode)
            {
                throw new Exception("hacker-news error");
            }

            var title = JsonConvert.DeserializeObject<string>(await titleResponse.Content.ReadAsStringAsync());

            return title;
        }

        private async Task<string> AuthorByItemNumberAsync(string topEntry)
        {
            var client = new HttpClient();
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var authorResponse = await client.GetAsync($"https://hacker-news.firebaseio.com/v0/item/{topEntry}/by.json");
            if (!authorResponse.IsSuccessStatusCode)
            {
                throw new Exception("hacker-news error");
            }

            var author = JsonConvert.DeserializeObject<string>(await authorResponse.Content.ReadAsStringAsync());

            return author;
        }
    }
}
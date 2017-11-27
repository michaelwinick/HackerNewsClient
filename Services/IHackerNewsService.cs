using HackerNewsClient.Models;
using System.Threading.Tasks;

namespace HackerNewsClient.Services
{
    public interface IHackerNewsService
    {
        Task<StoryModel> TopStoryAsync();
    }
}
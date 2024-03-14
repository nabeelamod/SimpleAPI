using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace SimpleAPI.Controllers
{
    [ApiController]
    [Route("")]
    public class BlogController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public BlogController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        }

        [HttpGet("{tags}")]
        public async Task<IActionResult> GetBlogPosts(string tags, string sortBy = "id", string direction = "asc")
        {
            if (string.IsNullOrWhiteSpace(tags))
            {
                return BadRequest("Tags parameter is required.");
            }

            var tagList = tags.Split(',');

            var httpClient = _httpClientFactory.CreateClient();
            var allPosts = new List<Post>();
            var uniquePostIds = new HashSet<int>();

            foreach (var tag in tagList)
            {
                var response = await httpClient.GetAsync($"https://api.hatchways.io/assessment/blog/posts?tag={tag}");

                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode((int)response.StatusCode, "API Failed");
                }

                var responseDataString = await response.Content.ReadAsStringAsync();
                var responseData = JsonSerializer.Deserialize<BlogResponse>(responseDataString, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                foreach (var post in responseData.Posts.Where(p => p.tags.Contains(tag)))
                {
                    if (uniquePostIds.Add(post.id))
                    {
                        allPosts.Add(post);
                    }
                }
            }

            switch (sortBy.ToLower())
            {
                case "reads":
                    allPosts = direction.ToLower() == "asc" ? allPosts.OrderBy(p => p.reads).ToList() : allPosts.OrderByDescending(p => p.reads).ToList();
                    break;
                case "likes":
                    allPosts = direction.ToLower() == "asc" ? allPosts.OrderBy(p => p.likes).ToList() : allPosts.OrderByDescending(p => p.likes).ToList();
                    break;
                case "popularity":
                    allPosts = direction.ToLower() == "asc" ? allPosts.OrderBy(p => p.popularity).ToList() : allPosts.OrderByDescending(p => p.popularity).ToList();
                    break;
                default:
                    allPosts = direction.ToLower() == "asc" ? allPosts.OrderBy(p => p.id).ToList() : allPosts.OrderByDescending(p => p.id).ToList();
                    break;
            }

            return Ok(new BlogResponse { Posts = allPosts });
        }

    }
    public class BlogResponse
    {
        public List<Post> Posts { get; set; }
    }

    public class Post
    {
        public string author { get; set; }
        public int authorId { get; set; }
        public int id { get; set; }
        public int likes { get; set; }
        public decimal popularity { get; set; }
        public int reads { get; set; }
        public string[] tags { get; set; }
    }

}

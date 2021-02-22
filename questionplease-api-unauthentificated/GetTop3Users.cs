using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Cosmos;
using System.Collections.Generic;
using questionplease_api_unauthentificated.Items;
using System.Linq;

namespace questionplease_api_unauthentificated
{
    public class Score
    {
        [JsonProperty(PropertyName = "score")]
        public int InnerScore { get; set; }
    }

    public class GetTop3Users
    {
        private readonly ILogger _logger;
        private CosmosClient _cosmosClient;

        private Database _database;
        private Container _userContainer;

        public GetTop3Users(
            ILogger<GetTop3Users> logger,
            CosmosClient cosmosClient
            )
        {
            _logger = logger;
            _cosmosClient = cosmosClient;

            _database = _cosmosClient.GetDatabase(Constants.DATABASE_NAME);
            _userContainer = _database.GetContainer(Constants.USERS_COLLECTION_NAME);
        }

        [FunctionName("GetTop3Users")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "user/top")] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");
            var result = new List<ReturnedUser>();

            List<Score> scores = await GetDistinctScores(log);
            var sortedScore = scores.OrderByDescending(s => s.InnerScore).ToList();

            if (sortedScore == null || sortedScore.Count == 0)
            {
                return new OkObjectResult(result);
            }

            int minScore = sortedScore.Take(3).Last().InnerScore;

            List<DatabaseUser> topUsers = await GetUsersWithScoreEqualsOrAbove(minScore, log);
            var sortedTopUsers = topUsers.OrderByDescending(s => s.Score).ToList();

            foreach (var u in sortedTopUsers)
            {
                result.Add(new ReturnedUser { Id = u.Id, Login = u.Login, Score = u.Score });
            }

            return new OkObjectResult(result);
        }

        private async Task<List<Score>> GetDistinctScores(ILogger log)
        {
            List<Score> topScores = new List<Score>();
            QueryDefinition topScoreQuery = new QueryDefinition("SELECT DISTINCT u.score FROM users u GROUP BY u.score");
            using (FeedIterator<Score> feedIterator = _userContainer.GetItemQueryIterator<Score>(topScoreQuery))
            {
                while (feedIterator.HasMoreResults)
                {
                    foreach (var item in await feedIterator.ReadNextAsync())
                    {
                        topScores.Add(item);
                    }
                }
            }

            return topScores;
        }

        private async Task<List<DatabaseUser>> GetUsersWithScoreEqualsOrAbove(int minScore, ILogger log)
        {
            List<DatabaseUser> topUsers = new List<DatabaseUser>();
            QueryDefinition topUsersQuery = new QueryDefinition("SELECT * FROM users u WHERE u.score >= @minScore").WithParameter("@minScore", minScore);
            using (FeedIterator<DatabaseUser> feedIterator = _userContainer.GetItemQueryIterator<DatabaseUser>(topUsersQuery))
            {
                while (feedIterator.HasMoreResults)
                {
                    foreach (var item in await feedIterator.ReadNextAsync())
                    {
                        topUsers.Add(item);
                    }
                }
            }

            return topUsers;
        }
    }
}

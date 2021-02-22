using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Cosmos;
using questionplease_api_unauthentificated.Items;
using System.Collections.Generic;
using System.Linq;

namespace questionplease_api_unauthentificated
{
    public class ReadNextQuestion
    {
        private readonly ILogger _logger;
        private CosmosClient _cosmosClient;

        private Database _database;
        private Container _questionContainer;

        public ReadNextQuestion(
            ILogger<ReadNextQuestion> logger,
            CosmosClient cosmosClient
            )
        {
            _logger = logger;
            _cosmosClient = cosmosClient;

            _database = _cosmosClient.GetDatabase(Constants.DATABASE_NAME);
            _questionContainer = _database.GetContainer(Constants.QUESTIONS_COLLECTION_NAME);
        }

        [FunctionName("ReadNextQuestion")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "question")] HttpRequest req,
            ILogger log)
        {
            try
            {
                log.LogInformation("C# HTTP trigger function processed a request.");

                const int nbOfQuestion = 50;

                Random rnd = new Random();
                int randomNumber = rnd.Next(1, nbOfQuestion);

                QueryDefinition getQuestionWithId = new QueryDefinition("SELECT * FROM questions q WHERE q.id=@id")
                    .WithParameter("@id", randomNumber);

                var result = new List<Question>();
                using (FeedIterator<Question> feedIterator = _questionContainer.GetItemQueryIterator<Question>(getQuestionWithId))
                {
                    while (feedIterator.HasMoreResults)
                    {
                        foreach (var q in await feedIterator.ReadNextAsync())
                        {
                            result.Add(q);
                        }
                    }
                }

                if (result.Count == 0 || result.Count > 1)
                {
                    throw new Exception($"Several questions found with id {randomNumber}");
                }

                return new OkObjectResult(new ReturnedQuestion(result.Single()));
            }
            catch (Exception ex)
            {
                _logger.LogError($"Could not read next question. Exception thrown: {ex.Message}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }
    }
}

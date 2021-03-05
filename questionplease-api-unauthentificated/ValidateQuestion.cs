using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Azure.Cosmos;
using questionplease_api_unauthentificated.Items;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace questionplease_api_unauthentificated
{
    public class ValidateQuestion
    {
        private readonly ILogger _logger;
        private CosmosClient _cosmosClient;

        private Database _database;
        private Container _questionContainer;

        public ValidateQuestion(
            ILogger<ReadNextQuestion> logger,
            CosmosClient cosmosClient
            )
        {
            _logger = logger;
            _cosmosClient = cosmosClient;

            _database = _cosmosClient.GetDatabase(Constants.DATABASE_NAME);
            _questionContainer = _database.GetContainer(Constants.QUESTIONS_COLLECTION_NAME);
        }

        [FunctionName("ValidateQuestion")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "question/validate")] HttpRequest req,
            ILogger log)
        {
            try
            {
                log.LogInformation("C# HTTP trigger function processed a request.");

                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                dynamic data = JsonConvert.DeserializeObject(requestBody);

                int questionId = data?.questionId;
                string userAnswer = data?.answer;

                string correctAnswer = await GetCorrectAnswer(questionId, log);
                correctAnswer = WebUtility.HtmlDecode(correctAnswer);

                bool isValid = IsAnswerValid(userAnswer, correctAnswer, out int points);
                log.LogInformation($"User has correct answer for questionId {questionId} : {isValid}");

                var result = new ValidateQuestionReply
                {
                    IsValid = isValid,
                    Points = points,
                    NewScore = points
                };

                return new OkObjectResult(result);
            }
            catch (Exception ex)
            {
                log.LogError($"Error during question validation. Exception thrown: {ex.Message}");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
        }

        private async Task<string> GetCorrectAnswer(int questionId, ILogger log)
        {
            List<Question> questionList = new List<Question>();
            QueryDefinition questionById = new QueryDefinition("select * from questions u where u.id = @id").WithParameter("@id", questionId.ToString());
            using (FeedIterator<Question> feedIterator = _questionContainer.GetItemQueryIterator<Question>(questionById))
            {
                while (feedIterator.HasMoreResults)
                {
                    foreach (var item in await feedIterator.ReadNextAsync())
                    {
                        questionList.Add(item);
                    }
                }
            }

            if (questionList.Count == 0 || questionList.Count > 1)
            {
                throw new Exception($"Several questions found with id {questionId}");
            }

            return questionList.Single().CorrectAnswer;
        }

        private bool IsAnswerValid(string userAnswer, string correctAnswer, out int points)
        {
            points = 0;

            if (userAnswer.ToLower() == correctAnswer.ToLower())
            {
                points = 1;
                return true;
            }
            return false;
        }
    }
}

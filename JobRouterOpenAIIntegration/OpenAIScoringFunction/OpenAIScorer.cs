// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using Azure.AI.OpenAI;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System;
using Azure;
using Newtonsoft.Json;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace OpenAIScoringFunction
{
    public static class OpenAIScorer
    {
        private const string Id = "Id";
        private const string CSAT = "CSAT";
        private const string OutcomeScore = "Outcome";
        private const string AHTime = "AHT";

        [FunctionName("OpenAIScorer")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req, ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var requestBodyAsString = await new StreamReader(req.Body).ReadToEndAsync();
            log.LogInformation(requestBodyAsString);

            var options = new JsonSerializerOptions();
            options.Converters.Add(new ObjectDictionaryConverter());

            var parameters = JsonSerializer.Deserialize<Dictionary<string, object>>(requestBodyAsString, options);

            var workerLabelsRaw = (List<object>?)parameters?.GetValueOrDefault("workers", new List<Dictionary<string, object>>());
            if (workerLabelsRaw is null || workerLabelsRaw.Count == 0)
            {
                var msg = $"Invalid worker labels. Worker labels found: {workerLabelsRaw?.Count ?? 0}";
                log.LogError(msg);
                return new BadRequestObjectResult(msg);
            }

            var workerLabels = workerLabelsRaw.Cast<Dictionary<string, object>>().ToList();
            Dictionary<string, double> scoreResults;

            try
            {
                scoreResults = await GetScoreAsync(workerLabels, log);
            }
            catch (Exception ex)
            {
                log.LogError($"Unable to fetch results, returning with default scores for all workers. {ex}");
                scoreResults = workerLabels.ToDictionary(worker => worker["Id"].ToString() ?? "", worker => 1.0);
            }

            return new OkObjectResult(scoreResults);
        }

        private static async Task<Dictionary<string, double>> GetScoreAsync(List<Dictionary<string, object>> workerLabels, ILogger log)
        {
            var agents = new List<Agent>();

            foreach (var worker in workerLabels)
            {
                // Start Performance indicators at top so that Agent will be initially offered some jobs and can start building stats
                decimal? csat = null;
                if (decimal.TryParse(Environment.GetEnvironmentVariable("DefaultCSAT"), out var defaultCSAT)) csat = defaultCSAT;
                decimal? outcome = null;
                if (decimal.TryParse(Environment.GetEnvironmentVariable("DefaultOutcome"), out var defaultOutcome)) outcome = defaultOutcome;

                var agent = new Agent
                {
                    Id = worker[Id].ToString(),
                    PerformanceIndicators = new PerformanceIndicators
                    {
                        AHT = Environment.GetEnvironmentVariable("DefaultAHT") ?? "00:00",
                        CSAT = csat ?? 1,
                        Outcome = outcome ?? 1
                    }
                };

                foreach (var label in worker)
                {
                    if (label.Key == CSAT ) 
                    {
                        agent.PerformanceIndicators.CSAT = decimal.Parse(label.Value.ToString());
                    }
                    else if (label.Key == OutcomeScore) 
                    {
                        agent.PerformanceIndicators.Outcome = decimal.Parse(label.Value.ToString());
                    }
                    else if (label.Key == AHTime) 
                    {
                        var aht = TimeSpan.Parse(label.Value.ToString());
                        agent.PerformanceIndicators.AHT = $"{aht.Minutes}:{aht.Seconds}";
                    }
                }

                agents.Add(agent);
            }

            var baseUri = Environment.GetEnvironmentVariable("OpenAIBaseUri");
            var apiKey = Environment.GetEnvironmentVariable("OpenAIApiKey");
            var deploymentName = Environment.GetEnvironmentVariable("DeploymentName");

            var body = JsonSerializer.Serialize(agents, new JsonSerializerOptions { WriteIndented = true });

            var openAiClient = new OpenAIClient(new Uri(baseUri), new AzureKeyCredential(apiKey));

            var prePrompt = Environment.GetEnvironmentVariable("Preprompt") ?? @"You are helping pair a customer with an agent in a contact center. You will evaluate the best available agent based on their performance indicators below. Note that CSAT holds the average customer satisfaction score between 1 and 3, higher is better.Outcome is a score between 0 and 1, higher is better. AHT is average handling time, lower is better. If AHT provided is 00:00, please ignore it in the scoring.";

            var postPrompt = Environment.GetEnvironmentVariable("Postprompt") ?? "Respond with only a json object with agent Id as the key, and scores based on suitability for this customer as the value in a range of 0 to 1. Do not include any other information.";

            var prompt = $"{prePrompt}\r\n\r\n{body}\r\n\r\n{postPrompt}";

            log.LogInformation($"Prompt sent to OpenAI: {prompt}");

            CompletionsOptions completionsOptions = new()
            {
                Temperature = 0,
                MaxTokens = 1000,
            };

            completionsOptions.Prompts.Add(prompt);

            var completionResponse = await openAiClient.GetCompletionsAsync(deploymentName, completionsOptions);

            var result = completionResponse.Value.Choices[0].Text;

            log.LogInformation(result);

            var scoreResultDictionary = JsonConvert.DeserializeObject<Dictionary<string, double>>(result);
            
            return scoreResultDictionary;
        }
    }
}

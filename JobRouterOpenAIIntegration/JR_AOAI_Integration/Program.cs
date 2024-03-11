using Azure;
using Azure.Communication.JobRouter;
using Microsoft.Extensions.Configuration;
using System;

namespace JR_AOAI_Integration
{
    internal class Program
    {
        static IConfiguration Configuration;
        static JobRouterAdministrationClient JobRouterAdministrationClient;
        static JobRouterClient JobRouterClient;
        static async Task Main(string[] args)
        {
            // Build configuration
            Configuration = new ConfigurationBuilder()
                .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();

            // Create JobRouter Clients
            JobRouterAdministrationClient = new JobRouterAdministrationClient(Configuration["ConnectionStrings:ACSConnectionString"]);
            JobRouterClient = new JobRouterClient(Configuration["ConnectionStrings:ACSConnectionString"]);

            // Provisioning Job Router Resources
            await ProvisionQueuesAndPolicies();
            await ProvisionWorkers();
            Console.WriteLine("Job Router Distribution Policy, Queue and workers have been provisioned.");

            // Execute Demo
            while (true)
            {
                Console.WriteLine("Do you want to create a job? (yes/no)");
                string userInput = Console.ReadLine().Trim().ToLower();

                if (userInput == "yes")
                {
                    var jobId = await CreateJob();

                    var waitForOffer = true;

                    while (waitForOffer)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(2));

                        Console.WriteLine("Checking for Offers");

                        await foreach (var worker in JobRouterClient.GetWorkersAsync(queueId: Configuration["JobRouterResources:Queue:id"]))
                        { 
                            if (worker.Offers.Count > 0 && worker.Offers.FirstOrDefault().JobId == jobId)
                            {
                                Console.WriteLine($"Worker {worker.Id} has received the offer for new Job. Taking the Job to Closes state.");
                                var acceptOffer = await JobRouterClient.AcceptJobOfferAsync(worker.Id, worker.Offers.FirstOrDefault().OfferId);
                                await Task.Delay(TimeSpan.FromSeconds(2));
                                await JobRouterClient.CompleteJobAsync(new CompleteJobOptions(jobId, acceptOffer.Value.AssignmentId));
                                await Task.Delay(TimeSpan.FromSeconds(2));
                                await JobRouterClient.CloseJobAsync(new CloseJobOptions(jobId, acceptOffer.Value.AssignmentId));
                                waitForOffer = false;
                            }
                        }
                    }
                }
                else if (userInput == "no")
                {
                    await CleanUpWorkers();
                    await CleanUpJobsPolicyAndQueue();
                    Console.WriteLine("Exiting program.");
                    break;
                }
                else
                {
                    Console.WriteLine("Invalid input. Please type 'yes' or 'no'.");
                }
            }
        }

        private static async Task CleanUpJobsPolicyAndQueue()
        {
            await foreach (var job in JobRouterClient.GetJobsAsync(queueId: Configuration["JobRouterResources:Queue:id"]))
            {
                if (job.Status != RouterJobStatus.Closed)
                {
                    await JobRouterClient.CancelJobAsync(new CancelJobOptions(job.Id));
                }
                await JobRouterClient.DeleteJobAsync(job.Id);
                Console.WriteLine($"Job: {job.Id} has been deleted.");
            }
            await JobRouterAdministrationClient.DeleteQueueAsync(Configuration["JobRouterResources:Queue:id"]);
            Console.WriteLine($"Queue: {Configuration["JobRouterResources:Queue:id"]} has been deleted.");
            await JobRouterAdministrationClient.DeleteDistributionPolicyAsync(Configuration["JobRouterResources:DistributionPolicy:id"]);
            Console.WriteLine($"DistributionPolicy: {Configuration["JobRouterResources:DistributionPolicy:id"]} has been deleted.");
        }

        private static async Task ProvisionWorkers()
        {
            var workersConfig = new WorkersConfig();
            Configuration.Bind("Workers", workersConfig);

            foreach (var worker in workersConfig.Defaults)
            {
                var workerOptions = new CreateWorkerOptions(workerId: worker.Key, capacity: 1)
                {
                    Queues = { worker.Value.QueueId },
                    Channels = { new RouterChannel(channelId: "voice", capacityCostPerJob: 1) },
                    AvailableForOffers = true
                };

                foreach (var label in worker.Value.Labels)
                {
                    workerOptions.Labels.Add(label.Key, label.Value.ToRouterValue());
                }

                foreach (var tag in worker.Value.Tags)
                {
                   workerOptions.Tags.Add(tag.Key, tag.Value.ToRouterValue());
                }

                var newWorker = await JobRouterClient.CreateWorkerAsync(workerOptions);

                Console.WriteLine($"Worker: {newWorker.Value.Id} has been created.");
            }
        }

        private static async Task CleanUpWorkers()
        {
            await foreach (var worker in JobRouterClient.GetWorkersAsync(queueId: Configuration["JobRouterResources:Queue:id"]))
            {
                // Check if worker has any assignments that need to be closed
                if (worker.AssignedJobs.Count > 0)
                {
                    foreach (var job in worker.AssignedJobs)
                    {
                        await JobRouterClient.CompleteJobAsync(new CompleteJobOptions(job.JobId, job.AssignmentId));
                        await JobRouterClient.CloseJobAsync(new CloseJobOptions(job.JobId, job.AssignmentId));
                    }
                }
                await JobRouterClient.DeleteWorkerAsync(worker.Id);
                Console.WriteLine($"Worker: {worker.Id} has been deleted.");
            }
        }

        private static async Task ProvisionQueuesAndPolicies()
        {
            var distributionPolicy = await CreateDistributionPolicyAsync(
                Configuration["JobRouterResources:DistributionPolicy:id"], 
                Configuration["JobRouterResources:DistributionPolicy:name"], 
                new TimeSpan(0, 5, 0));
            Console.WriteLine($"{distributionPolicy.Name} was created");

            var queue = await CreateQueueAsync(
                Configuration["JobRouterResources:Queue:id"],
                Configuration["JobRouterResources:Queue:name"],
                distributionPolicy.Id);
            Console.WriteLine($"{queue.Name} was created");
        }

        private static async Task<DistributionPolicy> CreateDistributionPolicyAsync(string id, string name, TimeSpan offerTTL)
        {
            try
            {
                var distributionPolicyResponse = await JobRouterAdministrationClient.CreateDistributionPolicyAsync(new CreateDistributionPolicyOptions(id, offerTTL, SetBestWorkerOpenAiMode())
                {
                    Name = name
                });

                return distributionPolicyResponse.Value;
            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine($"Error occurred while creating distribution policy: {ex}");
                throw;
            }
        }

        private static async Task<RouterQueue> CreateQueueAsync(string id, string name, string distributionPolicyId)
        {
            try
            {
                var queueResponse = await JobRouterAdministrationClient.CreateQueueAsync(new CreateQueueOptions(id, distributionPolicyId) 
                { 
                    Name = name 
                });

                return queueResponse.Value;
            }
            catch (RequestFailedException ex)
            {
                Console.WriteLine($"Error occurred while creating queue: {ex}");
                throw;
            }
        }

        private async static Task<string> CreateJob()
        {
            Console.WriteLine("Creating a job...");

            var jobOptions = new CreateJobOptions(Guid.NewGuid().ToString(), "voice", Configuration["JobRouterResources:Queue:id"]);
            await JobRouterClient.CreateJobAsync(jobOptions);

            Console.WriteLine("Job created successfully.");

            return jobOptions.JobId;
        }

        private static BestWorkerMode SetBestWorkerOpenAiMode()
        {
            var mode = new BestWorkerMode() { ScoringRule = new FunctionRouterRule(new Uri(Configuration["OpenAiPairing:AzureFunctionUri"]))
            {
                Credential = new FunctionRouterRuleCredential(Configuration["OpenAiPairing:AzureFunctionKey"])
            },
                ScoringRuleOptions = 
                new ScoringRuleOptions() { 
                    IsBatchScoringEnabled = true
                }
            };

            return mode;
        }
    }
}
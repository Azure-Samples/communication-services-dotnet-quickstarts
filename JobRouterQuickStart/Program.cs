﻿// See https://aka.ms/new-console-template for more information
using Azure.Communication.JobRouter;

Console.WriteLine("Azure Communication Services - Job Router Quickstart");

var connectionString = "your_connection_string";
var routerAdminClient = new JobRouterAdministrationClient(connectionString);
var routerClient = new JobRouterClient(connectionString);

var distributionPolicy = await routerAdminClient.CreateDistributionPolicyAsync(
    new CreateDistributionPolicyOptions(
        distributionPolicyId: "distribution-policy-1",
        offerExpiresAfter: TimeSpan.FromMinutes(1),
        mode: new LongestIdleMode())
    {
        Name = "My distribution policy"
    }
);

var queue = await routerAdminClient.CreateQueueAsync(
    new CreateQueueOptions(queueId: "queue-1", distributionPolicyId: distributionPolicy.Value.Id)
    {
        Name = "My Queue"
    });

var job = await routerClient.CreateJobAsync(
    new CreateJobOptions(jobId: "job-1", channelId: "voice", queueId: queue.Value.Id)
    {
        Priority = 1,
        RequestedWorkerSelectors =
        {
            new RouterWorkerSelector(key: "Some-Skill", labelOperator: LabelOperator.GreaterThan, value: new LabelValue(10))
        }
    });

var worker = await routerClient.CreateWorkerAsync(
    new CreateWorkerOptions(workerId: "worker-1", totalCapacity: 1)
    {
        QueueAssignments = { [queue.Value.Id] = new RouterQueueAssignment() },
        Labels = { ["Some-Skill"] = new LabelValue(11) },
        ChannelConfigurations = { ["voice"] = new ChannelConfiguration(capacityCostPerJob: 1) },
        AvailableForOffers = true
    });

await Task.Delay(TimeSpan.FromSeconds(10));
worker = await routerClient.GetWorkerAsync(worker.Value.Id);
foreach (var offer in worker.Value.Offers)
{
    Console.WriteLine($"Worker {worker.Value.Id} has an active offer for job {offer.JobId}");
}

var accept = await routerClient.AcceptJobOfferAsync(worker.Value.Id, worker.Value.Offers.FirstOrDefault().OfferId);
Console.WriteLine($"Worker {worker.Value.Id} is assigned job {accept.Value.JobId}");

await routerClient.CompleteJobAsync(new CompleteJobOptions(accept.Value.JobId, accept.Value.AssignmentId));
Console.WriteLine($"Worker {worker.Value.Id} has completed job {accept.Value.JobId}");

await routerClient.CloseJobAsync(new CloseJobOptions(accept.Value.JobId, accept.Value.AssignmentId) { DispositionCode = "Resolved" });
Console.WriteLine($"Worker {worker.Value.Id} has closed job {accept.Value.JobId}");

await routerClient.DeleteJobAsync(accept.Value.JobId);
Console.WriteLine($"Deleted job {accept.Value.JobId}");

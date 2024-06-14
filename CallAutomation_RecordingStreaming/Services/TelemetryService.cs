using Kusto.Data;
using Kusto.Data.Common;
using Kusto.Ingest;
using RecordingStreaming.Interfaces;
using RecordingStreaming.Models;
using System.Text;
using System.Text.Json;

namespace RecordingStreaming.Services
{
    public class TelemetryService : ITelemetryService
    {
        private readonly IConfiguration _configuration;
        private readonly IKustoIngestClient _kustoClient;

        public TelemetryService(IConfiguration configuration)
        {
            _configuration = configuration;
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development") return;
            var ingestConnectionStringBuilder = new KustoConnectionStringBuilder(configuration["Kusto:IngestionUri"])
                .WithAadSystemManagedIdentity();
            _kustoClient = KustoIngestFactory.CreateManagedStreamingIngestClient(ingestConnectionStringBuilder);
        }

        /// <summary>
        /// Log latency
        /// </summary>
        /// <param name="records"></param>
        /// <returns></returns>
        /// <remarks>
        /// To allow data ingestion from a stream, the ingestion policy must be enabled on a target table.  
        /// Use ".show table <tableName> policy streamingingestion" to check the streaming ingestion policy of a table. 
        /// Use ".alter table <tableName> policy streamingingestion enable" to enable the streaming ingestion policy for a table. 
        /// The streaming ingestion also need to be anbled on cluster level through Azure Portal. 
        /// See more details in https://learn.microsoft.com/en-us/azure/data-explorer/ingest-data-streaming?tabs=azure-portal%2Ccsharp
        /// </remarks>
        public async Task<string> LogLatenciesAsync(LatencyRecord[] records)
        {
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                return "Data ingestion is disabled in development environment.";
            }

            // Ingest from a file according to the required properties
            var databaseName = _configuration.GetValue<string>("Kusto:DatabaseName");
            var tableName = _configuration.GetValue<string>("Kusto:TableName");
            var kustoIngestionProperties = new KustoQueuedIngestionProperties(databaseName, tableName)
            {
                // Setting the report level to FailuresAndSuccesses will cause both successful and failed ingestions to be reported
                // (Rather than the default "FailuresOnly" level)
                ReportLevel = IngestionReportLevel.FailuresAndSuccesses,
                // Choose the report method of choice
                ReportMethod = IngestionReportMethod.Table,
                Format = DataSourceFormat.json,
            };

            // Serialize the records to JSON and create a stream from the JSON string
            int streamSize = records.Length * typeof(LatencyRecord).GetProperties().Length * 100;    // per property allocate 100 bytes. 
            var stream = new MemoryStream(streamSize);
            await using (var sw = new StreamWriter(stream, Encoding.UTF8, streamSize, true))
            {
                foreach (var rec in records)
                {
                    rec.timestamp = DateTimeOffset.UtcNow;
                    var recJson = JsonSerializer.Serialize(rec);
                    await sw.WriteLineAsync(recJson);
                }
            }
            stream.Seek(0, SeekOrigin.Begin);

            // Execute the ingest operation and save the result.
            var sourceId = Guid.NewGuid();
            var sourceOptions = new StreamSourceOptions { SourceId = sourceId };
            var clientResult = await _kustoClient.IngestFromStreamAsync(stream, kustoIngestionProperties, sourceOptions);

            // Use the sourceId to get the status. It can be called multiple times if the status is pending.
            var ingestionStatus = clientResult.GetIngestionStatusBySourceId(sourceId);

            return $"Data sent to Kusto with sourceID: {sourceId}. Status is {ingestionStatus.Status}";
        }
    }
}

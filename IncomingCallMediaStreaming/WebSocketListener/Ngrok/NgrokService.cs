// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace WebSocketListener.Ngrok
{
    using Microsoft.Extensions.Logging;
    using System;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Tasks;

    /// <summary>
    /// The NGROK service for starting ngrok process and fetching the assigned forwarding url.
    /// </summary>
    public class NgrokService : IDisposable
    {
        /// <summary>
        /// The NGROK process
        /// </summary>
        private static Process ngrokProcess;
        /// <summary>
        /// Gets the connector.
        /// </summary>
        private static NgrokConnector Connector { set; get; }

        private static ILogger Logger { get; set; } 

        private static readonly NgrokService instance = new NgrokService();

        public static NgrokService Instance
        { 
            get { return instance; } 
        }


        private NgrokService()
        {
            var logFacotry = new LoggerFactory();
            Logger = logFacotry.CreateLogger<NgrokService>();
            Connector = new NgrokConnector();
        }

        public string StartNgrokService(string ngrokPath, string authToken = null)
        {
            if (!NgrokRunning())
            {
                CreateNgrokProcess(ngrokPath, authToken);
            }

            try
            {
                if (string.IsNullOrEmpty(ngrokPath))
                {
                    Logger.LogInformation("Ngrok path not provided");
                    return "";
                }

                Logger.LogInformation("Fetching Ngrok Url");
                var ngrokUrl = GetNgrokUrl().Result;

                Logger.LogInformation($"Ngrok Started with url: {ngrokUrl}");

                return ngrokUrl;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.Message);
                return "";
            }
        }

        public void Dispose()
        {
            if (ngrokProcess != null && !ngrokProcess.HasExited)
            {
                ngrokProcess.Kill();
            }

            ngrokProcess?.Dispose();
        }

        /// <summary>
        /// Ensures that NGROK is not running.
        /// </summary>
        private static bool NgrokRunning()
        {
            var processes = Process.GetProcessesByName("ngrok");
            return processes.Any();
        }

        /// <summary>
        /// Creates the NGROK process.
        /// </summary>
        /// <param name="enableConsole">if set to <c>true</c> [enable console].</param>
        /// <param name="authToken">NGROK authentication token</param>
        private static void CreateNgrokProcess(string ngrokPath, string authToken)
        {
            ngrokProcess = new Process();
            var startInfo = new ProcessStartInfo();

            startInfo.WindowStyle = ProcessWindowStyle.Normal;

            var authTokenArgs = string.Empty;
            if (!string.IsNullOrWhiteSpace(authToken))
            {
                authTokenArgs = $@"--authtoken {authToken}";
            }

            startInfo.FileName = $@"{ngrokPath}\ngrok.exe";
            startInfo.Arguments = $"http http://localhost:8080/ --host-header=\"localhost:8080\" {authTokenArgs}";
            ngrokProcess.StartInfo = startInfo;
            ngrokProcess.Start();
        }

        /// <summary>
        /// Get ngrok url from NGROK web service.
        /// </summary>
        /// <returns>true if operation is successful or else false.</returns>
        public async Task<string> GetNgrokUrl()
        {
            var totalAttempts = 3;
            do
            {
                //Wait for fetching the ngrok url as ngrok process might not be started yet.
                await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

                var tunnels = await Connector.GetAllTunnelsAsync().ConfigureAwait(false);

                if (tunnels?.Count() > 0)
                {
                    return tunnels.First().PublicUrl;
                }
            }
            while (--totalAttempts > 0);

            throw new ApplicationException("Filed to retrieve ngrok url");
        }
    }
}
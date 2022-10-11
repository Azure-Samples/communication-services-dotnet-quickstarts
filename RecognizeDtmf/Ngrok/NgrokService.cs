// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Calling.RecognizeDTMF.Ngrok
{
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
        private Process ngrokProcess;

        public NgrokService(string ngrokPath, string authToken = null)
        {
            this.Connector = new NgrokConnector();

            this.EnsureNgrokNotRunning();
            this.CreateNgrokProcess(ngrokPath, authToken);
        }

        /// <summary>
        /// Gets the connector.
        /// </summary>
        private NgrokConnector Connector { get; }

        public void Dispose()
        {
            if (this.ngrokProcess != null && !this.ngrokProcess.HasExited)
            {
                this.ngrokProcess.Kill();
            }

            this.ngrokProcess?.Dispose();
        }

        /// <summary>
        /// Ensures that NGROK is not running.
        /// </summary>
        private void EnsureNgrokNotRunning()
        {
            var processes = Process.GetProcessesByName("ngrok");
            if (processes.Any())
            {
                throw new ApplicationException("Looks like NGROK is still running. Please kill it before running the provider again.");
            }
        }

        /// <summary>
        /// Creates the NGROK process.
        /// </summary>
        /// <param name="enableConsole">if set to <c>true</c> [enable console].</param>
        /// <param name="authToken">NGROK authentication token</param>
        private void CreateNgrokProcess(string ngrokPath, string authToken)
        {
            this.ngrokProcess = new Process();
            var startInfo = new ProcessStartInfo();

            startInfo.WindowStyle = ProcessWindowStyle.Normal;

            var authTokenArgs = string.Empty;
            if (!string.IsNullOrWhiteSpace(authToken))
            {
                authTokenArgs = $@"--authtoken {authToken}";
            }

            startInfo.FileName = $@"{ngrokPath}\ngrok.exe";
            startInfo.Arguments = $"http http://localhost:9007/ --host-header=\"localhost:9007\" {authTokenArgs}";
            this.ngrokProcess.StartInfo = startInfo;
            this.ngrokProcess.Start();
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

                var tunnels = await this.Connector.GetAllTunnelsAsync().ConfigureAwait(false);

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
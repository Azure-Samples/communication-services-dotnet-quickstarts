// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Calling.RecognizeDTMF.Ngrok
{
    using System;
    using System.Collections.Generic;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;
    using Newtonsoft.Json;

    /// <summary>
    /// The class that can connect to local ngrok for APIs.
    /// </summary>
    internal class NgrokConnector
    {
        private const string TunnelsRoute = "api/tunnels";

        private static readonly Uri NgrokUrl = new Uri("http://127.0.0.1:4040");

        /// <summary>
        /// The HTTP client.
        /// </summary>
        private readonly HttpClient httpClient;

        /// <summary>
        /// Initializes a new instance of the <see cref="NgrokConnector"/> class.
        /// </summary>
        public NgrokConnector()
        {
            this.httpClient = new HttpClient()
            {
                BaseAddress = NgrokUrl,
            };

            this.httpClient.DefaultRequestHeaders.Accept.Add(
                new MediaTypeWithQualityHeaderValue("application/json"));
        }

        /// <summary>
        /// Gets all the tunnels asynchronous.
        /// </summary>
        /// <returns>List of tunnels</returns>
        public async Task<IEnumerable<Tunnel>> GetAllTunnelsAsync()
        {
            var response = await this.httpClient.GetAsync(TunnelsRoute).ConfigureAwait(false);
            var responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            return JsonConvert.DeserializeObject<Tunnels>(responseString).TunnelList;
        }

    }

    /// <summary>
    /// The structure for all tunnels.
    /// </summary>
    public class Tunnels
    {
        /// <summary>
        /// Gets or sets the tunnels.
        /// </summary>
        [JsonProperty("tunnels")]
        public IEnumerable<Tunnel> TunnelList { get; set; }

        /// <summary>
        /// Gets or sets the URI.
        /// </summary>
        public string Uri { get; set; }
    }

    /// <summary>
    /// Description of an ngrok tunnel
    /// </summary>
    public class Tunnel
    {
        /// <summary>
        /// Gets or sets the name of the tunnel.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the URI for the tunnel.
        /// </summary>
        public string Uri { get; set; }

        /// <summary>
        /// Gets or sets the public URL for the tunnel.
        /// </summary>
        [JsonProperty("public_url")]
        public string PublicUrl { get; set; }

        /// <summary>
        /// Gets or sets the tunnel protocol.
        /// </summary>
        [JsonProperty("proto")]
        public string Protocol { get; set; }


    }
}
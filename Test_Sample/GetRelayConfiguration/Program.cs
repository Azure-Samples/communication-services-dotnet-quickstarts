using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Azure;
using Azure.Communication;
using Azure.Communication.Identity;
using Azure.Communication.NetworkTraversal;
using Microsoft.MixedReality.WebRTC;

namespace GetRelayConfiguration
{
    class Program
    {
        private static CommunicationRelayClient _communicationRelayClient;
        private static CommunicationIdentityClient _communicationIdentityClient;

        static async Task Main(string[] args)
        {
            // Get a connection string to our Azure Communication resource.
            var connectionString = "endpoint=https://smstestapp.communication.azure.com/;accesskey=RGrzrWY80oV4a1KgTHXJ3mJAC/8V7mubH26IjYjF+LXYVtedhw1DejYxPc9wkIPC0b0vtA7oXDsmGxME2N5Ccg==";
            _communicationIdentityClient = new CommunicationIdentityClient(connectionString);
            _communicationRelayClient = new CommunicationRelayClient(connectionString);

            Console.WriteLine("Get a relay configuration");
            var relayConfiguration = GetRelayConfiguration();

            Console.WriteLine("Get a relay configuration passing a User Identity");
            GetRelayConfigurationWithIdentity();

            Console.WriteLine("Get a relay configuration passing a Route Type");
            GetRelayConfigurationWithNearestRouteType();

            // Now we can set up a WebRTC Peer Connection using the relay configuration
            var config = GetPeerConnectionConfiguration(relayConfiguration);
            PeerConnection peerConnection = new PeerConnection();
            _ = peerConnection.InitializeAsync(config);
        }

        public static CommunicationRelayConfiguration GetRelayConfiguration()
        {
            Response<CommunicationRelayConfiguration> relayConfiguration = _communicationRelayClient.GetRelayConfiguration();
            DateTimeOffset turnTokenExpiresOn = relayConfiguration.Value.ExpiresOn;
            IReadOnlyList<CommunicationIceServer> iceServers = (IReadOnlyList<CommunicationIceServer>)relayConfiguration.Value.IceServers;
            
            Console.WriteLine($"Expires On: {turnTokenExpiresOn}");
            foreach (CommunicationIceServer iceServer in iceServers)
            {
                foreach (string url in iceServer.Urls)
                {
                    Console.WriteLine($"ICE Server Url: {url}");
                }
                Console.WriteLine($"ICE Server Username: {iceServer.Username}");
                Console.WriteLine($"ICE Server Credential: {iceServer.Credential}");
                Console.WriteLine($"ICE Server RouteType: {iceServer.RouteType}");
            }

            return relayConfiguration;
        }

        public static CommunicationRelayConfiguration GetRelayConfigurationWithIdentity()
        {
            Response<CommunicationUserIdentifier> response = _communicationIdentityClient.CreateUser();
            var user = response.Value;

            Response<CommunicationRelayConfiguration> relayConfiguration = _communicationRelayClient.GetRelayConfiguration(response);
            DateTimeOffset turnTokenExpiresOn = relayConfiguration.Value.ExpiresOn;
            IReadOnlyList<CommunicationIceServer> iceServers = (IReadOnlyList<CommunicationIceServer>)relayConfiguration.Value.IceServers;
            Console.WriteLine($"Expires On: {turnTokenExpiresOn}");
            foreach (CommunicationIceServer iceServer in iceServers)
            {
                foreach (string url in iceServer.Urls)
                {
                    Console.WriteLine($"ICE Server Url: {url}");
                }
                Console.WriteLine($"ICE Server Username: {iceServer.Username}");
                Console.WriteLine($"ICE Server Credential: {iceServer.Credential}");
                Console.WriteLine($"ICE Server RouteType: {iceServer.RouteType}");
            }

            return relayConfiguration;
        }

        public static CommunicationRelayConfiguration GetRelayConfigurationWithNearestRouteType()
        {
            Response<CommunicationRelayConfiguration> relayConfiguration = _communicationRelayClient.GetRelayConfiguration(routeType: RouteType.Nearest);
            DateTimeOffset turnTokenExpiresOn = relayConfiguration.Value.ExpiresOn;
            
            IReadOnlyList<CommunicationIceServer> iceServers = (IReadOnlyList<CommunicationIceServer>)relayConfiguration.Value.IceServers;
            Console.WriteLine($"Expires On: {turnTokenExpiresOn}");
            foreach (CommunicationIceServer iceServer in iceServers)
            {
                foreach (string url in iceServer.Urls)
                {
                    Console.WriteLine($"ICE Server Url: {url}");
                }
                Console.WriteLine($"ICE Server Username: {iceServer.Username}");
                Console.WriteLine($"ICE Server Credential: {iceServer.Credential}");
                Console.WriteLine($"ICE Server RouteType: {iceServer.RouteType}");
            }

            return relayConfiguration;
        }
        
        public static CommunicationRelayConfiguration GetRelayConfigurationWithTtl()
        {
            Response<CommunicationRelayConfiguration> relayConfiguration = _communicationRelayClient.GetRelayConfiguration(ttl: 5000);
            DateTimeOffset turnTokenExpiresOn = relayConfiguration.Value.ExpiresOn;

            IReadOnlyList<CommunicationIceServer> iceServers = (IReadOnlyList<CommunicationIceServer>)relayConfiguration;
            Console.WriteLine($"Expires On: {turnTokenExpiresOn}");
            foreach (CommunicationIceServer iceServer in iceServers)
            {
                foreach (string url in iceServer.Urls)
                {
                    Console.WriteLine($"ICE Server Url: {url}");
                }
                Console.WriteLine($"ICE Server Username: {iceServer.Username}");
                Console.WriteLine($"ICE Server Credential: {iceServer.Credential}");
                Console.WriteLine($"ICE Server RouteType: {iceServer.RouteType}");
            }

            return relayConfiguration;
        }

        public static PeerConnectionConfiguration GetPeerConnectionConfiguration(CommunicationRelayConfiguration relayConfiguration)
        {
            IReadOnlyList<CommunicationIceServer> iceServers = (IReadOnlyList<CommunicationIceServer>)relayConfiguration.IceServers;
            var webRTCIceServers = new List<IceServer>();

            foreach (CommunicationIceServer iceServer in iceServers)
            {
                var webRTCIceServer = new IceServer();
                
                webRTCIceServer.Urls = (List<string>)iceServer.Urls;
                webRTCIceServer.TurnUserName = iceServer.Username;
                webRTCIceServer.TurnPassword = iceServer.Credential;
                
                webRTCIceServers.Add(webRTCIceServer);
            }

            PeerConnectionConfiguration configuration = new PeerConnectionConfiguration();
            configuration.IceServers = webRTCIceServers;
            return configuration;
        }
    }
}

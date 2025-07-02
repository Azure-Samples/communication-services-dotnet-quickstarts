using System.Security.Cryptography;
using System.Text;
using System.Net.Http.Headers;
using Newtonsoft.Json;

namespace EntraIdUsersSupportQuickstart
{
    /*
     * This class provides methods to manage Microsoft Entra ID user access assignments 
     * for Azure Communication Services (ACS) resources.
     * It demonstrates how to list, create/update, get, and delete access assignments by making authenticated HTTP requests 
     * to the ACS REST APIs.
     */
    class ManageEntraUserAccess
    {
        // Replace <ACS_Resource_Endpoint> and <ACS_Resource_Key> with your actual ACS resource endpoint and key
        // You can find these in the Azure portal under your Communication Services resource
        private static string endpoint = "<ACS_Resource_Endpoint>";
        private static string key = "<ACS_Resource_Key>";
        private static string apiVersion = "2025-03-02-preview";

        public static async Task ManageAccessAsync()
        {
            // Replace with the actual object ID
            string objectId = "<OBJECT_ID>";
            // Replace with the actual tenant ID
            string tenantId = "<TENANT_ID>";
            // Replace with the actual client ID
            string clientId = "<CLIENT_ID>";

            var assignment = new
            {
                tenantId = tenantId,
                // Type of the principal accessing the resource. Possible values are: "user", "group" or "tenant".
                principalType = "user",
                clientIds = new[] { clientId }
            };

            try
            {
                var assignments = await ListAssignments();
                Console.WriteLine("Assignments: " + assignments);

                var createResponse = await CreateOrUpdateAssignment(objectId, assignment);
                Console.WriteLine("Create or Update Assignment Response: " + createResponse);

                var getResponse = await GetAssignment(objectId);
                Console.WriteLine("Get Assignment Response: " + getResponse);

                var updateAssignmentsDict = new Dictionary<string, object>
                {
                    [objectId] = new
                    {
                        principalType = "group",
                        tenantId = tenantId,
                        clientIds = new[] { clientId }
                    }
                };
                var updateResponse = await UpdateAssignments(updateAssignmentsDict);
                Console.WriteLine("Update Assignments Response: " + updateResponse);

                var deleteResponse = await DeleteAssignment(objectId);
                Console.WriteLine("Delete Assignment Response: " + deleteResponse);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error: " + ex);
            }
        }

        static async Task<string> ListAssignments()
        {
            string apiUrl = $"{endpoint.TrimEnd('/')}/access/entra/assignments?api-version={apiVersion}";
            using var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
            AddSignedHeaders(request, apiUrl, "GET", "");
            var response = await client.SendAsync(request);
            return await response.Content.ReadAsStringAsync();
        }

        static async Task<string> CreateOrUpdateAssignment(string objectId, object assignment)
        {
            string path = $"/access/entra/assignments/{objectId}?api-version={apiVersion}";
            string apiUrl = $"{endpoint.TrimEnd('/')}{path}";
            string body = JsonConvert.SerializeObject(assignment);
            using var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Put, apiUrl)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
            AddSignedHeaders(request, apiUrl, "PUT", body);
            var response = await client.SendAsync(request);
            return await response.Content.ReadAsStringAsync();
        }

        static async Task<string> UpdateAssignments(Dictionary<string, object> assignments)
        {
            string path = $"/access/entra/assignments?api-version={apiVersion}";
            string apiUrl = $"{endpoint.TrimEnd('/')}{path}";
            string body = JsonConvert.SerializeObject(assignments);
            using var client = new HttpClient();
            var request = new HttpRequestMessage(new HttpMethod("PATCH"), apiUrl)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/merge-patch+json")
            };
            AddSignedHeaders(request, apiUrl, "PATCH", body);
            var response = await client.SendAsync(request);
            return await response.Content.ReadAsStringAsync();
        }

        static async Task<string> GetAssignment(string objectId)
        {
            string path = $"/access/entra/assignments/{objectId}?api-version={apiVersion}";
            string apiUrl = $"{endpoint.TrimEnd('/')}{path}";
            using var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Get, apiUrl);
            AddSignedHeaders(request, apiUrl, "GET", "");
            var response = await client.SendAsync(request);
            return await response.Content.ReadAsStringAsync();
        }

        static async Task<string> DeleteAssignment(string objectId)
        {
            string path = $"/access/entra/assignments/{objectId}?api-version={apiVersion}";
            string apiUrl = $"{endpoint.TrimEnd('/')}{path}";
            using var client = new HttpClient();
            var request = new HttpRequestMessage(HttpMethod.Delete, apiUrl);
            AddSignedHeaders(request, apiUrl, "DELETE", "");
            var response = await client.SendAsync(request);
            return await response.Content.ReadAsStringAsync();
        }

        static void AddSignedHeaders(HttpRequestMessage request, string url, string method, string body)
        {
            string verb = method.ToUpper();
            string utcNow = DateTime.UtcNow.ToString("r");
            string contentHash;
            using (var sha256 = SHA256.Create())
            {
                contentHash = Convert.ToBase64String(sha256.ComputeHash(Encoding.UTF8.GetBytes(body ?? "")));
            }
            string dateHeader = "x-ms-date";
            string signedHeaders = $"{dateHeader};host;x-ms-content-sha256";

            var urlObj = new Uri(url);
            string query = urlObj.Query;
            string urlPathAndQuery = urlObj.AbsolutePath + query;
            string hostAndPort = urlObj.IsDefaultPort ? urlObj.Host : $"{urlObj.Host}:{urlObj.Port}";

            string stringToSign = $"{verb}\n{urlPathAndQuery}\n{utcNow};{hostAndPort};{contentHash}";
            string signature;
            using (var hmac = new HMACSHA256(Convert.FromBase64String(key)))
            {
                signature = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(stringToSign)));
            }

            request.Headers.Host = hostAndPort;
            request.Headers.Add(dateHeader, utcNow);
            request.Headers.Add("x-ms-content-sha256", contentHash);
            request.Headers.Authorization = new AuthenticationHeaderValue("HMAC-SHA256", $"SignedHeaders={signedHeaders}&Signature={signature}");
        }
    }
}

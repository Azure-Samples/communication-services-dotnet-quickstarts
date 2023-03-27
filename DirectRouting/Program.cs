using Azure.Communication.PhoneNumbers.SipRouting;

var connectionString = "endpoint=https://<RESOURCE_NAME>.communication.azure.com/;accesskey=<ACCESS_KEY>";
var client = new SipRoutingClient(connectionString);

var usSbcFqdn = "sbc.us.contoso.com";
var euSbcFqdn = "sbc.eu.contoso.com";
var sbcPort = 1234;
var usTrunk = new SipTrunk(usSbcFqdn, sbcPort);
var euTrunk = new SipTrunk(euSbcFqdn, sbcPort);

var response = await client.SetTrunksAsync(new List<SipTrunk> { usTrunk, euTrunk });

Console.WriteLine($"SetTrunks operation result: { response.Status }.{Environment.NewLine}Trunks: { response.Content }");

var usRoute = new SipTrunkRoute("UsRoute", "^\\+1(\\d{10})$", trunks: new List<string> { usSbcFqdn });
var defaultRoute = new SipTrunkRoute("DefaultRoute", "^\\+\\d+$", trunks: new List<string> { usSbcFqdn, euSbcFqdn });

response = await client.SetRoutesAsync(new List<SipTrunkRoute> { usRoute, defaultRoute });

Console.WriteLine($"SetRoutes operation result: {response.Status}.{Environment.NewLine}Routes: {response.Content}");

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using Dasync.Collections;
using Serilog;

namespace dotnet_5_httpclient_rest_bind
{
    public class BindTester
    {
        private readonly ILogger _logger;

        public readonly ConcurrentBag<IPAddress> PublicInterfaces;

        public readonly ConcurrentBag<IPAddress> PrivateInterfaces;

        public readonly List<IPAddress> DiscoveredAddresses;

        public BindTester(ILogger logger)
        {
            _logger = logger;
            PublicInterfaces = new ConcurrentBag<IPAddress>();
            PrivateInterfaces = new ConcurrentBag<IPAddress>();
            DiscoveredAddresses = new List<IPAddress>();
        }

        public async Task DiscoverInterfacesAsync()
        {
            foreach (var @interface in NetworkInterface.GetAllNetworkInterfaces())
            {
                var properties = @interface.GetIPProperties();

                if (@interface.NetworkInterfaceType == NetworkInterfaceType.Loopback || // if it's a loopback interface.
                    @interface.OperationalStatus != OperationalStatus.Up || // or is down.
                    !@interface.Supports(NetworkInterfaceComponent.IPv4) || // or doesn't support ipv4.
                    properties.GatewayAddresses.Count == 0 || // or has no gateways
                    properties.DnsAddresses.Count == 0) // or has no dns servers.
                {
                    continue; // just skip it.
                }

                // an interface may have multiple ip addresses set, check all of them.
                var addresses = @interface.GetIPProperties().UnicastAddresses.Where(x => x.Address.AddressFamily == AddressFamily.InterNetwork);

                foreach (var entry in addresses)
                {
                    DiscoveredAddresses.Add(entry.Address);
                }
            }

            _logger.Information("testing {Count} interfaces..", DiscoveredAddresses.Count);

            // loop through all available ip addresses in interface.
            await DiscoveredAddresses.ParallelForEachAsync(async entry =>
            {
                var isPublic = await TestInterface(entry);
                if (isPublic) PublicInterfaces.Add(entry);
                else PrivateInterfaces.Add(entry);
            }, maxDegreeOfParallelism: 10);
        }

        private async Task<bool> TestInterface(IPAddress ipAddress)
        {
            try
            {
                _logger.Information("testing interface: {Interface}..", ipAddress);

                var handler = new HttpClientHandler();
                var socketsHandler = (SocketsHttpHandler)GetUnderlyingSocketsHttpHandler(handler);
                socketsHandler.ConnectCallback = async (context, token) =>
                {
                    var s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                    s.Bind(new IPEndPoint(ipAddress, 0));
                    await s.ConnectAsync(context.DnsEndPoint, token);
                    s.NoDelay = true;
                    return new NetworkStream(s, ownsSocket: true);
                };

                var client = new HttpClient(handler)
                {
                    Timeout = new TimeSpan(0, 0, 30)
                };

                var response = await client.GetFromJsonAsync<JsonElement>("https://httpbin.org/ip");
                var origin = response.GetProperty("origin").GetString();

                _logger.Verbose(
                    ipAddress.ToString() == origin
                        ? "found public ip: {IpAddress} [request ip address: {Origin}]"
                        : "found private ip: {IpAddress} [request ip address: {Origin}]",
                    ipAddress, origin);

                return ipAddress.ToString() == origin;
            }
            catch (Exception e)
            {
                _logger.Error("error testing interface: {Interface} reason: {Message}", ipAddress.ToString(), e.Message);
                return false;
            }
        }

        protected static object GetUnderlyingSocketsHttpHandler(HttpClientHandler handler)
        {
            var field = typeof(HttpClientHandler).GetField("_underlyingHandler", BindingFlags.Instance | BindingFlags.NonPublic);
            return field?.GetValue(handler);
        }
    }
}

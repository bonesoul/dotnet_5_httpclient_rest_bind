using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading.Tasks;
using Serilog;
using Serilog.Events;
using dotnet_5_httpclient_rest_bind;
    
var logger = new LoggerConfiguration()
.MinimumLevel.Verbose()
.WriteTo.Console()
.CreateLogger();

var tester = new BindTester(logger);
await tester.DiscoverInterfacesAsync();

logger.Information("public interfaces: {List}", tester.PublicInterfaces);
logger.Information("private interfaces: {List}", tester.PrivateInterfaces);
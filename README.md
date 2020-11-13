# dotnet_5_httpclient_rest_bind

an example for .net 5.0's new [connection abstractions api](https://github.com/dotnet/runtime/issues/1793).

prior to .net 5.0 (anything including up to dotnet core 3.x) it was not possible to bind a local interface to httpclient. with .net 5.0, you can now use an local interface in system so that outgoing requests will be using the interface -- which was a [long requested feature](https://github.com/dotnet/runtime/issues/23267).

this example code will be discovering all interfaces in the system and for everyone of them will bind httpclient and make a request to [https://httpbin.org/ip](https://httpbin.org/ip) which will return a response similar to:

```
{
  "origin": "141.196.162.15"
}
```

the code will then read the origin and compare it to actual local interface we binded to. if they are same, will the interface as public. otherwise will mark the interface as private -- as this means the interface is probably behind a NAT. 

# requirements

- .net 5.0+.
- tested on debian 10 and windows 10. should also work in macos systems.

# run

you can run the sample with: 

```
dotnet run dotnet_5_httpclient_rest_bind.csproj 
```

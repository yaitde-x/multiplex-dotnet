
This research was about System.IO.Pipelines vs System.IO.Stream and using them to multiplex datastreams. At the time, my company was building a Cloud -> OnPrem relay using Azure Relay. 

We were using PortBridge as a reference and the code is a bit dated. It still uses Begin/End Async patterns so if you want to see what it was like to read asynchronous code back in the day, check it out.

https://github.com/Azure/azure-relay/tree/master/samples/hybrid-connections/dotnet/portbridge

I remember being intensely focused on this project for a few weeks :). Then I went back to whatever I was doing before.

Perhaps you will find it useful.

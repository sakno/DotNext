.NEXT 3.x
====

.NEXT (dotNext) is the family of powerful libaries aimed to improve development productivity and extend .NET API with unique features which potentially will be implemented in the next versions of C# compiler or .NET Runtime.

> [!IMPORTANT]
> The repo has been transferred to .NET Foundation. This documentation is no longer maintained. Up-to-date version of documentation is located [here](https://dotnet.github.io/dotNext). The sources are [here](https://github.com/dotnet/dotNext).

This chapter gives quick overview of these libraries. Read [articles](./features/core/index.md) for closer look at all available features.

Prerequisites:
* Runtime: .NET 5 or any runtime compatible with .NET Standard 2.1
* OS: Linux, Windows, MacOS
* Architecture: any if supported by underlying .NET Runtime

# DotNext
<a href="https://www.nuget.org/packages/dotnext/absoluteLatest"><img src="https://img.shields.io/nuget/v/dotnext.svg?logo=NuGet"></a> ![Downloads](https://img.shields.io/nuget/dt/dotnext.svg)<br/>
This library is the core of .NEXT which extends .NET standard library with
  * Enum API to work with arbitrary **enum** types
  * Thread-safe advanced atomic operations to work with **int**, **long**, **bool**, **double**, **float**, **IntPtr**, arbitrary reference and value types
  * Unified representation of various synchronization primitives in the form of the lock
  * Generic specialization with constant values
  * Generation of random strings
  * Low-level methods to work with value types
  * Fast comparison of arrays
  * Ad-hoc user data associated with arbitrary object
  * Rich set of advanced buffer types

# DotNext.Reflection
<a href="https://www.nuget.org/packages/dotnext.reflection/absoluteLatest"><img src="https://img.shields.io/nuget/v/dotnext.reflection.svg?logo=NuGet"></a>  ![Downloads](https://img.shields.io/nuget/dt/dotnext.reflection.svg)<br/>
.NET Reflection is slow because relies on late-bound calls when every actual argument should be validated. There is alternative approach: dynamic code generation optimized for every member call. Reflection library from .NEXT family provides provides fully-featured fast reflection using dynamic code generation. Invocation cost is comparable to direct call. Check [Benchmarks](benchmarks.md) to see how it is fast.

Additionally, the library provides support of [Type Classes](https://github.com/dotnet/csharplang/issues/110). You don't need to wait C# language of version _X_ to obtain this feature.

# DotNext.Metaprogramming
<a href="https://www.nuget.org/packages/dotnext.metaprogramming/absoluteLatest"><img src="https://img.shields.io/nuget/v/dotnext.metaprogramming.svg?logo=NuGet"></a>  ![Downloads](https://img.shields.io/nuget/dt/dotnext.metaprogramming.svg)<br/>
This library provides a rich API to write and execute code on-the-fly. It extends [C# Expression Tree](https://docs.microsoft.com/en-us/dotnet/csharp/programming-guide/concepts/expression-trees/) programming model with ordinary things for C# such as `foreach` loop, `for` loop, `while` loop, `using` statement, `lock` statement, string interpolation and even asynchronous lambda expressions with full support of `async`/`await` semantics.

# DotNext.Unsafe
<a href="https://www.nuget.org/packages/dotnext.unsafe/absoluteLatest"><img src="https://img.shields.io/nuget/v/dotnext.unsafe.svg?logo=NuGet"></a>  ![Downloads](https://img.shields.io/nuget/dt/dotnext.unsafe.svg)<br/>
This library provides a special types to work with unmanaged memory in safe manner:
* Structured access to unmanaged memory
* Unmanaged [memory pool](https://docs.microsoft.com/en-us/dotnet/api/system.buffers.memorypool-1)
* Interop with unmanaged memory via [Memory](https://docs.microsoft.com/en-us/dotnet/api/system.memory-1) value type
* CLS-compliant generic pointer type for .NET languages without direct support of such type. Use this feature to work with pointers from VB.NET or F#.
* Atomic thread-safe operations applicable to data placed into unmanaged memory: increment, decrement, compare-and-set etc, volatile access

# DotNext.Threading
<a href="https://www.nuget.org/packages/dotnext.threading/absoluteLatest"><img src="https://img.shields.io/nuget/v/dotnext.threading.svg?logo=NuGet"></a>  ![Downloads](https://img.shields.io/nuget/dt/dotnext.threading.svg)<br/>
The library allows you to reuse experience of blocking synchronization with help of [ReaderWriteLockSlim](https://docs.microsoft.com/en-us/dotnet/api/system.threading.readerwriterlockslim), [CountdownEvent](https://docs.microsoft.com/en-us/dotnet/api/system.threading.countdownevent) and friends in asynchronous code using their alternatives such as asynchronous locks.

The following code describes these alternative implementations of synchronization primitives for asynchronous code:

| Synchronization Primitive | Asynchronous Version |
| ---- | ---- |
| [ReaderWriteLockSlim](https://docs.microsoft.com/en-us/dotnet/api/system.threading.readerwriterlockslim) | [AsyncReaderWriterLock](xref:DotNext.Threading.AsyncReaderWriterLock) |
| [Monitor](https://docs.microsoft.com/en-us/dotnet/api/system.threading.monitor) | [AsyncExclusiveLock](xref:DotNext.Threading.AsyncExclusiveLock)
| [ManualResetEvent](https://docs.microsoft.com/en-us/dotnet/api/system.threading.manualresetevent) | [AsyncManualResetEvent](xref:DotNext.Threading.AsyncManualResetEvent)
| [AutoResetEvent](https://docs.microsoft.com/en-us/dotnet/api/system.threading.autoresetevent) | [AsyncAutoResetEvent](xref:DotNext.Threading.AsyncAutoResetEvent)
| [Barrier](https://docs.microsoft.com/en-us/dotnet/api/system.threading.barrier) | [AsyncBarrier](xref:DotNext.Threading.AsyncBarrier)
| [CountdownEvent](https://docs.microsoft.com/en-us/dotnet/api/system.threading.countdownevent) | [AsyncCountdownEvent](xref:DotNext.Threading.AsyncCountdownEvent)

But this is not all features of this library. Read more [here](./features/threading/index.md).

# DotNext.IO
<a href="https://www.nuget.org/packages/dotnext.io/absoluteLatest"><img src="https://img.shields.io/nuget/v/dotnext.io.svg?logo=NuGet"></a>  ![Downloads](https://img.shields.io/nuget/dt/dotnext.io.svg)<br/>
Extending streams and I/O pipelines with methods for reading and writing typed values including strings asynchronously. Arbitrary character encodings are supported. Now encoding or decoding data using [pipes](https://docs.microsoft.com/en-us/dotnet/api/system.io.pipelines.pipe) is much easier and comparable with [BinaryWriter](https://docs.microsoft.com/en-us/dotnet/api/system.io.binarywriter) or [BinaryReader](https://docs.microsoft.com/en-us/dotnet/api/system.io.binaryreader). Read more [here](features/io/index.md).

# DotNext.Net.Cluster
<a href="https://www.nuget.org/packages/dotnext.net.cluster/absoluteLatest"><img src="https://img.shields.io/nuget/v/dotnext.net.cluster.svg?logo=NuGet"></a>  ![Downloads](https://img.shields.io/nuget/dt/dotnext.net.cluster.svg)<br/>
Provides rich framework for building [clustered microservices](https://en.wikipedia.org/wiki/Computer_cluster) based on network consensus and distributed messaging. It includes transport-agnostic implementation of [Raft Consensus Algorithm](https://raft.github.io/) that can be adopted for any communication protocol, TCP/UDP transports for Raft, high-performance persistent Write-Ahead Log suitable for general-purpose usage.

# DotNext.AspNetCore.Cluster
<a href="https://www.nuget.org/packages/dotnext.aspnetcore.cluster/absoluteLatest"><img src="https://img.shields.io/nuget/v/dotnext.aspnetcore.cluster.svg?logo=NuGet"></a>  ![Downloads](https://img.shields.io/nuget/dt/dotnext.aspnetcore.cluster.svg)<br/>
Allows to build clustered microservices which rely on network consensus and distributed messaging with [ASP.NET Core](https://docs.microsoft.com/en-us/aspnet/core/) framework. This library contains HTTP-based implementation of [Raft](https://raft.github.io/) Consensus Algorithm, HTTP-based distributed messaging across cluster nodes, cluster leader detection, automatic redirection to leader node and many other things.

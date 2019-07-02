﻿using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using IServer = Microsoft.AspNetCore.Hosting.Server.IServer;

namespace DotNext.Net.Cluster.Consensus.Raft.Http
{
    using Messaging;

    internal abstract class RaftHttpCluster : RaftCluster<RaftClusterMember>, IHostedService, IHostingContext, IExpandableCluster, IMessageBus
    {
        private const string RaftClientHandlerName = "raftClient";
        private delegate ICollection<IPEndPoint> HostingAddressesProvider();

        private readonly IRaftClusterConfigurator configurator;
        private readonly IMessageHandler messageHandler;

        private readonly IDisposable configurationTracker;
        private volatile MemberMetadata metadata;
        private volatile ISet<IPNetwork> allowedNetworks;

        [SuppressMessage("Usage", "CA2213", Justification = "This object is disposed via RaftCluster.members collection")]
        private RaftClusterMember localMember;
        private readonly HostingAddressesProvider hostingAddresses;
        private readonly IHttpMessageHandlerFactory httpHandlerFactory;
        private protected readonly TimeSpan RequestTimeout;


        [SuppressMessage("Reliability", "CA2000", Justification = "The member will be disposed in RaftCluster.Dispose method")]
        private RaftHttpCluster(RaftClusterMemberConfiguration config, out MutableMemberCollection members)
            : base(config, out members)
        {
            allowedNetworks = config.AllowedNetworks;
            metadata = new MemberMetadata(config.Metadata);
            RequestTimeout = TimeSpan.FromMilliseconds(config.LowerElectionTimeout);
        }

        private RaftHttpCluster(IOptionsMonitor<RaftClusterMemberConfiguration> config, IServiceProvider dependencies, out MutableMemberCollection members)
            : this(config.CurrentValue, out members)
        {
            configurator = dependencies.GetService<IRaftClusterConfigurator>();
            messageHandler = dependencies.GetService<IMessageHandler>();
            AuditTrail = dependencies.GetService<IPersistentState>();
            hostingAddresses = dependencies.GetRequiredService<IServer>().GetHostingAddresses;
            httpHandlerFactory = dependencies.GetService<IHttpMessageHandlerFactory>();
            var loggerFactory = dependencies.GetRequiredService<ILoggerFactory>();
            Logger = loggerFactory.CreateLogger(GetType());
            //track changes in configuration
            configurationTracker = config.OnChange(ConfigurationChanged);
        }

        private protected RaftHttpCluster(IServiceProvider dependencies, out MutableMemberCollection members)
            : this(dependencies.GetRequiredService<IOptionsMonitor<RaftClusterMemberConfiguration>>(), dependencies, out members)
        {
        }

        private protected abstract RaftClusterMember CreateMember(Uri address);

        protected override ILogger Logger { get; }

        ILogger IHostingContext.Logger => Logger;

        IReadOnlyCollection<IAddressee> IMessageBus.Members => Members;

        IAddressee IMessageBus.Leader => Leader;

        private async void ConfigurationChanged(RaftClusterMemberConfiguration configuration, string name)
        {
            metadata = new MemberMetadata(configuration.Metadata);
            allowedNetworks = configuration.AllowedNetworks;
            await ChangeMembersAsync((in MutableMemberCollection members) =>
            {
                var existingMembers = new HashSet<Uri>();
                //remove members
                foreach (var holder in members)
                    if (configuration.Members.Contains(holder.Member.BaseAddress))
                        existingMembers.Add(holder.Member.BaseAddress);
                    else
                    {
                        var member = holder.Remove();
                        MemberRemoved?.Invoke(this, member);
                        member.CancelPendingRequests();
                    }

                //add new members
                foreach (var memberUri in configuration.Members)
                    if (!existingMembers.Contains(memberUri))
                    {
                        var member = CreateMember(memberUri);
                        members.Add(member);
                        MemberAdded?.Invoke(this, member);
                    }

                existingMembers.Clear();
            }).ConfigureAwait(false);
        }

        async Task<TResponse> IMessageBus.SendMessageToLeaderAsync<TResponse>(IMessage message, MessageReader<TResponse> responseReader, CancellationToken token)
        {
            do
            {
                var leader = Leader;
                if(leader is null)
                    throw new InvalidOperationException(ExceptionMessages.LeaderIsUnavailable);
                try
                {
                    return await leader.SendMessageAsync(message, responseReader, true, token).ConfigureAwait(false);
                }
                catch(MemberUnavailableException)
                {
                }
                catch(UnexpectedStatusCodeException e) when (e.StatusCode == HttpStatusCode.BadRequest) //keep in sync with ReceiveMessage behavior
                {
                }
            }
            while(!token.IsCancellationRequested);
            throw new OperationCanceledException(token);
        }

        async Task IMessageBus.SendSignalToLeaderAsync(IMessage message, CancellationToken token)
        {
            do
            {
                var leader = Leader;
                if(leader is null)
                    throw new InvalidOperationException(ExceptionMessages.LeaderIsUnavailable);
                try
                {
                    await leader.SendSignalAsync(message, true, true, token).ConfigureAwait(false);
                }
                catch(MemberUnavailableException)
                {
                    continue;
                }
                catch(UnexpectedStatusCodeException e) when (e.StatusCode == HttpStatusCode.ServiceUnavailable) //keep in sync with ReceiveMessage behavior
                {
                    continue;
                }
                return;
            }
            while(!token.IsCancellationRequested);
            throw new OperationCanceledException(token);
        }

        IReadOnlyDictionary<string, string> IHostingContext.Metadata => metadata;

        bool IHostingContext.IsLeader(IRaftClusterMember member) => ReferenceEquals(Leader, member);

        IPEndPoint IHostingContext.LocalEndpoint => localMember?.Endpoint;

        HttpMessageHandler IHostingContext.CreateHttpHandler()
            => httpHandlerFactory?.CreateHandler(RaftClientHandlerName) ?? new HttpClientHandler();

        public event ClusterChangedEventHandler MemberAdded;
        public event ClusterChangedEventHandler MemberRemoved;

        public override Task StartAsync(CancellationToken token)
        {
            //detect local member
            localMember = FindMember(hostingAddresses().Contains);
            if (localMember is null)
                throw new RaftProtocolException(ExceptionMessages.UnresolvedLocalMember);
            configurator?.Initialize(this, metadata);
            return base.StartAsync(token);
        }

        public override Task StopAsync(CancellationToken token)
        {
            configurator?.Shutdown(this);
            return base.StopAsync(token);
        }

        private async Task ReceiveVote(RequestVoteMessage request, HttpResponse response)
        {
            var sender = FindMember(request.Sender.Represents);
            if (sender is null)
                await request.SaveResponse(response, new Result<bool>(Term, false)).ConfigureAwait(false);
            else
            {
                await request.SaveResponse(response,
                    await ReceiveVote(sender, request.ConsensusTerm, request.LastLogIndex, request.LastLogTerm)
                        .ConfigureAwait(false)).ConfigureAwait(false);
                sender.Touch();
            }
        }

        private async Task Resign(ResignMessage request, HttpResponse response)
        {
            var sender = FindMember(request.Sender.Represents);
            await request.SaveResponse(response, await ReceiveResign().ConfigureAwait(false)).ConfigureAwait(false);
            sender?.Touch();
        }

        private Task GetMetadata(MetadataMessage request, HttpResponse response)
        {
            var sender = FindMember(request.Sender.Represents);
            var result = request.SaveResponse(response, metadata);
            sender?.Touch();
            return result;
        }

        private async Task ReceiveEntries(HttpRequest request, HttpResponse response)
        {
            var message = new AppendEntriesMessage(request);
            await message.ParseEntriesAsync(request, Token).ConfigureAwait(false);
            var sender = FindMember(message.Sender.Represents);
            if (sender is null)
            {
                response.StatusCode = StatusCodes.Status404NotFound;
            }
            else
            {
                await message.SaveResponse(response, await ReceiveEntries(sender, message.ConsensusTerm, message.Entries, message.PrevLogIndex, message.PrevLogTerm, message.CommitIndex).ConfigureAwait(false)).ConfigureAwait(false);
            }
        }

        private Task ReceiveOneWayMessage(IAddressee sender, CustomMessage message, HttpResponse response)
        {
            if(!message.RespectLeadership || IsLeaderLocal)
            {
                response.StatusCode = StatusCodes.Status204NoContent;
                return messageHandler.ReceiveSignal(sender, message.Message);
            }
            else
            {
                response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                return Task.CompletedTask;
            }
        }

        private async Task ReceiveMessage(IAddressee sender, CustomMessage message, HttpResponse response)
        {
            if(!message.RespectLeadership || IsLeaderLocal)
            {
                response.StatusCode = StatusCodes.Status200OK;
                await message.SaveResponse(response, await messageHandler.ReceiveMessage(sender, message.Message).ConfigureAwait(false)).ConfigureAwait(false);
            }
            else
                response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        }

        private Task ReceiveMessage(CustomMessage message, HttpResponse response)
        {
            var sender = FindMember(message.Sender.Represents);
            Task task;
            if (sender is null)
            {
                response.StatusCode = StatusCodes.Status404NotFound;
                task = Task.CompletedTask;
            }
            else if (messageHandler is null)
            {
                response.StatusCode = StatusCodes.Status501NotImplemented;
                task = Task.CompletedTask;
            }
            else if (message.IsOneWay)
                task = ReceiveOneWayMessage(sender, message, response);
            else
                task = ReceiveMessage(sender, message, response);
            sender?.Touch();
            return task;
        }

        internal Task ProcessRequest(HttpContext context)
        {
            var networks = allowedNetworks;
            //checks whether the client's address is allowed
            if (networks.Count > 0 && networks.FirstOrDefault(context.Connection.RemoteIpAddress.IsIn) is null)
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            }
            //process request
            switch (HttpMessage.GetMessageType(context.Request))
            {
                case RequestVoteMessage.MessageType:
                    return ReceiveVote(new RequestVoteMessage(context.Request), context.Response);
                case ResignMessage.MessageType:
                    return Resign(new ResignMessage(context.Request), context.Response);
                case MetadataMessage.MessageType:
                    return GetMetadata(new MetadataMessage(context.Request), context.Response);
                case AppendEntriesMessage.MessageType:
                    return ReceiveEntries(context.Request, context.Response);
                case CustomMessage.MessageType:
                    return ReceiveMessage(new CustomMessage(context.Request), context.Response);
                default:
                    context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    return Task.CompletedTask;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                localMember = null;
                configurationTracker.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
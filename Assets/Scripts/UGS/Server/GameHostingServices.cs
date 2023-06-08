using System;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Matchmaker.Models;
using UnityEngine;

namespace Unity.Services.Samples.GameServerHosting
{
    public class GameHostingServices
    {
        //Change to Timeout value
        const int k_EmptyServerTimeout = 30000;
        const int k_MultiplayServiceTimeout = 20000;
        BackfillService m_BackfillService;
        AllocationPayloadService m_AllocationPayloadService;
        ServerQueryService m_ServerQueryService;
        bool m_InitializedServices;
        bool m_ClosingServer = false;
        CancellationTokenSource m_CancelTimeoutToken;

        public GameHostingServices()
        {
            m_AllocationPayloadService = new AllocationPayloadService();
            m_ServerQueryService = new ServerQueryService();
            GameHostingServerEvents.UserJoinedServer += UserJoinedServer;
            GameHostingServerEvents.UserLeftServer += UserLeft;
            GameHostingServerEvents.MapChanged += ServerChangedMap;
            GameHostingServerEvents.ModeChanged += ServerChangedMode;
        }

        /// <summary>
        /// Attempts to initialize the server with services (If we are on Multiplay) and if we time out, we move on to default setup for local testing.
        /// </summary>
        public async Task<bool> StartGameServicesAsync(string serverEndpoint, GameServerInfo startingGameInfo)
        {
            m_CancelTimeoutToken = new CancellationTokenSource();
            if (!m_ServerQueryService.IsInitialized || !m_AllocationPayloadService.IsInitialized)
                return false;

            // The server should respond to query requests irrespective of the server being allocated.
            // Hence, start the handler as soon as we can.

            await m_ServerQueryService.BeginServerQueryHandler(startingGameInfo.MaxUsers,
                startingGameInfo.Name, startingGameInfo.GameMode, startingGameInfo.Map);

            //This does not stop players connecting via matchmaker yet.
            //await m_AllocationPayloadService.UnreadyServer();

            MatchmakingResults matchmakerPayload = null;
            try
            {
                matchmakerPayload = await GetMatchmakerPayload(k_MultiplayServiceTimeout);
            }
            catch (Exception e)
            {
                Debug.LogError("Error getting matchmaker payload.");
                Debug.LogException(e);
            }

            if (matchmakerPayload != null)
            {
                //A Common Gotcha is that the matchmaker connects the player before the server is fully set up, since the matchmaker
                // is also the service that requests a server in the first place.
                //This will slow down the first connections to give the server a chance to finish its setup.
                //If you know exactly when your server is ready, you can replace this with your own call to ReadyServer()

                Debug.Log($"[GSH] Readying server on {serverEndpoint} with:\n{startingGameInfo}.");

                SetMatchInfo(startingGameInfo);

                //This does not stop players connecting via matchmaker yet.
                //When it does, we want to do:
                // await Task.Delay(startinGameInfo.ServerStartupBufferMS);
                //await m_AllocationPayloadService.ReadyServer();
                //TODO Support setup without backfill
                await StartBackfill(serverEndpoint, matchmakerPayload, startingGameInfo);

                m_InitializedServices = true;
            }
            else
            {
                Debug.LogWarning("[GSH] Getting the Matchmaker Payload timed out, starting with defaults.");
            }

            return m_InitializedServices;
        }

        async Task<MatchmakingResults> GetMatchmakerPayload(int timeout)
        {
            //Try to get the matchmaker allocation payload from the multiplay services, and init the services if we do.
            var matchmakerPayloadTask = m_AllocationPayloadService.SubscribeAndAwaitMatchmakerAllocation();

            //If we don't get the payload by the timeout, we stop trying.
            if (await Task.WhenAny(matchmakerPayloadTask, Task.Delay(timeout)) == matchmakerPayloadTask)
                return matchmakerPayloadTask.Result;

            return null;
        }

        async Task StartBackfill(string serverEndpoint, MatchmakingResults payload, GameServerInfo startingGameInfo)
        {
            m_BackfillService = new BackfillService(serverEndpoint, payload.QueueName, payload.MatchProperties,
                startingGameInfo.MaxUsers);
            m_BackfillService.OnBackfillEnded += OnBackfillEnded;
            await m_BackfillService.TryBeginBackfilling();
        }

        private void OnBackfillEnded()
        {
#pragma warning disable 4014
            WaitAndShutdownServer();
#pragma warning restore 4014
        }

        #region ServerSynching

        //There are three data locations that need to be kept in sync, Game Server, Backfill Match Ticket, and the Multiplay Server
        //The Game Server is the source of truth, and we need to propagate the state of it to the multiplay server.
        //For the matchmaking ticket, it should already have knowledge of the players, unless a player joined outside of matchmaking.

        //For now we don't have any mechanics to change the map or mode mid-game. But if we did, we would update the backfill ticket to reflect that too.
        void ServerChangedMap(string newMap)
        {
            if (!IsInitialized())
                return;
            m_ServerQueryService.SetMap(newMap);
        }

        void ServerChangedMode(string newMode)
        {
            if (!IsInitialized())
                return;
            m_ServerQueryService.SetMode(newMode);
        }

        void UserJoinedServer(string joinedUserID)
        {
            if (!IsInitialized())
                return;
            Debug.Log($"[GSH] {joinedUserID} joined the game");
            m_ServerQueryService?.AddPlayer();
            if (m_ClosingServer)
                CancelShutdown();

            if (m_BackfillService == null)
                return;

            m_BackfillService.AddPlayerToMatch(joinedUserID);
        }

        void UserLeft(string id)
        {
            if (!IsInitialized())
                return;
            m_ServerQueryService?.RemovePlayer();
            if (m_BackfillService == null)
                return;

            //We use the Matchmaker Ticket player count here to determine shutdown,
            // because we might have incoming players that are not yet connected.
            var playerCount = m_BackfillService.RemovePlayerFromMatch(id);
            Debug.Log($"[GSH] '{id}' left the game, {playerCount} players left in game.");

            if (playerCount <= 0)
            {
#pragma warning disable 4014
                WaitAndShutdownServer();
#pragma warning restore 4014
                return;
            }

#pragma warning disable 4014
            m_BackfillService.TryBeginBackfilling();
#pragma warning restore 4014
        }

        async Task WaitAndShutdownServer()
        {
            if (!IsInitialized())
                return;
            Debug.Log($"[GSH] Shutting down Server in {k_EmptyServerTimeout} milliseconds.");
            try
            {
                m_ClosingServer = true;

                await Task.Delay(k_EmptyServerTimeout, m_CancelTimeoutToken.Token);
                await CloseServer();
            }
            catch (OperationCanceledException ex)
            {
                Debug.Log($"[GSH] Shutdown Cancelled.\n{ex}");
                m_ClosingServer = false;
            }
        }

        void CancelShutdown()
        {
            Debug.Log($"[GSH] Cancel Shutdown {m_ClosingServer}");
            m_CancelTimeoutToken.Cancel();
        }

        void SetMatchInfo(GameServerInfo startingGameInfo)
        {
            //Create a unique name for the server to show that we are joining the same one
            Debug.Log($"[GSH] Set Match Info {startingGameInfo.Name} {startingGameInfo.MaxUsers} {startingGameInfo.BuildID}");
            m_ServerQueryService.SetServerName(startingGameInfo.Name);
            m_ServerQueryService.SetMaxPlayers(startingGameInfo.MaxUsers);
            m_ServerQueryService.SetBuildID(startingGameInfo.BuildID);
            m_ServerQueryService.SetMap(startingGameInfo.Map);
            m_ServerQueryService.SetMode(startingGameInfo.GameMode);
        }

        #endregion

        bool IsInitialized()
        {
            if (!m_InitializedServices)
            {
                Debug.LogWarning("[GSH] Game Server Services not Initialized");
                return false;
            }

            return true;
        }

        async Task CloseServer()
        {
            Debug.Log("[GSH] Closing Server.");
            await m_BackfillService.StopBackfill();
            Dispose();
            Application.Quit();
        }

        public void Dispose()
        {
            m_InitializedServices = false;
            m_BackfillService?.Dispose();
            m_AllocationPayloadService?.Dispose();
            GameHostingServerEvents.UserJoinedServer -= UserJoinedServer;
            GameHostingServerEvents.UserLeftServer -= UserLeft;
            GameHostingServerEvents.MapChanged -= ServerChangedMap;
            GameHostingServerEvents.ModeChanged -= ServerChangedMode;
        }
    }
}
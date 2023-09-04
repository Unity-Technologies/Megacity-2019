using System;
using System.Linq;
using Unity.Megacity.Traffic;
using Unity.NetCode;
using Unity.Networking.Transport;
using UnityEngine;

namespace Unity.Megacity.UI
{
    /// <summary>
    /// Save bool parameters such as SkipMenu or QuitAfterFlyover,
    /// These parameters are reading during the execution.
    /// These parameters can be writing using command line.
    /// </summary>
    public class ExecutionOptions
    {
        public bool SkipMenu { get; }
        public bool QuitAfterFlyover { get; }
        public bool IsThinClient => TargetThinClientWorldCount > 0;
        public NetworkEndpoint UserSpecifiedEndpoint { get; }
        public int UGSQueryPort { get; }
        public int TargetThinClientWorldCount { get; }
        public int MaxCarCount { get; }

        public ExecutionOptions(bool skipMenu, bool quitAfterFlyover, NetworkEndpoint userSpecifiedEndpoint,
            int targetThinClientWorldCount, int maxCarCount)
        {
            Console.WriteLine($"Creating execution options: skipMenu {skipMenu}, quitAfterFlyover {quitAfterFlyover}");

            SkipMenu = skipMenu;
            QuitAfterFlyover = quitAfterFlyover;
            UserSpecifiedEndpoint = userSpecifiedEndpoint;
            TargetThinClientWorldCount = targetThinClientWorldCount;
            MaxCarCount = maxCarCount;
        }
    }

    /// <summary>
    /// This is a execution code to run via command line,
    /// This writes if the execution should skip the menu and close the execution after flyover the scene.
    /// </summary>
    public static class ModeBootstrap
    {
        private const string RunFlyoverAndExitSwitch = "--run-flyover-and-exit";
        private const string ThinClientsEnabled = "--enable-thin-clients";
        private const string MaxCars = "--max-cars";
        private const string MultiplayPort = "-port";
        private const string MultiplayServerIp = "0.0.0.0";

        public static ExecutionOptions Options => m_Options ??= CreateOptions();
        private static ExecutionOptions m_Options;

        private static ExecutionOptions CreateOptions()
        {
            var commandLineArgs = Environment.GetCommandLineArgs();
            var isAutomatedFlyOver = commandLineArgs.Contains(RunFlyoverAndExitSwitch);
            ParseThinClientArgs(commandLineArgs, out var userSpecifiedEndpoint, out var userSpecifiedNumThinClients);
            ParseMaxCarCountArgs(commandLineArgs, out var maxCarCount);
            //If we are running Multiplay UGS, it needs to set to endpoint specific to Multiplay
            ParseMultiplayEndpoint(commandLineArgs, out userSpecifiedEndpoint);
            return new ExecutionOptions(isAutomatedFlyOver, isAutomatedFlyOver, userSpecifiedEndpoint,
                userSpecifiedNumThinClients, maxCarCount);
        }

        private static void ParseThinClientArgs(string[] commandLineArgs, out NetworkEndpoint userSpecifiedEndpoint,
            out int userSpecifiedNumThinClients)
        {
            var hardcodedFallbackAddress = NetCodeBootstrap.MegacityServerIp;
            userSpecifiedEndpoint = hardcodedFallbackAddress;
            userSpecifiedNumThinClients = 0;

            var indexOfThinClientCount = Array.IndexOf(commandLineArgs, ThinClientsEnabled);
            var hasThinClientSpecified = indexOfThinClientCount != -1;

            if (!Application.isBatchMode)
            {
                if (hasThinClientSpecified)
                    Debug.LogWarning(
                        $"Warning: Commandline arg {ThinClientsEnabled} specified, but not running headless!");
            }

            // No ThinClientsEnabled arg specified. ThinClients are disabled.
            if (!hasThinClientSpecified)
                return;

            var indexOfEndpointString = indexOfThinClientCount + 1;
            if (indexOfEndpointString < commandLineArgs.Length)
            {
                var endpointString = commandLineArgs[indexOfEndpointString];
                var port = ClientServerBootstrap.AutoConnectPort != 0
                    ? ClientServerBootstrap.AutoConnectPort
                    : hardcodedFallbackAddress.Port;
                if (!NetworkEndpoint.TryParse(endpointString, port, out userSpecifiedEndpoint, NetworkFamily.Ipv4))
                {
                    Debug.LogError(
                        $"Cannot parse commandline arg {indexOfEndpointString} '{endpointString}' as NetworkEndpoint (for arg '{ThinClientsEnabled}')! Using hardcoded address '{hardcodedFallbackAddress}' instead! Note that NetworkEndpoint.TryParse does not support passing in a port! The port is therefore hardcoded to '{port}'!");
                    userSpecifiedEndpoint = hardcodedFallbackAddress;
                }
            }
            else
                Debug.LogError(
                    $"Expecting an endpoint after commandline arg '{ThinClientsEnabled}' but there was none!");

            var indexOfCountString = indexOfThinClientCount + 2;
            if (indexOfCountString < commandLineArgs.Length)
            {
                var countString = commandLineArgs[indexOfCountString];
                if (!int.TryParse(countString, out userSpecifiedNumThinClients))
                    Debug.LogError(
                        $"Cannot parse commandline arg {indexOfCountString} '{countString}' as int count (for arg '{ThinClientsEnabled}')!");
            }
            else
                Debug.LogError(
                    $"Expecting an int after commandline arg '{ThinClientsEnabled}' (after the endpoint) but there was none!");

            Debug.Log(
                $"ThinClients enabled via commandline arg '{ThinClientsEnabled}': User-specified address '{userSpecifiedEndpoint}' and thin client count: '{userSpecifiedNumThinClients}'.");
        }

        private static void ParseMaxCarCountArgs(string[] commandLineArgs, out int maxCarCount)
        {
            maxCarCount = -1;
            var indexOfMaxCarCount = Array.IndexOf(commandLineArgs, MaxCars);

            // No MaxCars arg specified. Leaving default.
            if (indexOfMaxCarCount == -1)
                return;

            var indexOfMaxCarInt = indexOfMaxCarCount + 1;
            if (indexOfMaxCarInt >= commandLineArgs.Length)
            {
                Debug.LogError($"Expecting an int after commandline arg '{MaxCars}' but there was none!");
                return;
            }

            var maxCarString = commandLineArgs[indexOfMaxCarInt];
            if (!int.TryParse(maxCarString, out maxCarCount))
            {
                Debug.LogError(
                    $"Cannot parse commandline arg {indexOfMaxCarInt} '{maxCarString}' as int (for arg '{MaxCars}')! Leaving existing value.");
                return;
            }

            Debug.Log(
                $"Successfully parsed commandline arg {indexOfMaxCarCount} '{maxCarString}' as int '{maxCarCount}' (for arg '{MaxCars}')!");
        }

        private static void ParseMultiplayEndpoint(string[] commandlineArgs, out NetworkEndpoint multiplayEndpoint)
        {
            var hardcodedFallbackAddress = NetCodeBootstrap.MegacityServerIp;
            multiplayEndpoint = hardcodedFallbackAddress;
            if (commandlineArgs.Length < 1)
                return;

            var indexOfPort = Array.IndexOf(commandlineArgs, MultiplayPort);

            if (indexOfPort != -1 && indexOfPort < commandlineArgs.Length)
            {
                var portString = commandlineArgs[indexOfPort + 1];
                if (!ushort.TryParse(portString, out var portShort))
                {
                    Debug.LogError($"Cannot parse -port arg: {portString} not a ushort!");
                    return;
                }

                if (!NetworkEndpoint.TryParse(MultiplayServerIp, portShort, out multiplayEndpoint, NetworkFamily.Ipv4))
                {
                    Debug.LogError(
                        $"Cannot parse ip : port - '{MultiplayServerIp}: {portShort}' as NetworkEndpoint (for arg '{MultiplayPort}')! Using hardcoded address '{hardcodedFallbackAddress}' instead!");
                }

                Debug.Log(
                    $"Using {MultiplayPort} arg. Starting server in Multiplay UGS Mode on {multiplayEndpoint.Address}");
            }
            else
            {
#if !UNITY_EDITOR
                Debug.LogWarning($"Expecting a port after commandline arg '{MultiplayPort}' but there was none!");
#endif
            }
        }
    }
}
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace PortalHeimSG1
{
    /// <summary>
    /// The messages passed between a player and the server. The player asks, the server decides and answers:
    ///   Hello / HelloAck : the player announces itself; an answer proves the server also has this mod.
    ///   Gates            : the server's list of every gate and what it's doing.
    ///   Dial / HangUp    : the player asks to connect or disconnect a gate.
    ///   Result           : the server's answer to a request (a short message to show).
    /// A server without the mod never answers Hello, so the player's mod knows to leave dialing off.
    /// </summary>
    internal static class GateNet
    {
        private const string Hello = "SG1_Hello";
        private const string HelloAck = "SG1_HelloAck";
        private const string ListRequest = "SG1_ListRequest";
        private const string Gates = "SG1_Gates";
        private const string DialRpc = "SG1_Dial";
        private const string HangUpRpc = "SG1_HangUp";
        private const string Result = "SG1_Result";

        private static ZRoutedRpc _registeredOn;

        /// <summary>Registers the messages once per connection.</summary>
        public static void Register()
        {
            var rpc = ZRoutedRpc.instance;
            if (rpc == null || rpc == _registeredOn) return;
            _registeredOn = rpc;

            rpc.Register<string>(Hello, RpcHello);
            rpc.Register<string>(HelloAck, RpcHelloAck);
            rpc.Register(ListRequest, RpcListRequest);
            rpc.Register<ZPackage>(Gates, RpcGates);
            rpc.Register<ZDOID, ZDOID>(DialRpc, RpcDial);
            rpc.Register<ZDOID>(HangUpRpc, RpcHangUp);
            rpc.Register<string>(Result, RpcResult);
        }

        // --- Player to server ---

        public static void SendHello()
        {
            if (ZRoutedRpc.instance == null) return;
            GateClient.HelloSentAt = Time.time;
            ZRoutedRpc.instance.InvokeRoutedRPC(Hello, PortalHeimSG1Plugin.Version);
        }

        public static void RequestGates()
        {
            if (ZRoutedRpc.instance != null) ZRoutedRpc.instance.InvokeRoutedRPC(ListRequest);
        }

        public static void RequestDial(ZDOID from, ZDOID to)
        {
            if (ZRoutedRpc.instance != null) ZRoutedRpc.instance.InvokeRoutedRPC(DialRpc, from, to);
        }

        public static void RequestHangUp(ZDOID gate)
        {
            if (ZRoutedRpc.instance != null) ZRoutedRpc.instance.InvokeRoutedRPC(HangUpRpc, gate);
        }

        // --- Server side ---

        private static void RpcHello(long sender, string version)
        {
            if (!GateServer.IsServer) return;
            ZRoutedRpc.instance.InvokeRoutedRPC(sender, HelloAck, PortalHeimSG1Plugin.Version);
            SendGates(sender);
        }

        private static void RpcListRequest(long sender)
        {
            if (GateServer.IsServer) SendGates(sender);
        }

        private static void RpcDial(long sender, ZDOID from, ZDOID to)
        {
            if (!GateServer.IsServer) return;
            string problem = GateServer.TryDial(sender, from, to);
            if (problem != null) ZRoutedRpc.instance.InvokeRoutedRPC(sender, Result, problem);
        }

        private static void RpcHangUp(long sender, ZDOID gate)
        {
            if (!GateServer.IsServer) return;
            string problem = GateServer.TryHangUp(gate);
            if (problem != null) ZRoutedRpc.instance.InvokeRoutedRPC(sender, Result, problem);
        }

        private static void SendGates(long target)
        {
            ZRoutedRpc.instance.InvokeRoutedRPC(target, Gates, Pack(GateServer.Snapshot()));
        }

        /// <summary>Tells every player that gates changed.</summary>
        public static void BroadcastGates()
        {
            if (ZRoutedRpc.instance == null || !GateServer.IsServer) return;
            SendGates(ZRoutedRpc.Everybody);
        }

        private static ZPackage Pack(List<GateInfo> gates)
        {
            var pkg = new ZPackage();
            pkg.Write(gates.Count);
            foreach (var gate in gates)
            {
                pkg.Write(gate.Id);
                pkg.Write(gate.Name ?? "");
                pkg.Write(gate.Position);
                pkg.Write((int)gate.State);
                pkg.Write(gate.Partner);
            }

            return pkg;
        }

        // --- Player side ---

        private static void RpcHelloAck(long sender, string version)
        {
            GateClient.ServerHasMod = true;
            GateClient.ServerVersion = version;
            PortalHeimSG1Plugin.Log.LogInfo($"The server has {PortalHeimSG1Plugin.Name} {version}: dialing is on.");
        }

        private static void RpcGates(long sender, ZPackage pkg)
        {
            try
            {
                var list = new List<GateInfo>();
                int count = pkg.ReadInt();
                for (int i = 0; i < count; i++)
                {
                    var gate = new GateInfo
                    {
                        Id = pkg.ReadZDOID(),
                        Name = pkg.ReadString(),
                        Position = pkg.ReadVector3(),
                        State = (GateState)pkg.ReadInt(),
                        Partner = pkg.ReadZDOID(),
                    };
                    list.Add(gate);
                }

                GateClient.SetGates(list);
            }
            catch (System.Exception e)
            {
                PortalHeimSG1Plugin.Log.LogError($"Couldn't read the gate list from the server. {e}");
            }
        }

        private static void RpcResult(long sender, string message)
        {
            if (Player.m_localPlayer != null && !string.IsNullOrEmpty(message))
                Player.m_localPlayer.Message(MessageHud.MessageType.Center, message);
        }
    }

    /// <summary>Hooks the messages up as soon as the game has a connection to use.</summary>
    [HarmonyPatch(typeof(Game), "Start")]
    internal static class GameStartPatch
    {
        private static void Postfix() => GateNet.Register();
    }
}

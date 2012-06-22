﻿/*
 * Copyright 2011 MCForge
 * Dual-licensed under the Educational Community License, Version 2.0 and
 * the GNU General Public License, Version 3 (the "Licenses"); you may
 * not use this file except in compliance with the Licenses. You may
obtain a copy of the Licenses at
http://www.opensource.org/licenses/ecl2.php
http://www.gnu.org/licenses/gpl-3.0.html
Unless required by applicable law or agreed to in writing,
software distributed under the Licenses are distributed on an "AS IS"
BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
or implied. See the Licenses for the specific language governing
permissions and limitations under the Licenses.
*/﻿
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.IO;
using System.Text.RegularExpressions;
using System.Net.Sockets;
using System.Threading;
using MCForge;
using MCForge.API;
using MCForge.API.Events;
using MCForge.World;
using MCForge.Interface.Plugin;
using MCForge.Entity;
using MCForge.Core;
using MCForge.Utils.Settings;
using MCForge.Utils;
namespace Plugins.WomPlugin {
    public class PluginWomTextures : IPlugin {

        public string Name {
            get { return "WomTextures"; }
        }

        public string Author {
            get { return "headdetect and Gamemakergm"; }
        }

        public int Version {
            get { return 1; }
        }

        public string CUD {
            get { return ""; }
        }

        private WomSettings WomSettings { get; set; }
        public void OnLoad(string[] args1) {
            Server.OnServerFinishSetup += OnLoadDone;
        }
        //Wait for server to finish so we make sure all of the levels are loaded
        void OnLoadDone() {
            Logger.Log("[WomTextures] Succesfully initiated!");
            WomSettings = new WomSettings();
            WomSettings.OnLoad();

            Server.OnServerFinishSetup -= OnLoadDone;
            Player.OnAllPlayersReceiveUnknownPacket.Normal += new Event<Player, PacketEventArgs>.EventHandler(OnIncomingData);
            Player.OnAllPlayersSendPacket.Normal += new Event<Player, PacketEventArgs>.EventHandler(OnOutgoingData);

            Player.OnAllPlayersChat.Normal += ((sender, args) =>
                SendDetailToPlayer(sender, "This is a detail, deal &4With &3It"));
        }

        private readonly Regex Parser = new Regex("GET /([a-zA-Z0-9_]{1,16})(~motd)? .+", RegexOptions.Compiled);

        void OnIncomingData(Player p, PacketEventArgs args) {
            if (args.Data.Length < 0)
                return;

            if (args.Data[0] != (byte)'G')
                return;

            args.Cancel();

            var netStream = p.Client.GetStream();
            using (var Writer = new StreamWriter(netStream)) {
                var line = Encoding.UTF8.GetString(args.Data, 0, args.Data.Length).Split('\n')[0];
                var match = Parser.Match(line);

                if (match.Success) {
                    var lvl = Level.FindLevel(match.Groups[1].Value);
                    var userNameLine = Encoding.UTF8.GetString(args.Data, 0, args.Data.Length).Split('\n')[3];
                    var username = userNameLine.Remove(0, "X-WoM-Username: ".Length).Replace("\r", "");
                    var player = Player.Find(username);
                    if (player != null)
                        player.ExtraData.Add("UsingWom", true);

                    if (lvl == null) {
                        Writer.Write("HTTP/1.1 404 Not Found");
                        Writer.Flush();
                    }
                    else {
                        if (!lvl.ExtraData.ContainsKey("WomConfig")) {
                            Writer.Write("HTTP/1.1 500 Internal Server Error");
                            Writer.Flush();
                        }
                        else {
                            var Config = (string[])lvl.ExtraData["WomConfig"];
                            var bytes = Encoding.UTF8.GetBytes(Config.ToString<string>());
                            Writer.WriteLine("HTTP/1.1 200 OK");
                            Writer.WriteLine("Date: " + DateTime.UtcNow.ToString("R"));
                            Writer.WriteLine("Server: Apache/2.2.21 (CentOS)");
                            Writer.WriteLine("Last-Modified: " + DateTime.UtcNow.ToString("R"));
                            Writer.WriteLine("Accept-Ranges: bytes");
                            Writer.WriteLine("Content-Length: " + bytes.Length);
                            Writer.WriteLine("Connection: close");
                            Writer.WriteLine("Content-Type: text/plain");
                            Writer.WriteLine();
                            foreach (var entry in Config)
                                Writer.WriteLine(entry);
                        }
                    }

                }

                else {
                    Writer.Write("HTTP/1.1 400 Bad Request");
                    Writer.Flush();
                }
            }
        }

        void OnOutgoingData(Player p, PacketEventArgs e) {
            if (e.Type != Packet.Types.MOTD)
                return;
#if DEBUG
            string ip = "127.0.0.1";
#else
            string ip = InetUtils.GrabWebpage("http://www.mcforge.net/serverdata/ip.php");
#endif
            Packet pa = new Packet();
            pa.Add(Packet.Types.MOTD);
            pa.Add((byte)7);
            pa.Add(ServerSettings.GetSetting("ServerName"), 64);
            pa.Add(ServerSettings.GetSetting("MOTD") + " &0cfg=" + ip + ":" + ServerSettings.GetSetting("Port") + "/" + p.Level.Name, 64);
            pa.Add((byte)0);

            e.Data = pa.bytes;
        }



        public void OnUnload() {
            Player.OnAllPlayersSendPacket.Normal -= OnOutgoingData;
            Player.OnAllPlayersReceivePacket.Normal -= OnIncomingData;
        }

        #region Static Helper Methods


        /// <summary>
        /// Send a detail message to any player with wom.
        /// If user is not using wom, message will be ignored
        /// </summary>
        /// <param name="player">Player to send detail to</param>
        /// <param name="detail">Detail message to send </param>
        public static void SendDetailToPlayer(Player player, string detail) {
            if (!player.ExtraData.ContainsKey("UsingWom"))
                return;
            player.SendMessage(String.Format("^detail.user={0}", detail));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="detail"></param>
        public static void SendDetailToAll(string detail) {
            Server.Players.ForEach(p => SendDetailToPlayer(p, detail));
        }
        #endregion
    }
}
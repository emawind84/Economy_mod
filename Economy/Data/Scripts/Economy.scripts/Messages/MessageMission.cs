namespace Economy.scripts.Messages
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using EconConfig;
    using Economy.scripts.EconStructures;
    using MissionStructures;
    using ProtoBuf;
    using Sandbox.ModAPI;
    using VRage.Game.ModAPI;
    using VRage.ModAPI;
    using VRageMath;

    /*
     * Basic summary of logic to go here -
     * Testing: return text based on mission id
     * 
     * Further testing: update mission hud
     * 
     * Final testing: make it pull mission from mission file not a switch
     * 
     * If all good then:     * 
     * 1: look up mission text from server misson file - including any immediate chains?
       2: return this text to client side for storage in the objectives[] array
     * 3: roll / update the mission display to the appropriate position based on players mission ID 
     * 4: sundry logic in regard to win conditions ie isplayer position within 100 metres of objective gps
     * 5: check they are carrying mission related items, joined faction x etc
    
     * */

    [ProtoContract]
    public class MessageMission : MessageBase
    {

        const long DefaultMissionDeadline = 10800;

        #region properties

        [ProtoMember(201)]
        public PlayerMissionManage CommandType;

        [ProtoMember(202)]
        public int MissionId;

        [ProtoMember(203)]
        public MissionBaseStruct Mission;

        [ProtoMember(204)]
        public bool StartMission;

        #endregion

        #region send messages

        public static void SendMissionComplete(MissionBaseStruct mission)
        {
            ConnectionHelper.SendMessageToPlayer(mission.AcceptedBy, new MessageMission { CommandType = PlayerMissionManage.MissionComplete, MissionId = mission.MissionId });
        }

        public static void SendMissionFailed(MissionBaseStruct mission)
        {
            ConnectionHelper.SendMessageToPlayer(mission.AcceptedBy, new MessageMission { CommandType = PlayerMissionManage.MissionFailed, MissionId = mission.MissionId });
        }

        public static void SendMessage(int missionId)
        {
            ConnectionHelper.SendMessageToServer(new MessageMission { CommandType = PlayerMissionManage.Test, MissionId = missionId });
        }

        public static void SendCreateSampleMissions(bool startMission)
        {
            ConnectionHelper.SendMessageToServer(new MessageMission { CommandType = PlayerMissionManage.AddSample, StartMission = startMission });
        }

        public static void SendPrepareMission(MissionBaseStruct mission)
        {
            ConnectionHelper.SendMessageToServer(new MessageMission { CommandType = PlayerMissionManage.PrepareMission, Mission = mission });
        }

        public static void SendAddMission(MissionBaseStruct mission)
        {
            ConnectionHelper.SendMessageToServer(new MessageMission { CommandType = PlayerMissionManage.AddMission, Mission = mission });
        }

        public static void SendDeleteMission(int missionId)
        {
            ConnectionHelper.SendMessageToServer(new MessageMission { CommandType = PlayerMissionManage.DeleteMission, MissionId = missionId });
        }

        public static void SendAcceptMission(int missionId)
        {
            ConnectionHelper.SendMessageToServer(new MessageMission { CommandType = PlayerMissionManage.AcceptMission, MissionId = missionId });
        }

        public static void SendAbandonMission(int missionId)
        {
            ConnectionHelper.SendMessageToServer(new MessageMission { CommandType = PlayerMissionManage.AbandonMission, MissionId = missionId });
        }

        #endregion

        public override void ProcessClient()
        {
            switch (CommandType)
            {
                case PlayerMissionManage.PrepareMission:
                    MyAPIGateway.Utilities.ShowMissionScreen("New Contract", "", Mission.GetName(), Mission.GetFullDescription(),
                        result => {
                            if (result == ResultEnum.OK)
                                SendAddMission(Mission);
                        }, "Create");

                    break;

                case PlayerMissionManage.AcceptMission:
                    {
                        HudManager.FetchMission(MissionId);
                        MyAPIGateway.Utilities.ShowMessage("Contract", "Contract No. {0} has been accepted", MissionId);
                        if (!HudManager.UpdateHud()) { MyAPIGateway.Utilities.ShowMessage("Error", "Hud Failed"); }
                    }
                    break;
                case PlayerMissionManage.AbandonMission:
                    {
                        HudManager.FetchMission(-1);
                        var currentMission = EconomyScript.Instance.ClientConfig.Missions.FirstOrDefault(m => m.MissionId == MissionId);
                        currentMission.RemoveGps();
                        MyAPIGateway.Utilities.ShowMessage("Contract", "Contract No. {0} has been abandoned", MissionId);
                    }
                    break;

                case PlayerMissionManage.MissionComplete:
                    {
                        var clientConfig = EconomyScript.Instance.ClientConfig;
                        var mission = clientConfig.Missions.FirstOrDefault(m => m.MissionId == MissionId);
                        mission.RemoveGps();
                        clientConfig.Missions.Remove(mission);

                        string msg = mission.GetSuccessMessage();
                        if (mission.Reward != 0)
                            msg += string.Format("\r\n{0:#,##0.00} {1} Transferred to your account.", mission.Reward, clientConfig.ServerConfig.CurrencyName);
                        //MyAPIGateway.Utilities.ShowMissionScreen("Mission:" + mission.MissionId, "", "Completed", msg, null, "Okay");

                        Sandbox.Game.MyVisualScriptLogicProvider.ReplaceQuestlogDetail(0, msg, true, MyAPIGateway.Session.Player.IdentityId);
                        Sandbox.Game.MyVisualScriptLogicProvider.SetQuestlogDetailCompleted(0, true, MyAPIGateway.Session.Player.IdentityId);

                        clientConfig.LazyMissionText = MissionId + " Mission: completed";
                        HudManager.FetchMission(0);
                        HudManager.UpdateHud();
                    }
                    break;

                case PlayerMissionManage.MissionFailed:
                    {
                        var currentMission = EconomyScript.Instance.ClientConfig.Missions.FirstOrDefault(m => m.MissionId == MissionId);
                        currentMission.RemoveGps();

                        Sandbox.Game.MyVisualScriptLogicProvider.ReplaceQuestlogDetail(0, "Contract failed.", true, MyAPIGateway.Session.Player.IdentityId);
                        Sandbox.Game.MyVisualScriptLogicProvider.SetQuestlogDetailCompleted(0, true, MyAPIGateway.Session.Player.IdentityId);

                        MyAPIGateway.Utilities.ShowMessage("Contract", $"Contract No. {currentMission.MissionId} failed");
                        HudManager.FetchMission(0);
                    }
                    break;

                default:
                    //MessageClientTextMessage.SendMessage(SenderSteamId, "mission", (MissionId + "client side"));
                    break;
            }
        }

        public override void ProcessServer()
        {
            // update our own timestamp here
            AccountManager.UpdateLastSeen(SenderSteamId, SenderLanguage);
            EconomyScript.Instance.ServerLogger.WriteVerbose("Manage Player Mission from '{0}'", SenderSteamId);

            switch (CommandType)
            {
                case PlayerMissionManage.Test:
                    //EconomyScript.Instance.ServerLogger.WriteVerbose("Mission Text request '{0}' from '{1}'", MissionID, SenderSteamId);
                    var entities = new HashSet<IMyEntity>();
                    MyAPIGateway.Entities.GetEntities(entities);
                    MessageClientTextMessage.SendMessage(SenderSteamId, "Server", $"entities in world: {entities.Count()}");
                    //MessageClientTextMessage.SendMessage(SenderSteamId, "Contract", (MissionId + " server side"));
                    break;

                case PlayerMissionManage.AddSample:
                    {
                        var player = MyAPIGateway.Players.FindPlayerBySteamId(SenderSteamId);
                        var playerMatrix = player.Character.WorldMatrix;
                        //var position = player.Character.GetPosition();
                        Vector3D position = playerMatrix.Translation + playerMatrix.Forward * 60f;
                        MissionBaseStruct newMission = CreateMission(new TravelMission
                        {
                            AreaSphere = new BoundingSphereD(position, 50),
                            Reward = 100,
                            OfferDate = EconDateTime.Now,
                        }, 0);

                        MessageClientTextMessage.SendMessage(SenderSteamId, "Contract", "Contract No. {0} has been opened and {1:#,##0.00} {2} has been detracted from your account", newMission.MissionId, newMission.Reward, EconomyScript.Instance.ServerConfig.CurrencyName);
                        MessageUpdateClient.SendServerMissions();
                    }
                    break;

                case PlayerMissionManage.PrepareMission:
                    string messageToSender;
                    if (Mission.PrepareMission(out messageToSender))
                    {
                        ConnectionHelper.SendMessageToPlayer(SenderSteamId, new MessageMission { CommandType = PlayerMissionManage.PrepareMission, Mission = Mission });
                    }
                    else
                    {
                        MessageClientTextMessage.SendMessage(SenderSteamId, "Contract", messageToSender);
                    }
                    break;

                case PlayerMissionManage.AddMission:
                    {
                        var senderAccount = AccountManager.FindOrCreateAccount(SenderSteamId, SenderDisplayName, SenderLanguage);
                        if (senderAccount.BankBalance < Mission.Reward)
                        {
                            MessageClientTextMessage.SendMessage(SenderSteamId, "Contract", "You don't have enough credits to open this contract");
                            break;
                        }

                        CreateMission(Mission, 0);
                        EconomyScript.Instance.Data.CreditBalance += Mission.Reward;
                        senderAccount.BankBalance -= Mission.Reward;
                        senderAccount.Date = DateTime.Now;

                        MessageClientTextMessage.SendMessage(SenderSteamId, "Contract", "Contract number {0} has been opened and {1:#,##0.00} {2} has been detracted from your account", Mission.MissionId, Mission.Reward, EconomyScript.Instance.ServerConfig.CurrencyName);
                        MessageUpdateClient.SendAccountMessage(senderAccount);
                        MessageUpdateClient.SendServerMissions();
                        EconomyScript.Instance.ServerLogger.WriteInfo($"Contract {Mission.MissionId} created by {SenderSteamId}");
                    }
                    break;

                case PlayerMissionManage.DeleteMission:
                    {
                        var mission = GetMission(MissionId);
                        var sender = MyAPIGateway.Players.FindPlayerBySteamId(SenderSteamId);
                        if (mission != null && (mission.CreatedBy == SenderSteamId || sender.IsAdmin()) && mission.AcceptedBy == 0)
                        {
                            var player = MyAPIGateway.Players.FindPlayerBySteamId(mission.CreatedBy);
                            if (player != null)
                            {
                                EconomyScript.Instance.Data.CreditBalance -= mission.Reward;
                                var playerAccount = AccountManager.FindOrCreateAccount(player.SteamUserId, player.DisplayName, SenderLanguage);
                                playerAccount.BankBalance += mission.Reward;
                                playerAccount.Date = DateTime.Now;

                                MessageUpdateClient.SendAccountMessage(playerAccount);
                            }
                            RemoveMission(mission);
                            MessageClientTextMessage.SendMessage(SenderSteamId, "Contract", "Contract number {0} has been closed, {1:#,##0.00} {2} has been refunded into your account", mission.MissionId, mission.Reward, EconomyScript.Instance.ServerConfig.CurrencyName);
                            MessageUpdateClient.SendServerMissions();
                            EconomyScript.Instance.ServerLogger.WriteInfo($"Contract {MissionId} closed by {SenderSteamId}");
                        }
                    }
                    break;

                case PlayerMissionManage.AcceptMission:
                    {
                        var mission = GetMission(MissionId);
                        if (mission != null && (mission.AcceptedBy == SenderSteamId || mission.AcceptedBy == 0))
                        {
                            mission.AcceptedBy = SenderSteamId;
                            mission.Expiration = EconDateTime.Now;
                            mission.Expiration.Date += TimeSpan.FromSeconds(DefaultMissionDeadline);

                            MessageUpdateClient.SendServerMissions();
                            ConnectionHelper.SendMessageToPlayer(SenderSteamId, new MessageMission { CommandType = PlayerMissionManage.AcceptMission, MissionId = MissionId });

                            var senderAccount = AccountManager.FindOrCreateAccount(SenderSteamId, SenderDisplayName, SenderLanguage);
                            senderAccount.MissionId = MissionId;
                            senderAccount.Date = DateTime.Now;
                            MessageUpdateClient.SendAccountMessage(senderAccount);
                            EconomyScript.Instance.ServerLogger.WriteInfo($"Contract {MissionId} accepted by {SenderSteamId}");
                        }
                        else
                        {
                            MessageClientTextMessage.SendMessage(SenderSteamId, "Contract", "The contract has been accepted already by another pilot");
                        }
                    }
                    break;

                case PlayerMissionManage.MissionComplete:
                    {
                        // This should process the mission reward if appropriate and then delete from server.
                        // We aren't archiving finished missions.
                        // moved to MissionManager
                    }
                    break;

                case PlayerMissionManage.AbandonMission:
                    {
                        var mission = GetMission(MissionId);
                        if (mission != null && mission.AcceptedBy == SenderSteamId)
                        {
                            mission.ResetMission();

                            MessageUpdateClient.SendServerMissions();
                            ConnectionHelper.SendMessageToPlayer(SenderSteamId, new MessageMission { CommandType = PlayerMissionManage.AbandonMission, MissionId = MissionId });

                            var senderAccount = AccountManager.FindOrCreateAccount(SenderSteamId, SenderDisplayName, SenderLanguage);
                            senderAccount.MissionId = 0;
                            senderAccount.Date = DateTime.Now;
                            MessageUpdateClient.SendAccountMessage(senderAccount);
                            EconomyScript.Instance.ServerLogger.WriteInfo($"Contract {MissionId} abandoned by {SenderSteamId}");
                        }
                    }
                    break;
            }
        }

        private void CreateSampleMissions()
        {
            // TODO: this is a temporary structure, before we move this to a configurable data store that can be modified and persisted.
            // Missions will be stored on the server, but only current mission will be passed to the client.
            // the following are an example of each potential mission type, in a custom missions system these are the types 
            // of missions available for admins to create, or as a generic set of tutorial missions to teach player how to use economy

            CreateMission(new StayAliveMission
            {
                MissionId = 0,
                Reward = 0
            }, 0);

            CreateMission(new UseAccountBalanceMission
            {
                MissionId = 1,
                Reward = 10
            }, 0);

            CreateMission(new MineMission
            {
                MissionId = 2,
                Reward = 0
            }, 0);

            CreateMission(new BuySomethingMission
            {
                MissionId = 3,
                Reward = 100
            }, 0);

            CreateMission(new PayPlayerMission
            {
                MissionId = 4,
                Reward = 600
            }, 0);

            CreateMission(new TradeWithPlayerMission
            {
                MissionId = 5,
                Reward = 600
            }, 0);

            CreateMission(new UseWorthMission
            {
                MissionId = 6,
                Reward = 1000
            }, 0);

            CreateMission(new WeldMission
            {
                MissionId = 7,
                Reward = 10000
            }, 0);

            CreateMission(new JoinFactionMission
            {
                MissionId = 8,
                Reward = 10000
            }, 0);

            CreateMission(new TravelMission
            {
                MissionId = 9,
                AreaSphere = new BoundingSphereD(new Vector3D(0, 0, 0), 50),
                Reward = 100
            }, 0);

            CreateMission(new KillPlayerMission
            {
                MissionId = 10,
                TargetEntityId = 0,
                Reward = 10000
            }, 0);

            CreateMission(new UseBuySellShipMission
            {
                MissionId = 11,
                Reward = 10000
            }, 0);

            CreateMission(new DeliverItemToTradeZoneMission
            {
                MissionId = 12,
                Reward = 10000
            }, 0);

            CreateMission(new BlockDeactivateMission
            {
                MissionId = 13,
                Reward = 10000
            }, 0);

            CreateMission(new BlockActivateMission
            {
                MissionId = 14,
                Reward = 10000
            }, 0);

            CreateMission(new BlockDestroyMission
            {
                MissionId = 15,
                Reward = 10000
            }, 0);

            CreateMission(new BlockCaptureMission
            {
                MissionId = 16,
                Reward = 10000
            }, 0);
        }

        [Obsolete("Replaced with MissionManager", false)]
        private static MissionBaseStruct CreateMission(MissionBaseStruct mission, ulong assignToPlayer)
        {
            return MissionManager.CreateMission(mission, assignToPlayer);
        }

        [Obsolete("Replaced with MissionManager", false)]
        private static void RemoveMission(MissionBaseStruct mission)
        {
            MissionManager.RemoveMission(mission);
        }

        [Obsolete("Replaced with MissionManager", false)]
        private static MissionBaseStruct GetMission(int missionId)
        {
            return MissionManager.GetMission(missionId);
        }
    }
}

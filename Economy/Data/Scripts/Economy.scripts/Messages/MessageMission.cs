namespace Economy.scripts.Messages
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using EconConfig;
    using MissionStructures;
    using ProtoBuf;
    using Sandbox.Definitions;
    using Sandbox.ModAPI;
    using VRage;
    using VRage.Game;
    using VRage.ObjectBuilders;
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
            mission.RemoveGps();
            EconomyScript.Instance.ClientConfig.Missions.Remove(mission);

            ConnectionHelper.SendMessageToServer(new MessageMission { CommandType = PlayerMissionManage.MissionComplete, MissionId = mission.MissionId });

            string msg = mission.GetSuccessMessage();
            if (mission.Reward != 0)
                msg += string.Format("\r\n{0} {1} Transferred to your account.", mission.Reward, EconomyScript.Instance.ClientConfig.ServerConfig.CurrencyName);
            //MyAPIGateway.Utilities.ShowMissionScreen("Mission:" + mission.MissionId, "", "Completed", msg, null, "Okay");

            Sandbox.Game.MyVisualScriptLogicProvider.ReplaceQuestlogDetail(0, msg);
            Sandbox.Game.MyVisualScriptLogicProvider.SetQuestlogDetailCompleted(0, true);
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

        public static void SendSyncMission(MissionBaseStruct mission)
        {
            ConnectionHelper.SendMessageToServer(new MessageMission { CommandType = PlayerMissionManage.SyncMission, Mission = mission });
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
                case PlayerMissionManage.AddMission:
                    // nothing to do here
                    break;

                case PlayerMissionManage.AcceptMission:
                    {
                        HudManager.FetchMission(MissionId);
                        MyAPIGateway.Utilities.ShowMessage("Server", "Setting contract {0}", MissionId);
                        if (!HudManager.UpdateHud()) { MyAPIGateway.Utilities.ShowMessage("Error", "Hud Failed"); }
                    }
                    break;
                case PlayerMissionManage.AbandonMission:
                    {
                        HudManager.FetchMission(-1);
                        var currentMission = EconomyScript.Instance.ClientConfig.Missions.FirstOrDefault(m => m.MissionId == MissionId);
                        currentMission.RemoveGps();
                    }
                    break;

                case PlayerMissionManage.MissionFailed:
                    {
                        var currentMission = EconomyScript.Instance.ClientConfig.Missions.FirstOrDefault(m => m.MissionId == MissionId);
                        currentMission.RemoveGps();

                        Sandbox.Game.MyVisualScriptLogicProvider.ReplaceQuestlogDetail(0, "Contract failed.");
                        Sandbox.Game.MyVisualScriptLogicProvider.SetQuestlogDetailCompleted(0, true);

                        MyAPIGateway.Utilities.ShowMessage("Server", $"Contract {currentMission.MissionId} failed");
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
                    MessageClientTextMessage.SendMessage(SenderSteamId, "mission", (MissionId + " server side"));
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
                            OfferDate = DateTime.Now,
                        }, 0);

                        MessageClientTextMessage.SendMessage(SenderSteamId, "Server", "Contract number {0} has been opened, {1} {2} has been detracted from your account", newMission.MissionId, newMission.Reward, EconomyScript.Instance.ServerConfig.CurrencyName);
                        MessageUpdateClient.SendServerMissions();
                    }
                    break;

                case PlayerMissionManage.AddMission:
                    {
                        var player = MyAPIGateway.Players.FindPlayerBySteamId(SenderSteamId);

                        var senderAccount = AccountManager.FindOrCreateAccount(SenderSteamId, SenderDisplayName, SenderLanguage);
                        if (senderAccount.BankBalance < Mission.Reward)
                        {
                            MessageClientTextMessage.SendMessage(SenderSteamId, "Server", "You don't have enough credits to open this contract");
                            break;
                        }

                        CreateMission(Mission, 0);
                        senderAccount.BankBalance -= Mission.Reward;
                        senderAccount.Date = DateTime.Now;

                        MessageClientTextMessage.SendMessage(SenderSteamId, "Server", "Contract number {0} has been opened and {1} {2} has been detracted from your account", Mission.MissionId, Mission.Reward, EconomyScript.Instance.ServerConfig.CurrencyName);
                        MessageUpdateClient.SendAccountMessage(senderAccount);
                        MessageUpdateClient.SendServerMissions();
                    }
                    break;

                case PlayerMissionManage.SyncMission:
                    {
                        var mission = GetMission(Mission.MissionId);
                        EconomyScript.Instance.Data.Missions.Remove(mission);
                        EconomyScript.Instance.Data.Missions.Add(Mission);
                        //mission.AcceptedBy = Mission.AcceptedBy;
                        //mission.SeenBriefing = Mission.SeenBriefing;
                        //mission.Expiration = Mission.Expiration;
                        MessageUpdateClient.SendServerMissions();
                    }
                    break;

                case PlayerMissionManage.DeleteMission:
                    {
                        var mission = GetMission(MissionId);
                        if (mission != null && mission.CreatedBy == SenderSteamId && mission.AcceptedBy == 0)
                        {
                            var player = MyAPIGateway.Players.FindPlayerBySteamId(mission.CreatedBy);
                            if (player != null)
                            {
                                var playerAccount = AccountManager.FindOrCreateAccount(player.SteamUserId, player.DisplayName, SenderLanguage);
                                playerAccount.BankBalance += mission.Reward;
                                playerAccount.Date = DateTime.Now;

                                MessageUpdateClient.SendAccountMessage(playerAccount);
                            }
                            RemoveMission(mission);
                            MessageClientTextMessage.SendMessage(SenderSteamId, "Server", "Contract number {0} has been closed, {1} {2} has been refunded into your account", mission.MissionId, mission.Reward, EconomyScript.Instance.ServerConfig.CurrencyName);
                            MessageUpdateClient.SendServerMissions();
                        }
                    }
                    break;

                case PlayerMissionManage.AcceptMission:
                    {
                        var mission = GetMission(MissionId);
                        if (mission != null && (mission.AcceptedBy == SenderSteamId || mission.AcceptedBy == 0))
                        {
                            mission.AcceptedBy = SenderSteamId;
                            mission.Expiration = DateTime.Now + TimeSpan.FromSeconds(3600);

                            MessageUpdateClient.SendServerMissions();
                            ConnectionHelper.SendMessageToPlayer(SenderSteamId, new MessageMission { CommandType = PlayerMissionManage.AcceptMission, MissionId = MissionId });

                            var senderAccount = AccountManager.FindOrCreateAccount(SenderSteamId, SenderDisplayName, SenderLanguage);
                            senderAccount.MissionId = MissionId;
                            senderAccount.Date = DateTime.Now;
                            MessageUpdateClient.SendAccountMessage(senderAccount);
                        }
                        else
                        {
                            MessageClientTextMessage.SendMessage(SenderSteamId, "Server", "The contract has been accepted by another pilot");
                        }
                    }
                    break;

                case PlayerMissionManage.MissionComplete:
                    {
                        // This should process the mission reward if appropriate and then delete from server.
                        // We aren't archiving finished missions.
                        var mission = GetMission(MissionId);
                        if (mission != null && mission.AcceptedBy == SenderSteamId)
                        {
                            mission.CompleteMission();
                            RemoveMission(mission);
                            MessageUpdateClient.SendServerMissions();
                        }
                    }
                    break;

                case PlayerMissionManage.AbandonMission:
                    {
                        var mission = GetMission(MissionId);
                        if (mission != null && mission.AcceptedBy == SenderSteamId)
                        {
                            mission.AcceptedBy = 0;
                            mission.Expiration = null;

                            MessageUpdateClient.SendServerMissions();
                            ConnectionHelper.SendMessageToPlayer(SenderSteamId, new MessageMission { CommandType = PlayerMissionManage.AbandonMission, MissionId = MissionId });

                            var senderAccount = AccountManager.FindOrCreateAccount(SenderSteamId, SenderDisplayName, SenderLanguage);
                            senderAccount.MissionId = 0;
                            senderAccount.Date = DateTime.Now;
                            MessageUpdateClient.SendAccountMessage(senderAccount);
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

        private static readonly FastResourceLock ExecutionLock = new FastResourceLock();

        private static MissionBaseStruct CreateMission(MissionBaseStruct mission, ulong assignToPlayer)
        {
            using (ExecutionLock.AcquireExclusiveUsing())
            {
                int newMissionId = 1;
                if (EconomyScript.Instance.Data.Missions.Count != 0)
                    newMissionId = EconomyScript.Instance.Data.Missions.Max(m => m.MissionId) + 1;

                mission.MissionId = newMissionId;
                mission.AcceptedBy = assignToPlayer;

                EconomyScript.Instance.Data.Missions.Add(mission);
            }
            return mission;
        }

        private static void RemoveMission(MissionBaseStruct mission)
        {
            using (ExecutionLock.AcquireExclusiveUsing())
            {
                EconomyScript.Instance.Data.Missions.Remove(mission);
            }
        }

        private static MissionBaseStruct GetMission(int missionId)
        {
            using (ExecutionLock.AcquireExclusiveUsing())
            {
                return EconomyScript.Instance.Data.Missions.FirstOrDefault(m => m.MissionId == missionId);
            }
        }
    }
}

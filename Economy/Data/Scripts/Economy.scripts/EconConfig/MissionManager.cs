using Economy.scripts.Messages;
using Economy.scripts.MissionStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage;

namespace Economy.scripts.EconConfig
{
    public static class MissionManager
    {
        public static void CheckMissions()
        {
            if (EconomyScript.Instance.Data != null)
            {
                foreach (var mission in EconomyScript.Instance.Data?.Missions?.Where(m => m.AcceptedBy != 0))
                {
                    if (DateTime.Now > mission.Expiration)
                    {
                        EconomyScript.Instance.ServerLogger.WriteInfo($"Contract {mission.MissionId} failed by {mission.AcceptedBy}");

                        var senderAccount = AccountManager.FindAccount(mission.AcceptedBy);
                        if (senderAccount != null)
                        {
                            senderAccount.MissionId = 0;
                            senderAccount.Date = DateTime.Now;
                            MessageUpdateClient.SendAccountMessage(senderAccount);
                        }

                        MessageMission.SendMissionFailed(mission);
                        mission.ResetMission();
                        MessageUpdateClient.SendServerMissions();
                    }
                    else if (mission.CheckMission())
                    {
                        mission.CompleteMission();
                        RemoveMission(mission);
                        MessageMission.SendMissionComplete(mission);
                        MessageUpdateClient.SendServerMissions();
                        EconomyScript.Instance.ServerLogger.WriteInfo($"Contract {mission.MissionId} completed by {mission.AcceptedBy}");
                        break;
                    }
                }
            }
        }

        private static readonly FastResourceLock ExecutionLock = new FastResourceLock();

        public static MissionBaseStruct CreateMission(MissionBaseStruct mission, ulong assignToPlayer)
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

        public static void RemoveMission(MissionBaseStruct mission)
        {
            using (ExecutionLock.AcquireExclusiveUsing())
            {
                EconomyScript.Instance.Data.Missions.Remove(mission);
            }
        }

        public static MissionBaseStruct GetMission(int missionId)
        {
            using (ExecutionLock.AcquireExclusiveUsing())
            {
                return EconomyScript.Instance.Data.Missions.FirstOrDefault(m => m.MissionId == missionId);
            }
        }
    }
}

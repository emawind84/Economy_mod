using Economy.scripts.Messages;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Economy.scripts.EconConfig
{
    public static class MissionManager
    {
        /// <summary>
        /// [SERVER SIDE] Check accepted missions and make sure they fail if expired
        /// </summary>
        public static void CheckMissionTimeouts()
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
                }
            }
        }
    }
}

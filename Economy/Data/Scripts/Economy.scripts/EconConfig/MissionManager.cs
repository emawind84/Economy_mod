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
        /// Check accepted missions and make sure they fail if expired
        /// </summary>
        public static void CheckMissionTimeouts()
        {
            if (EconomyScript.Instance.Data != null)
            {
                foreach (var mission in EconomyScript.Instance.Data?.Missions?.Where(m => m.AcceptedBy != 0))
                {
                    if (DateTime.Now > mission.Expiration)
                    {
                        MessageMission.SendMissionFailed(mission);
                        mission.ResetMission();
                        MessageUpdateClient.SendServerMissions();
                    }
                }
            }
        }
    }
}

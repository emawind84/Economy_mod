namespace Economy.scripts.MissionStructures
{
    using Economy.scripts.EconConfig;
    using Economy.scripts.Messages;
    using ProtoBuf;
    using Sandbox.Game.Entities;
    using Sandbox.ModAPI;
    using Sandbox.ModAPI.Ingame;
    using System;
    using System.Collections.Generic;
    using System.Text;
    using VRage;
    using VRage.Game.ModAPI;
    using VRage.ModAPI;
    using VRageMath;

    //eg destroy/remove warhead, remove reactor etc - grinding or blowing up
    [ProtoContract]
    public class BlockDestroyMission : MissionBaseStruct
    {

        [ProtoMember(201)]
        public long EntityId { get; set; }

        [ProtoMember(202)]
        public string EntityName { get; set; }

        [ProtoMember(203)]
        public float EntityIntegrity { get; set; } = -1;

        [ProtoMember(204)]
        public SerializableVector3D EntityLastPosition { get; set; }

        public override string GetName()
        {
            return "A delicate matter for discreet pilot.";
        }

        public override string GetDescription()
        {
            return $"We are looking for a discreet pilot to resolve a case dear to us." +
                $"\r\nWe need to get rid of a ship, its name is {EntityName}." +
                $"\r\nWe count on your discretion to keep these information for yourself.";
        }

        public override string GetShortDescription()
        {
            return $"We ask you to get rid of a ship named {EntityName}.";
        }

        public override string GetSuccessMessage()
        {
            return $"The contract has ended, thank you for your service.";
        }

        public override void AddGps()
        {
            if (EntityLastPosition != Vector3D.Zero)
                Sandbox.Game.MyVisualScriptLogicProvider.AddGPSObjective($"{EntityName} - Last Position", 
                    GetDescription(), new Vector3D(EntityLastPosition.X, EntityLastPosition.Y, EntityLastPosition.Z), Color.Red);
        }

        public override void RemoveGps()
        {
            if (EntityLastPosition != Vector3D.Zero)
                Sandbox.Game.MyVisualScriptLogicProvider.RemoveGPS($"{EntityName} - Last Position");
        }

        public override bool CheckMission()
        {
            IMyPlayer player;
            if (MyAPIGateway.Players.TryGetPlayer(AcceptedBy, out player))
            {
                var entity = MyAPIGateway.Entities.GetEntityById(EntityId) as MyCubeGrid;
                if (entity != null)
                {
                    Vector3D position = player.GetPosition();
                    BoundingSphereD boundingSphereD = new BoundingSphereD(player.GetPosition(), 500);
                    if (boundingSphereD.Contains((entity as IMyEntity).GetPosition()) == ContainmentType.Contains)
                    {
                        float totalIntegrity = 0;
                        foreach (IMySlimBlock block in entity.CubeBlocks)
                        {
                            totalIntegrity += block.Integrity;
                        }
                        if (EntityIntegrity == -1)
                        {
                            EntityIntegrity = totalIntegrity;
                            MessageMission.SendSyncMission(this);
                        }

                        //MyAPIGateway.Utilities.ShowMessage("Server", $"Integrity: {EntityIntegrity}/{totalIntegrity}");
                        Sandbox.Game.MyVisualScriptLogicProvider.ClearNotifications();
                        Sandbox.Game.MyVisualScriptLogicProvider.ShowNotification($"{EntityName}: {EntityIntegrity}/{totalIntegrity}", 5000);
                        return totalIntegrity * 100 / EntityIntegrity < 40;
                    }
                }
                else
                {
                    return true;
                }
            }
            return false;
        }

        public override void CompleteMission()
        {
            var player = MyAPIGateway.Players.FindPlayerBySteamId(AcceptedBy);
            if (player != null)
            {
                var playerAccount = AccountManager.FindOrCreateAccount(player.SteamUserId, player.DisplayName, 0);

                EconomyScript.Instance.Data.CreditBalance -= Reward;
                playerAccount.BankBalance += Reward;
                playerAccount.Date = DateTime.Now;

                MessageUpdateClient.SendAccountMessage(playerAccount);
                MessageClientSound.SendMessage(AcceptedBy, "SoundBlockObjectiveComplete");
            }
        }
    }
}

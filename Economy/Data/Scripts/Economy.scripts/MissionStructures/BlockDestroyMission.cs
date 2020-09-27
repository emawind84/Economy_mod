namespace Economy.scripts.MissionStructures
{
    using Economy.scripts.EconConfig;
    using Economy.scripts.Messages;
    using ProtoBuf;
    using Sandbox.Game.Entities;
    using Sandbox.ModAPI;
    using System;
    using System.Collections.Generic;
    using System.Linq;
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
                $"\r\nWe need to get rid of a target, its name is {EntityName}." +
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
                    GetDescription(), new Vector3D(EntityLastPosition.X, EntityLastPosition.Y, EntityLastPosition.Z), Color.Red, 0, MyAPIGateway.Session.Player.IdentityId);
        }

        public override void RemoveGps()
        {
            if (EntityLastPosition != Vector3D.Zero)
                Sandbox.Game.MyVisualScriptLogicProvider.RemoveGPS($"{EntityName} - Last Position", MyAPIGateway.Session.Player.IdentityId);
        }

        public override bool PrepareMission(out string message)
        {
            IMyEntity entity;
            Vector3D entityLastPosition = Vector3D.Zero;

            if (!MyAPIGateway.Entities.TryGetEntityById(EntityId, out entity))
            {
                var entities = new HashSet<IMyEntity>();
                MyAPIGateway.Entities.GetEntities(entities, e => e is IMyCubeGrid);
                entity = entities.FirstOrDefault(e => e.DisplayName.Equals(EntityName, StringComparison.OrdinalIgnoreCase));
            }

            IMyPlayer creator = MyAPIGateway.Players.GetPlayer(CreatedBy);
            if (entity != null && creator?.Character != null)
            {
                BoundingSphereD boundingSphereD = new BoundingSphereD(creator.GetPosition(), 500);
                if (boundingSphereD.Contains(entity.GetPosition()) == ContainmentType.Contains)
                {
                    entityLastPosition = entity.GetPosition();
                }
            }
            else
            {
                message = "No valid target found";
                return false;
            }
            message = "";
            return true;
        }

        /// <summary>
        /// This method is executed on client side to check if the mission is completed
        /// </summary>
        /// <returns></returns>
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
                        Sandbox.Game.MyVisualScriptLogicProvider.ClearNotifications(MyAPIGateway.Session.Player.IdentityId);
                        Sandbox.Game.MyVisualScriptLogicProvider.ShowNotification($"{EntityName}: {EntityIntegrity}/{totalIntegrity}", 5000, "White", MyAPIGateway.Session.Player.IdentityId);
                        return totalIntegrity * 100 / EntityIntegrity < 20;
                    }
                }
                else
                {
                    // we can not return true, the entity might exists in the world but not being loaded on clien side
                    //return true;
                }
            }
            return false;
        }

        /// <summary>
        /// This method is executed on server side to reward the player
        /// </summary>
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
                MessageClientTextMessage.SendMessage(AcceptedBy, "CONTRACT", "The payment has been added to your balance");
            }
        }

        public override void ResetMission()
        {
            base.ResetMission();

            EntityIntegrity = -1;
        }
    }
}

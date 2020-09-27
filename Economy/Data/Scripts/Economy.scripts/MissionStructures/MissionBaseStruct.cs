namespace Economy.scripts.MissionStructures
{
    using System;
    using System.Text;
    using System.Xml.Serialization;
    using ProtoBuf;
    using Sandbox.ModAPI;

    // for xml serialization to save/load disk by server.
    [XmlType("Mission")]
    [XmlInclude(typeof(BlockActivateMission))]
    [XmlInclude(typeof(BlockCaptureMission))]
    [XmlInclude(typeof(BlockDeactivateMission))]
    [XmlInclude(typeof(BlockDestroyMission))]
    [XmlInclude(typeof(BuySomethingMission))]
    [XmlInclude(typeof(DeliverItemToTradeZoneMission))]
    [XmlInclude(typeof(JoinFactionMission))]
    [XmlInclude(typeof(KillPlayerMission))]
    [XmlInclude(typeof(MineMission))]
    [XmlInclude(typeof(PayPlayerMission))]
    [XmlInclude(typeof(StayAliveMission))]
    [XmlInclude(typeof(TradeWithPlayerMission))]
    [XmlInclude(typeof(TravelMission))]
    [XmlInclude(typeof(UseAccountBalanceMission))]
    [XmlInclude(typeof(UseBuySellShipMission))]
    [XmlInclude(typeof(UseWorthMission))]
    [XmlInclude(typeof(WeldMission))]

    [ProtoContract]
    // for binary serialization to send server->client
    [ProtoInclude(1, typeof(BlockActivateMission))]
    [ProtoInclude(2, typeof(BlockCaptureMission))]
    [ProtoInclude(3, typeof(BlockDeactivateMission))]
    [ProtoInclude(4, typeof(BlockDestroyMission))]
    [ProtoInclude(5, typeof(BuySomethingMission))]
    [ProtoInclude(6, typeof(DeliverItemToTradeZoneMission))]
    [ProtoInclude(7, typeof(JoinFactionMission))]
    [ProtoInclude(8, typeof(KillPlayerMission))]
    [ProtoInclude(9, typeof(MineMission))]
    [ProtoInclude(10, typeof(PayPlayerMission))]
    [ProtoInclude(11, typeof(StayAliveMission))]
    [ProtoInclude(12, typeof(TradeWithPlayerMission))]
    [ProtoInclude(13, typeof(TravelMission))]
    [ProtoInclude(14, typeof(UseAccountBalanceMission))]
    [ProtoInclude(15, typeof(UseBuySellShipMission))]
    [ProtoInclude(16, typeof(UseWorthMission))]
    [ProtoInclude(17, typeof(WeldMission))]
    public abstract class MissionBaseStruct
    {

        protected MissionBaseStruct()
        {
            OfferDate = DateTime.Now;
            if (MyAPIGateway.Multiplayer.IsServer)
                CreatedBy = MyAPIGateway.Multiplayer.ServerId;
            if (MyAPIGateway.Session.Player != null)
                CreatedBy = MyAPIGateway.Session.Player.SteamUserId;
        }

        /// <summary>
        /// Unique identifier of the mission.
        /// </summary>
        [ProtoMember(101)]
        public int MissionId { get; set; }

        /// <summary>
        /// Indicates what sort of player/group the mission is assigned to.
        /// </summary>
        [ProtoMember(102)]
        public MissionAssignmentType AssignmentType { get; set; }

        /// <summary>
        /// The player that created the mission.
        /// </summary>
        [ProtoMember(103)]
        public ulong CreatedBy { get; set; }

        /// <summary>
        /// The player the mission is assigned to.
        /// </summary>
        [ProtoMember(104)]
        public ulong AcceptedBy { get; set; }

        /// <summary>
        /// An identifier is used when the same mission is assigned to many people.
        /// When one individual wins, some rule may be applied to the other missions.
        /// </summary>
        // Wish we could use System.Guid, except it is not allowed in ModAPI.
        [ProtoMember(105)]
        public Int64 GroupMissionId { get; set; }

        /// <summary>
        /// Indicates who can complete the mission and recieve the reward out of the assigned players.
        /// </summary>
        [ProtoMember(106)]
        public MissionWinRule WinRule { get; set; }

        /// <summary>
        /// How much credit is recieved when the mission is completed sucessfully.
        /// </summary>
        [ProtoMember(107)]
        public decimal Reward { get; set; }

        /// <summary>
        /// When the Mission was created and listed.
        /// </summary>
        [ProtoMember(108)]
        public DateTime OfferDate { get; set; }

        /// <summary>
        /// The Date/Time that a Mission will expire (if it expires).
        /// </summary>
        [ProtoMember(109)]
        public DateTime? Expiration { get; set; }

        /// <summary>
        /// If the assigned player has been presented with the Briefing yet.
        /// </summary>
        [ProtoMember(110)]
        public bool SeenBriefing { get; set; }

        /// <summary>
        /// The short name of the mission, to appear in the Hud.
        /// </summary>
        /// <returns></returns>
        public virtual string GetName()
        {
            return string.Empty;
        }

        /// <summary>
        /// A full description of the mission parameters.
        /// </summary>
        /// <returns></returns>
        public virtual string GetDescription()
        {
            return string.Empty;
        }

        /// <summary>
        /// Brief description used in the contract list
        /// </summary>
        /// <returns></returns>
        public virtual string GetShortDescription()
        {
            return string.Empty;
        }

        /// <summary>
        /// Message displayed when the mission is completed sucessfully.
        /// Note, that it may be concatenated with other text, including the reward.
        /// </summary>
        public virtual string GetSuccessMessage()
        {
            return string.Empty;
        }

        public virtual bool PrepareMission(out string message)
        {
            message = "";
            return true;
        }

        /// <summary>
        /// Checks if the mission has met it's criteria and someone has won.
        /// </summary>
        /// <returns></returns>
        public virtual bool CheckMission()
        {
            return false;
        }

        /// <summary>
        /// Complete the mission
        /// </summary>
        public virtual void CompleteMission() {}

        public virtual void ResetMission()
        {
            AcceptedBy = 0;
            Expiration = null;
        }

        /// <summary>
        /// Checks and adds GPS coorindate to player if required by the Mission.
        /// </summary>
        public virtual void AddGps()
        {
        }

        public virtual void RemoveGps()
        {
        }

        public virtual string GetFullDescription()
        {
            ulong SenderSteamId = 0;
            if (MyAPIGateway.Multiplayer.IsServer)
                SenderSteamId = MyAPIGateway.Multiplayer.ServerId;
            if (MyAPIGateway.Session.Player != null)
                SenderSteamId = MyAPIGateway.Session.Player.SteamUserId;

            StringBuilder description = new StringBuilder();
            if (EconomyScript.Instance.ClientConfig != null)
                description.AppendLine(string.Format("- Payment: {0:#,##0.00} {1}", Reward, EconomyScript.Instance.ClientConfig.ServerConfig.CurrencyName));
            else
                description.AppendLine(string.Format("- Payment: {0:#,##0.00} {1}", Reward, EconomyScript.Instance.ServerConfig.CurrencyName));

            var createdBy = MyAPIGateway.Players.FindPlayerBySteamId(CreatedBy);
            //description.AppendLine($"- From: {createdBy?.DisplayName ?? "Unknown"}");
            if (AcceptedBy != 0 && CreatedBy == SenderSteamId)
            {
                var acceptedBy = MyAPIGateway.Players.FindPlayerBySteamId(AcceptedBy);
                description.AppendLine($"- Accepted by: {acceptedBy?.DisplayName ?? "Unknown"}");
            }

            if (Expiration != null)
            {
                var timeleft = (Expiration - DateTime.Now);
                //description.AppendLine($"- Valid until: {Expiration?.ToString("dddd, dd MMMM yyyy HH:mm:ss") ?? "NA"}");
                description.AppendLine($"- Time left: {timeleft.Value.Hours}H {timeleft.Value.Minutes}MIN");
            }
            description.AppendLine();
            description.AppendLine();
            description.AppendLine(GetDescription());

            return description.ToString();
        }
    }
}

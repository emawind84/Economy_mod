namespace Economy.scripts.MissionStructures
{
    using Economy.scripts.EconConfig;
    using Economy.scripts.Messages;
    using ProtoBuf;
    using Sandbox.ModAPI;
    using System;
    using System.Linq;
    using VRage.Game.ModAPI;
    using VRageMath;

    //only applies if we implement emergency restock contract missions
    [ProtoContract]
    public class DeliverItemToTradeZoneMission : MissionBaseStruct
    {
        [ProtoMember(201)]
        public decimal ItemQuantity { get; set; }

        [ProtoMember(202)]
        public string ItemTypeId { get; set; }

        [ProtoMember(203)]
        public string ItemSubTypeName { get; set; }

        [ProtoMember(204)]
        public string MarketName { get; set; }

        [ProtoMember(205)]
        public ulong MarketId { get; set; }

        [ProtoMember(206)]
        public bool Delivered { get; set; }
        
        public DeliverItemToTradeZoneMission() : base()
        {
            MessageSell.OnSellCommandExecuted += CheckDeliverAndUpdateStatus;
        }

        private bool CheckDeliverAndUpdateStatus(ulong senderSteamId, ulong marketId, string itemTypeId, string itemSubtypeName, decimal itemQuantity, out string message)
        {
            message = null;  // sending a direct text message here might result in multiple request so we use an output variable
            IMyPlayer player;
            if (MyAPIGateway.Players.TryGetPlayer(AcceptedBy, out player))
            {
                //MessageClientTextMessage.SendMessage(AcceptedBy, "Contract", "Sell command received");

                var markets = MarketManager.FindMarketsFromLocation(player.GetPosition());
                var market = markets.FirstOrDefault(m => m.DisplayName.Equals(MarketName, StringComparison.OrdinalIgnoreCase));
                if (market != null 
                    && marketId == MarketId
                    && senderSteamId == AcceptedBy && itemTypeId == ItemTypeId 
                    && itemSubtypeName == ItemSubTypeName)
                {
                    if (itemQuantity == ItemQuantity)
                    {
                        Delivered = true;
                        MessageSell.OnSellCommandExecuted -= CheckDeliverAndUpdateStatus;
                        MessageUpdateClient.SendServerMissions();
                    }
                    else
                    {
                        message = $"{ItemQuantity} kg are requested by the contract, this is not negotiable";
                    }
                    return true;
                }
            }
            return false;
        }

        public override string GetName()
        {
            return "We need you to run a shipment for us.";
        }

        public override string GetDescription()
        {
            string suffix = "";
            if (ItemTypeId == "MyObjectBuilder_Ore") suffix = " Ore";
            else if (ItemTypeId == "MyObjectBuilder_Ingot") suffix = " Ingot";
            var item = ItemSubTypeName + suffix;
            return $"We need {ItemQuantity} kg of {item} delivered over at {MarketName}.\r\nA GPS point will be created for you.";
        }

        public override string GetShortDescription()
        {
            string suffix = "";
            if (ItemTypeId == "MyObjectBuilder_Ore") suffix = " Ore";
            else if (ItemTypeId == "MyObjectBuilder_Ingot") suffix = " Ingot";
            var item = ItemSubTypeName + suffix;
            return $"We need {ItemQuantity} kg of {item} delivered to {MarketName}.";
        }

        public override string GetSuccessMessage()
        {
            return "Thank you for your business.";
        }

        public override void AddGps()
        {
            var market = EconomyScript.Instance.ClientConfig.Markets.Find(m => m.DisplayName == MarketName);
            if (market != null)
            {
                var position = MarketManager.FindPositionFromMarket(market);
                //EconConfig.HudManager.GPS(position.X, position.Y, position.Z, "Contract Objective " + MissionId, GetName(), true);
                Sandbox.Game.MyVisualScriptLogicProvider.AddGPSObjective("Delivering Location", GetDescription(), new Vector3D(position.X, position.Y, position.Z), Color.Green, 0, MyAPIGateway.Session.Player.IdentityId);
            }
        }

        public override void RemoveGps()
        {
            var market = EconomyScript.Instance.ClientConfig.Markets.Find(m => m.DisplayName == MarketName);
            if (market != null)
            {
                var position = MarketManager.FindPositionFromMarket(market);
                Sandbox.Game.MyVisualScriptLogicProvider.RemoveGPS("Delivering Location", MyAPIGateway.Session.Player.IdentityId);
                //HudManager.GPS(position.X, position.Y, position.Z, "Contract Objective " + MissionId, GetName(), false);
            }
        }
        
        public override bool CheckMission()
        {
            var markets = MarketManager.ClientFindMarketsFromName(EconomyScript.Instance.Data.Markets, MarketName);
            if (markets.Count() != 1)
            {
                // we close the mission since the market cannot be found anymore
                // player might have closed it or removed it, we reward the contractor anyway.
                return true;
            }
            return Delivered;
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
                MessageClientTextMessage.SendMessage(AcceptedBy, "Contract", "The payment has been added to your balance");
            }

            var market = MarketManager.FindMarketsFromName(MarketName).FirstOrDefault();
            if (market != null)
            {
                var marketItem = market.MarketItems.FirstOrDefault(e => e.TypeId == ItemTypeId && e.SubtypeName == ItemSubTypeName);
                if (marketItem != null)
                {
                    marketItem.Quantity += ItemQuantity;
                    MessageClientTextMessage.SendMessage(CreatedBy, "Contract", $"The item you requested has been delivered");
                }
            }
        }

    }
}

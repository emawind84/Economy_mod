namespace Economy.scripts.Messages
{
    using ProtoBuf;
    using Sandbox.ModAPI;

    [ProtoContract]
    public class MessageClientDialogMessage : MessageBase
    {
        [ProtoMember(201)]
        public string Title;

        [ProtoMember(202)]
        public string Prefix;

        [ProtoMember(203)]
        public string Content;

        public override void ProcessClient()
        {
            MyAPIGateway.Utilities.ShowMissionScreen(Title, Prefix, " ", Content);
        }

        public override void ProcessServer()
        {
            // never processed on server.
        }

        public static void SendMessage(ulong steamId, string title, string prefix, string content, params object[] args)
        {
            string message;
            if (args == null || args.Length == 0)
                message = content;
            else
                message = string.Format(content, args);

            ConnectionHelper.SendMessageToPlayer(steamId, new MessageClientDialogMessage { Title = title, Prefix = prefix, Content = message });
        }
    }
}

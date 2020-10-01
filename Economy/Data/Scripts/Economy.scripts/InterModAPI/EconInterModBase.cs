namespace Economy.scripts.InterModAPI
{
    using Economy.scripts;
    using Economy.scripts.InterModAPI;
    using ProtoBuf;
    using Sandbox.ModAPI;
    using System;
    using VRage;

    // ALL CLASSES DERIVED FROM MessageBase MUST BE ADDED HERE
    [ProtoContract]
    [ProtoInclude(1, typeof(EconPayUser))]
    [ProtoInclude(2, typeof(EconPayUserResponse))]
    [ProtoInclude(3, typeof(EconCommandMessage))]
    public abstract class EconInterModBase
    {
        [ProtoMember(101)]
        public ushort CallbackModChannel;

        [ProtoMember(102)]
        public ulong SenderId;

        public void InvokeProcessing()
        {
            EconomyScript.Instance.ServerLogger.WriteVerbose("Received - {0}", this.GetType().Name);
            try
            {
                if (MyAPIGateway.Multiplayer.IsServer)
                    ProcessServer();
                else
                    ProcessClient();
            }
            catch (Exception ex)
            {
                EconomyScript.Instance.ServerLogger.WriteException(ex);
            }
        }

        public virtual void ProcessServer() { }

        public virtual void ProcessClient() { }

        public void SendResponseMessage(ushort callbackModChannel)
        {
            // a channel of zero means it hasn't been set by the caller.
            if (callbackModChannel == 0)
                return;

            EconomyScript.Instance.ServerLogger.WriteStart("Sending Reponse: {0}, Channel={1}", this.GetType().Name, callbackModChannel);
            byte[] byteData = MyAPIGateway.Utilities.SerializeToBinary(this);
            var compressedData = MyCompression.Compress(byteData);
            //MyAPIGateway.Multiplayer.SendMessageToServer(callbackModChannel, compressedData);
            MyAPIGateway.Utilities.SendModMessage(callbackModChannel, compressedData);
        }
    }
}

namespace Economy.scripts.InterModAPI
{
    using ProtoBuf;

    public enum EconPayUserMessage
    {
        PaymentsNotEnabled = 0,
        InvalidRequest = 1,
        NoSenderAccount = 2,
        NoRepientAccount = 3,
        InsufficientFunds = 4,
        Success = 5
    }

    [ProtoContract]
    public class EconPayUserResponse : EconInterModBase
    {
        [ProtoMember(201)]
        public EconPayUserMessage Message;

        [ProtoMember(202)]
        public long TransactionId;

        public static void SendMessage(ushort callbackModChannel, long transactionId, EconPayUserMessage message)
        {
            EconPayUserResponse response = new EconPayUserResponse { Message = message, TransactionId = transactionId };
            response.SendResponseMessage(callbackModChannel);
        }
    }
}

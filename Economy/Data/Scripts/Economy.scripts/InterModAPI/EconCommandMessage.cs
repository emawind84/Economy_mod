using Economy.scripts.Messages;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Economy.scripts.InterModAPI
{
    [ProtoContract]
    class EconCommandMessage : EconInterModBase
    {
        [ProtoMember(201)]
        public string Command;

        public override void ProcessClient()
        {
            EconomyScript.Instance.ProcessMessage(Command);
        }
    }
}

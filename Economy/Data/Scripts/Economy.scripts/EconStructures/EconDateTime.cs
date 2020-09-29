using ProtoBuf;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Economy.scripts.EconStructures
{
    [ProtoContract]
    public class EconDateTime
    {
        [XmlIgnore]
        public DateTime? Date;

        public static EconDateTime Now => new EconDateTime { Date = DateTime.SpecifyKind(DateTime.Now, DateTimeKind.Local) };

        [XmlText(typeof(string))]
        [ProtoMember(1)]
        public string FormattedDate {
            get {
                return Date?.ToString("o");
            }
            set
            {
                Date = DateTime.Parse(value, null, DateTimeStyles.RoundtripKind);
            }
        }
    }
}

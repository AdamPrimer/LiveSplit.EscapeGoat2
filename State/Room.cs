using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LiveSplit.EscapeGoat2Autosplitter.State
{
    public class Room
    {
        public int id { get; set; }
        public string name { get; set; }
        public object wing { get; set; }
        public object room { get; set; }
        public RoomType type { get; set; }

        public Room(int id, string name, object wing, object room, RoomType type) {
            this.id = id;
            this.name = name;
            this.wing = wing;
            this.room = room;
            this.type = type;
        }

        public override string ToString() {
            if (this.wing.ToString() != "Spine") {
                return String.Format("Room {0}-{1} ({2}): {3}", this.wing, this.room, this.id, this.name);
            } else {
                return String.Format("Room {0} ({1}): {2}", this.room, this.id, this.name);
            }
        }
    }

    public enum RoomType
    {
        Normal,
        Sheep,
        Secret
    }
}

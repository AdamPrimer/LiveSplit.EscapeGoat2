using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LiveSplit.EscapeGoat2Autosplitter.State
{
    public class WorldMap
    {
        private Dictionary<int, Room> _rooms = new Dictionary<int, Room>();

        private Dictionary<object, string> wingNames = new Dictionary<object, string>() {
            {"Spine", "Spine of the Stronghold"},
            {1, "Overgrown Pathway"},
            {2, "Woods of Duplicity"},
            {3, "Excavation"},
            {4, "Sequestered Library"},
            {5, "Parish of the Necromouser"},
            {6, "Halls of Gleaming Crystal"},
            {7, "Temple of Adoration"},
            {8, "Grave of Anguish"},
            {9, "Lair of Toragos"},
            {"S", "Through the Stained Glass"},
        };

        public WorldMap() {
            AddRooms();
        }

        public List<Room> GetRooms() {
            List<Room> rooms = new List<Room>();
            rooms = rooms.Concat(GetWing("Spine")).ToList();
            rooms = rooms.Concat(GetWing(1)).ToList();
            rooms = rooms.Concat(GetWing(2)).ToList();
            rooms = rooms.Concat(GetWing(3)).ToList();
            rooms = rooms.Concat(GetWing(4)).ToList();
            rooms = rooms.Concat(GetWing(5)).ToList();
            rooms = rooms.Concat(GetWing(6)).ToList();
            rooms = rooms.Concat(GetWing(7)).ToList();
            rooms = rooms.Concat(GetWing(8)).ToList();
            rooms = rooms.Concat(GetWing(9)).ToList();
            rooms = rooms.Concat(GetWing("S")).ToList();
            return rooms;
        }

        public List<Room> GetWing(object wing) {
            List<Room> rooms = new List<Room>();
            foreach (var pair in _rooms) {
                int roomID = pair.Key;
                Room room = pair.Value;

                if (room.wing.Equals(wing)) {
                    rooms.Add(room);
                }
            }
            return rooms.OrderBy(o => o.room).ToList();
        }

        public Room GetRoom(int id) {
            return _rooms[id];
        }

        private void AddRoom(int id, object wing, object room) {
            _rooms[id] = new Room(id, wingNames[wing], wing, room, RoomType.Normal);
        }
        private void AddRoom(int id, object wing, object room, RoomType type) {
            _rooms[id] = new Room(id, wingNames[wing], wing, room, type);
        }

        private void AddRooms() {
            // Row 1
            AddRoom(212, 8, 9);
            AddRoom(161, 8, 8);
            AddRoom(223, 8, 7);
            AddRoom(22, 9, 9);
            AddRoom(201, 9, 8);
            AddRoom(166, 9, 7);
            AddRoom(180, 9, 6);
            AddRoom(231, 9, 5);
            AddRoom(147, 9, 4);
            AddRoom(138, 7, 7);
            AddRoom(41, 7, 8);

            // Row 2
            AddRoom(89, 8, 10);
            AddRoom(142, 8, 11, RoomType.Sheep);
            AddRoom(207, 8, 6);
            AddRoom(221, 9, 10);
            AddRoom(95, 9, 11);
            AddRoom(113, "Spine", 8);
            AddRoom(217, 9, 1);
            AddRoom(220, 9, 2);
            AddRoom(219, 9, 3);
            AddRoom(215, 7, 6);
            AddRoom(156, 7, 9);

            // Row 3
            AddRoom(179, 6, 11, RoomType.Sheep);
            AddRoom(165, "S", 9, RoomType.Secret);
            AddRoom(175, 8, 5, RoomType.Sheep);
            AddRoom(57, 8, 4);
            AddRoom(119, 8, 1);
            AddRoom(117, "Spine", 7);
            AddRoom(99, "S", 12, RoomType.Secret);
            AddRoom(136, 7, 3);
            AddRoom(139, 7, 4);
            AddRoom(176, 7, 5, RoomType.Sheep);
            AddRoom(132, 7, 10);

            // Row 4
            AddRoom(227, 6, 10);
            AddRoom(203, 6, 7);
            AddRoom(68, 6, 6);
            AddRoom(18, 8, 3);
            AddRoom(54, 8, 2);
            AddRoom(116, "Spine", 6);
            AddRoom(186, 7, 1);
            AddRoom(195, 7, 2);
            AddRoom(105, "S", 10, RoomType.Secret);
            AddRoom(311, "S", 11, RoomType.Secret);
            AddRoom(14, 7, 11, RoomType.Sheep);

            // Row 5
            AddRoom(44, 6, 9);
            AddRoom(155, 6, 8);
            AddRoom(42, 6, 5, RoomType.Sheep);
            AddRoom(129, 6, 4);
            AddRoom(151, 6, 1);
            AddRoom(115, "Spine", 5);
            AddRoom(193, 5, 2);
            AddRoom(16, 5, 3);
            AddRoom(12, 5, 4);
            AddRoom(76, 5, 5, RoomType.Sheep);
            AddRoom(64, 5, 6);

            // Row 6
            AddRoom(206, "S", 5, RoomType.Secret);
            AddRoom(86, 4, 11, RoomType.Sheep);
            AddRoom(153, "S", 6, RoomType.Secret);
            AddRoom(170, 6, 3);
            AddRoom(104, 6, 2);
            AddRoom(110, "Spine", 4);
            AddRoom(141, 5, 1);
            AddRoom(81, "S", 4, RoomType.Secret);
            AddRoom(183, 5, 11, RoomType.Sheep);
            AddRoom(172, 5, 10);
            AddRoom(171, 5, 7);

            // Row 7
            AddRoom(123, 4, 9);
            AddRoom(224, 4, 10);
            AddRoom(9, 4, 3);
            AddRoom(124, 4, 2);
            AddRoom(15, 4, 1);
            AddRoom(109, "Spine", 3);
            AddRoom(58, 3, 2);
            AddRoom(78, 3, 3);
            AddRoom(204, "S", 7, RoomType.Secret);
            AddRoom(38, 5, 9);
            AddRoom(66, 5, 8);

            // Row 8
            AddRoom(158, 4, 8);
            AddRoom(177, 4, 5, RoomType.Sheep);
            AddRoom(4, 4, 4);
            AddRoom(232, 2, 3);
            AddRoom(23, 2, 2);
            AddRoom(108, "Spine", 2);
            AddRoom(1, 3, 1);
            AddRoom(37, 3, 4);
            AddRoom(80, 3, 5, RoomType.Sheep);
            AddRoom(145, 3, 6);
            AddRoom(210, "S", 8, RoomType.Secret);

            // Row 9
            AddRoom(26, 4, 7);
            AddRoom(25, 4, 6);
            AddRoom(154, "S", 1, RoomType.Secret);
            AddRoom(47, 2, 4);
            AddRoom(11, 2, 1);
            AddRoom(107, "Spine", 1);
            AddRoom(103, 1, 6, RoomType.Sheep);
            AddRoom(169, 1, 5);
            AddRoom(75, 1, 4);
            AddRoom(30, 3, 7);
            AddRoom(137, 3, 8);

            // Row 10
            AddRoom(131, 2, 10);
            AddRoom(182, 2, 11, RoomType.Sheep);
            AddRoom(202, 2, 6);
            AddRoom(102, 2, 5, RoomType.Sheep);
            AddRoom(36, "S", 2, RoomType.Secret);
            AddRoom(213, "S", 13, RoomType.Secret);
            AddRoom(70, 1, 7);
            AddRoom(72, 1, 2);
            AddRoom(73, 1, 3);
            AddRoom(121, "S", 3, RoomType.Secret);
            AddRoom(34, 3, 9);

            // Row 11
            AddRoom(194, 2, 9);
            AddRoom(61, 2, 8);
            AddRoom(53, 2, 7);
            AddRoom(178, 1, 11, RoomType.Sheep);
            AddRoom(65, 1, 10);
            AddRoom(173, 1, 9);
            AddRoom(50, 1, 8);
            AddRoom(149, 1, 1);
            AddRoom(125, "S", 14, RoomType.Secret);
            AddRoom(181, 3, 11, RoomType.Sheep);
            AddRoom(98, 3, 10);
        }
    }
}

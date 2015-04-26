using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Microsoft.Diagnostics.Runtime;
using Microsoft.Diagnostics.Runtime.Interop;

namespace LiveSplit.EscapeGoat2Autosplitter
{
    class EscapeGoat
    {
        static void Main(string[] args) {
            Thread.CurrentThread.Priority = ThreadPriority.AboveNormal;

            using (var pm = AttachToProcess("escapegoat2")) {
                bool started = false;
                int roomID = 0;
                int lastRoomID = 0;
                bool inGame = false;
                bool lastInGame = false;
                bool isDead = false;

                while (true) {
                    var title = new StaticField(pm.Runtime, "MagicalTimeBean.Bastille.Scenes.SceneManager", "<TitleScreenInstance>k__BackingField");
                    bool titleShown = title.Value.Value.GetFieldValue<Boolean>("_titleShown");
                    int titleFadeTimer = title.Value.Value.GetFieldValue<Int32>("_titleTextFadeTimer");

                    if (!started && titleShown && titleFadeTimer > 0) {
                        Console.WriteLine("Start Timer!");
                        started = true;
                    }

                    var action = new StaticField(pm.Runtime, "MagicalTimeBean.Bastille.Scenes.SceneManager", "<ActionSceneInstance>k__BackingField");
                    var roomInstance = action.Value.Value["RoomInstance"];
                    var player = action.Value.Value["_player"];

                    bool pauseMenuVisible = action.Value.Value["PauseMenu"].Value["Visible"].Value.Read<Boolean>();
                    bool pauseMenuFaderVisible = action.Value.Value["StageSelectDecorations"].Value["Fader"].Value["Enabled"].Value.Read<Boolean>();

                    if (roomInstance != null) {
                        var room                = roomInstance.Value;
                        bool stopCounting       = room["<StopCountingElapsedTime>k__BackingField"].Value.Read<Boolean>();
                        bool hasRunFirstFrame   = room["<HasRunFirstFrame>k__BackingField"].Value.Read<Boolean>();
                        bool frozen             = room["_frozen"].Value.Read<Boolean>();
                        roomID                  = room["<RoomID>k__BackingField"].Value.Read<Int32>();

                        if (!frozen) {
                            isDead                  = (player == null);
                        }

                        if (!stopCounting && hasRunFirstFrame && (!frozen || pauseMenuFaderVisible || pauseMenuVisible || isDead)) {
                            inGame = true;
                        } else {
                            inGame = false;
                        }

                        Console.WriteLine("{0} {1} {2} {3} {4} {5} {6}", !frozen, pauseMenuFaderVisible, pauseMenuVisible, isDead, roomID, stopCounting, hasRunFirstFrame);
                    } else if (isDead) {
                        inGame = true;
                    } else {
                        inGame = false;
                    }

                    if (inGame != lastInGame) {
                        switch (roomID) {
                            case 107:
                            case 108:
                            case 109:
                            case 110:
                            case 115:
                            case 116:
                            case 117:
                            case 113:
                                break;
                            default:
                                if (inGame == false) {
                                    Console.WriteLine("Room {0}: Done!", roomID);
                                    break;
                                } else {
                                    Console.WriteLine("Room {0}: Start!", roomID);
                                }
                                break;
                        }

                        lastInGame = inGame;
                        lastRoomID = roomID;
                    }

                    /*
                    var postracker = GetField(actionScene, "PositionTracker");
                    
                    var gameState = GetField(postracker, "<MyGameState>k__BackingField");
                    var currentPos = GetField(gameState, "_currentPosition");
                    var x = GetFieldValue(currentPos, "_x");
                    var y = GetFieldValue(currentPos, "_y");

                    Console.WriteLine("{0},{1}", ((Int32)x).ToString(), ((Int32)y).ToString());

                    var gameWorld = GetField(postracker, "<MyWorld>k__BackingField");
                    var h = GetFieldValue(gameWorld, "Height");
                    var w = GetFieldValue(gameWorld, "Width");

                    Console.WriteLine("{0},{1}", h.ToString(), w.ToString());
                    //*/

                    Thread.Sleep(100);
                }
            }
        }

        static ProcessMangler AttachToProcess(string processName) {
            var processes = Process.GetProcessesByName(processName);
            if (processes.Count() > 1) {
                throw new Exception("Multiple processes found with name '" + processName + "'");
            } else if (!processes.Any()) {
                throw new Exception("No process found with name '" + processName + "'");
            }

            var proc = processes.First();
            int id = proc.Id;
            proc.Dispose();
            return new ProcessMangler(id);
        }

        static public void ViewFields(ValuePointer point, string name) {
            foreach (var ffield in point.Type.Fields) {
                Console.WriteLine(ffield);
            }
        }

        static public ValuePointer[] GetObjectsByTypeName(ProcessMangler pm, string name) {
            var type = pm.Heap.GetTypeByName(name);

            List<ValuePointer> objects = new List<ValuePointer>();
            foreach (ulong obj in pm.Heap.EnumerateObjects()) {
                ClrType objtype = pm.Heap.GetObjectType(obj);
                if (objtype == null || objtype != type)
                    continue;

                ValuePointer gobj = new ValuePointer(obj, type, pm.Heap);
                objects.Add(gobj);
            }
            return objects.ToArray();
        }
    }
}

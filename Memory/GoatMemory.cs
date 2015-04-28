using System;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Microsoft.Diagnostics.Runtime;
using Microsoft.Diagnostics.Runtime.Interop;

namespace LiveSplit.EscapeGoat2.Memory
{
    public struct MapPosition
    {
        public int _x;
        public int _y;
    }

    public class GoatMemory
    {
        public Process proc;
        public ProcessMangler pm;

        public bool isHooked = false;
        public bool isMangled = false;
        public DateTime hookedTime;

        public Dictionary<string, StaticField> staticCache = new Dictionary<string, StaticField>();
        public Dictionary<string, ValuePointer> pointerCache = new Dictionary<string, ValuePointer>();


        public StaticField GetCurrentScene() {
            return GetCachedStaticField("MagicalTimeBean.Bastille.Scenes.SceneManager", "_currentScene");
        }

        public StaticField GetActionStage() {
            return GetCachedStaticField("MagicalTimeBean.Bastille.Scenes.SceneManager", "<ActionSceneInstance>k__BackingField");
        }

        public bool GetStartOfGame() {
            try {
                var title = GetCachedStaticField("MagicalTimeBean.Bastille.Scenes.SceneManager", "<TitleScreenInstance>k__BackingField");
                bool titleShown = title.Value.Value.GetFieldValue<Boolean>("_titleShown");
                int titleFadeTimer = title.Value.Value.GetFieldValue<Int32>("_titleTextFadeTimer");

                return (titleShown && titleFadeTimer > 0);
            } catch (Exception e) {
                write(e.ToString());
                throw e;
            }
        }

        public ValuePointer? GetRoomInstance() {
            var action = GetActionStage();
            return GetCachedValuePointer(action, "RoomInstance");
        }

        public int? GetRoomID() {
            var roomInstance = GetRoomInstance();
            if (roomInstance != null) {
                return roomInstance.Value.GetFieldValue<Int32>("<RoomID>k__BackingField");
            }
            return null;
        }

        public bool? GetRoomFrozen() {
            var roomInstance = GetRoomInstance();
            if (roomInstance != null) {
                return roomInstance.Value.GetFieldValue<Boolean>("_frozen");
            }
            return null;
        }

        public bool? GetRoomHasRunFirstFrame() {
            var roomInstance = GetRoomInstance();
            if (roomInstance != null) {
                return roomInstance.Value.GetFieldValue<Boolean>("<HasRunFirstFrame>k__BackingField");
            }
            return null;
        }

        public bool? GetRoomTimerStopped() {
            var roomInstance = GetRoomInstance();
            if (roomInstance != null) {
                return roomInstance.Value.GetFieldValue<Boolean>("<StopCountingElapsedTime>k__BackingField");
            }
            return null;
        }

        public int? GetSheepOrbsCollected() {
            var action = GetActionStage();
            var state = action.Value.Value["<GameState>k__BackingField"];
            if (!state.HasValue) return null;

            try {
                ArrayPointer sheepOrbs = new ArrayPointer(state.Value, "_orbObtainedPositions", "MagicalTimeBean.Bastille.LevelData.MapPosition");
                return sheepOrbs.Length;
            } catch (Exception e) { write(e.ToString()); }

            return 0;
        }

        public TimeSpan GetGameTime() {
            try {
                var action = GetActionStage();
                Int64 time = action.Value.Value["<GameState>k__BackingField"].Value["_totalTime"].Value.ForceCast("System.Int64").Read<Int64>();
                return new TimeSpan(time);
            } catch {
                return TimeSpan.Zero;
            }
        }

        public bool? GetOnActionStage() {
            StaticField current = GetCurrentScene();
            StaticField action = GetActionStage();

            return (current.Value.Value.Address == action.Value.Value.Address);
        }

        public void ViewFields(ValuePointer point) {
            write(point.Type.Name.ToString());
            foreach (var field in point.Type.Fields) {
                string output;
                if (field.HasSimpleValue)
                    output = field.GetValue(point.Address).ToString();
                else
                    output = field.GetAddress(point.Address).ToString("X");

                write(string.Format("  +{0,2:X2} {1} {2} = {3}", field.Offset, field.Type.Name, field.Name, output));
            }
        }

        public ValuePointer[] GetObjectsByTypeName(ProcessMangler pm, string name) {
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

        public bool HookProcess() {
            if (proc == null || proc.HasExited) {
                Process[] processes = Process.GetProcessesByName("EscapeGoat2");

                if (processes.Length == 0) {
                    this.isHooked = false;
                    this.isMangled = false;
                    return this.isHooked && this.isMangled;
                }

                proc = processes[0];
                if (proc.HasExited) {
                    this.isHooked = false;
                    this.isMangled = false;
                    return this.isHooked && this.isMangled;
                }

                this.isHooked = true;
                hookedTime = DateTime.Now;
            }

            if (!this.isMangled && this.isHooked && hookedTime.AddSeconds(2) < DateTime.Now) {
                try {
                    pm = new ProcessMangler(proc.Id);
                    this.isMangled = true;
                } catch (Exception e) {
                    write("Exception Occured");
                    write(e.ToString());
                    proc.Dispose();
                }
            }

            return this.isHooked && this.isMangled;
        }

        public void Dispose() {
            if (pm != null) {
                this.pm.Dispose();
            }
            if (proc != null) {
                this.proc.Dispose();
            }
        }

        public StaticField GetCachedStaticField(string klass, string fieldName) {
            string key = string.Format("{0}.{1}", klass, fieldName);
            if (!staticCache.ContainsKey(key)) {
                try {
                    staticCache[key] = new StaticField(pm.Runtime, klass, fieldName);
                } catch (Exception e) {
                    write(key);
                    write(e.ToString());
                }
            }
            return staticCache[key];
        }

        public ValuePointer? GetCachedValuePointer(StaticField field, string fieldName) {
            string key = string.Format("{0}.{1}", field.Value.Value.Type.Name, fieldName);
            if (!pointerCache.ContainsKey(key)) {
                try {
                    ValuePointer? vp = field.Value.Value[fieldName];
                    if (vp == null) {
                        return null;
                    }
                    pointerCache[key] = vp.Value;
                } catch (Exception e) {
                    write(key);
                    write(e.ToString());
                }
            }
            return pointerCache[key];
        }

        public void ClearCaches() {
            pointerCache.Clear();
            staticCache.Clear();
        }

        private void write(string str) {
#if DEBUG
            StreamWriter wr = new StreamWriter("_goatauto.log", true);
            wr.WriteLine("[" + DateTime.Now + "] " + str);
            wr.Close();
#endif
        }
    }
}

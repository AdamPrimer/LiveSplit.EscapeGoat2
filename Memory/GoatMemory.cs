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
    public class GoatMemory
    {
        public Process proc;
        public ProcessMangler pm;

        public bool isHooked = false;
        public bool isMangled = false;
        public DateTime hookedTime;

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
            this.pm.Dispose();
            this.proc.Dispose();
        }

        public class CurrentRoom
        {
            public bool active { get; set; }
            public int id { get; set; }
            public CurrentRoom(int id, bool active) {
                this.id = id;
                this.active = active;
            }
        }

        public StaticField GetCurrentScene() {
            return new StaticField(pm.Runtime, "MagicalTimeBean.Bastille.Scenes.SceneManager", "_currentScene");
        }

        public StaticField GetActionStage() {
            return new StaticField(pm.Runtime, "MagicalTimeBean.Bastille.Scenes.SceneManager", "<ActionSceneInstance>k__BackingField");
        }

        public ValuePointer? GetPlayer() {
            var action = GetActionStage();
            return action.Value.Value["_player"];
        }

        public bool GetStartOfGame() {
            try {
                var title = new StaticField(pm.Runtime, "MagicalTimeBean.Bastille.Scenes.SceneManager", "<TitleScreenInstance>k__BackingField");
                bool titleShown = title.Value.Value.GetFieldValue<Boolean>("_titleShown");
                int titleFadeTimer = title.Value.Value.GetFieldValue<Int32>("_titleTextFadeTimer");

                bool started = false;
                if (titleShown && titleFadeTimer > 0) {
                    started = true;
                }

                return started;
            } catch (Exception e) {
                write(e.ToString());
                throw e;
            }
        }

        public ValuePointer? GetRoomInstance() {
            var action = GetActionStage();
            return action.Value.Value["RoomInstance"];
        }

        public int? GetRoomID() {
            var roomInstance = GetRoomInstance();
            if (roomInstance != null) {
                return roomInstance.Value["<RoomID>k__BackingField"].Value.Read<Int32>();
            }
            return null;
        }

        public bool? GetRoomTimerStopped() {
            var roomInstance = GetRoomInstance();
            if (roomInstance != null) {
                return roomInstance.Value["<StopCountingElapsedTime>k__BackingField"].Value.Read<Boolean>();
            }
            return null;
        }

        public bool? GetRoomFrozen() {
            var roomInstance = GetRoomInstance();
            if (roomInstance != null) {
                return roomInstance.Value["_frozen"].Value.Read<Boolean>();
            }
            return null;
        }

        public bool? GetRoomHasRunFirstFrame() {
            var roomInstance = GetRoomInstance();
            if (roomInstance != null) {
                return roomInstance.Value["<HasRunFirstFrame>k__BackingField"].Value.Read<Boolean>();
            }
            return null;
        }

        public bool? GetIsQuittingGame() {
            var action = GetActionStage();
            return action.Value.Value["QuitGameFader"].Value["Enabled"].Value.Read<Boolean>();
        }
        
        public bool? GetIsGamePaused() {
            var action = GetActionStage();

            // Either the pause menu is open, or the fader is transitioning it.
            return (
                action.Value.Value["PauseMenu"].Value["Visible"].Value.Read<Boolean>() 
                || action.Value.Value["StageSelectDecorations"].Value["Fader"].Value["Enabled"].Value.Read<Boolean>());
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

        private void write(string str) {
#if DEBUG
            StreamWriter wr = new StreamWriter("_goatauto.log", true);
            wr.WriteLine("[" + DateTime.Now + "] " + str);
            wr.Close();
#endif
        }
    }
}

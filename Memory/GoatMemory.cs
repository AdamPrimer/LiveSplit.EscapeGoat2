using System;
using System.Diagnostics;
using System.Collections.Generic;
using LiveSplit.EscapeGoat2.Debugging;

namespace LiveSplit.EscapeGoat2.Memory
{
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
                LogWriter.WriteLine(e.ToString());
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

        public bool? GetReplayRecordingPaused() {
            var action = GetActionStage();
            try {
                return action.Value.Value.GetFieldValue<bool>("_pauseReplayRecording");
            } catch (Exception e) {
                LogWriter.WriteLine(e.ToString());
                return null;
            }
        }

        public int? GetSheepOrbsCollected() {
            var action = GetActionStage();
            var state = GetCachedValuePointer(action, "<GameState>k__BackingField");
            if (!state.HasValue) return null;

            try {
                ArrayPointer sheepOrbs = new ArrayPointer(state.Value, "_orbObtainedPositions", "MagicalTimeBean.Bastille.LevelData.MapPosition");
                return sheepOrbs.Length;
            } catch (Exception e) { LogWriter.WriteLine(e.ToString()); }

            return 0;
        }

        public int? GetShardsCollected() {
            var action = GetActionStage();
            var state = GetCachedValuePointer(action, "<GameState>k__BackingField");
            if (!state.HasValue) return null;

            try {
                ArrayPointer secretRooms = new ArrayPointer(state.Value, "_secretRoomsBeaten", "MagicalTimeBean.Bastille.LevelData.MapPosition");
                return secretRooms.Length;
            } catch (Exception e) { LogWriter.WriteLine(e.ToString()); }

            return 0;
        }

        public TimeSpan GetGameTime() {
            try {
                var action = GetActionStage();
                var state = GetCachedValuePointer(action, "<GameState>k__BackingField");
                Int64 time = state.Value["_totalTime"].Value.ForceCast("System.Int64").Read<Int64>();
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
                    LogWriter.WriteLine(e.ToString());
                    proc.Dispose();
                }
            }

            return this.isHooked && this.isMangled;
        }

        public void Dispose() {
            if (pm != null)   this.pm.Dispose();
            if (proc != null) this.proc.Dispose();
        }

        public StaticField GetCachedStaticField(string klass, string fieldName) {
            string key = string.Format("{0}.{1}", klass, fieldName);
            if (!staticCache.ContainsKey(key)) {
                try {
                    staticCache[key] = new StaticField(pm.Runtime, klass, fieldName);
                } catch (Exception e) {
                    LogWriter.WriteLine(key);
                    LogWriter.WriteLine(e.ToString());
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
                    LogWriter.WriteLine(key);
                    LogWriter.WriteLine(e.ToString());
                }
            }
            return pointerCache[key];
        }

        public void ClearCaches() {
            pointerCache.Clear();
            staticCache.Clear();
        }
    }
}

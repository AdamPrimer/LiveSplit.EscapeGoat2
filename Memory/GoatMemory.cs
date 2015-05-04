using System;
using System.Diagnostics;
using System.Collections.Generic;
using LiveSplit.EscapeGoat2.State;
using LiveSplit.EscapeGoat2.Debugging;

namespace LiveSplit.EscapeGoat2.Memory
{
    public class GoatMemory
    {
        private const float mangleDelay = 2.5f;

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
            // We determine if a new game has started because the `_titleTextFadeTimer` in the `TitleScreen` 
            // is set to be greater than zero as it fades to the intro text. `_titleShown` is an additional
            // check as a sort of sanity check but is not strictly necessary.
            var title = GetCachedStaticField("MagicalTimeBean.Bastille.Scenes.SceneManager", "<TitleScreenInstance>k__BackingField");
            bool titleShown = title.Value.Value.GetFieldValue<Boolean>("_titleShown");
            int titleFadeTimer = title.Value.Value.GetFieldValue<Int32>("_titleTextFadeTimer");

            return (titleShown && titleFadeTimer > 0);
        }

        public ValuePointer? GetRoomInstance() {
            var action = GetActionStage();
            return GetCachedValuePointer(action, "RoomInstance");
        }

        public int? GetRoomID() {
            // This tells us what room we are currently in by its ID. This is used mostly for debugging purposes
            // as it enables us to determine where we are. This is not strictly required.
            var roomInstance = GetRoomInstance();
            if (!roomInstance.HasValue) return null;
            return roomInstance.Value.GetFieldValue<Int32>("<RoomID>k__BackingField");
        }

        public bool? GetReplayRecordingPaused() {
            // This value tells us if we have paused replay recording for the current room,
            // this occurs if you enter a door exit.
            var action = GetActionStage();
            if (!action.Value.HasValue) return null;
            return action.Value.Value.GetFieldValue<bool>("_pauseReplayRecording");
        }

        public bool? GetIsPlayerObject() {
            // We determine if the player object is there or not by trying to cast the `_player`
            // property in the `ActionStage` to a boolean. If there is a player object, this will
            // return true, and false if not.
            var action = GetActionStage();
            if (!action.Value.HasValue) return null;
            return action.Value.Value.GetFieldValue<bool>("_player");
        }

        public MapPosition? GetCurrentPosition() {
            // The current map position is located in the `GameState` in the `ActionStage`. We can
            // read the x,y coordinate of the current room, allowing us to determine when room 
            // changes have occured. This position changes at the end of a room to the next 
            // appropriate room whether you later change rooms using the map. This position is used
            // to sanity check when a user has left a room. This is to resolve rare issues where
            // `_pauseReplayRecording` would toggle (likely due to reading memory just as it is 
            // being freed/moved) causing additional splits.
            var action = GetActionStage();
            var state = GetCachedValuePointer(action, "<GameState>k__BackingField");
            if (!state.HasValue) return null;

            var currentPosition = state.Value["_currentPosition"];
            if (!currentPosition.HasValue) return null;;

            return currentPosition.Value.ReadValue<MapPosition>();
        }

        public int? GetSheepOrbsCollected() {
            // We determine the end of Sheep Orb rooms by detecting an increase in the number of
            // Sheep Orbs collected. We do this simply by reading the length of the `_orbObtainedPositions`
            // located in the `GameState`.
            var action = GetActionStage();
            var state = GetCachedValuePointer(action, "<GameState>k__BackingField");
            if (!state.HasValue) return null;

            ArrayPointer sheepOrbs = new ArrayPointer(state.Value, "_orbObtainedPositions", "MagicalTimeBean.Bastille.LevelData.MapPosition");
            return sheepOrbs.Length;
        }

        public int? GetShardsCollected() {
            // We determine the end of Glass Fragments rooms by detecting an increase in the number of
            // Glass Framents collected. Internally these fragments are called shards, and we actually 
            // count them by reading the length of the `_secretRoomsBeaten` array located in the `GameState`.
            var action = GetActionStage();
            var state = GetCachedValuePointer(action, "<GameState>k__BackingField");
            if (!state.HasValue) return null;

            ArrayPointer secretRooms = new ArrayPointer(state.Value, "_secretRoomsBeaten", "MagicalTimeBean.Bastille.LevelData.MapPosition");
            return secretRooms.Length;
        }

        public TimeSpan GetGameTime() {
            // To use In-Game time inside LiveSplit we read the value of the In-Game time directly from
            // the `GameState`. `_totalTime` is a TimeSpan, which internally is a struct that stores a 
            // Int64 with the number of "ticks". We therefore read this value as an Int64 and then create
            // a new TimeSpan object using that value.
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
            // To hook Escape Goat 2 for reading its memory we must first check to find the
            // active process.
            if (proc == null || proc.HasExited) {
                Process[] processes = Process.GetProcessesByName("EscapeGoat2");

                if (processes.Length == 0) {
                    this.isHooked = false;
                    this.isMangled = false;
                    return this.isHooked && this.isMangled;
                }

                // Ensure the process isn't exiting.
                proc = processes[0];
                if (proc.HasExited) {
                    this.isHooked = false;
                    this.isMangled = false;
                    return this.isHooked && this.isMangled;
                }

                this.isHooked = true;
                hookedTime = DateTime.Now;
            }

            // Once we have the process, we also need to hook the `ProcessMangler`. This basically
            // just detects the version of the .NET runtime that the process it running under and
            // initialises the objects required for us to read from the process Heap and Runtime. 
            
            // If you try and initialize the `ProcessMangler` onto the process too quickly after 
            // opening the game, it will cause all sorts of problems. These are resolved simply
            // by waiting two seconds between hooking the process and starting the `ProcessMangler`
            // leaving ample time for the process to initialise before starting to read from it.
            if (!this.isMangled && this.isHooked && hookedTime.AddSeconds(GoatMemory.mangleDelay) < DateTime.Now) {
                try {
                    pm = new ProcessMangler(proc.Id);
                    this.isMangled = true;
                } catch (Exception e) {
                    proc.Dispose();
                    throw e;
                }
            }

            return this.isHooked && this.isMangled;
        }

        public void Dispose() {
            // We want to appropriately dispose of the `Process` and `ProcessMangler` that are
            // attached to the process to avoid being unable to close LiveSplit.
            if (pm != null) this.pm.Dispose();
            if (proc != null) this.proc.Dispose();
        }

        public StaticField GetCachedStaticField(string klass, string fieldName) {
            // Some static fields such as the `ActionStage` are read multiple times in a single update 
            // loop. We therefore cache these results to limit the number of values we have to read
            // from memory. This is done because external memory reads like we are doing are slow
            // so we'd like to reduce the number of them we make as much as possible.
            string key = string.Format("{0}.{1}", klass, fieldName);
            if (!staticCache.ContainsKey(key)) {
                try {
                    staticCache[key] = new StaticField(pm.Runtime, klass, fieldName);
                } catch (Exception e) {
                    LogWriter.WriteLine(key);
                    throw e;
                }
            }
            return staticCache[key];
        }

        public ValuePointer? GetCachedValuePointer(StaticField field, string fieldName) {
            // See `GetCachedStaticField` for the explanation for why we want to cache `ValuePointer`.
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
                    throw e;
                }
            }
            return pointerCache[key];
        }

        public void ClearCaches() {
            // Clear the caches, called by the `GoatState` when it is done using `GoatMemory` for each
            // loop. This is required as the pointers may become stale as memory is moved about during 
            // execution, so we cannot cache longer than a single cycle without potentially introducing
            // lag between when events occur and when we detect them.
            pointerCache.Clear();
            staticCache.Clear();
        }
    }
}

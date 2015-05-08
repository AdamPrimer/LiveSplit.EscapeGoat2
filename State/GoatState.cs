using System;
using System.Collections.Generic;
using LiveSplit.EscapeGoat2.Memory;
using LiveSplit.EscapeGoat2.Debugging;

namespace LiveSplit.EscapeGoat2.State
{
    public enum DoorState
    {
        Clear,
        Entering
    }

    public enum LevelState
    {
        Inside,
        Outside
    }

    public enum PlayerState
    {
        Alive,
        Dead
        // No cats, only goats.
    }

    public class GoatState
    {
        public bool isOpen = false;

        public WorldMap map;
        public GoatMemory goatMemory;
        public GoatTriggers goatTriggers;

        public bool isStarted = false;                                  // Set to True when New Game is selected.
        public DoorState doorEnteredState = DoorState.Clear;            // Set to Entering on entering a door, set Clear on entering a room
        public MapPosition currentPosition = new MapPosition(0, 0);     // The current map location of the player
        public bool hasPositionChangedSinceExit = false;                // Set to False when doorEnteredState is set to True, set to True when currentPosition changes.

        // Used for debugging purposes
        public LevelState levelState = LevelState.Outside;              // Set to Outside when exiting a room, set to Inside when entering a room.
        public PlayerState playerState = PlayerState.Dead;              // Set to Alive when the Goat is created, set to Dead when the Goat is destroyed (death).

        public int lastRoomID = 0;                                      // The room ID of the last room an exit occured on
        public int collectedShards = 0;                                 // The number of collected Glass Fragments (called Shards internally)
        public int collectedSheepOrbs = 0;                              // The number of collected Sheep Orbs

        public TimeSpan lastSeen = TimeSpan.Zero;                       // The last time the player was seen (in In-Game Time)
        private DateTime lastSaneTime = DateTime.Now;

        public event EventHandler OnTimerFixed;                         // Fires whenever the IGT between updates has not changed
        public event EventHandler OnTimerChanged;                       // Fires whenever the IGT between updates has changed
        public event EventHandler OnTimerUpdated;                       // Fires every update after the IGT is updated.

        public int exceptionsCaught = 0;
        public int totalExceptionsCaught = 0;
        private ulong pulseCount = 0;

        private int positionChangedSanity = 30;

        public GoatState() {
            map = new WorldMap();
            goatMemory = new GoatMemory();
            goatTriggers = new GoatTriggers();
        }

        public void Reset() {
            this.isStarted = false;
            this.hasPositionChangedSinceExit = false;

            this.lastRoomID = 0;
            this.collectedShards = 0;
            this.collectedSheepOrbs = 0;

            this.levelState = LevelState.Outside;
            this.playerState = PlayerState.Dead;
            this.doorEnteredState = DoorState.Clear;

            this.lastSeen = TimeSpan.Zero;
            this.currentPosition = new MapPosition(0, 0);

            this.exceptionsCaught = 0;
            this.totalExceptionsCaught = 0;
            this.pulseCount = 0;
        }

        public void Dispose() {
            // Unhook from reading the game memory
            goatMemory.Dispose();
        }

        public void Loop() {
            try {
                // Hook the game process so we can read the memory
                bool isNowOpen = (goatMemory.HookProcess() && !goatMemory.proc.HasExited);
                if (isNowOpen != isOpen) {
                    if (!isNowOpen) LogWriter.WriteLine("escapegoat2.exe is unavailable.");
                    else LogWriter.WriteLine("escapegoat2.exe is available.");
                    isOpen = isNowOpen;
                }

                // If we're open, do all the magic
                if (isOpen) Pulse();
            } catch (Exception e) {
                if (this.exceptionsCaught < 10 && this.totalExceptionsCaught < 30) {
                    this.exceptionsCaught++;
                    this.totalExceptionsCaught++;
                    LogWriter.WriteLine("Exception #{0} (P:{2}): {1}", this.exceptionsCaught, e.ToString(), this.pulseCount);
                } else if (this.totalExceptionsCaught < 30) {
                    LogWriter.WriteLine("Too many exceptions, rebooting autosplitter. (P:{0})", this.pulseCount);
                    this.goatMemory.Dispose();
                    this.goatMemory = new GoatMemory();
                    this.exceptionsCaught = 0;
                } else if (this.totalExceptionsCaught == 30) {
                    LogWriter.WriteLine("Too many total exceptions, no longer logging them. (P:{0})", this.pulseCount);
                    this.totalExceptionsCaught++;
                }
            }

            // We cache memory pointers during each pulse inside goatMemory for
            // performance reasons, we need to manually clear the cache here so
            // that goatMemory knows we are done making calls to it and that we
            // do not need these values anymore.
            //
            // We need this to occur even if (actually, especially if) and
            // exception occurs to clear any potentially dead memory pointers
            // that occured due to reading memory just as it's being
            // moved/freed.
            goatMemory.ClearCaches();
        }

        public void Pulse() {
            this.pulseCount++;

            // If we haven't detected the start of a new game, check the memory
            // for the event
            if (!this.isStarted) UpdateStartOfGame();

            // If we have detected the start of a game, then check for end of
            // level events and updated in-game time.
            if (this.isStarted) {
                UpdateEndOfLevel();
                UpdateGameTime();
            }

            goatMemory.ClearCaches();
        }

        public void UpdateEndOfLevel() {
            // All of our checks are dependent on there being an active room
            // available.  This requires both the RoomInstance to be available,
            // and that we are on the "ActionStage" in the SceneManager
            // indicating that the RoomInstance is the active scene.
            var roomInstance = goatMemory.GetRoomInstance();
            bool isOnAction  = (bool)goatMemory.GetOnActionStage();

            if (roomInstance != null && isOnAction) {
                // Check for position changes indicating that the player has moved on the map
                UpdateCurrentPosition();

                // These are currently for debugging only
                UpdatePlayerStatus();
                UpdateLevelStatus();

                bool newDoor      = (bool)HaveEnteredDoor();
                bool newSheepOrb  = (bool)HaveCollectedNewSheepOrb();
                bool newShard     = (bool)HaveCollectedNewShard();

                // A room ends on one of three conditions, a door is entered, a
                // glass fragment (shard) is collected, or a sheep orb is
                // collected.
                if (newDoor || newSheepOrb || newShard) {
                    int roomID = (int)goatMemory.GetRoomID();
                    goatTriggers.SplitOnEndRoom(this.map.GetRoom(roomID));
                }
            }
        }

        public void UpdateStartOfGame() {
            // If selecting "New Game" is detected, then call the start game trigger.
            bool isStarted = goatMemory.GetStartOfGame();
            if (this.isStarted != isStarted) {
                goatTriggers.SplitOnGameStart(isStarted);
                this.isStarted = isStarted;
            }
        }

        public void UpdateGameTime() {
            TimeSpan now = goatMemory.GetGameTime();

            // Due to the fact we are polling memory, and only at around 30HZ while the 
            // time runs at 60HZ, there is a variance of 1 frame on the times we get 
            // from the game. (now - this.lastSeen) therefore tends to vary from 1 frame
            // in magnitude, to 3 frames in magnitude. Given the polling period is just over
            // two frames, this means only when we get the three frame window do we observe
            // In-Game Time ahead of real time. We can therefore sometimes be up to a frame
            // ahead of real time without an error having occured. Although this only
            // requires ~17ms of delay, I have allowed 50ms (three frames) of delay just
            // in case something wacky happens.
            if (now < this.lastSeen || now - this.lastSeen > (DateTime.Now - this.lastSaneTime).Add(TimeSpan.FromMilliseconds(50))) return;

            this.lastSaneTime = DateTime.Now;

            // Call all the relevant IGT based events depending on the time delta since the last pulse.
            if (this.OnTimerUpdated != null) this.OnTimerUpdated(now, EventArgs.Empty);

            if (now > this.lastSeen) {
                if (this.OnTimerChanged != null) this.OnTimerChanged(now, EventArgs.Empty);
            } else {
                if (this.OnTimerFixed != null) this.OnTimerFixed(now, EventArgs.Empty);
            }

            this.lastSeen = now;
        }

        public bool HaveEnteredDoor() {
            // If we haven't updated our map position since the last exit, then
            // we don't want to trigger another exit. This is to prevent issues
            // with double splitting.
            if (!this.hasPositionChangedSinceExit) return false;

            // We detect a room exit by seeing it the ReplayRecordingPaused is set to True
            bool? replayPaused = goatMemory.GetReplayRecordingPaused();
            if (!replayPaused.HasValue) return false;

            // If the DoorState is clear and we have a paused replay timer, then set the DoorEnteredState to Entering
            if (this.doorEnteredState == DoorState.Clear && replayPaused.Value) {
                int roomID = (int)goatMemory.GetRoomID();
                LogWriter.WriteLine("Door Entered (Last Exit {0}, This Exit {1}) (P:{2})", this.lastRoomID, roomID, this.pulseCount);

                this.lastRoomID = roomID;
                this.hasPositionChangedSinceExit = false;
                this.doorEnteredState = DoorState.Entering;
                return true;
            }

            // If we are not already Clear but recording a replay, then set the DoorEnteredState to Clear
            else if (this.doorEnteredState != DoorState.Clear && !replayPaused.Value) {
                LogWriter.WriteLine("Resetting Door State for Room {1} (Last Exit {0}) (P:{2})", this.lastRoomID, (int)goatMemory.GetRoomID(), this.pulseCount);

                this.doorEnteredState = DoorState.Clear;
            }

            return false;
        }

        public void UpdateLevelStatus() {
            // We detect a room exit by seeing it the ReplayRecordingPaused is set to True,
            // this is set back to False when we enter a new room.
            bool? replayPaused = goatMemory.GetReplayRecordingPaused();
            if (!replayPaused.HasValue) return;

            // If we are currently Outside, but we are recording a replay, transition to Inside
            if (levelState == LevelState.Outside && !replayPaused.Value) {
                LogWriter.WriteLine("Entering Room {1} (Last Exit {0}) (P:{2})", this.lastRoomID, (int)goatMemory.GetRoomID(), this.pulseCount);

                this.levelState = LevelState.Inside;
            }
                // If we are currently Inside, but not recording a replay, transition to Outside
            else if (levelState == LevelState.Inside && replayPaused.Value) {
                LogWriter.WriteLine("Leaving Room {1} (Last Exit {0}) (P:{2})", this.lastRoomID, (int)goatMemory.GetRoomID(), this.pulseCount);

                this.levelState = LevelState.Outside;
            }
        }

        public void UpdatePlayerStatus() {
            // This checks when the Goat player object exists. It is set to True when entering the first room,
            // and is set to False when the player dies. It is set to True again once respawned.
            bool? player = goatMemory.GetIsPlayerObject();
            if (!player.HasValue) return;

            // If we are currently Alive, but there is no player object, transition to Dead
            if (this.playerState == PlayerState.Alive && !player.Value) {
                LogWriter.WriteLine("Player Object Destroyed in Room {1} (Last Exit {0}) (P:{2})", this.lastRoomID, (int)goatMemory.GetRoomID(), this.pulseCount);
                this.playerState = PlayerState.Dead;
            }

            // If we are currently Dead, but there is a Player object, transition to Alive
            else if (this.playerState == PlayerState.Dead && player.Value) {
                LogWriter.WriteLine("Player Object Created in Room {1} (Last Exit {0}) (P:{2})", this.lastRoomID, (int)goatMemory.GetRoomID(), this.pulseCount);
                this.playerState = PlayerState.Alive;
            }
        }

        public void UpdateCurrentPosition() {
            // This is the player's current position on the game map. This is updated after a level exit to be
            // the next room whether you actually enter that room or navigate to another room. Therefore, this
            // position will change (triggering hasPositionChangedSinceExit) even if you finish a room, then
            // navigate back to the same room to do it again. So even though the same room is done twice in a
            // row, we will still know they are seperate exits.
            MapPosition? pos = goatMemory.GetCurrentPosition();
            if (!pos.HasValue) return;

            int x = pos.Value._x;
            int y = pos.Value._y;

            // Check if the current position has changed
            if (this.currentPosition._x != x || this.currentPosition._y != y) {
                // Sanity check position is sensical.
                if (x >= 0 && x <= this.positionChangedSanity && y >= 0 && y <= this.positionChangedSanity) {
                    LogWriter.WriteLine("Player Position Changed in Room {1} ({2},{3} to {4},{5}) (Last Exit {0}) (P:{6})",
                            this.lastRoomID, (int)goatMemory.GetRoomID(),
                            this.currentPosition._x, this.currentPosition._y,
                            x, y,
                            this.pulseCount);
                    this.currentPosition = pos.Value;
                    this.hasPositionChangedSinceExit = true;
                }
            }
        }

        public bool HaveCollectedNewSheepOrb() {
            // We detect a Sheep Orb is collected because the length of the game's SheepOrbsCollected array increases.
            int curSheepOrbsCollected = this.collectedSheepOrbs;
            int numSheepOrbsCollected = (int)goatMemory.GetSheepOrbsCollected();

            // Check if we have more sheep orbs than we used to
            if (numSheepOrbsCollected == curSheepOrbsCollected + 1) {
                int roomID = (int)goatMemory.GetRoomID();
                LogWriter.WriteLine("Sheep Orb Obtained: {0} -> {1} ({2} -> {3}) (P:{4})",
                        this.collectedSheepOrbs, numSheepOrbsCollected,
                        this.lastRoomID, roomID, this.pulseCount);
                this.lastRoomID = roomID;
            }

            this.collectedSheepOrbs = numSheepOrbsCollected;
            return (numSheepOrbsCollected == curSheepOrbsCollected + 1);
        }

        public bool HaveCollectedNewShard() {
            // We detect a Sheep Orb is collected because the length of the game's SecretRoomsBeaten array increases,
            // which is equivalent to saying a Glass Fragment (shard) was collected.
            int curShardsCollected = this.collectedShards;
            int numShardsCollected = (int)goatMemory.GetShardsCollected();

            // Check if we have more glass fragments than we used to
            if (numShardsCollected == curShardsCollected + 1) {
                int roomID = (int)goatMemory.GetRoomID();
                LogWriter.WriteLine("Shard Obtained: {0} -> {1} ({2} -> {3}) (P:{4})",
                        this.collectedShards, numShardsCollected,
                        this.lastRoomID, roomID,
                        this.pulseCount);
                this.lastRoomID = roomID;
            }

            this.collectedShards = numShardsCollected;
            return (numShardsCollected == curShardsCollected + 1);
        }
    }
}

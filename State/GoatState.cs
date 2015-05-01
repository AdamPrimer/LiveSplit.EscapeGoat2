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

        public bool isStarted = false;
        public DoorState doorEnteredState = DoorState.Clear;
        public LevelState levelState = LevelState.Outside;
        public PlayerState playerState = PlayerState.Dead;
        public MapPosition currentPosition = new MapPosition(0, 0);
        public bool hasPositionChangedSinceExit = false;

        public int lastRoomID = 0;
        public int collectedShards = 0;
        public int collectedSheepOrbs = 0;

        public TimeSpan lastSeen = TimeSpan.Zero;

        public event EventHandler OnTimerFixed;
        public event EventHandler OnTimerChanged;
        public event EventHandler OnTimerUpdated;

        public GoatState() {
            map = new WorldMap();
            goatMemory = new GoatMemory();
            goatTriggers = new GoatTriggers();
        }

        public void Reset() {
            this.isStarted = false;

            this.lastRoomID = 0;
            this.collectedShards = 0;
            this.collectedSheepOrbs = 0;

            this.doorEnteredState = DoorState.Clear;
            this.levelState = LevelState.Outside;
            this.playerState = PlayerState.Dead;
            this.currentPosition = new MapPosition(0, 0);
            this.hasPositionChangedSinceExit = false;

            this.lastSeen = TimeSpan.Zero;
        }

        public void Dispose() {
            goatMemory.Dispose();
        }

        public void Loop() {
            bool isNowOpen = (goatMemory.HookProcess() && !goatMemory.proc.HasExited);

            if (isNowOpen != isOpen) {
                if (!isNowOpen) LogWriter.WriteLine("escapegoat2.exe is unavailable.");
                else            LogWriter.WriteLine("escapegoat2.exe is available.");
                isOpen = isNowOpen;
            }

            if (isOpen) Pulse();

            goatMemory.ClearCaches();
        }

        public void Pulse() {
            try {
                if (!this.isStarted) {
                    UpdateStartOfGame();
                }

                if (this.isStarted) {
                    UpdateEndOfLevel();
                    UpdateGameTime();
                }
            } catch (Exception e) { LogWriter.WriteLine(e.ToString()); }
        }

        public void UpdateEndOfLevel() {
            var roomInstance = goatMemory.GetRoomInstance();
            bool isOnAction  = (bool)goatMemory.GetOnActionStage();

            if (roomInstance != null && isOnAction) {
                UpdateCurrentPosition();

                // These are currently for debugging only
                UpdatePlayerStatus();
                UpdateLevelStatus();

                bool newDoor      = (bool)HaveEnteredDoor();
                bool newSheepOrb  = (bool)HaveCollectedNewSheepOrb();
                bool newShard     = (bool)HaveCollectedNewShard();

                //LogWriter.WriteLine("{0} {1} {2} {3}", roomID, newDoor, newSheepOrb, newShard);
                if (newDoor || newSheepOrb || newShard) {
                    int roomID = (int)goatMemory.GetRoomID();
                    goatTriggers.SplitOnEndRoom(this.map.GetRoom(roomID));
                }
            }
        }

        public void UpdateStartOfGame() {
            bool isStarted = goatMemory.GetStartOfGame();
            if (this.isStarted != isStarted) {
                goatTriggers.SplitOnGameStart(isStarted);
                this.isStarted = isStarted;
            }
        }

        public void UpdateGameTime() {
            TimeSpan now = goatMemory.GetGameTime();
            
            if (now == this.lastSeen) {
                if (this.OnTimerFixed != null) this.OnTimerFixed(now, EventArgs.Empty);
            } else if (now > this.lastSeen && now - this.lastSeen < TimeSpan.FromSeconds(2)) {
                this.lastSeen = now;
                if (this.OnTimerChanged != null) this.OnTimerChanged(now, EventArgs.Empty);
            }

            if (now >= this.lastSeen && now - this.lastSeen < TimeSpan.FromSeconds(2)) {
                if (this.OnTimerUpdated != null) this.OnTimerUpdated(now, EventArgs.Empty);
            }
        }

        public bool HaveEnteredDoor() {
            if (!this.hasPositionChangedSinceExit) return false;

            int roomID         = (int)goatMemory.GetRoomID();
            bool? replayPaused = goatMemory.GetReplayRecordingPaused();
            if (!replayPaused.HasValue) return false;

            // If the DoorState is clear and we have a paused replay timer
            if (this.doorEnteredState == DoorState.Clear && replayPaused.Value) {
                LogWriter.WriteLine("Door Entered (Last Exit {0}, This Exit {1})", this.lastRoomID, roomID);
                this.doorEnteredState = DoorState.Entering;
                this.lastRoomID = roomID;
                this.hasPositionChangedSinceExit = false;
                return true;
            }
            
            // If we are not already Clear but recording a replay, then set the DoorEnteredState to Clear
            else if (this.doorEnteredState != DoorState.Clear && !replayPaused.Value) {
                LogWriter.WriteLine("Resetting Door State for Room {1} (Last Exit {0})", this.lastRoomID, roomID);
                this.doorEnteredState = DoorState.Clear;
            }

            return false;
        }

        public void UpdateLevelStatus() {
            int roomID         = (int)goatMemory.GetRoomID();
            bool? replayPaused = goatMemory.GetReplayRecordingPaused();
            if (!replayPaused.HasValue) return;

            // If we are currently Outside, but we are recording a replay, transition to Inside
            if (levelState == LevelState.Outside && !replayPaused.Value) {
                LogWriter.WriteLine("Entering Room {1} (Last Exit {0})", this.lastRoomID, roomID);
                this.levelState = LevelState.Inside;
            }
            // If we are currently Inside, but not recording a replay, transition to Outside
            else if (levelState == LevelState.Inside && replayPaused.Value) {
                LogWriter.WriteLine("Leaving Room {1} (Last Exit {0})", this.lastRoomID, roomID);
                this.levelState = LevelState.Outside;
            }
        }

        public void UpdatePlayerStatus() {
            int roomID         = (int)goatMemory.GetRoomID();
            bool? player       = goatMemory.GetIsPlayerObject();
            if (!player.HasValue) return;

            // If we are currently Alive, but there is no player object, transition to Dead
            if (this.playerState == PlayerState.Alive && !player.Value) {
                LogWriter.WriteLine("Player Object Destroyed in Room {1} (Last Exit {0})", this.lastRoomID, roomID);
                this.playerState = PlayerState.Dead;
            }

            // If we are currently Dead, but there is a Player object, transition to Alive
            else if (this.playerState == PlayerState.Dead && player.Value) {
                LogWriter.WriteLine("Player Object Created in Room {1} (Last Exit {0})", this.lastRoomID, roomID);
                this.playerState = PlayerState.Alive;
            }
        }

        public void UpdateCurrentPosition() {
            int roomID         = (int)goatMemory.GetRoomID();
            MapPosition? pos   = goatMemory.GetCurrentPosition();
            if (!pos.HasValue) return;

            // Check if the current position has changed
            if (this.currentPosition._x != pos.Value._x || this.currentPosition._y != pos.Value._y) {
                LogWriter.WriteLine("Player Position Changed in Room {1} ({2},{3} to {4},{5}) (Last Exit {0})", this.lastRoomID, roomID, this.currentPosition._x, this.currentPosition._y, pos.Value._x, pos.Value._y);
                this.currentPosition = pos.Value;
                this.hasPositionChangedSinceExit = true;
            }
        }

        public bool HaveCollectedNewSheepOrb() {
            int curSheepOrbsCollected = this.collectedSheepOrbs;
            int numSheepOrbsCollected = (int)goatMemory.GetSheepOrbsCollected();

            if (numSheepOrbsCollected > curSheepOrbsCollected) {
                int roomID = (int)goatMemory.GetRoomID();
                LogWriter.WriteLine("Sheep Orb Obtained: {0} -> {1} ({2} -> {3})", this.collectedSheepOrbs, numSheepOrbsCollected, this.lastRoomID, roomID);
                this.lastRoomID = roomID;
            }

            this.collectedSheepOrbs = numSheepOrbsCollected;
            return (numSheepOrbsCollected > curSheepOrbsCollected);
        }

        public bool HaveCollectedNewShard() {
            int curShardsCollected = this.collectedShards;
            int numShardsCollected = (int)goatMemory.GetShardsCollected();

            if (numShardsCollected > curShardsCollected) {
                int roomID = (int)goatMemory.GetRoomID();
                LogWriter.WriteLine("Shard Obtained: {0} -> {1} ({2} -> {3})", this.collectedShards, numShardsCollected, this.lastRoomID, roomID);
                this.lastRoomID = roomID;
            }

            this.collectedShards = numShardsCollected;
            return (numShardsCollected > curShardsCollected);
        }
    }
}

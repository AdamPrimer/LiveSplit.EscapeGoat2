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

    public class GoatState
    {
        public bool isOpen = false;

        public WorldMap map;
        public GoatMemory goatMemory;
        public GoatTriggers goatTriggers;

        public bool isStarted = false;
        public bool isRoomCounting = false;
        public DoorState doorEnteredState = DoorState.Clear;

        public int lastRoomID = 0;
        public int wantToSplit = 0;
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
            this.isRoomCounting = false;

            this.lastRoomID = 0;
            this.wantToSplit = 0;
            this.collectedShards = 0;
            this.collectedSheepOrbs = 0;

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
            int roomID        = (int)goatMemory.GetRoomID();
            bool? replayPaused = goatMemory.GetReplayRecordingPaused();

            if (!replayPaused.HasValue) return false;

            bool increasedSplitLagTimer = false;

            // If the DoorState is currently Clear
            if (this.doorEnteredState == DoorState.Clear) {
                // If the DoorState is clear and we have a paused replay timer
                if (replayPaused.Value) {
                    this.wantToSplit++;
                    increasedSplitLagTimer = true;

                    if (this.wantToSplit > 1) {
                        LogWriter.WriteLine("Door Entered ({0} -> {1}) {2}", this.lastRoomID, roomID, this.wantToSplit);

                        this.doorEnteredState = DoorState.Entering;
                        this.lastRoomID = roomID;
                        return true;
                    }
                }
            } else {
                if (!replayPaused.Value) {
                    this.doorEnteredState = DoorState.Clear;
                }
            }

            // If we didn't increase the split lag timer this frame, reset the timer
            if (!increasedSplitLagTimer) {
                wantToSplit = 0;
            }

            return false;
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

using System;
using System.Collections.Generic;
using LiveSplit.EscapeGoat2.Memory;
using LiveSplit.EscapeGoat2.Debugging;

namespace LiveSplit.EscapeGoat2.State
{
    public class GoatState
    {
        public bool isOpen = false;

        public WorldMap map;
        public GoatMemory goatMemory;
        public GoatTriggers goatTriggers;

        public bool isStarted = false;
        public bool isRoomCounting = false;

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
            bool stopCounting = (bool)goatMemory.GetRoomTimerStopped();
            bool frozen       = (bool)goatMemory.GetRoomFrozen();
            bool firstFrame   = (bool)goatMemory.GetRoomHasRunFirstFrame();
            bool timeStopped  = (firstFrame && stopCounting && !frozen);
            bool isNewRoom    = (roomID != this.lastRoomID);

            if (isNewRoom && this.isRoomCounting && timeStopped) {
                this.wantToSplit++;
                if (this.wantToSplit > 2) {
                    LogWriter.WriteLine("Door Entered. {0} -> {1} {2}", this.lastRoomID, roomID, this.wantToSplit);

                    this.wantToSplit = 0;
                    this.isRoomCounting = !timeStopped;
                    this.lastRoomID = roomID;
                    return true;
                }
            } else {
                this.wantToSplit = 0;
                this.isRoomCounting = !timeStopped;
            }

            return false;
        }

        public bool HaveCollectedNewSheepOrb() {
            int curSheepOrbsCollected = this.collectedSheepOrbs;
            int numSheepOrbsCollected = (int)goatMemory.GetSheepOrbsCollected();

            if (numSheepOrbsCollected > curSheepOrbsCollected) {
                LogWriter.WriteLine("Sheep Orb Obtained: {0} -> {1}", this.collectedSheepOrbs, numSheepOrbsCollected);
            }

            this.collectedSheepOrbs = numSheepOrbsCollected;
            return (numSheepOrbsCollected > curSheepOrbsCollected);
        }

        public bool HaveCollectedNewShard() {
            int curShardsCollected = this.collectedShards;
            int numShardsCollected = (int)goatMemory.GetShardsCollected();

            if (numShardsCollected > curShardsCollected) {
                LogWriter.WriteLine("Shard Obtained: {0} -> {1}", this.collectedShards, numShardsCollected);
            }

            this.collectedShards = numShardsCollected;
            return (numShardsCollected > curShardsCollected);
        }
    }
}

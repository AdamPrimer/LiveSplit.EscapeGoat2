using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using LiveSplit.EscapeGoat2;
using LiveSplit.EscapeGoat2.Memory;

namespace LiveSplit.EscapeGoat2.State
{
    public class GoatState
    {
        public event EventHandler OnTimerStarted;
        public event EventHandler OnTimerFinished;
        public event EventHandler OnTimerChanged;

        public bool isOpen = false;

        public TimeSpan lastSeen = TimeSpan.Zero;
        public WorldMap map;
        public GoatMemory goatMemory;
        public GoatTriggers goatTriggers;

        public bool isInGame = false;
        public bool isStarted = false;

        public int collectedSheepOrbs = 0;
        public bool isRoomCounting = false;



        public GoatState() {
            map = new WorldMap();
            goatMemory = new GoatMemory();
            goatTriggers = new GoatTriggers();
        }

        public void Loop() {
            bool isNowOpen = (goatMemory.HookProcess() && !goatMemory.proc.HasExited);

            if (isNowOpen != isOpen) {
                if (!isNowOpen) {
                    this.isInGame = false;
                    write("escapegoat2.exe is unavailable.");
                } else {
                    write("escapegoat2.exe is available.");
                }
                isOpen = isNowOpen;
            }

            if (isOpen) {
                Pulse();
            }
        }

        public void Pulse() {
            if (!this.isStarted) {
                UpdateStartOfGame(goatMemory.GetStartOfGame());
            }

            //if (this.isStarted) {
                UpdateEndOfLevel();
                UpdateGameTime();
            //}
        }

        public void Dispose() {
            goatMemory.Dispose();
        }

        public void UpdateGameTime() {
            TimeSpan now = goatMemory.GetGameTime();
            
            if (now == this.lastSeen) {
                if (this.OnTimerFinished!= null) this.OnTimerFinished(now, EventArgs.Empty);
            } else if (now > this.lastSeen) {
                this.lastSeen = now;
                if (this.OnTimerStarted != null) this.OnTimerStarted(now, EventArgs.Empty);
            }

            if (now >= this.lastSeen) {
                if (this.OnTimerChanged != null) this.OnTimerChanged(now, EventArgs.Empty);
            }
        }

        public bool HaveEnteredDoor() {
            bool stopCounting = (bool)goatMemory.GetRoomTimerStopped();
            bool frozen       = (bool)goatMemory.GetRoomFrozen();
            bool firstFrame   = (bool)goatMemory.GetRoomHasRunFirstFrame();

            bool timeStopped  = (firstFrame && stopCounting && !frozen);

            if (this.isRoomCounting && timeStopped) {
                write("Door Entered.");
                this.isRoomCounting = !timeStopped;
                return true;
            } else {
                this.isRoomCounting = !timeStopped;
                return false;
            }
        }

        public bool HaveCollectedNewSheepOrb() {
            int numSheepOrbsCollected = (int)goatMemory.GetSheepOrbsCollected();

            if (numSheepOrbsCollected != this.collectedSheepOrbs) {
                write(string.Format("Sheep Orb Obtained: {0} -> {1}", this.collectedSheepOrbs, numSheepOrbsCollected));
                this.collectedSheepOrbs = numSheepOrbsCollected;
                return true;
            } else {
                this.collectedSheepOrbs = numSheepOrbsCollected;
                return false;
            }
        }

        public void UpdateEndOfLevel() {
            var roomInstance = goatMemory.GetRoomInstance();
            bool isOnAction  = (bool)goatMemory.GetOnActionStage();

            if (roomInstance != null && isOnAction) {
                var room                = roomInstance.Value;
                
                bool newDoor      = (bool)HaveEnteredDoor();
                bool newSheepOrb  = (bool)HaveCollectedNewSheepOrb();

                if (newDoor || newSheepOrb) {
                    int roomID = (int)goatMemory.GetRoomID();
                    goatTriggers.SplitOnEndRoom(this.map.GetRoom(roomID));
                }
            }
        }

        public void UpdateStartOfGame(bool isStarted) {
            if (this.isStarted != isStarted) {
                goatTriggers.SplitOnGameStart(isStarted);
                this.isStarted = isStarted;
            }
        }

        public void Reset() {
            this.isStarted = false;
            this.isInGame = false;
            this.lastSeen = TimeSpan.Zero;
            this.collectedSheepOrbs = 0;
            this.isRoomCounting = false;
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

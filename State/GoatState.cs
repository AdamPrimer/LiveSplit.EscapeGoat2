using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using LiveSplit.EscapeGoat2Autosplitter;
using LiveSplit.EscapeGoat2Autosplitter.Memory;

namespace LiveSplit.EscapeGoat2Autosplitter.State
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
        public bool isDead = false;
        public bool hasQuit = false;

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

            if (this.isStarted) {
                UpdateEndOfLevel();
                UpdateGameTime();
            }
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

        public void UpdateEndOfLevel() {
            var roomInstance = goatMemory.GetRoomInstance();
            bool isOnAction  = (bool)goatMemory.GetOnActionStage();

            if (roomInstance != null && isOnAction) {
                var room                = roomInstance.Value;
                bool stopCounting       = (bool)goatMemory.GetRoomTimerStopped();
                bool hasRunFirstFrame   = (bool)goatMemory.GetRoomHasRunFirstFrame();
                bool frozen             = (bool)goatMemory.GetRoomFrozen();
                bool isPaused           = (bool)goatMemory.GetIsGamePaused();
                bool isQuitting         = (bool)goatMemory.GetIsQuittingGame();

                // Update the players alive state only if control is returned
                // otherwise when the level is recreated on death the change in
                // room causes a split.
                if (!frozen) {
                    var player  = goatMemory.GetPlayer();
                    this.isDead = (player == null);
                }

                // If we're not quitting anymore, and we've regained control
                // then remove the quit status.
                if (!isQuitting && !frozen) {
                    this.hasQuit = false;
                } 
                
                // If we're in the middle of quitting, set quit
                else if (isQuitting) {
                    this.hasQuit = true;
                }

                // Stay out of game until we gain control in the next level
                bool inGame = true;
                if ((frozen && this.isInGame == false) || !hasRunFirstFrame) {
                    inGame = false;
                }

                if (hasRunFirstFrame && (stopCounting || (frozen && !hasQuit && !isPaused && !isDead))) {
                    inGame = false;
                }
                
                if (inGame != this.isInGame) {
                    if (inGame == false) {
                        write(string.Format("Split Debug: {0} {1} {2} {3} {4} {5} {6}", this.hasQuit, !frozen, isOnAction, isPaused, isDead, stopCounting, hasRunFirstFrame));
                        int roomID = (int)goatMemory.GetRoomID();
                        goatTriggers.SplitOnEndRoom(this.map.GetRoom(roomID));
                    } else {
                        write(string.Format("Active Debug: {0} {1} {2} {3} {4} {5} {6}", this.hasQuit, !frozen, isOnAction, isPaused, isDead, stopCounting, hasRunFirstFrame));
                    }
                    this.isInGame = inGame;
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
            this.hasQuit = false;
            this.isDead = false;
            this.isInGame = false;
            this.lastSeen = TimeSpan.Zero;
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

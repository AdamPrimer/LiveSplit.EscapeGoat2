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
        public bool isOpen = false;

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

            //if (this.isStarted) {
                UpdateEndOfLevel();
            //}
        }

        public void UpdateEndOfLevel() {
            var roomInstance = goatMemory.GetRoomInstance();
            if (roomInstance != null) {
                var room                = roomInstance.Value;
                bool stopCounting       = (bool)goatMemory.GetRoomTimerStopped();
                bool hasRunFirstFrame   = (bool)goatMemory.GetRoomHasRunFirstFrame();
                bool frozen             = (bool)goatMemory.GetRoomFrozen();
                bool isPaused           = (bool)goatMemory.GetIsGamePaused();
                bool isQuitting         = (bool)goatMemory.GetIsQuittingGame();
                bool isOnAction         = (bool)goatMemory.GetOnActionStage();
                
                if (!frozen) {
                    var player  = goatMemory.GetPlayer();
                    this.isDead = (player == null);
                }

                if (!isQuitting && !frozen) {
                    this.hasQuit = false;
                } else if (isQuitting) {
                    this.hasQuit = true;
                }

                bool inGame = false;
                if (isOnAction && !stopCounting && hasRunFirstFrame && (!frozen || hasQuit || isPaused || isDead)) {
                    inGame = true;
                }

                //write(string.Format("{0} {1} {2} {3} {4} {5} {6}", this.hasQuit, !frozen, isOnAction, isPaused, isDead, stopCounting, hasRunFirstFrame));

                if (inGame != this.isInGame) {
                    if (inGame == false) {
                        int roomID = (int)goatMemory.GetRoomID();
                        goatTriggers.SplitOnEndRoom(this.map.GetRoom(roomID));
                    }
                    this.isInGame = inGame;
                }
            }
        }

        public void UpdateStartOfGame(bool isStarted) {
            if (this.isStarted != isStarted) {
                goatTriggers.SplitOnGameStart(isStarted);
            }
        }

        public void Reset() {
            this.isStarted = false;
            this.hasQuit = false;
            this.isDead = false;
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

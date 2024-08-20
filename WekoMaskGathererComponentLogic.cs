using System;
using System.Linq;

namespace LiveSplit.WekoMaskGatherer {
    public partial class WekoMaskGathererComponent {

        private readonly RemainingDictionary remainingSplits;

        public override bool Update() {
            return memory.Update();
        }

        public override void OnStart() {
            //remainingSplits.Setup(settings.Splits);
            memory.ResetData();
        }


        public override bool Start() {
            return memory.CurrentMap == MapName.Tutorial
                && memory.ComingFromMainMenu
                && memory.SaveData.New == IntPtr.Zero;
            // Start later?
            // memory.Weko.New
        }

        public override bool Reset() {
            return !memory.IsInMainMenu
                && memory.WasInMainMenu
                && memory.SaveData.New == IntPtr.Zero;
        }

        public override bool Loading() {
            if(!memory.IsGameRunning) {
                return true;
            }

            var map = memory.CurrentMap;
            if(string.IsNullOrEmpty(map) || map == MapName.Startup) {
                return true;
            }

            if(memory.IsInMainMenu) {
                return memory.Weko.New != IntPtr.Zero ||
                       memory.SaveData.New != IntPtr.Zero;
            }

            return memory.Weko.New == IntPtr.Zero ||
                   memory.InstructionHud.New == IntPtr.Zero;
        }


        // public override bool Split() {
        //     const string T = "Tracker", TT = "TransitionText", M = "Match";
        //
        //     return remainingSplits.Count() != 0 && (SplitTransitionText() || SplitTracker() || SplitMatch());
        //
        //     bool SplitTransitionText() {
        //         if(!remainingSplits.ContainsKey(TT)
        //         || !memory.TransitionText.Changed || String.IsNullOrEmpty(memory.TransitionText.New)) {
        //             return false;
        //         }
        //         switch(memory.TransitionText.New) {
        //             case "ui_thenextday":
        //             case "ui_theend":
        //                 return remainingSplits.Split(TT, memory.GetEpisode());
        //             default:
        //                 return false;
        //         }
        //     }
        //     
        //     bool SplitTracker() {
        //         if(!remainingSplits.ContainsKey(T)) {
        //             return false;
        //         }
        //         foreach(string name in memory.NewTrackerSequence()) {
        //             if(remainingSplits.Split(T, name)) {
        //                 return true;
        //             }
        //         }
        //         return false;
        //     }
        //
        //     bool SplitMatch() {
        //         if(!remainingSplits.ContainsKey(M) || !memory.Match.Changed) {
        //             return false;
        //         }
        //
        //         bool start = memory.Match.New != IntPtr.Zero;
        //         int mode = memory.MatchMode.New;
        //
        //         if(mode != 0) {
        //             var key = "minigame_" + mode + "-" + memory.MatchMinigameDifficulty.New;
        //             if(start) {
        //                 return remainingSplits.Split(M, key + "_start");
        //             }
        //
        //             if(memory.MatchMinigameRank.New <= 0) {
        //                 return false;
        //             }
        //             //Todo: splits for S ranks
        //             return remainingSplits.Split(M, key + "_end");
        //         }
        //
        //         var script = memory.MatchScript.New;
        //         if(script == null) {
        //             //Todo: splits for Underground Tournament matches
        //             return false;
        //         }
        //
        //         if(start) {
        //             return remainingSplits.Split(M, script + "_start");
        //         }
        //
        //         int winningTeam = memory.MatchWinner.New;
        //         bool lossAllowed = memory.MatchCanLose.New;
        //         if(winningTeam >= 0 && (lossAllowed || winningTeam == 0)) {
        //             //Todo: adjust names for Parking Lot matches in Ep.7
        //             return remainingSplits.Split(M, script + "_end");
        //         }
        //
        //         return false;
        //     }
        // }
    }
}
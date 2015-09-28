# Escape Goat 2 Autosplitter #

## Downloading ##

1. Go to the releases page:

    https://github.com/AdamPrimer/LiveSplit.EscapeGoat2/releases

2. Download `LiveSplit.EscapeGoat2.dll`
3. Download `Microsoft.Diagnostics.Runtime.dll`
4. Download `EscapeGoat2.Autosplitter.exe`

You can optionally download the default LiveSplit splits files: `eg2_any%.lss`
or `eg2_100%.lss`.

## Installing ##

1. Close LiveSplit completely
2. Place `LiveSplit.EscapeGoat2.dll`, `EscapeGoat2.Autosplitter.exe` and
   `Microsoft.Diagnostics.Runtime.dll` in the `Components` directory which is
   inside your `LiveSplit` directory.
3. Open LiveSplit
4. Open the default splits for the route you want, or your own existing splits.
5. Right click -> `Edit Layout` -> `+ Icon` -> `Control` -> `Escape Goat 2
   Autosplitter`
6. Click `OK` then save your layout.

## About the Autosplitter ##

- Timing starts on New Game select
- Will split on entering a door, or picking up a soul.
- You therefore need a split for every exit in your route.
- Default splits files `eg2_any.lss` and `eg2_100%.lss` are provided to make
  this easier since there are many exits.

## About In-Game Time ##

### To Enable ###

- Right Click -> `Compare Against` -> `Game Time`

### About ###

- Sets the time to be equal to the in-game timer
- The In-Game Time is the time seen on the file select menu
- The In-Game Time is independent from the Level Timer seen in the speedrunners
  overlay.
- Splits still occur on exiting doors, even though the In-Game Time continues
  until unload.
- Pulls the current In-Game Time right from memory, yes this is as accurate as
  it gets.
- RTA time will pause on final exit, IGT continues until end of the fade out. 

## Split Lag ##

There is approximately 0.07s of delay between a split trigger and the split
occuring. 

This will be reduced in the future if possible. There is a trade-off between
accuracy, CPU performance and the latency in the splits. The current settings
have been chosen to maximise accuracy and minimise CPU performance without
considerable noticable latency.

## Developers ##

See `Developers.md` for build and release instructions as well as some tips on
how to maintain or modify this codebase.

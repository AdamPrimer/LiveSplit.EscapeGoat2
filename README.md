# Escape Goat 2 Autosplitter #

## Downloading ##

1. Go to the releases page:

    https://github.com/AdamPrimer/LiveSplit.EscapeGoat2Autosplitter/releases

2. Download `LiveSplit.EscapeGoat2Autosplitter.dll`
3. Download `Microsoft.Diagnostics.Runtime.dll`

You can optionally download the default LiveSplit splits files: `eg2_any%.lss` or `eg2_100%.lss`.

## Installing ##

1. Close LiveSplit completely
2. Place `LiveSplit.EscapeGoat2Autosplitter.dll` and `LiveSplit.EscapeGoat2Autosplitter.dll` in the `Components` directory which is
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

## Build Intructions ##

1. Download the source
2. Open `LiveSplit.EscapeGoat2Autosplitter.sln` in Visual Studio 2013 Community Edition
3. Expand `References` and delete `LiveSplit.Core` and `UpdateManager`
4. Right click `References` -> `Add Reference` -> `Browse`
5. Navigate to your LiveSplit install directory, select `LiveSplit.Core.dll`
   and `UpdateManager.dll`
6. Click OK.
7. Build the solution.

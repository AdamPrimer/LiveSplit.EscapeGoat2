# Escape Goat 2 Autosplitter #

## Downloading ##

1. Go to the releases page:

    https://github.com/AdamPrimer/LiveSplit.EscapeGoat2/releases

2. Download `LiveSplit.EscapeGoat2.dll`
3. Download `Microsoft.Diagnostics.Runtime.dll`

You can optionally download the default LiveSplit splits files: `eg2_any%.lss`
or `eg2_100%.lss`.

## Installing ##

1. Close LiveSplit completely
2. Place `LiveSplit.EscapeGoat2.dll` and `Microsoft.Diagnostics.Runtime.dll` in
   the `Components` directory which is inside your `LiveSplit` directory.
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

## Build Intructions ##

1. Download the source
2. Open `LiveSplit.EscapeGoat2.sln` in Visual Studio 2013 Community Edition
3. Expand `References` and delete `LiveSplit.Core` and `UpdateManager`
4. Right click `References` -> `Add Reference` -> `Browse`
5. Navigate to your LiveSplit install directory, select `LiveSplit.Core.dll`
   and `UpdateManager.dll`
6. Click OK.
7. Build the solution.

## Code Breakdown ##

The two core files interfacing with `LiveSplit` are `Component.cs` and
`Factory.cs`.

`Component.cs` defines the component, and defines all functions related to
interacting with `LiveSplit` itself. It is the class called by `LiveSplit` to
initalize the component, and hence needs to initialize the autosplitters state.

`Factory.cs` Defines some metadata about the autosplitter such as its name,
description. This is the class that actually defines that we should call the
`EscapeGoat2Component` found in `Component.cs`.

The autosplitter is then broken into 2 primary files: `State/GoatState.cs` and
`Memory/GoatMemory.cs`, and a secondary file `State/GoatTriggers.cs`.

`GoatState.cs` initializes both `GoatMemory` and `GoatTriggers`. It calls
`GoatMemory` extensively to maintain the state of the autosplitter. When the
state is such that a split should occur, `GoatTriggers` is called to send off
the event.

`GoatMemory.cs` anything that requires reading from the process memory is
handled in this class. It hooks the process, reads the various structures, and
makes available a cleaner API for `GoatState` to call.

`GoatTriggers.cs` this class defines an OnSplit event handler that
`Component.cs` attaches a function to. This class then calls this event handler
whenever `GoatState` instructs it to.

The state finally contains a few ancillary files, `State/Room.cs`,
`State/WorldMap.cs` and `Debugging/LogWriter.cs`.

`Room.cs` simply defines a room structure it is mostly used internally for
debugging purposes and logging.

`WorldMap.cs` which defines the `MapPosition` struct that is core to the
autosplitter, but also defines `WorldMap` which is mostly useful for debugging.
This class is mostly useful if a settings menu is added in the future, as it
would allow for toggling splits on/off for certain rooms.

Finally, there are four files that define the lower level classes used to read
the memory from the process. These classes were predominantly written by others
however they also have seen extensive modification to extend them to suit this
autosplitter. They are: `Memory/ProcessMangler.cs`, `Memory/StaticField.cs`,
`Memory/ArrayPointer.cs` and `Memory/ValuePointer.cs`.

`ProcessMangler.cs` attaches to the `Escape Goat 2` process in order to create
a CLR runtime that can be used to read the Heap from the process.

`StaticField.cs` defines a class for reading the values of static fields from
static classes. This is used to get the core objects from the game state such
as the `SceneManager` from which the other objects can be traversed to.

`ArrayPointer.cs` is used to access properties that are arrays and lists.

`ValuePointer.cs` provides a nice way to traverse down object structures
without needing to know their type.

This is just a brief summary of each file, I hope it provides a decent starting
point for understanding the code base for any future modifications, or fixes,
provided by others in the future.

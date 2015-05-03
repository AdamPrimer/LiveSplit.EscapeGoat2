# For Developers #

The following information is only for people who wish to modify, understand, or
reuse this code either to update it for future patches/changes or perhaps to
use it as the basis for their own autosplitter. If you are a user, you need not
continue past this point.

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

## Releasing a New Version ##

1. Make whatever changes you like
2. Build and test the autosplitter still actually works.
    - Test new game works
    - Test regular rooms split
    - Test sheep rooms split
    - Test shard rooms split
    - Reset LiveSplit and check a new game still works
3. Update `Properties/AssemblyInfo.cs`
    - Update `AssemblyVersion`
    - Update `AssemblyFileVersion`
4. Update `Components/LiveSplit.EscapeGoat2.Updates.xml`
    - Add a new update to the XML with the appropriate version and change log
5. Commit and push the changes to GitHub
6. LiveSplit should now detect the new version when opening it for your users!

## Changing the Release Channel ##

In the event this distribution goes dark and someone else has to fork this
release, it is quite simple to change the automatic update channel.

1. Modify `Factory.cs`
2. Change `UpdateURL` to be the URL to where you will host
   `LiveSplit.EscapeGoat2.Updates.xml`. 
3. If you change the directory structure/name you will also need to modify
   `XMLURL`.

Please note, making this change will not get it detected by existing users,
they will need to manually update to the new fork at least once before updates
will be detected from then on. 

It is therefore my sincere desire to keep this fork alive by accepting pull
requests, as well as am I willing to push an update over this fork
transitioning users to another fork should a developer take up the mantle.

## How To Find Memory Addresses/Structure ##

The names of memory components were found in two main ways to create this
autosplitter. 

Firstly, `EscapeGoat2.exe` was decompiled (it is written in C#) using `ILSpy`
with the `Reflexil` plugin. This code can then be searched to find the names of
classes and properties. This is the easiest method to get a grasp of the
overall structure of the game, read the source directly. Note, the code
decompiled will be C#, not assembly, it is very readable. 

Class properties that are encapsulated like `int RoomID { get; set; }` can be
accessed by wrapping the name in angle brackets and appending `__BackingField` like
so: `<RoomID>k__BackingField`.

Secondly, once you have a class, you can examine its fields from within the
autosplitter by using the `LogWriter.ViewFields` function. This is of course
not as easy as just reading the decompiled source, but suffices if you are
unable to do so. It also has the benefit of allowing you to see the actual
field values in real time so you can see what the values of fields are, not
just their names.

The result of a `StaticField` is a nullable `ValuePointer` so you need to both
check it `HasValue` and then call `Value` on it twice like so:
`if (StaticFieldVar.HasValue) StaticFieldVar.Value.Value;`

The `ValuePointer` class has a `Read()` method, however I recommend against
using it unless you must. 

You can read primitives by using the `GetFieldValue(string fieldName)` method
on the `ValuePointer`. It takes a template that is the Type of the field.

You can read more complex structs by using the `ReadValue` method. You first
simply "dereference" the class rather than getting the field value, then you
call `ReadValue<Struct>()` where `Struct` is your struct. Your struct must have
identical field names to that of the struct being read. See the code as an
example, this is used to read the `MapPosition` struct for the determining when
the player changes map position.

If you get really stuct reading a weird structure, then `CheatEngine` is decent
software for inspecting the memory. Use the memory viewer and then view an
address that you can get by writing out the `Address` property of a
`ValuePointer` then you can take a look at what is going on.

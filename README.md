gmad-helper
===========

Library to help developers interface with gmad.exe within their applications for manipulating Garry's Mod addons

To add to your project:
- Open Git Shell to your project's root directory.
- git submodule add https://github.com/BrianAllred/gmad-helper.git gmad-helper
- git submodule init
- git submodule update
- Add gmadhelper.csproj to your project's solution and add the solution reference

Constructor
===========

Pass the path of gmad.exe into the constructor to create the GMADHelper object.

Public Properties
===========

- AddonPath returns the path of the file/folder uses to either extract an addon from a .gma file or create an addon.
- Mode determines whether we are extracting a file or all of the addons in a folder.
- LastMessage is the last update from the extraction/creation workers. This is useful in order to update your users on the status of the work.

Public Events
===========

These are intended to make status updates from the workers to your application seamless. Hook these up to a UI element to keep your users informed on the work being performed.

- OnExtractMessage fires when there is an update from the extraction worker.
- OnCreateMessage fires when there is an update from the creation worker.

Public Methods
===========

For addon extraction, it is determined when the method is called whether we are extracting a single addon or a folder of them. No work is required by you, the developer to do this.

- Extract(string[, bool]): Extracts the file located at <string> or all the files in folder <string> into the parent folder. <bool> determines if console output from gmad.exe is redirected into the LastMessage event handlers.
- Extract(string1, string2[, bool]): Same as above, except extraction output is <string2> as opposed to <string1>'s parent folder.

Creation only works with folders.

- Create(stringIn, stringOut[, bool]): Uses the folder located at <stringIn> to create a .gma addon file at <stringOut>. <bool> works as noted above.

LUA writing is for writing an addons.lua file for your server. It's useful only when extracting a folder of addons, and so should only be used then.

- WriteLUA(): Writes a lua file that contains all the "resource.AddWorkshop" IDs of the addons you've extracted. No parameters, as it uses the public AddonPath of the GMADHelper object. This will probably change in the future.

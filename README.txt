KSoft OptiOBJ
=============
This is a program I wrote within a day to help me optimize OBJ files to remove duplicate vertices/UVs/normals, it may be messy, but idc =]
Hopefully it can help you if you have an oddly specific problem with your OBJ files.
It is written in C# 4.0 for the .NET Framework 4.0, in Visual Studio 2010. You should be able to load it in any higher version though.

USAGE
=====
Drag and drop an OBJ file onto the exe, or run it from the command line.
There's no exception handling yet, if it crashes you probably have a malformed OBJ file.
The program accepts the -auto flag if you want to run it without pausing for user input.

NOTES
=====
Objects (o) and Groups (g) are not preserved after the optimization process. This program assumes you want your model to be a single, solid, unnamed object.
usemtl is preserved, as faces are grouped into their respective materials.
This does not optimize MTL files and currently only supports one MTL reference in the optimized file.
Smoothing groups (s) are not preserved and I have found no use for them.
Lines are not preserved as I have found no use for them.
Vertex colors (where vertices have 3 extra values after the position) are not supported and are removed from the optimized file.

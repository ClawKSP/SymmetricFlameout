SymmetricFlameout

=========================


Makes engines that are symmetrically placed flame out symmetrically.

Will function on any existing craft (in flight or upon launch). Any engines that are built as symmetric partners will roll back and flame out together.

Engines that are air starved will highlight in yellow. Engines that flame out will highlight in red. To get rid of the highlighting, simply mouse over the part and it will go away (until activated again).



Still a WIP, but seems to work pretty well. Be warned, that I didn't solve all asymmetric flameout issues. Engines are still subject to stock flameout principles and are limited by engine spool up/down time. You still need to design, build, and fly your ship. If you blow past the Intake Air pool too quickly, bad things may still happen. I could roll them back harder, but I wanted to maintain the stock flameout principles rather than supply a set of "auto-throttles."

But of extra note, when the Intake Air reaches 0, you can expect the engines to start rolling back (symmetrically). So you can actually manage the throttles based off of the Intake Air reading.


Installation

============


Just place the SymmetricFlameout.dll in your GameData directory.

SPECIAL NOTE: I strive really hard to maintain stock compatibility with my mods. However, if you UNINSTALL this mod, it may cause issues for any in-flight craft that uses jet engines. It does not modify and will not harm your .craft files in any way.

If you have any craft in-flight when you uninstall this mod, edit your quicksave.sfs and search for any ModuleEngines that state "manuallyOverridden = True". Change this to "manuallyOverridden = False" and everything should be back to normal.



License

=======



Covered under the CC-BY-NC-SA license. See the license.txt for more details.

(https://creativecommons.org/licenses/by-nc-sa/4.0/)





Change Log

==========
v0.01.00 (18 Jan 15) - Initial release

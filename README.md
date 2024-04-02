# BitCrackLauncher

### About

This is a Windows console application that will launch multiple concurrent instances of [BitCrack](https://github.com/brichard19/BitCrack) in an attempt to brute-force solve [Bitcoin Puzzle](https://privatekeyfinder.io/bitcoin-puzzle) private key keys.

The specific puzzle this program attempts to solve is hardcoded to the least-difficult unsolved puzzle at the time of this writing, the keyspace each BitCrack instance searches is random within the given puzzle's solution space, and after a predefined period of time (e.g. six hours), all BitCrack instances are killed and new ones are respawned in new random keyspaces.

If a solution is found, the program will open the solution file and beep incessantly until the program is exited. If the program is relaunched after a solution has been found, the same incessant beeping will resume.

This program is designed to be run as a scheduled task, e.g. 1x/hour or day. (See `Bitcoin Puzzle Solver.xml` for an example Task that can be imported into Task Scheduler.) For this reason, multiple instances of this program will not run at the same time; a new instance will quit if another instance is still alive.

### Setup

1. Clone [BitCrack](https://github.com/brichard19/BitCrack) and build either `cuBitCrack.exe` or `clBitCrack.exe`, depending on your GPU. Or simply download the executable from the project's releases page.
2. In `Program.cs`, update the `ProgramConfig.CrackerPath` variable value to the path where cuBitCrack/clBitCrack is.
3. Check whether the hardcoded puzzle (i.e. the parameters passed into the default `ProgramConfig.Puzzle`) is still unsolved by going to [Bitcoin Puzzle](https://privatekeyfinder.io/bitcoin-puzzle), and change those parameter values to a new puzzle if the hardcoded puzzle is already solved.
4. Either launch this program at-will or import the `Bitcoin Puzzle Solver.xml` task into Windows' Task Scheduler to launch it on a schedule. 
5. Wait for cosmic luck.

> **Note**: You can test your setup by running BitCrackLauncher with a single `debug` command-line argument. The program will target an easy, alreay-solved puzzle. This will demonstrate the experience of having found a solution to the puzzle. Remember to delete the solution file ( `ProgramConfig.SolutionOutputFilePattern`) after you're done.

### To-Dos

Additional ideas for improving this program:

1. Take the configuration (e.g. which puzzle to solve, how often to respawn searches in new random space, how many concurrent BitCrack instances to run) from command line arguments instead of having it be hardcoded.
2. Automatically fetch least-complex unsolved puzzle instead of having it hardcoded or configuration-based.
3. Avoid already-searched keyspaces by either using the "save progress" feature of BitCrack or baking in a solution directly into this program.
4. Evaluate usefulness of and potentially use additional BitCrack command line options.
5. Improve alerting beyond just beeping; send email, SMS, or something else.

### License: CC BY-NC
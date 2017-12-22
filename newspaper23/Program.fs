open SystemTypeExtensions
open SystemUtilities
open CommandLineHelper
open ProgramTypes
open Lenses
open Utilities
open Persist

    // To use this template:
    // 1. Rename the ProgramTypes file to <MY-PROGRAM-NAME>Types
    // 2. Inside the old ProgramTypes file, change the MyProgramConfig to your name <MY-PROGRAM-NAME>Config
    // 3. Add all the help text, record members, and defaults for your new program config file
    // 4. Update all the references and compile. You will also need to change the "opens" 
    // that use the old ProgramTypes
    // 5. 

/// The new Main
let doStuff (opts:MyProgramConfig) =
    // Use the functional onion pattern
    // First, thoroughly clean everything coming in
    // Used shared source code Common directory to share types
    // (When your program types get shared, you will need to move the <>Types file
    // out to the common folder, delete the local one, and use a link to access it
    // Either quit (HIGHLY unlikely), skip pieces, or force defaults for bad data

    // Then do whatever it is your program does

    // Then send the new data out to whomever wants to consume it
    // Continue to use shared type code files for compatability
    // Remember to use the combination of Verbosity and InterimProgress (from ConfigBase)
    // to update the world on how things are going
    ()

[<EntryPoint>]
let main argv = 
    try
        let opts = loadConfigFromCommandLine argv
        commandLinePrintWhileEnter opts.configBase (opts.printThis)
        let outputDirectories = doStuff opts
        commandLinePrintWhileExit opts.configBase
        0
    with
        | :? UserNeedsHelp as hex ->
            defaultBaseOptions.printThis
            0
        | ex ->
            System.Console.WriteLine ("Program terminated abnormally " + ex.Message)
            System.Console.WriteLine (ex.StackTrace)
            if ex.InnerException = null
                then
                    0
                else
                    System.Console.WriteLine("---   Inner Exception   ---")
                    System.Console.WriteLine (ex.InnerException.Message)
                    System.Console.WriteLine (ex.InnerException.StackTrace)
                    0    
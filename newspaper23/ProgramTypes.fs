module ProgramTypes
    open SystemTypeExtensions
    open SystemUtilities
    open CommandLineHelper

    //
    // HOW TO MAKE A CONFIG for Command-Line Programs
    //

    // Create a type that will handle your program config
    // Use configBase to handle common stuff
    // Then put whatever you want, inheriting from ConfigEntry
    [<NoComparison>]
    type MyProgramConfig =
        {
            configBase:ConfigBase
            sourceDirectory:ConfigEntry<DirectoryParm>
            destinationDirectory:ConfigEntry<DirectoryParm>
            sampleStringParameter:ConfigEntry<string>
        }
        member this.printThis() =
            printfn "MyProgram Parameters Provided"
            this.configBase.verbose.printVal
            this.sourceDirectory.printVal
            printfn "sourceDirectoryExists: %b" (snd this.sourceDirectory.parameterValue).IsSome
            this.destinationDirectory.printVal
            printfn "destinationDirectoryExists: %b" (snd this.destinationDirectory.parameterValue).IsSome
            this.sampleStringParameter.printVal
            printfn "Namespace: %s" this.sampleStringParameter.parameterValue


    // Add any help text you want
    let programHelp = [|
                        "Here's some program help."
                        ;"and some more.. as much as you want to provide,"
                        |]
    // Add in default values
    let defaultBaseOptions = createNewBaseOptions "easyam" "Command-line analysis model compiler" programHelp defaultVerbosity
    let defaulSourceDirectory = createNewConfigEntry "S" "Source Directory (Optional)" [|"/S:<path> -> path to the directory having the source files."|] (System.AppDomain.CurrentDomain.BaseDirectory, Some(System.IO.DirectoryInfo(System.AppDomain.CurrentDomain.BaseDirectory)))
    let defaultDestinationDirectory = createNewConfigEntry "D" "Destination Directory (Optional)" [|"/D:<path> -> path to the directory where compiled files will be deployed."|] (System.AppDomain.CurrentDomain.BaseDirectory, Some(System.IO.DirectoryInfo(System.AppDomain.CurrentDomain.BaseDirectory)))
    let defaultSampleStringParameter = createNewConfigEntry "N" "Namespace (Optional)" [|"/N:<namespace> -> namespace filter to show in output."; "Example: a team's sprint stories may have a namespace of BadgerTeam: Sprint 3"|] ""

    let loadConfigFromCommandLine (args:string []):MyProgramConfig =
        if args.Length>0 && (args.[0]="?"||args.[0]="/?"||args.[0]="-?"||args.[0]="--?"||args.[0]="help"||args.[0]="/help"||args.[0]="-help"||args.[0]="--help") then raise (UserNeedsHelp args.[0]) else
        let newVerbosity =ConfigEntry<_>.populateValueFromCommandLine(defaultVerbosity, args)
        let newSourceDirectory = ConfigEntry<_>.populateValueFromCommandLine(defaulSourceDirectory, args)
        let newDestinationDirectory = ConfigEntry<_>.populateValueFromCommandLine(defaultDestinationDirectory, args)
        let newConfigBase = {defaultBaseOptions with verbose=newVerbosity}
        let newSampleStringParameter = ConfigEntry<_>.populateValueFromCommandLine(defaultSampleStringParameter, args)
        { 
            configBase = newConfigBase
            sourceDirectory=newSourceDirectory
            destinationDirectory=newDestinationDirectory
            sampleStringParameter=newSampleStringParameter
        }


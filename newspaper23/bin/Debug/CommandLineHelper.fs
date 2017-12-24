module CommandLineHelper
    open SystemTypeExtensions
    open SystemUtilities
    open System.IO
    open Newtonsoft.Json

    exception UserNeedsHelp of string
    type Verbosity =
        | Silent            = 1
        | Normal            = 5
        | Anal              = 9
    //
    // Program Command Line Config Settings
    //
    let getMatchingParameters (args:string []) (symbol:string) = 
        args |> Array.filter(fun x->
                    let argParms = x.Split([|':'|],2)
                    let parmName = (argParms.[0]).Substring(1).ToUpper()
                    if argParms.Length > 0 then parmName=symbol.ToUpper() else false
                    )
    let getValuePartOfMostRelevantCommandLineMatch (args:string []) (symbol:string) =
        let matchingParms = getMatchingParameters args symbol
        if matchingParms.Length > 0
            then
                // if there are multiple entries, last one overrides the rest
                let commandLineParm = matchingParms.[matchingParms.Length-1]
                let parmSections=commandLineParm.Split([|':'|], 2)
                if parmSections.Length<2 then Some "" else Some parmSections.[1]
            else
                None
    //type FileParm = string*System.IO.FileInfo option
    type FileParm = 
        {
            FileName:string
            FileInfoOption:System.IO.FileInfo option
        }
        member x.LoadJsonDataOrCreateJsonFileIfMissing<'a> defaultJsonData =
            let fileContents= 
                if (x.FileInfoOption.IsNone) || (File.Exists(x.FileInfoOption.Value.FullName)=false)
                    then
                        System.IO.File.CreateText(x.FileName) |> ignore
                        JsonConvert.SerializeObject(defaultJsonData)
                    else System.IO.File.ReadAllText(x.FileInfoOption.Value.FullName)
            let fileData = 
                if fileContents="" then defaultJsonData else JsonConvert.DeserializeObject<'a>(fileContents)
            fileData
    type DirectoryParm = 
        {
            DirectoryName:string
            DirectoryInfoOption:System.IO.DirectoryInfo option
        }
    type SortOrder = Ascending | Descending
                        static member ToList()=[Ascending;Descending]
                        override this.ToString()=
                            match this with
                                | Ascending->"Ascending"
                                | Descending->"Descending"
                        static member TryParse(stringToParse:string) =
                            match stringToParse with
                                |"a"|"asc"|"ascending"|"A"|"ASC"|"Ascending"|"Asc"|"ASCENDING"->true,SortOrder.Ascending
                                |"d"|"desc"|"descending"|"D"|"DESC"|"Descending"|"Desc"|"DESCENDING"->true,SortOrder.Descending
                                |_->false, SortOrder.Ascending
                        static member Parse(stringToParse:string) =
                            match stringToParse with
                                |"a"|"asc"|"ascending"|"A"|"ASC"|"Ascending"|"Asc"|"ASCENDING"->SortOrder.Ascending
                                |"d"|"desc"|"descending"|"D"|"DESC"|"Descending"|"Desc"|"DESCENDING"->SortOrder.Descending
                                |_->raise(new System.ArgumentOutOfRangeException("Sort Order","The string value provided for Sort Order is not in the Sort Order enum"))

    /// Parameterized type to allow command-line argument processing without a lot of extra coder work
    /// Instantiate the type with the type of value you want. Make a default entry in case nothing is found
    /// Then call the populate method. Will pull from args and return a val and args with the found value (if any consumed)
    type ConfigEntry<'A> =
        {
            commandLineParameterSymbol:string
            commandLineParameterName:string
            parameterHelpText:string[]
            parameterValue:'A
        } with
            member this.printVal =
                printfn "%s: %s" this.commandLineParameterName (this.parameterValue.ToString())
            member this.printHelp =
                printfn "%s" this.commandLineParameterName
                this.parameterHelpText |> Seq.iter(System.Console.WriteLine)
            member this.swapInNewValue x =
                {this with parameterValue=x}
            static member populateValueFromCommandLine ((defaultConfig:ConfigEntry<Verbosity>), (args:string[])):ConfigEntry<Verbosity>  =
                let parmValue = getValuePartOfMostRelevantCommandLineMatch args defaultConfig.commandLineParameterSymbol
                if parmValue.IsSome
                    then
                        let parsedNumValue = System.Int32.Parse("0" + parmValue.Value)
                        let parsedVerbosityValue = enum<Verbosity>(parsedNumValue)
                        defaultConfig.swapInNewValue parsedVerbosityValue
                    else
                        defaultConfig
            static member populateValueFromCommandLine ((defaultConfig:ConfigEntry<string>), (args:string[])):ConfigEntry<string>  =
                let parmValue = getValuePartOfMostRelevantCommandLineMatch args defaultConfig.commandLineParameterSymbol
                if parmValue.IsSome
                    then
                        defaultConfig.swapInNewValue parmValue.Value
                    else
                        defaultConfig
            static member populateValueFromCommandLine ((defaultConfig:ConfigEntry<DirectoryParm>), (args:string[])):ConfigEntry<DirectoryParm>  =
                let parmValue = getValuePartOfMostRelevantCommandLineMatch args defaultConfig.commandLineParameterSymbol
                if parmValue.IsSome
                    then
                        if System.IO.Directory.Exists(parmValue.Value)
                            then 
                                let tempDirectoryInfoOption = Some(System.IO.DirectoryInfo(parmValue.Value))
                                defaultConfig.swapInNewValue ({DirectoryName=parmValue.Value; DirectoryInfoOption=tempDirectoryInfoOption})
                            else defaultConfig.swapInNewValue ({DirectoryName=parmValue.Value; DirectoryInfoOption=Option.None})
                    else
                        defaultConfig
            static member populateValueFromCommandLine ((defaultConfig:ConfigEntry<FileParm>), (args:string[])):ConfigEntry<FileParm>  =
                let parmValue = getValuePartOfMostRelevantCommandLineMatch args defaultConfig.commandLineParameterSymbol
                if parmValue.IsSome
                    then
                        if System.IO.File.Exists(parmValue.Value)
                            then
                                let tempFileInfoOption = Some(System.IO.FileInfo(parmValue.Value))
                                defaultConfig.swapInNewValue ({FileName=parmValue.Value; FileInfoOption=tempFileInfoOption})
                            else
                                defaultConfig.swapInNewValue ({FileName=parmValue.Value; FileInfoOption=Option.None})
                    else
                        defaultConfig
            static member populateValueFromCommandLine ((defaultConfig:ConfigEntry<bool>), (args:string[])):ConfigEntry<bool> =
                let parmValue = getValuePartOfMostRelevantCommandLineMatch args defaultConfig.commandLineParameterSymbol
                if parmValue.IsSome
                    then
                        if parmValue.Value.ToUpper() = "FALSE" || parmValue.Value = "0" || parmValue.Value.ToUpper() = "F" || parmValue.Value.ToUpper() = "NO"
                            then
                                defaultConfig.swapInNewValue false
                            else
                                defaultConfig.swapInNewValue true
                    else
                        defaultConfig
            static member populateValueFromCommandLine ((defaultConfig:ConfigEntry<int>), (args:string[])):ConfigEntry<int>  =
                let parmValue = getValuePartOfMostRelevantCommandLineMatch args defaultConfig.commandLineParameterSymbol
                if parmValue.IsSome
                    then
                        let parmInt = System.Int32.Parse("0" + parmValue.Value)
                        defaultConfig.swapInNewValue parmInt
                    else
                        defaultConfig
            static member populateValueFromCommandLine ((defaultConfig:ConfigEntry<System.Uri>), (args:string[])):ConfigEntry<System.Uri>  =
                let parmValue = getValuePartOfMostRelevantCommandLineMatch args defaultConfig.commandLineParameterSymbol
                if parmValue.IsSome
                    then
                        defaultConfig.swapInNewValue (new System.Uri(parmValue.Value))
                    else
                        defaultConfig
            static member populateValueFromCommandLine ((defaultConfig:ConfigEntry<System.DateTime>), (args:string[])):ConfigEntry<System.DateTime>  =
                let parmValue = getValuePartOfMostRelevantCommandLineMatch args defaultConfig.commandLineParameterSymbol
                if parmValue.IsSome
                    then
                        defaultConfig.swapInNewValue (System.DateTime.Parse(parmValue.Value))
                    else
                        defaultConfig
            static member populateValueFromCommandLine ((defaultConfig:ConfigEntry<SortOrder>), (args:string[])):ConfigEntry<SortOrder>  =
                let parmValue = getValuePartOfMostRelevantCommandLineMatch args defaultConfig.commandLineParameterSymbol
                let newVal=if parmValue.IsNone then defaultConfig.parameterValue else
                            let tp=SortOrder.TryParse parmValue.Value
                            if fst tp=true then snd tp else defaultConfig.parameterValue
                defaultConfig.swapInNewValue newVal
    /// A type so that programs can report what they're doing as they do it
    // This was the programmer can decide what to do with it instead of the OS
    [<NoComparison>]
    type InterimProgress =
        {
            items:System.Collections.Generic.Dictionary<string, System.Text.StringBuilder>
        } with
        member this.addItem key (vl:string) =
            let lookup = 
                if this.items.ContainsKey key then this.items.Item(key)
                    else
                        let newItem = new System.Text.StringBuilder(65535)
                        this.items.Add(key,newItem)
                        newItem
            lookup.Append("\r\n" + vl) |> ignore
        member this.getItem key  =
            if this.items.ContainsKey key
                then
                    this.items.Item(key).ToString()
                else
                    ""
    // All programs have at least this configuration on the command line
    [<NoComparison>]
    type ConfigBase =
        {
            programName:string
            programTagLine:string
            programHelpText:string[]
            verbose:ConfigEntry<Verbosity>
            interimProgress:InterimProgress
        }
        member this.printProgramDescription =
            this.programHelpText |> Seq.iter(System.Console.WriteLine)
        member this.printThis =
            printfn "%s" this.programName
            this.programHelpText |> Seq.iter(System.Console.WriteLine)

    let directoryExists (dir:ConfigEntry<DirectoryParm>) = dir.parameterValue.DirectoryInfoOption.IsSome
    let fileExists (dir:ConfigEntry<FileParm>) = dir.parameterValue.FileInfoOption.IsSome




    /// Prints out the options for the command before it runs. Detail level is based on verbosity setting
    let commandLinePrintWhileEnter (opts:ConfigBase) fnPrintMe =
                // Entering program command line report
            let nowString = string System.DateTime.Now
            match opts.verbose.parameterValue with
                | Verbosity.Silent ->
                    ()
                | Verbosity.Normal ->
                    printfn "%s. %s" opts.programName opts.programTagLine
                    printfn "Begin: %s" (nowString)
                    printfn "Verbosity: Normal" 
                | Verbosity.Anal ->
                    printfn "%s. %s" opts.programName opts.programTagLine
                    printfn "Begin: %s" (nowString)
                    fnPrintMe()
                |_ ->
                    printfn "%s. %s" opts.programName opts.programTagLine
                    printfn "Begin: %s" (nowString)
                    fnPrintMe()

    /// Exiting program command line report. Detail level is based on verbosity setting
    let commandLinePrintWhileExit (baseOptions:ConfigBase) =
        let nowString = string System.DateTime.Now
        match baseOptions.verbose.parameterValue with
            | Verbosity.Silent ->
                ()
            | Verbosity.Normal ->
                printfn "End:   %s" (nowString)
            | Verbosity.Anal ->
                printfn "End:   %s" (nowString)
            |_ ->
                ()

    let defaultVerbosity  =
        {
            commandLineParameterSymbol="V"
            commandLineParameterName="Verbosity"
            parameterHelpText=[|"/V:[0-9]           -> Amount of trace info to report. 0=none, 5=normal, 9=max."|]           
            parameterValue=Verbosity.Normal
        }

    let createNewBaseOptions programName programTagLine programHelpText verbose =
        {
            programName = programName
            programTagLine = programTagLine
            programHelpText=programHelpText
            verbose = verbose
            interimProgress = {items=new System.Collections.Generic.Dictionary<string, System.Text.StringBuilder>()}
        }

    let createNewConfigEntry commandlineSymbol commandlineParameterName parameterHelpText initialValue =
        {
            commandLineParameterSymbol=commandlineSymbol
            commandLineParameterName=commandlineParameterName
            parameterHelpText=parameterHelpText
            parameterValue=initialValue
        }

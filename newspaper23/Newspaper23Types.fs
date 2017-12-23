﻿module Newspaper23Types
    open SystemTypeExtensions
    open SystemUtilities
    open CommandLineHelper
    open System
    open Newtonsoft.Json.Converters
    open Newtonsoft.Json

    //
    // HOW TO MAKE A CONFIG for Command-Line Programs
    //

    // Create a type that will handle your program config
    // Use configBase to handle common stuff
    // Then put whatever you want, inheriting from ConfigEntry
    [<NoComparison>]
    type Newspaper23Config =
        {
            ConfigBase:ConfigBase
            InputFile:ConfigEntry<FileParm>
            OutputFile:ConfigEntry<FileParm>
        }
        member this.printThis() =
            printfn "MyProgram Parameters Provided"
            this.ConfigBase.verbose.printVal
            this.InputFile.printVal
            printfn "sourceFileExists: %b" (snd this.InputFile.parameterValue).IsSome
            this.OutputFile.printVal
            printfn "destinationDirectoryExists: %b" (snd this.OutputFile.parameterValue).IsSome


    // Add any help text you want
    let programHelp = [|
                        "Here's some program help."
                        ;"and some more.. as much as you want to provide,"
                        |]
    // Add in default values
    let defaultBaseOptions = createNewBaseOptions "newspaper23" "gets new links from a site list" programHelp defaultVerbosity
    let defaultInputFileName="input.json"
    let defaultInputFileExists=System.IO.File.Exists(defaultInputFileName)
    let defaultInputFileInfo = if defaultInputFileExists then Some (new System.IO.FileInfo(defaultInputFileName)) else option.None
    let defaultInputFile= createNewConfigEntry "I" "Input File (Optional)" [|"/I:<path> -> full name of the file having program input."|] (defaultInputFileName, defaultInputFileInfo)
    let defaultOutputFileName="output.json"
    let defaultOutputFileExists=System.IO.File.Exists(defaultOutputFileName)
    let defaultOutputFileInfo = if defaultOutputFileExists then Some (new System.IO.FileInfo(defaultOutputFileName)) else option.None
    let defaultOutputFile = createNewConfigEntry "D=O" "Outpuf File (Optional)" [|"/O:<path> -> full name of the file where program output will be deployed."|] (defaultOutputFileName, defaultOutputFileInfo)

    let loadConfigFromCommandLine (args:string []):Newspaper23Config =
        if args.Length>0 && (args.[0]="?"||args.[0]="/?"||args.[0]="-?"||args.[0]="--?"||args.[0]="help"||args.[0]="/help"||args.[0]="-help"||args.[0]="--help") then raise (UserNeedsHelp args.[0]) else
        let newVerbosity =ConfigEntry<_>.populateValueFromCommandLine(defaultVerbosity, args)
        let newInputFile = ConfigEntry<_>.populateValueFromCommandLine(defaultInputFile, args)
        let newOutputFile = ConfigEntry<_>.populateValueFromCommandLine(defaultOutputFile, args)
        let newConfigBase = {defaultBaseOptions with verbose=newVerbosity}
        { 
            ConfigBase = newConfigBase
            InputFile=newInputFile
            OutputFile=newOutputFile
        }


    type SiteToVist =
        {
            Category:string;
            SiteName:string;
            URLToVisit:string;
            NumberOfLinksToGather:int;
            MorePagesXPath:string;
        }
    let defaultSiteToVist =
        {
            Category="News";
            SiteName="CNN";
            URLToVisit="https://www.cnn.com";
            NumberOfLinksToGather=30;
            MorePagesXPath=""
        }
    [<NoComparison>]
    type Newspaper23Input =
        {
            LastRunTime:DateTime;
            SitesToVisit:SiteToVist list;
        }
    let defaultNewspaper23Input =
        {
            LastRunTime=DateTime.Now.AddDays(-1.0);
            SitesToVisit=[defaultSiteToVist]
        }
    //let defaultInputFileContents = @"
    //    "
    let defaultInputFileContents = JsonConvert.SerializeObject(defaultNewspaper23Input)


    [<NoComparison>]
    type SiteVisitedLinkRecord =
        {
            Category:string;
            SiteName:string;
            LinkText:string;
            Link:string;
            RipTime:DateTime;
        }
    [<NoComparison>]
    type Newspaper23Output=
        {
            Links:SiteVisitedLinkRecord[];
        }
    let defaultNewspaper23Output =
        {
            Links=[||];
        }
    //let defaultOutputFileContents = @"
    //    "
    let defaultOutputFileContents = JsonConvert.SerializeObject(defaultNewspaper23Output)

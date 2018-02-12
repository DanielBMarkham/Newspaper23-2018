module Newspaper23Types
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
            FilteredOutputFile:ConfigEntry<FileParm>
            HoursRecent:ConfigEntry<int>
        }
        member this.printThis() =
            printfn "MyProgram Parameters Provided"
            this.ConfigBase.verbose.printVal
            this.InputFile.printVal
            printfn "Input File Exists: %b" this.InputFile.parameterValue.FileInfoOption.IsSome
            this.OutputFile.printVal
            printfn "Output File Exists: %b" this.OutputFile.parameterValue.FileInfoOption.IsSome
            this.FilteredOutputFile.printVal
            printfn "Filtered Output File Exists: %b" this.FilteredOutputFile.parameterValue.FileInfoOption.IsSome
            this.HoursRecent.printVal

    // Add any help text you want
    let programHelp = [|
                        "Here's some program help."
                        ;"and some more.. as much as you want to provide,"
                        |]
    // Add in default values
    let defaultBaseOptions = createNewBaseOptions "newspaper23" "gets new links from a site list" programHelp defaultVerbosity
    let defaultInputFileName="newspaper23Input.json"
    let defaultInputFileExists=System.IO.File.Exists(defaultInputFileName)
    let defaultInputFileInfo = if defaultInputFileExists then Some (new System.IO.FileInfo(defaultInputFileName)) else option.None
    let defaultInputFile= createNewConfigEntry "I" "Input File (Optional)" [|"/I:<fullName> -> full name of the file having program input."|] ({FileName=defaultInputFileName; FileInfoOption=defaultInputFileInfo})
    let defaultOutputFileName="linkLibrary.json"
    let defaultOutputFileExists=System.IO.File.Exists(defaultOutputFileName)
    let defaultOutputFileInfo = if defaultOutputFileExists then Some (new System.IO.FileInfo(defaultOutputFileName)) else option.None
    let defaultOutputFile = createNewConfigEntry "O" "Output File (Optional)" [|"/O:<fullName> -> full name of the file where program output will be deployed."|] ({FileName=defaultOutputFileName; FileInfoOption=defaultOutputFileInfo})
    let defaultFilteredOutputFileName="recentLinks.json"
    let defaultFilteredOutputFileExists=System.IO.File.Exists(defaultFilteredOutputFileName)
    let defaultFilteredOutputFileInfo = if defaultFilteredOutputFileExists then Some (new System.IO.FileInfo(defaultFilteredOutputFileName)) else option.None
    let defaultFilteredOutputFile = createNewConfigEntry "F" "Filtered Output File (Optional)" [|"/F:<fullName> -> full name of the file where program filtered output will be deployed."|] ({FileName=defaultFilteredOutputFileName; FileInfoOption=defaultFilteredOutputFileInfo})
    let defaultHoursRecent= createNewConfigEntry "H" "How many hours ago to filter for recent links" [|"/H:<int number of hours> -> number of hours."|] 1

    let loadConfigFromCommandLine (args:string []):Newspaper23Config =
        if args.Length>0 && (args.[0]="?"||args.[0]="/?"||args.[0]="-?"||args.[0]="--?"||args.[0]="help"||args.[0]="/help"||args.[0]="-help"||args.[0]="--help") then raise (UserNeedsHelp args.[0]) else
        let newVerbosity =ConfigEntry<_>.populateValueFromCommandLine(defaultVerbosity, args)
        let newInputFile = ConfigEntry<_>.populateValueFromCommandLine(defaultInputFile, args)
        let newOutputFile = ConfigEntry<_>.populateValueFromCommandLine(defaultOutputFile, args)
        let newFilteredOutputFile = ConfigEntry<_>.populateValueFromCommandLine(defaultFilteredOutputFile, args)
        let newHoursRecent = ConfigEntry<_>.populateValueFromCommandLine(defaultHoursRecent, args)

        let newConfigBase = {defaultBaseOptions with verbose=newVerbosity}
        { 
            ConfigBase = newConfigBase
            InputFile=newInputFile
            OutputFile=newOutputFile
            FilteredOutputFile=newFilteredOutputFile
            HoursRecent=newHoursRecent
        }


    type SiteToVist =
        {
            Category:string;
            SiteName:string;
            URLToVisit:string;
            NumberOfLinksToGather:int;
            MorePagesXPath:string;
            CustomXPath:string;
            LastVisitTime:DateTime
            LastVisitLinkCount:int
            TotalVisits:int
            TotalVisitsLinksGathered:int
            LastTimeLinksWereFound:DateTime
        }
    let defaultSiteToVist =
        {
            Category="News";
            SiteName="CNN";
            URLToVisit="https://www.cnn.com";
            NumberOfLinksToGather=30;
            MorePagesXPath=""
            CustomXPath=""
            LastVisitTime=DateTime.MinValue
            LastVisitLinkCount=0
            TotalVisits=0
            TotalVisitsLinksGathered=0
            LastTimeLinksWereFound=DateTime.MinValue
        }
    [<NoComparison>]
    type Newspaper23Input =
        {
            LastRunTime:DateTime;
            SitesToVisit:SiteToVist list;
        }
    let defaultNewspaper23Input =
        {
            LastRunTime=DateTime.Now.AddDays(-1.0)
            SitesToVisit=[defaultSiteToVist]
        }
    let defaultInputFileContents = JsonConvert.SerializeObject(defaultNewspaper23Input)


    [<NoComparison>]
    type SiteVisitedLinkRecord =
        {
            Category:string;
            SiteName:string;
            LinkText:string;
            Link:string;
            LocalLinkFileName:string;
            CentileRanking:int;
            RipTime:DateTime;
        }
    [<NoComparison>]
    type Newspaper23Output=
        {
            CategoryList:string[]
            SiteList:string[]
            Links:SiteVisitedLinkRecord[];
        }
    let defaultNewspaper23Output =
        {
            CategoryList=[||]
            SiteList=[||]
            Links=[||]
        }
    let defaultFilteredNewspaper23Output=defaultNewspaper23Output
    let defaultOutputFileContents = JsonConvert.SerializeObject(defaultNewspaper23Output)
    let defaultFilteredOutputFileContents = JsonConvert.SerializeObject(defaultFilteredNewspaper23Output)


open SystemTypeExtensions
open SystemUtilities
open CommandLineHelper
open Newspaper23Types
open Lenses
open Utilities
open Persist
open Newtonsoft.Json
open Newtonsoft.Json.Converters


/// The new Main
let doStuff (opts:Newspaper23Config) =
    // first thing to do is clean/verify input data so that it's bulletproof
    let inputFileContents= 
        if ((snd opts.InputFile.parameterValue).IsNone)
            then
                System.IO.File.CreateText(fst opts.InputFile.parameterValue) |> ignore
                defaultInputFileContents
            else System.IO.File.ReadAllText((snd opts.InputFile.parameterValue).Value.FullName)
    let inputData = 
        if inputFileContents="" 
            then defaultNewspaper23Input
            else JsonConvert.DeserializeObject<Newspaper23Input>(defaultInputFileContents)
    let outputFileContents= 
        if ((snd opts.OutputFile.parameterValue).IsNone)
            then
                System.IO.File.CreateText(fst opts.OutputFile.parameterValue) |> ignore
                defaultOutputFileContents
            else System.IO.File.ReadAllText((snd opts.OutputFile.parameterValue).Value.FullName)
    let outputData = 
        if outputFileContents=""
            then defaultNewspaper23Output
            else JsonConvert.DeserializeObject<Newspaper23Output> outputFileContents
    // then the main processing loop
    let newOutputData = 
        inputData.SitesToVisit 
            |> List.fold(fun (outerAccumulatorOutputFile,index) siteToVisit->
            let linkTextAndTargets=findTextLinksOnAPage siteToVisit.URLToVisit
            // for each link, see if it already exists in the output file
            // if so, skip. Otherwise add it
            let newOuterAccumulatorOutputFile = 
                linkTextAndTargets 
                |> Array.fold(fun (innerAccumulatorOutputFile:Newspaper23Output) (incomingLinkText,incomingLinkTarget)->
                let alreadyExistsInOutputFile =
                    innerAccumulatorOutputFile.Links<>null
                    && innerAccumulatorOutputFile.Links.Length>0
                    && innerAccumulatorOutputFile.Links|>Array.exists(fun x->x.Link=incomingLinkTarget)
                if alreadyExistsInOutputFile 
                    then innerAccumulatorOutputFile
                    else
                        let newLinkItem:SiteVisitedLinkRecord =
                            {
                                Category=siteToVisit.Category
                                SiteName=siteToVisit.SiteName
                                LinkText=incomingLinkText
                                Link=incomingLinkTarget
                                RipTime=System.DateTime.Now;
                            }
                        let newLinks = [|newLinkItem|] |> Array.append innerAccumulatorOutputFile.Links 
                        {innerAccumulatorOutputFile with Links=newLinks}
                ) outerAccumulatorOutputFile
            (newOuterAccumulatorOutputFile,index+1)
            ) (outputData,0)
    // persist the program output
    let programOutput=JsonConvert.SerializeObject newOutputData
    System.IO.File.WriteAllText((snd opts.OutputFile.parameterValue).Value.FullName,programOutput)
    ()

[<EntryPoint; System.STAThreadAttribute>]
let main argv = 
    try
        let opts = loadConfigFromCommandLine argv
        commandLinePrintWhileEnter opts.ConfigBase (opts.printThis)
        let outputDirectories = doStuff opts
        commandLinePrintWhileExit opts.ConfigBase
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
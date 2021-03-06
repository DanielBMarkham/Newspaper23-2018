﻿open SystemTypeExtensions
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
    //let inputData = opts.InputFile.parameterValue.LoadJsonDataOrCreateJsonFileIfMissing<Newspaper23Input> defaultNewspaper23Input
    let sitesToProcess=getTheNextSitesToProcess opts
    let outputData = opts.OutputFile.parameterValue.LoadJsonDataOrCreateJsonFileIfMissing<Newspaper23Output> defaultNewspaper23Output
    let filteredOutputputData = opts.FilteredOutputFile.parameterValue.LoadJsonDataOrCreateJsonFileIfMissing<Newspaper23Output> defaultNewspaper23Output
    // then the main processing loop
    let newOutputDataAndCount = 
        sitesToProcess
            |> Array.fold(fun (outerAccumulatorOutputFile,index) siteToVisit->
            let linkTextAndTargets=findTextLinksOnAPage siteToVisit.URLToVisit siteToVisit.CustomXPath
            // for each link, see if it already exists in the output file
            // if so, skip. Otherwise add it
            let newOuterAccumulatorOutputFile = 
                linkTextAndTargets 
                |> Array.fold(fun (innerAccumulatorOutputFile:Newspaper23Output) (incomingLinkText,incomingLinkTarget)->
                let alreadyExistsInOutputFile =
                    innerAccumulatorOutputFile.Links<>null
                    && innerAccumulatorOutputFile.Links.Length>0
                    && innerAccumulatorOutputFile.Links|>Array.exists(fun x->(x.Link=incomingLinkTarget) || ((x.SiteName=siteToVisit.SiteName) && (x.LinkText=incomingLinkText)))
                if alreadyExistsInOutputFile 
                    then innerAccumulatorOutputFile
                    else
                        let newLinkItem:SiteVisitedLinkRecord =
                            {
                                Category=siteToVisit.Category
                                SiteName=siteToVisit.SiteName
                                LinkText=incomingLinkText
                                Link=incomingLinkTarget
                                LocalLinkFileName=makeLocalLinkFileName incomingLinkText
                                RipTime=System.DateTime.Now;
                                CentileRanking=0;
                            }
                        let newLinks = [|newLinkItem|] |> Array.append innerAccumulatorOutputFile.Links 
                        {innerAccumulatorOutputFile with Links=newLinks}
                ) outerAccumulatorOutputFile
            (newOuterAccumulatorOutputFile,index+1)
            ) (outputData,0)
    // persist the program output
    let newSerialSettings=new JsonSerializerSettings()
    newSerialSettings.Formatting<-Formatting.Indented

    let newOutputData = fst newOutputDataAndCount
    let newCategoryList =
        newOutputData.Links |> Array.toList |> removeDuplicatesBy(fun x->x.Category) |> Array.map(fun x->x.Category)
    let newSiteList =
        newOutputData.Links |> Array.toList |> removeDuplicatesBy(fun x->x.SiteName) |> Array.map(fun x->x.SiteName)
    let newOutputDataWithCategoryListUpdated = {newOutputData with CategoryList=newCategoryList; SiteList=newSiteList}
    let programOutput=JsonConvert.SerializeObject(newOutputDataWithCategoryListUpdated, newSerialSettings)
    let fullOutputFileName = if opts.OutputFile.parameterValue.FileInfoOption.IsSome then opts.OutputFile.parameterValue.FileInfoOption.Value.FullName else opts.OutputFile.parameterValue.FileName
    System.IO.File.WriteAllText(fullOutputFileName,programOutput)

    //let newInputData = {inputData with LastRunTime=System.DateTime.Now}
    //let programInput=JsonConvert.SerializeObject(newInputData,newSerialSettings)
    //let fullInputFileName = if opts.InputFile.parameterValue.FileInfoOption.IsSome then opts.InputFile.parameterValue.FileInfoOption.Value.FullName else opts.InputFile.parameterValue.FileName
    //System.IO.File.WriteAllText(fullInputFileName,programInput)
    updateProcessedSite opts newOutputDataWithCategoryListUpdated


    let filteredOutputDataLinks = newOutputDataWithCategoryListUpdated.Links |> Array.filter(fun x->x.RipTime>System.DateTime.Now.AddHours((-12.0) * (float)opts.HoursRecent.parameterValue))
    let preFilteredOutput = addStatsToLinks newOutputDataWithCategoryListUpdated.Links filteredOutputDataLinks
    let finalFilteredOutputData={newOutputDataWithCategoryListUpdated with Links=preFilteredOutput}
    let filteredOutputDataString = JsonConvert.SerializeObject(finalFilteredOutputData, newSerialSettings)
    let fullFilteredOutputFileName = if opts.FilteredOutputFile.parameterValue.FileInfoOption.IsSome then opts.FilteredOutputFile.parameterValue.FileInfoOption.Value.FullName else opts.FilteredOutputFile.parameterValue.FileName
    System.IO.File.WriteAllText(fullFilteredOutputFileName,filteredOutputDataString)
    saveLinkStats newOutputDataWithCategoryListUpdated.Links finalFilteredOutputData.Links
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
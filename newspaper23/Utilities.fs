module Utilities
    open SystemTypeExtensions
    open SystemUtilities
    open CommandLineHelper
    open Newspaper23Types
    open Lenses
    open Newtonsoft.Json
    open System.IO


    type System.Net.WebRequest with
      member req.AsyncGetResponseWithTimeout () =
        let impl = async {
          let iar = req.BeginGetResponse (null, null)
          let! success = Async.AwaitIAsyncResult (iar, req.Timeout)
          return if success then req.EndGetResponse iar
                 else req.Abort ()
                      raise (System.Net.WebException "The operation has timed out") }
        Async.TryCancelled (impl, fun _ -> req.Abort ())

    let wordCount (sentence:string) =
        let textNoPunctuation=System.Text.RegularExpressions.Regex.Replace(sentence, @"\p{P}", "")
        let words=textNoPunctuation.Split([|" "|], System.StringSplitOptions.None)
        let wordsSortedByLength= words |> Array.sortBy(fun x->x.Trim().Length) |> Array.rev
        (wordsSortedByLength |> Array.filter(fun x->x.Length>0)).Length

    /// takes a sentence and returns top N words, with a cutoff in wordLength
    let longestNWords (sentence:string) (numberOfWordsToReturn:int) (minimumWordLength:int) =
        let textNoPunctuation=System.Text.RegularExpressions.Regex.Replace(sentence, @"\p{P}", "")
        let words=textNoPunctuation.Split([|" "|], System.StringSplitOptions.None)
        let wordsSortedByLength= words |> Array.sortBy(fun x->x.Trim().Length) |> Array.rev
        let wordsFilteredByMinimumLength = wordsSortedByLength |> Array.filter(fun x->x.Length<=minimumWordLength)
        if wordsFilteredByMinimumLength.Length > numberOfWordsToReturn
            then wordsFilteredByMinimumLength |> Array.take numberOfWordsToReturn
            else wordsFilteredByMinimumLength

    // nice find from http://www.fssnip.net/n5/title/Create-a-histogram-of-a-sequence-using-map
    let histogram =
        Seq.fold (fun acc key ->
            if Map.containsKey key acc
            then Map.add key (acc.[key] + 1) acc
            else Map.add key 1 acc
        ) Map.empty
        >> Seq.sortBy (fun kvp -> -kvp.Value)

    let makeLinkTitleHistogram (links:SiteVisitedLinkRecord []) =
        let linkTitleWords = 
            links
            |> Array.map(fun x->x.LinkText.Split([|" "|], System.StringSplitOptions.None))
            |> Array.concat
        let linkTitleWordsHistogram = 
            let initialHistogram=(histogram linkTitleWords) 
            let totalWordCount = initialHistogram |> Seq.sumBy(fun x->x.Value)
            initialHistogram
            |> Seq.map(fun x->x.Key,((float)x.Value/(float)totalWordCount))
            |> dict
        linkTitleWordsHistogram 

    let scoreTitle (titleText:string) (histogramToScoreAgainst:System.Collections.Generic.IDictionary<string,float>) =
        let topFourWords = longestNWords titleText 4 4
        if topFourWords.Length=0 then 0.0 else
            let topFourWordsWithScore =
                topFourWords
                |> Array.map(fun x->
                    let wordWithHistogramTotals=
                        if histogramToScoreAgainst.ContainsKey(x)
                            then x,histogramToScoreAgainst.Item(x)
                            else x,0.0
                    wordWithHistogramTotals
                    )
            topFourWordsWithScore |> Array.averageBy(fun x->snd x)

    type TitleStats = 
        {
        Title:string
        ScoreAgainstLibrary:float
        ScoreAgainstCurrentList:float
        TitleWordCount:int
        }
    let saveLinkStats (linkLibrary:SiteVisitedLinkRecord []) (currentLinks:SiteVisitedLinkRecord[])=
        let libraryHistogram = makeLinkTitleHistogram linkLibrary
        let currentLinksHistogram = makeLinkTitleHistogram currentLinks
        let linkStats=
            currentLinks
            |> Array.map(fun x->
                let scoreAgainstLibrary = scoreTitle x.LinkText libraryHistogram
                let scoreAgainstCurrentList = scoreTitle x.LinkText currentLinksHistogram
                let newWordCount = (longestNWords x.LinkText 4 4).Length
                {
                    Title=x.LinkText
                    ScoreAgainstLibrary=scoreAgainstLibrary
                    ScoreAgainstCurrentList=scoreAgainstCurrentList
                    TitleWordCount=newWordCount
                }
            ) |> Array.sortBy(fun x->
                x.ScoreAgainstCurrentList + x.ScoreAgainstLibrary
                )
        let linkStatsJson = JsonConvert.SerializeObject linkStats
        System.IO.File.WriteAllText("currentLinkStats.json", linkStatsJson)

    let addStatsToLinks (linkLibrary:SiteVisitedLinkRecord []) (currentLinks:SiteVisitedLinkRecord[]) =
        let libraryHistogram = makeLinkTitleHistogram linkLibrary
        let currentLinksHistogram = makeLinkTitleHistogram currentLinks
        let linkStats=
            currentLinks
            |> Array.map(fun x->
                let scoreAgainstLibrary = scoreTitle x.LinkText libraryHistogram
                let scoreAgainstCurrentList = scoreTitle x.LinkText currentLinksHistogram
                let newWordCount = (longestNWords x.LinkText 4 4).Length
                let newTitleStat=
                    {
                        Title=x.LinkText
                        ScoreAgainstLibrary=scoreAgainstLibrary
                        ScoreAgainstCurrentList=scoreAgainstCurrentList
                        TitleWordCount=newWordCount
                    }
                (x,newTitleStat)
            )
            |> Array.filter(fun (linkRecord,titleStats)->titleStats.TitleWordCount>2 && ((titleStats.ScoreAgainstLibrary + titleStats.ScoreAgainstCurrentList > 0.0) ))
            |> Array.map(fun (linkRecord,titleStats)->linkRecord)
        linkStats
    let makeLocalLinkFileName (linkText:string) =
        let year=System.DateTime.Now.Year.ToString()
        let month=System.DateTime.Now.Month.ToString()
        let day=System.DateTime.Now.Day.ToString()
        let hour=System.DateTime.Now.Hour.ToString()
        let linkTextNoPunctuation=System.Text.RegularExpressions.Regex.Replace(linkText, @"\p{P}", "")
        let titleWords=linkTextNoPunctuation.Split([|" "|], System.StringSplitOptions.None)
        let titleWordsSortedByLength= titleWords |> Array.sortBy(fun x->x.Trim().Length) |> Array.rev
        let longestWordPlusHyphen1=
            if titleWordsSortedByLength.Length>0 
                then titleWordsSortedByLength.[0].Trim() + "-"
                else ""
        let longestWordPlusHyphen2=
            if titleWordsSortedByLength.Length>1 && titleWordsSortedByLength.[1].Length>4
                then titleWordsSortedByLength.[1].Trim() + "-"
                else ""
        let longestWordPlusHyphen3=
            if titleWordsSortedByLength.Length>2 && titleWordsSortedByLength.[2].Length>4
                then titleWordsSortedByLength.[2].Trim() + "-"
                else ""
        let longestWordPlusHyphen4=
            if titleWordsSortedByLength.Length>3 && titleWordsSortedByLength.[3].Length>4
                then titleWordsSortedByLength.[3].Trim() + "-"
                else ""
        let longestWordPlusHyphen = longestWordPlusHyphen1 + longestWordPlusHyphen2 + longestWordPlusHyphen3 + longestWordPlusHyphen4
        let randomFileName = System.IO.Path.GetRandomFileName()
        (year + "-" + month + "-" + day + "-" + hour + "-" + longestWordPlusHyphen + randomFileName + ".html")
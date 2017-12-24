module Utilities
    open SystemTypeExtensions
    open SystemUtilities
    open CommandLineHelper
    open Newspaper23Types
    open Lenses
    open Newtonsoft.Json
    open System.IO

    let makeLocalLinkFileName (linkText:string) =
        let year=System.DateTime.Now.Year.ToString()
        let month=System.DateTime.Now.Month.ToString()
        let day=System.DateTime.Now.Day.ToString()
        let hour=System.DateTime.Now.Hour.ToString()
        let titleWords=linkText.Split([|" "|], System.StringSplitOptions.None)
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
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
        let longestWordPlusHyphen=
            if titleWordsSortedByLength.Length>0 
                then titleWordsSortedByLength.[0].Trim() + "-"
                else ""
        let randomFileName = System.IO.Path.GetRandomFileName()
        (year + "-" + month + "-" + day + "-" + hour + "-" + longestWordPlusHyphen + randomFileName + ".html")
module SystemUtilities
    open SystemTypeExtensions
    open System
    open System.IO
    open System.Net
    open HtmlAgilityPack
    open System.Threading

    let allCardinalNumbers = {1..10000}

    /// Are we running on linux?
    let isLinuxFileSystem =
        let os = Environment.OSVersion
        let platformId = os.Platform
        match platformId with
            | PlatformID.Win32NT | PlatformID.Win32S | PlatformID.Win32Windows | PlatformID.WinCE | PlatformID.Xbox -> false
            | PlatformID.MacOSX | PlatformID.Unix -> true
            | _ ->false
    /// OS-independent file copy from one place to another. Uses shell.
    let copyToDestinationDirectory (localFileName:string) (copyTo:string) =
        if System.IO.File.Exists(localFileName) = false
            then
                ()
            else
                if not isLinuxFileSystem
                    then
                        let systemProc = new System.Diagnostics.Process()
                        systemProc.EnableRaisingEvents<-false
                        systemProc.StartInfo.FileName<-"cmd.exe"
                        systemProc.StartInfo.Arguments<-("/C copy " + localFileName + " " + copyTo)
                        systemProc.Start() |> ignore
                        systemProc.WaitForExit()                
                    else
                        let systemProc = new System.Diagnostics.Process()
                        systemProc.EnableRaisingEvents<-false
                        systemProc.StartInfo.FileName<-"/bin/cp"
                        systemProc.StartInfo.Arguments<-(" " + localFileName + " " + copyTo)
                        //System.Console.WriteLine (systemProc.StartInfo.FileName + systemProc.StartInfo.Arguments)
                        systemProc.Start() |> ignore
                        systemProc.WaitForExit()

                
    let getOrMakeDirectory dirName =
        if System.IO.Directory.Exists dirName
            then System.IO.DirectoryInfo dirName
            else System.IO.Directory.CreateDirectory dirName

    let forceDirectoryCreation (fullDirectoryName:string) =
        if  Directory.Exists(fullDirectoryName)
            then fullDirectoryName
            else Directory.CreateDirectory(fullDirectoryName).FullName


    /// Eliminates duplicate items in a list. Items must be comparable
    let removeDuplicates a =
        a |> List.fold(fun acc x->
            let itemCount = (a |> List.filter(fun y->x=y)).Length
            if itemCount>1
                then
                    if acc |> List.exists(fun y->x=y)
                        then
                            acc
                        else
                            List.append acc [x]
                else
                    acc
            ) []

    let batchesOf n =
        Seq.mapi (fun i v -> i / n, v) >>
        Seq.groupBy fst >>
        Seq.map snd >>
        Seq.map (Seq.map snd)
    let batchesOfNBy f n =
        Seq.mapi (fun i v -> i / (f n), v) >>
        Seq.groupBy fst >>
        Seq.map snd >>
        Seq.map (Seq.map snd)

    let removeDuplicatesBy f a:'a[] =
        a |> List.fold(fun acc x->
            if acc.Length>0
                then
                    if acc |> Array.exists(fun y->(f y)=(f x))
                        then
                            acc
                        else
                            [|x|]|>Array.append acc 
                else
                    [|x|]
            ) [||]
    /// Finds only the duplicates in a list. Items must be comparable
    let duplicates a =
        a |> List.fold(fun acc x->
            let itemCount = (a |> List.filter(fun y->x=y)).Length
            if itemCount>1 then List.append acc [x] else acc
            ) []
    /// finds only the duplicates in a list by applying a function to to each item
    /// new items must be comparable
    let duplicatesBy f a =
        a |> List.fold(fun acc x->
            let itemCount = (a |> List.filter(fun y->f x y)).Length
            if itemCount>1 then List.append acc [x] else acc
            ) []
    let prependToDelimitedList (prependString:string) (currentString:string) (newStringItem:string) =
        let prepend = if currentString.Length=0 || (currentString.GetRight 1) = prependString
                        then ""
                        else prependString.ToString()
        if newStringItem.Length=0 then currentString else
            (currentString + prepend + newStringItem)

    /// Create a dummy file in the OS and return a .NET FileInfo object. Used as a mock for testing
    let getFakeFileInfo() = 
        let tempColl = (new System.CodeDom.Compiler.TempFileCollection(System.AppDomain.CurrentDomain.BaseDirectory, false))
        tempColl.AddExtension("bsx") |> ignore
        let rndPrefix = System.IO.Path.GetRandomFileName()
        let tempFileName = System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, (rndPrefix + "_tester.bsx"))
        tempColl.AddFile(tempFileName,false)
        let fs1=System.IO.File.OpenWrite(tempFileName)
        let sw1=new System.IO.StreamWriter(fs1)
        sw1.WriteLine("test")
        sw1.Close()
        fs1.Close()
        let ret=new System.IO.FileInfo(tempFileName)
        tempColl.Delete()
        ret
    // memoize one to reuse
    let dummyFileInfo = getFakeFileInfo()

    let agentArray = [|
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/63.0.3239.84 Safari/537.36";
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64; ServiceUI 9) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/52.0.2743.116 Safari/537.36 Edge/15.15063";
            "Mozilla/5.0 (Windows; U; Windows NT 6.0; en-US; rv:1.9.1.5) Gecko/20091102 Firefox/3.5.5 (.NET CLR 3.5.30729)";
        |]
    let referralArray = [|
            "https://www.google.com";
            "https://www.bing.com";
            "https://www.duckduckgo.com";
            "https://www.yahoo.com";
            ""
        |]
    let makeWebClient  = 
        let client = new System.Net.WebClient();
        let newUserAgentHeader = agentArray.randomItem
        let newReferrer = referralArray.randomItem
        client.Headers.Add(HttpRequestHeader.UserAgent, newUserAgentHeader)
        client.Headers.Add(HttpRequestHeader.UserAgent, "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/63.0.3239.84 Safari/537.36")
        client.Headers.Add(HttpRequestHeader.Referer, "https://www.google.com")
        client.Headers.Add(HttpRequestHeader.Accept, "text/html,application/xhtml+xml,application/xml")
        client.Headers.Add(HttpRequestHeader.AcceptLanguage, "en-us,en;q=0.5")
        client.Headers.Add(HttpRequestHeader.Upgrade, "1")
        client.Headers.Add("DNT", "1")
        client
    /// Fetch the contents of a web page
    let rec http (url:string) (tryCount:int)  = 
        try
            ServicePointManager.ServerCertificateValidationCallback<-(new Security.RemoteCertificateValidationCallback(fun a b c d->true))
            ServicePointManager.SecurityProtocol<-(SecurityProtocolType.Ssl3 ||| SecurityProtocolType.Tls12 ||| SecurityProtocolType.Tls11 ||| SecurityProtocolType.Tls)
            HtmlAgilityPack.HtmlWeb.PreRequestHandler(fun webRequest->
                webRequest.MaximumAutomaticRedirections<-5
                webRequest.MaximumResponseHeadersLength<-4
                webRequest.Timeout<-15000
                webRequest.Credentials<-CredentialCache.DefaultCredentials
                webRequest.IfModifiedSince<-DateTime.Now.AddDays(-1.0)
                true
                ) |>ignore
            let Client = makeWebClient
            let strm = Client.OpenRead(url)
            let sr = new System.IO.StreamReader(strm)
            let html = sr.ReadToEnd()
            html
        with
            | :? System.Net.WebException as webex ->
                let newTryCount=tryCount+1
                if newTryCount<3
                    then http url newTryCount
                    else ""
            | :? System.IO.IOException as iox->
                ""
            | :? System.Exception as ex ->
                System.Console.WriteLine("Exception in Utils.http trying to load " + url)
                System.Console.WriteLine(ex.Message)
                System.Console.WriteLine(ex.StackTrace)
                if ex.InnerException = null
                    then
                        ""
                    else
                        System.Console.WriteLine("Inner")
                        System.Console.WriteLine(ex.InnerException.Message)
                        ""
    let withTimeout (timeOut: option<int>) (operation: Async<'x>) : Async<option<'x>> =
      match timeOut with
       | None -> async {
           let! result = operation
           return Some result
         }
       | Some timeOut -> async {
           let! child = Async.StartChild (operation, timeOut)
           try
             let! result = child
             return Some result
           with :? System.TimeoutException ->
             return None
         }

    let loadDoc url =
        let tOut=new System.Timers.Timer((float)60000)
        let doc =
            try
                tOut.Elapsed.Add(fun x->
                    tOut.Stop()
                    tOut.Enabled<-false
                    tOut.Dispose()                    
                    raise (new System.TimeoutException()))
                tOut.Start()
                let web=new HtmlWeb()
                web.BrowserTimeout<-TimeSpan(0,0,25)
                web.BrowserDelay<-TimeSpan(0,0,25)                    
                let ret=web.LoadFromBrowser url
                tOut.Stop()
                tOut.Enabled<-false
                tOut.Dispose()
                ret
            with
            | :? System.TimeoutException as tEx->
                tOut.Stop()
                tOut.Enabled<-false
                tOut.Dispose()
                new HtmlDocument()
            | :? System.Exception as ex ->
                tOut.Stop()
                tOut.Enabled<-false
                tOut.Dispose()
                new HtmlDocument()        
        if doc.ParsedText<>""
            then doc
            else
                let doc2 = new HtmlDocument()
                let htmlResponse = http url 1
                doc2.LoadHtml htmlResponse
                doc2            
    let findTextLinksOnAPage (url:string) (customXPathForLinks:string) =
        let xPathForLinks =
            if customXPathForLinks.Length=0 
                then "//a[text()][not(img) and not(@href='#')]"
                else customXPathForLinks
        try
            let doc = loadDoc url
            let docUri = new Uri(url)
            if (doc.ToString()="") 
                then [||]
                else
                    let nodeResults=doc.DocumentNode.SelectNodes(xPathForLinks)
                    let nodesWithHrefs = 
                        nodeResults
                        |>Seq.filter(fun htmlnode->
                            htmlnode.Attributes.Contains("href")
                            && htmlnode.Attributes.["href"].Value.Trim()<>""
                            )
                    let nodesWithFixedHrefs=
                        nodesWithHrefs
                        |> Seq.map(fun x->(
                                            let originalLinkText=x.InnerText
                                            let originalHrefAttribute=x.Attributes.["href"].Value
                                            let urlLink = new Uri(originalHrefAttribute, UriKind.RelativeOrAbsolute)
                                            let fixedUrl = if urlLink.IsAbsoluteUri=false then (new Uri(docUri,urlLink)) else urlLink
                                            let trimmedLinkText=x.InnerText.Trim()
                                            let newlineCount=System.Text.RegularExpressions.Regex.Matches(trimmedLinkText,"\n").Count
                                            let newlineHappyPosterFix =
                                                if newlineCount <1 
                                                    then trimmedLinkText
                                                    else
                                                        let splitOnNewlines=trimmedLinkText.Split([|"\n"|], System.StringSplitOptions.None)
                                                        let filterOutAnyHunkThatLooksLikeAnHtmlNode=splitOnNewlines|>Array.filter(fun x->(x.Contains("&lt;") && x.Contains("&gt;"))=false)
                                                        let biggestHunk=(filterOutAnyHunkThatLooksLikeAnHtmlNode|>Array.sortBy(fun x->x.Trim().Length)|>Array.rev).[0]
                                                        biggestHunk.Trim()
                                            (newlineHappyPosterFix,fixedUrl.ToString())
                            ))
                    nodesWithFixedHrefs|>Seq.toArray
        with
            | :? System.Exception as ex ->[||]
    let downloadFile (url:System.Uri) (fileName:string) = 
        let Client = makeWebClient 
        Client.DownloadFile(url, fileName)
        ()


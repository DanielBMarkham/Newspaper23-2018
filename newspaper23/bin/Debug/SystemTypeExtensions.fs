module SystemTypeExtensions
    open System.Text.RegularExpressions
    open System.Collections
    open System.Collections.Generic
    open System

    /// for option types, gimme either the value or a default I'll provide
    let inline (|?) (a: 'a option) b = if a.IsSome then a.Value else b
    /// for .NET nullable types, gimme the value or the default I'll provide
    let inline (|??) (a: 'a Nullable) b = if a.HasValue then a.Value else b


    type 'a ``[]`` with         
        member x.randomItem = 
            let rnd = new System.Random()
            let idx = rnd.Next(x.Length)
            x.[idx]
    type System.Random with
        /// Generates an infinite sequence of random numbers within the given range.
        member this.GetValues(minValue, maxValue) =
            Seq.initInfinite (fun _ -> this.Next(minValue, maxValue))

    type System.String with
        member x.ContainsAny (possibleMatches:string[]) =
            let ret = possibleMatches |> Array.tryFind(fun y->
                x.Contains(y)
                )
            ret.IsSome
        member x.ContainsAnyRegex(possibleRegexMatches:string[]) =
            let ret = possibleRegexMatches |> Array.tryFind(fun y->
                let rg = new System.Text.RegularExpressions.Regex(y)
                rg.IsMatch(x)
                )
            ret.IsSome
        member x.ContainsRegex(regexMatch:string) =
            let rg = new System.Text.RegularExpressions.Regex(regexMatch)
            rg.IsMatch(x)
        member x.ReplaceWithRegex (regexMatchString:string) (replacementString:string) = 
            System.Text.RegularExpressions.Regex.Replace(x, regexMatchString, replacementString)
        member x.ReplaceAny (charactersToReplace:char []) (characterToUse:char) =
            let sb = new System.Text.StringBuilder(x)
            let newString = charactersToReplace |> Array.fold(fun (acc:System.Text.StringBuilder) x->
                            acc.Replace(x,characterToUse)
                            ) sb
            newString.ToString()
        member x.CountOccurences (token:string) = 
            let mts = x.Split([|token|], System.StringSplitOptions.None)
            if mts = null then 0 else mts.Length
        member x.CountOccurencesRegex (regexMatchString:string) =
            let mts = System.Text.RegularExpressions.Regex.Matches(x, regexMatchString)
            if mts = null then 0 else mts.Count
        member this.GetRight (iLen:int) =
            try
                this.Substring(this.Length - iLen, iLen)
            with |_ -> ""
        member this.GetLeft (iLen:int) =
            try
                this.Substring(0, iLen)
            with |_ -> ""
        member this.TrimLeft (iCount:int) =
            this.Substring(iCount, this.Length - iCount)
        member this.TrimRight (iCount:int) =
            this.Substring(0, this.Length - iCount)    
        member this.TrimBoth (iLeft:int) (iRight:int) =
            if iLeft + iRight > this.Length
                then
                    ""
                else
                    (this.TrimLeft iLeft) |> (fun x-> x.TrimRight iRight)
        member this.TrimTo (desiredLength:int) =
            if this.Length <= desiredLength
                then
                    this
                else
                    this.GetLeft desiredLength
        /// adds the number of spaces to the beginning of the string
        member this.AddSpaces (numSpaces) =
            let prefix = new System.String(' ', numSpaces)
            prefix+this
        /// Centers text using spaces given a certain line length
        member this.PadBoth (len:int) =
            let leftPadCount = len/2 + this.Length/2
            this.PadLeft(leftPadCount).PadRight(len)
        member this.ToSafeFileName() =
            let temp=this.ToLower().ToCharArray() |> Array.map(fun x->
                let badChar=System.IO.Path.GetInvalidFileNameChars()|>Seq.exists(fun y->y=x)
                if badChar || x=' ' then '-' else x
            )
             new System.String(temp)
    type System.Text.StringBuilder with
        /// Write a line ending with the current OS newline character
        member x.wl (stringToWrite:string) =
            x.Append(stringToWrite + System.Environment.NewLine) |> ignore
        /// Write a line at a certain tab level ending with the current OS newline character
        member x.wt (level:int) (content:string) =
            let prefix = new System.String(' ', level*2)
            x.Append(prefix+content + System.Environment.NewLine) |> ignore
        /// Centers text across line using spaces on both sides. Default 80-character line can be overridden
        member x.wc (content:string) (?lineLength:int) =
            if lineLength.IsSome
                then
                    x.Append((content.PadBoth lineLength.Value) + System.Environment.NewLine) |> ignore
                else
                    x.Append((content.PadBoth 80) + System.Environment.NewLine) |> ignore
    type System.IO.TextWriter with
        /// Shorter version of WriteLine
        member x.wl (stringToWrite:string) =
            x.WriteLine(stringToWrite)
        /// WriteLine at a certain tab level
        member x.wt (level:int) (content:string) =
            let prefix = new System.String(' ', level*2)
            x.WriteLine(prefix+content)
        /// Centers text across line using spaces on both sides. Default 80-character line can be overridden
        member x.wc (content:string) (?lineLength:int) =
            if lineLength.IsSome
                then
                    x.WriteLine(content.PadBoth lineLength.Value)
                else
                    x.WriteLine(content.PadBoth 80) 

    type System.Collections.Generic.Dictionary<'A, 'B> with
        member x.stringValueOrEmptyForKey n = 
            if x.ContainsKey n then x.Item(n).ToString() else ""
        member x.TryFind n = 
            let x,(y:'B) = x.TryGetValue n
            if x then Some y else None
    //type Microsoft.FSharp.Collections.List<'T when 'T : equality> with
    //    member this.IntersectionWithOtherList (b:List<'T> when 'T : equality) = this |> List.filter (fun x -> not (List.contains x b))
    type System.Text.RegularExpressions.MatchCollection with
        member this.toSeq =
            seq {for i = 0 to this.Count - 1 do yield this.[i]}
        member this.toArray =
            [|for i = 0 to this.Count - 1 do yield this.[i] |]
    type System.Text.RegularExpressions.Match with
        member this.lastGroup =
            this.Groups.[this.Groups.Count-1]
        member this.lastIndex =
            this.lastGroup.Index + this.lastGroup.Length
 /// Homegrown/copied pure functional stack implementation
 /// from https://viralfsharp.com/2012/02/11/implementing-a-stack-in-f/
    [<StructuredFormatDisplay("{StructuredFormatDisplay}")>]
    type Stack<'a> = 
        | StackNode of 'a list
        with
            member private t.StructuredFormatDisplay = 
                if t.length = 0 then "()"
                else
                    let str = t |> Seq.fold (fun st e -> st + e.ToString() + "; ") "("
                    str.Substring(0, str.Length - 2) + ")"
 
            member t.length =
                t |> Seq.length
            member internal t.asList = 
                match t with StackNode(x) -> x
 
            member t.isEmpty = t.length = 0
            static member empty=StackNode(FSharp.Collections.List<'a>.Empty)
            interface IEnumerable<'a> with
                member x.GetEnumerator() = (x.asList |> List.toSeq).GetEnumerator()
 
            interface IEnumerable with
                member x.GetEnumerator() =  (x.asList |> List.toSeq).GetEnumerator() :> IEnumerator
    let peek = function
        | StackNode([]) -> Unchecked.defaultof<'a>
        | StackNode(hd::tl) -> hd
 
    let pushStack hd tl = 
        match tl with
        |StackNode(x) -> StackNode(hd::x)
    let concatStack firstStack secondStack  =
        match firstStack, secondStack with
        |StackNode(stack1),StackNode(stack2)->
        let ret = (List.concat [stack1;stack2])
        StackNode(ret)
    let pushStackNTimes (stack:Stack<'a>) (itemToAdd:'a) (n:int) =
        let stackToAdd=StackNode(List.init n (fun y->itemToAdd))
        stack |> concatStack stackToAdd
    let pop = function
        | StackNode([]) -> Unchecked.defaultof<'a>, StackNode([])
        | StackNode(hd::tl) -> hd, StackNode(tl)
    let popMany n (stack : Stack<'a>) =
         let noopReturn = [], stack
         if stack.length = 0 then noopReturn
         else
             match n with
             | x when x <= 0 || stack.length < n -> noopReturn
             | x -> 
                 let rec popManyTail n st acc =
                     match n with
                     | 0 -> acc   // exit recursion
                     | _ -> 
                         let hd, tl = List.head st, List.tail st
                         popManyTail (n - 1) tl (hd::fst acc, StackNode(tl)) //keep track of intermediate results
                 popManyTail n stack.asList ([],StackNode(FSharp.Collections.List<'a>.Empty)) // call the actual worker function


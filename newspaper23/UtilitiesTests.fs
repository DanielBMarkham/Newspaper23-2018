module UtilitiesTests
    open SystemTypeExtensions
    open SystemUtilities
    open CommandLineHelper
    open ProgramTypes
    open Lenses
    open Utilities
    open NUnit.Framework
    open FsUnit

    [<Test>]
    let ``FSUnit Works``()=
        1 |> should equal 1

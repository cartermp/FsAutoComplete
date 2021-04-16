module FsAutoComplete.Tests.CodeFixTests

open Expecto
open System.IO
open Helpers
open LanguageServerProtocol.Types
open FsAutoComplete.Utils

let abstractClassGenerationTests state =
  let server =
    async {
      let path = Path.Combine(__SOURCE_DIRECTORY__, "TestCases", "AbstractClassGeneration")
      let! (server, events) = serverInitialize path { defaultConfigDto with AbstractClassStubGeneration = Some true } state
      do! waitForWorkspaceFinishedParsing events
      let path = Path.Combine(path, "Script.fsx")
      let tdop : DidOpenTextDocumentParams = { TextDocument = loadDocument path }
      do! server.TextDocumentDidOpen tdop
      let! diagnostics = waitForParseResultsForFile "Script.fsx" events |> AsyncResult.bimap (fun _ -> failtest "Should have had errors") (fun e -> e)
      return (server, path, diagnostics)
    }
    |> Async.Cache

  let canGenerateForLongIdent = testCaseAsync "can generate a derivative of a long ident - System.IO.Stream" (async {
    let! server, file, diagnostics = server
    let diagnostic = diagnostics |> Array.tryFind (fun d -> d.Code = Some "365" && d.Range.Start.Line = 0 ) |> Option.defaultWith (fun _ -> failtest "Should have gotten an error of type 365")
    let! response = server.TextDocumentCodeAction { CodeActionParams.TextDocument = { Uri = Path.FilePathToUri file }
                                                    Range = diagnostic.Range
                                                    Context = { Diagnostics = [| diagnostic |] } }
    match response with
    | Ok (Some (TextDocumentCodeActionResult.CodeActions [| { Title = "Generate abstract class members" } |] )) -> ()
    | Ok other -> failtestf $"Should have generated the rest of the base class, but instead generated %A{other}"
    | Error reason -> failtestf $"Should have succeeded, but failed with %A{reason}"
  })

  let canGenerateForIdent = testCaseAsync "can generate a derivative for a simple ident - Stream" (async {
    let! server, file, diagnostics = server
    let diagnostic = diagnostics |> Array.tryFind (fun d -> d.Code = Some "365" && d.Range.Start.Line = 5 ) |> Option.defaultWith (fun _ -> failtest "Should have gotten an error of type 365")
    let! response = server.TextDocumentCodeAction { CodeActionParams.TextDocument = { Uri = Path.FilePathToUri file }
                                                    Range = diagnostic.Range
                                                    Context = { Diagnostics = [| diagnostic |] } }
    match response with
    | Ok (Some (TextDocumentCodeActionResult.CodeActions [| { Title = "Generate abstract class members" } |] )) -> ()
    | Ok other -> failtestf $"Should have generated the rest of the base class, but instead generated %A{other}"
    | Error reason -> failtestf $"Should have succeeded, but failed with %A{reason}"
  })


  testList "abstract class generation" [
    canGenerateForLongIdent
    canGenerateForIdent
  ]

let tests state = testList "codefix tests" [
  abstractClassGenerationTests state
]

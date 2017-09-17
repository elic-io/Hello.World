// include Fake libs
#I "packages/FAKE/tools/"
#r "packages/FAKE/tools/FakeLib.dll"
#r "packages/FAKE/tools/Fake.Deploy.Lib.dll"


open Fake
open Fake.AssemblyInfoFile
open Fake.GitVersionHelper
open System.IO
open System

// Directories
let getPath localPath = 
    (Directory.GetCurrentDirectory(),localPath)
    |> Path.Combine
    |> Path.GetFullPath

let rootDir = getPath "."
let buildDir  = getPath "./build/"
let deployDir = getPath "./deploy/"
let nugetDir = getPath "./nuget"
let sourcePath = getPath "./src"

let gitversion = "gitversion"


// Filesets
let appReferences  =
    !! "/**/*.csproj"
    ++ "/**/*.fsproj"

// version info
let version = "0.1"  // or retrieve from CI server

// Targets
Target "Clean" (fun _ ->
    DotNetCli.RunCommand id "clean"
    CleanDirs [deployDir]
)

Target "AssemblyInfo" (fun _ ->
    let verInfo = 
        let gitVersion = 
            let gvString = Environment.GetEnvironmentVariable("GitVersion")
            match gvString with
            | "" -> "GitVersion.exe"
            | _ -> gvString
        GitVersion (fun p -> { p with ToolPath = gitVersion })
    [ Attribute.Version verInfo.AssemblySemVer
      Attribute.FileVersion verInfo.AssemblySemVer
      Attribute.Title "Gotcha.Core"
      Attribute.Description "Core Gotcha Game Engine" ]
    |> CreateFSharpAssemblyInfo ("src/" </> "AssemblyInfo.fs")
)

Target "Build" (fun _ ->
    DotNetCli.Restore id
    DotNetCli.Build
        (fun p -> 
           { p with
                Configuration = "Release" })
)

Target "Pack" (fun _ ->
    Paket.Pack (fun p ->
        {p with
            OutputPath = nugetDir
            WorkingDir = sourcePath
            // Version = gitVer.NuGetVersionV2
         })
)

Target "Deploy" (fun _ ->
    !! (buildDir + "/**/*.*")
    -- "*.zip"
    |> Zip buildDir (deployDir + "ApplicationName." + version + ".zip")
)

// Build order
"Clean"
    ==> "AssemblyInfo"
    ==> "Build"
    ==> "Pack"

// start build
RunTargetOrDefault "Pack"

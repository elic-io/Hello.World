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

let verInfo = 
    let gitVersion = 
        let gvString = Environment.GetEnvironmentVariable("GitVersion")
        match gvString with
        | "" -> "GitVersion.exe"
        | _ -> gvString
    GitVersion (fun p -> { p with ToolPath = gitVersion })

Target "AssemblyInfo" (fun _ ->
    [ Attribute.Version verInfo.AssemblySemVer
      Attribute.FileVersion verInfo.AssemblySemVer
      Attribute.Title "Hello.World"
      Attribute.Description "Silly Test Package" ]
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
    // workaround to avoid these issues with Paket Pack on DotNetCli:
    // https://github.com/fsprojects/Paket/issues/2330
    // https://github.com/fsprojects/Paket/issues/2248
    DotNetCli.Pack (fun p ->
        { p with
            OutputPath = nugetDir
            WorkingDir = sourcePath
            //Major = verInfo.Major
            
        })
    // Paket.Pack (fun p ->
    //     {p with
    //         OutputPath = nugetDir
    //         WorkingDir = sourcePath
    //         Version = verInfo.AssemblySemVer
    //         // Version = gitVer.NuGetVersionV2
    //      })
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

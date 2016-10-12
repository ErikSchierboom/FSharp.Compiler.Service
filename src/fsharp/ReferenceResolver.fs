// Copyright (c) Microsoft Corporation.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

namespace Microsoft.FSharp.Compiler

module internal ReferenceResolver = 

    exception ResolutionFailure

    type ResolutionEnvironment = 
        /// Indicates a script or source being compiled
        | CompileTimeLike 
        /// Indicates a script or source being interpreted
        | RuntimeLike 
        /// Indicates a script or source being edited
        | DesignTimeLike

    type ResolvedFile = 
        { /// Item specification.
          itemSpec:string
          /// Prepare textual information about where the assembly was resolved from, used for tooltip output
          prepareToolTip: string * string -> string
          /// Round-tripped baggage 
          baggage:string
          }

        override this.ToString() = sprintf "ResolvedFile(%s)" this.itemSpec

    type Resolver =
       /// Get the "v4.5.1"-style moniker for the highest installed .NET Framework version.
       /// This is the value passed back to Resolve if no explicit "mscorlib" has been given.
       ///
       /// Note: If an explicit "mscorlib" is given, then --noframework is being used, and the whole ReferenceResolver logic is essentially
       /// unused.  However in the future an option may be added to allow an expicit specification of
       /// a .NET Framework version to use for scripts.
       abstract HighestInstalledNetFrameworkVersion : unit -> string
    
       /// Get the Reference Assemblies directory for the .NET Framework (on Windows)
       /// This is added to the default resolution path for 
       /// design-time compilations.
       abstract DotNetFrameworkReferenceAssembliesRootDirectory : string

       /// Perform assembly resolution on the given references under the given conditions
       abstract Resolve :
           resolutionEnvironment: ResolutionEnvironment *
           // The actual reference paths or assemby name text, plus baggage
           references:(string (* baggage *) * string)[] *  
           // e.g. v4.5.1
           targetFrameworkVersion:string *
           targetFrameworkDirectories:string list *
           targetProcessorArchitecture:string *
           outputDirectory: string * 
           fsharpCoreDir:string *
           explicitIncludeDirs:string list *
           implicitIncludeDir:string *
           logmessage:(string->unit) *
           logwarning:(string->string->unit) *
           logerror:(string->string->unit)
             -> ResolvedFile[]

    let GetDefaultResolver() = 
        let msbuild12 = 
            let ass = System.Reflection.Assembly.Load("FSharp.Compiler.Service.MSBuild.v12") |> Option.ofObj
            let ty = ass |> Option.bind (fun ass -> ass.GetType("Microsoft.FSharp.Compiler.MSBuildReferenceResolver") |> Option.ofObj)
            let obj = ty |> Option.bind (fun ty -> ty.InvokeMember("Resolver",System.Reflection.BindingFlags.Static ||| System.Reflection.BindingFlags.Public ||| System.Reflection.BindingFlags.InvokeMethod ||| System.Reflection.BindingFlags.NonPublic, null, null, null) |> Option.ofObj)
            let resolver = obj |> Option.bind (fun obj -> match obj with :? Resolver as r -> Some r | _ -> None)
            resolver
        match msbuild12 with 
        | None -> failwith "couldn't find FSharp.Compiler.Service.MSBuild.v12 resolver"
        | Some r -> r
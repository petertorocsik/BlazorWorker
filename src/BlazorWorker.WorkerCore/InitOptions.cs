﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace BlazorWorker.Core
{
    /// <summary>
    /// Options for initializing the worker.
    /// </summary>
    public class WorkerInitOptions
    {
        /// <summary>
        /// Creates a new instance of WorkerInitOptions
        /// </summary>
        public WorkerInitOptions()
        {
            DependentAssemblyFilenames = new string[] { };

#if NETSTANDARD21
            DeployPrefix = "_framework/_bin";
            WasmRoot = "_framework/wasm";
#endif
#if NET5_0_OR_GREATER
            DeployPrefix = "_framework";
            WasmRoot = "_framework";
#endif
#if DEBUG
            Debug = true;
#endif
        }

        /// <summary>
        /// Specifies the location of binaries
        /// </summary>
        public string DeployPrefix { get; }

        /// <summary>
        /// Specifieds the location of the wasm binary
        /// </summary>
        public string WasmRoot { get; }

        /// <summary>
        /// Outputs additional debug information to the console
        /// </summary>
        public bool Debug { get; set; }

        /// <summary>
        /// Specifies a list of assembly files names (dlls) that should be loaded when initializing the worker.
        /// </summary>
        public string[] DependentAssemblyFilenames { get; set; }

        /// <summary>
        /// Mono-wasm-annotated endpoint for sending messages to the worker. Experts only.
        /// </summary>
        public string MessageEndPoint { get; set; }

        /// <summary>
        /// Mono-wasm-annotated endpoint for instanciating the worker. Experts only.
        /// </summary>
        public string InitEndPoint { get; set; }

        /// <summary>
        /// Unique blazor identifier for handling callbacks. As referenced by JSInvokableAttribute. Experts only.
        /// </summary>
        public string CallbackMethod { get; set; }

        /// <summary>
        /// If set to true, deducts the name of the assembly containing the service using the service type assembly name + dll extension as file name, and adds it as a dependency.
        /// </summary>
        public bool UseConventionalServiceAssembly { get; set; }

        /// <summary>
        /// Mono-wasm-annotated endpoint for doing callbacks on self invocations from the worker. Experts only.
        /// </summary>
        public string EndInvokeCallBackEndpoint { get; set; }

        /// <summary>
        /// Sets environment variables in the target worker. 
        /// </summary>
        /// <remarks>
        /// Defaults to a single entry: DOTNET_SYSTEM_GLOBALIZATION_INVARIANT = '1'.
        /// For more information see https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-environment-variables
        /// </remarks>
        public Dictionary<string, string> EnvMap { get; set; }
            = new Dictionary<string, string>() {
                { "DOTNET_SYSTEM_GLOBALIZATION_INVARIANT", "1" },
            };

        ///// <summary>
        ///// Specifies custom, known type names to register types to serializer instead of using the KnownTypeAttribute.
        ///// </summary>
        public string[] CustomKnownTypeNames { get; set; }

        public WorkerInitOptions MergeWith(WorkerInitOptions initOptions)
        {
            var newEnvMap = new Dictionary<string, string>(this.EnvMap);
            if (initOptions.EnvMap != null)
            {
                foreach (var entry in initOptions.EnvMap)
                {
                    newEnvMap[entry.Key] = entry.Value;
                }
            }
            return new WorkerInitOptions
            {
                CallbackMethod = initOptions.CallbackMethod ?? this.CallbackMethod,
                DependentAssemblyFilenames = this.DependentAssemblyFilenames
                    .Concat(initOptions.DependentAssemblyFilenames)
                    .Distinct()
                    .ToArray(),
                UseConventionalServiceAssembly = initOptions.UseConventionalServiceAssembly,
                MessageEndPoint = initOptions.MessageEndPoint ?? this.MessageEndPoint,
                InitEndPoint = initOptions.InitEndPoint ?? this.InitEndPoint,
                EndInvokeCallBackEndpoint = initOptions.EndInvokeCallBackEndpoint ?? this.EndInvokeCallBackEndpoint,
                EnvMap = newEnvMap,
                CustomKnownTypeNames = initOptions.CustomKnownTypeNames
            };
        }
    }

    /// <summary>
    /// Contains convenience extensions for <see cref="WorkerInitOptions"/>
    /// </summary>
    public static class WorkerInitOptionsExtensions
    {

        /// <summary>
        /// Adds the specified assembly filesnames as dependencies
        /// </summary>
        /// <param name="source"></param>
        /// <param name="dependentAssemblyFilenames"></param>
        /// <returns></returns>
        public static WorkerInitOptions AddAssemblies(this WorkerInitOptions source, params string[] dependentAssemblyFilenames)
        {
            source.DependentAssemblyFilenames =
                source.DependentAssemblyFilenames.Concat(dependentAssemblyFilenames).ToArray();
            return source;
        }

        /// <summary>
        /// Deducts the name of the assembly containing the service using the the service type assembly name with dll extension as file name, and adds it as a dependency.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static WorkerInitOptions AddConventionalAssemblyOfService(this WorkerInitOptions source)
        {
            source.UseConventionalServiceAssembly = true;
            return source;
        }

        /// <summary>
        /// Deducts the name of the assembly containing the specified <typeparamref name="T"/> using the assembly name with dll extension as file name, and adds it as a dependency.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static WorkerInitOptions AddAssemblyOf<T>(this WorkerInitOptions source)
        {
            return source.AddAssemblyOfType(typeof(T));
        }

        /// <summary>
        /// Deducts the name of the assembly containing the specified <paramref name="type"/> using the assembly name with dll extension as file name, and adds it as a dependency.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static WorkerInitOptions AddAssemblyOfType(this WorkerInitOptions source, Type type)
        {
            source.AddAssemblies($"{type.Assembly.GetName().Name}.dll");
            return source;
        }

        /// <summary>
        /// Registers the neccessary dependencies for injecting or instanciating <see cref="System.Net.Http.HttpClient"/> in the background service.
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        /// <remarks>When this method has been called, <see cref="System.Net.Http.HttpClient"/> can be used inside the service either by instanciating it or by injection into the constructor.</remarks>
        public static WorkerInitOptions AddHttpClient(this WorkerInitOptions source)
        {
#if NETSTANDARD21
            source.AddAssemblies("System.Net.Http.dll", "System.Net.Http.WebAssemblyHttpHandler.dll");
#endif

#if NET5_0_OR_GREATER
            source.AddAssemblies(
                "System.Net.Http.dll", 
                "System.Security.Cryptography.X509Certificates.dll",
                "System.Net.Primitives.dll",
                "System.Net.Requests.dll",
                "System.Net.Security.dll",
                "System.Net.dll",
                "System.Diagnostics.Tracing.dll");
#endif

            return source;
        }

        /// <summary>
        /// Set the specified <paramref name="environmentVariableName"/> to the specified <paramref name="value"/> when the worker runtime has been initialized
        /// </summary>
        /// <param name="source"></param>
        /// <param name="environmentVariableName"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        /// <remarks>
        /// For more information see https://docs.microsoft.com/en-us/dotnet/core/tools/dotnet-environment-variables
        /// </remarks>
        public static WorkerInitOptions SetEnv(this WorkerInitOptions source, string environmentVariableName, string value)
        {
            source.EnvMap[environmentVariableName] = value;
            return source;
        }

        /// <summary>
        /// Sets the AssemblyQualifiedName values from specified <paramref name="customKnownTypes"/> to <see cref="WorkerInitOptions.CustomKnownTypeNames"/>.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="customKnownTypes"></param>
        /// <returns></returns>
        public static WorkerInitOptions AddCustomKnownTypes(this WorkerInitOptions source, Type[] customKnownTypes)
        {
            source.CustomKnownTypeNames = customKnownTypes.Select(type => type.AssemblyQualifiedName).Distinct().ToArray();
            return source;
        }
    }
}
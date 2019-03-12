﻿// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.Extensions.DependencyModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DotHass.Middleware.Mvc.ApplicationParts
{
    /// <summary>
    /// 从入口查找出所有引用的dll
    /// 然后查找出引用了"DotHass.Middleware"的dll
    /// 只有引用了DotHass.Middleware的dll才会被Resolve
    /// </summary>
    internal class ApplicationAssembliesProvider
    {
        internal static HashSet<string> ReferenceAssemblies { get; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            // The deps file for the Microsoft.AspNetCore.All shared runtime is authored in a way where it does not say
            // it depends on Microsoft.AspNetCore.Mvc even though it does. Explicitly list it so that referencing this runtime causes
            // assembly discovery to work correctly.
            "DotHass.Middleware",
        };

        /// <summary>
        /// Returns an ordered list of application assemblies.
        /// <para>
        /// The order is as follows:
        /// * Entry assembly
        /// * Assemblies specified in the application's deps file ordered by name.
        /// <para>
        /// Each assembly is immediately followed by assemblies specified by annotated <see cref="RelatedAssemblyAttribute"/> ordered by name.
        /// </para>
        /// </para>
        /// </summary>
        public IEnumerable<Assembly> ResolveAssemblies(Assembly entryAssembly)
        {
            var dependencyContext = LoadDependencyContext(entryAssembly);

            IEnumerable<AssemblyItem> assemblyItems;

            if (dependencyContext == null)
            {
                assemblyItems = new[] { GetAssemblyItem(entryAssembly) };
            }
            else
            {
                assemblyItems = ResolveFromDependencyContext(dependencyContext);
            }

            assemblyItems = assemblyItems
                .OrderBy(item => item.Assembly == entryAssembly ? 0 : 1)
                .ThenBy(item => item.Assembly.FullName, StringComparer.Ordinal);

            foreach (var item in assemblyItems)
            {
                yield return item.Assembly;

                foreach (var associatedAssembly in item.RelatedAssemblies.Distinct().OrderBy(assembly => assembly.FullName, StringComparer.Ordinal))
                {
                    yield return associatedAssembly;
                }
            }
        }

        protected virtual DependencyContext LoadDependencyContext(Assembly assembly) => DependencyContext.Load(assembly);

        private List<AssemblyItem> ResolveFromDependencyContext(DependencyContext dependencyContext)
        {
            var assemblyItems = new List<AssemblyItem>();
            var relatedAssemblies = new Dictionary<Assembly, AssemblyItem>();

            var candidateAssemblies = GetCandidateLibraries(dependencyContext)
                .SelectMany(library => GetLibraryAssemblies(dependencyContext, library));

            foreach (var assembly in candidateAssemblies)
            {
                var assemblyItem = GetAssemblyItem(assembly);
                assemblyItems.Add(assemblyItem);

                foreach (var relatedAssembly in assemblyItem.RelatedAssemblies)
                {
                    if (relatedAssemblies.TryGetValue(relatedAssembly, out var otherEntry))
                    {
                        var message = string.Join(
                            Environment.NewLine,
                            Resources.FormatApplicationAssembliesProvider_DuplicateRelatedAssembly(relatedAssembly.FullName),
                            otherEntry.Assembly.FullName,
                            assembly.FullName);

                        throw new InvalidOperationException(message);
                    }

                    relatedAssemblies.Add(relatedAssembly, assemblyItem);
                }
            }

            // Remove any top level assemblies that appear as an associated assembly.
            assemblyItems.RemoveAll(item => relatedAssemblies.ContainsKey(item.Assembly));

            return assemblyItems;
        }

        protected virtual IEnumerable<Assembly> GetLibraryAssemblies(DependencyContext dependencyContext, RuntimeLibrary runtimeLibrary)
        {
            foreach (var assemblyName in runtimeLibrary.GetDefaultAssemblyNames(dependencyContext))
            {
                var assembly = Assembly.Load(assemblyName);
                yield return assembly;
            }
        }

        protected virtual IReadOnlyList<Assembly> GetRelatedAssemblies(Assembly assembly)
        {
            // Do not require related assemblies to be available in the default code path.
            return RelatedAssemblyAttribute.GetRelatedAssemblies(assembly, throwOnError: false);
        }

        private AssemblyItem GetAssemblyItem(Assembly assembly)
        {
            var relatedAssemblies = GetRelatedAssemblies(assembly);

            // Ensure we don't have any cycles. A cycle could be formed if a related assembly points to the primary assembly.
            foreach (var relatedAssembly in relatedAssemblies)
            {
                if (relatedAssembly.IsDefined(typeof(RelatedAssemblyAttribute)))
                {
                    throw new InvalidOperationException(
                        Resources.FormatApplicationAssembliesProvider_RelatedAssemblyCannotDefineAdditional(relatedAssembly.FullName, assembly.FullName));
                }
            }

            return new AssemblyItem(assembly, relatedAssemblies);
        }

        // Returns a list of libraries that references the assemblies in <see cref="ReferenceAssemblies"/>.
        // By default it returns all assemblies that reference any of the primary MVC assemblies
        // while ignoring MVC assemblies.
        // Internal for unit testing
        internal static IEnumerable<RuntimeLibrary> GetCandidateLibraries(DependencyContext dependencyContext)
        {
            var candidatesResolver = new CandidateResolver(dependencyContext.RuntimeLibraries, ReferenceAssemblies);
            return candidatesResolver.GetCandidates();
        }

        private class CandidateResolver
        {
            private readonly IDictionary<string, Dependency> _runtimeDependencies;

            public CandidateResolver(IReadOnlyList<RuntimeLibrary> runtimeDependencies, ISet<string> referenceAssemblies)
            {
                var dependenciesWithNoDuplicates = new Dictionary<string, Dependency>(StringComparer.OrdinalIgnoreCase);
                foreach (var dependency in runtimeDependencies)
                {
                    if (dependenciesWithNoDuplicates.ContainsKey(dependency.Name))
                    {
                        throw new InvalidOperationException(Resources.FormatCandidateResolver_DifferentCasedReference(dependency.Name));
                    }
                    dependenciesWithNoDuplicates.Add(dependency.Name, CreateDependency(dependency, referenceAssemblies));
                }

                _runtimeDependencies = dependenciesWithNoDuplicates;
            }

            private Dependency CreateDependency(RuntimeLibrary library, ISet<string> referenceAssemblies)
            {
                var classification = DependencyClassification.Unknown;
                if (referenceAssemblies.Contains(library.Name))
                {
                    classification = DependencyClassification.MvcReference;
                }

                return new Dependency(library, classification);
            }

            private DependencyClassification ComputeClassification(string dependency)
            {
                if (!_runtimeDependencies.ContainsKey(dependency))
                {
                    // Library does not have runtime dependency. Since we can't infer
                    // anything about it's references, we'll assume it does not have a reference to Mvc.
                    return DependencyClassification.DoesNotReferenceMvc;
                }

                var candidateEntry = _runtimeDependencies[dependency];
                if (candidateEntry.Classification != DependencyClassification.Unknown)
                {
                    return candidateEntry.Classification;
                }
                else
                {
                    var classification = DependencyClassification.DoesNotReferenceMvc;
                    foreach (var candidateDependency in candidateEntry.Library.Dependencies)
                    {
                        var dependencyClassification = ComputeClassification(candidateDependency.Name);
                        if (dependencyClassification == DependencyClassification.ReferencesMvc ||
                            dependencyClassification == DependencyClassification.MvcReference)
                        {
                            classification = DependencyClassification.ReferencesMvc;
                            break;
                        }
                    }

                    candidateEntry.Classification = classification;

                    return classification;
                }
            }

            public IEnumerable<RuntimeLibrary> GetCandidates()
            {
                foreach (var dependency in _runtimeDependencies)
                {
                    if (ComputeClassification(dependency.Key) == DependencyClassification.ReferencesMvc)
                    {
                        yield return dependency.Value.Library;
                    }
                }
            }

            private class Dependency
            {
                public Dependency(RuntimeLibrary library, DependencyClassification classification)
                {
                    Library = library;
                    Classification = classification;
                }

                public RuntimeLibrary Library { get; }

                public DependencyClassification Classification { get; set; }

                public override string ToString()
                {
                    return $"Library: {Library.Name}, Classification: {Classification}";
                }
            }

            private enum DependencyClassification
            {
                Unknown = 0,

                /// <summary>
                /// References (directly or transitively) one of the Mvc packages listed in
                /// <see cref="ReferenceAssemblies"/>.
                /// </summary>
                ReferencesMvc = 1,

                /// <summary>
                /// Does not reference (directly or transitively) one of the Mvc packages listed by
                /// <see cref="ReferenceAssemblies"/>.
                /// </summary>
                DoesNotReferenceMvc = 2,

                /// <summary>
                /// One of the references listed in <see cref="ReferenceAssemblies"/>.
                /// </summary>
                MvcReference = 3,
            }
        }

        private readonly struct AssemblyItem
        {
            public AssemblyItem(Assembly assembly, IReadOnlyList<Assembly> associatedAssemblies)
            {
                Assembly = assembly;
                RelatedAssemblies = associatedAssemblies;
            }

            public Assembly Assembly { get; }

            public IReadOnlyList<Assembly> RelatedAssemblies { get; }
        }
    }
}
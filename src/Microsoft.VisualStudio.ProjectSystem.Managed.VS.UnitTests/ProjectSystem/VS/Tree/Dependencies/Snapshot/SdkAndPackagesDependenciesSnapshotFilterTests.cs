﻿// Copyright (c) Microsoft.  All Rights Reserved.  Licensed under the Apache License, Version 2.0.  See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Snapshot.Filters;
using Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies.Subscriptions;
using Xunit;

namespace Microsoft.VisualStudio.ProjectSystem.VS.Tree.Dependencies
{
    [ProjectSystemTrait]
    public class SdkAndPackagesDependenciesSnapshotFilterTests
    {
        [Fact]
        public void SdkAndPackagesDependenciesSnapshotFilter_WhenNotTopLevelOrResolved_ShouldDoNothing()
        {
            var dependency = IDependencyFactory.Implement(
                id: "mydependency1",
                topLevel: false);

            var worldBuilder = new Dictionary<string, IDependency>()
            {
                { dependency.Object.Id, dependency.Object },
            }.ToImmutableDictionary().ToBuilder();
            
            var filter = new SdkAndPackagesDependenciesSnapshotFilter();

            var resultDependency = filter.BeforeAdd(
                projectPath: null,
                targetFramework: null,
                dependency: dependency.Object,
                worldBuilder: worldBuilder,
                topLevelBuilder: null);

            dependency.VerifyAll();
        }

        [Fact]
        public void SdkAndPackagesDependenciesSnapshotFilter_WhenSdk_ShouldFindMatchingPackageAndSetProperties()
        {
            var dependencyIDs = new List<string> { "id1", "id2" }.ToImmutableList();

            var mockTargetFramework = ITargetFrameworkFactory.Implement(moniker: "tfm");

            var flags = DependencyTreeFlags.SdkSubTreeNodeFlags
                               .Union(DependencyTreeFlags.ResolvedFlags)
                                .Except(DependencyTreeFlags.UnresolvedFlags);
            var sdkDependency = IDependencyFactory.Implement(
                flags: DependencyTreeFlags.SdkSubTreeNodeFlags,
                id: "mydependency1id",
                name: "mydependency1",
                topLevel: true,
                setPropertiesDependencyIDs: dependencyIDs,
                setPropertiesResolved:true,
                setPropertiesFlags: flags);

            var otherDependency = IDependencyFactory.Implement(
                    id: $"tfm\\{PackageRuleHandler.ProviderTypeString}\\mydependency1",
                    resolved: true,
                    dependencyIDs: dependencyIDs);

            var topLevelBuilder = ImmutableHashSet<IDependency>.Empty.Add(sdkDependency.Object).ToBuilder();
            var worldBuilder = new Dictionary<string, IDependency>()
            {
                { sdkDependency.Object.Id, sdkDependency.Object },
                { otherDependency.Object.Id, otherDependency.Object }
            }.ToImmutableDictionary().ToBuilder();

            var filter = new SdkAndPackagesDependenciesSnapshotFilter();

            var resultDependency = filter.BeforeAdd(
                projectPath: null,
                targetFramework: mockTargetFramework,
                dependency: sdkDependency.Object,
                worldBuilder: worldBuilder,
                topLevelBuilder: topLevelBuilder);

            sdkDependency.VerifyAll();
            otherDependency.VerifyAll();
        }

        [Fact]
        public void SdkAndPackagesDependenciesSnapshotFilter_WhenSdkAndPackageUnresolved_ShouldDoNothing()
        {
            var dependencyIDs = new List<string> { "id1", "id2" }.ToImmutableList();

            var mockTargetFramework = ITargetFrameworkFactory.Implement(moniker: "tfm");

            var dependency = IDependencyFactory.Implement(
                flags: DependencyTreeFlags.SdkSubTreeNodeFlags,
                id: "mydependency1id",
                name: "mydependency1",
                topLevel: true);

            var otherDependency = IDependencyFactory.Implement(
                    id: $"tfm\\{PackageRuleHandler.ProviderTypeString}\\mydependency1",
                    resolved: false);

            var worldBuilder = new Dictionary<string, IDependency>()
            {
                { dependency.Object.Id, dependency.Object },
                { otherDependency.Object.Id, otherDependency.Object }
            }.ToImmutableDictionary().ToBuilder();

            var filter = new SdkAndPackagesDependenciesSnapshotFilter();

            var resultDependency = filter.BeforeAdd(
                projectPath: null,
                targetFramework: mockTargetFramework,
                dependency: dependency.Object,
                worldBuilder: worldBuilder,
                topLevelBuilder: null);

            dependency.VerifyAll();
            otherDependency.VerifyAll();
        }

        [Fact]
        public void SdkAndPackagesDependenciesSnapshotFilter_WhenPackage_ShouldFindMatchingSdkAndSetProperties()
        {
            var dependencyIDs = new List<string> { "id1", "id2" }.ToImmutableList();

            var mockTargetFramework = ITargetFrameworkFactory.Implement(moniker: "tfm");

            var dependency = IDependencyFactory.Implement(
                flags: DependencyTreeFlags.PackageNodeFlags,
                id: "mydependency1id",
                name: "mydependency1",
                topLevel: true,
                resolved: true,
                dependencyIDs: dependencyIDs);

            var flags = DependencyTreeFlags.PackageNodeFlags
                                           .Union(DependencyTreeFlags.ResolvedFlags)
                                            .Except(DependencyTreeFlags.UnresolvedFlags);
            var sdkDependency = IDependencyFactory.Implement(
                    id: $"tfm\\{SdkRuleHandler.ProviderTypeString}\\mydependency1",
                    setPropertiesResolved:true,
                    setPropertiesDependencyIDs: dependencyIDs,
                    setPropertiesFlags: flags,
                    equals:true);

            var worldBuilder = new Dictionary<string, IDependency>()
            {
                { dependency.Object.Id, dependency.Object },
                { sdkDependency.Object.Id, sdkDependency.Object }
            }.ToImmutableDictionary().ToBuilder();

            var topLevelBuilder = ImmutableHashSet<IDependency>.Empty.Add(sdkDependency.Object).ToBuilder();
            var filter = new SdkAndPackagesDependenciesSnapshotFilter();

            var resultDependency = filter.BeforeAdd(
                projectPath: null,
                targetFramework: mockTargetFramework,
                dependency: dependency.Object,
                worldBuilder: worldBuilder,
                topLevelBuilder: topLevelBuilder);

            dependency.VerifyAll();
            sdkDependency.VerifyAll();

            Assert.True(topLevelBuilder.First().Id.Equals(sdkDependency.Object.Id));
        }

        [Fact]
        public void SdkAndPackagesDependenciesSnapshotFilter_WhenPackageRemoving_ShouldCleanupSdk()
        {
            var dependencyIDs = ImmutableList<string>.Empty;

            var mockTargetFramework = ITargetFrameworkFactory.Implement(moniker: "tfm");

            var dependency = IDependencyFactory.Implement(
                id: "mydependency1id",
                flags: DependencyTreeFlags.PackageNodeFlags,
                name: "mydependency1",
                topLevel: true,
                resolved: true);

            var flags = DependencyTreeFlags.SdkSubTreeNodeFlags
                                           .Union(DependencyTreeFlags.UnresolvedFlags)
                                           .Except(DependencyTreeFlags.ResolvedFlags);
            var sdkDependency = IDependencyFactory.Implement(
                    id: $"tfm\\{SdkRuleHandler.ProviderTypeString}\\mydependency1",
                    setPropertiesDependencyIDs: dependencyIDs,
                    setPropertiesResolved: false,
                    setPropertiesFlags: flags);

            var worldBuilder = new Dictionary<string, IDependency>()
            {
                { dependency.Object.Id, dependency.Object },
                { sdkDependency.Object.Id, sdkDependency.Object },
            }.ToImmutableDictionary().ToBuilder();
            
            // try to have empty top level hash set - no error should happen when removing sdk and readding 
            var topLevelBuilder = ImmutableHashSet<IDependency>.Empty.ToBuilder();

            var filter = new SdkAndPackagesDependenciesSnapshotFilter();

            filter.BeforeRemove(
                projectPath: null,
                targetFramework: mockTargetFramework,
                dependency: dependency.Object,
                worldBuilder: worldBuilder,
                topLevelBuilder: topLevelBuilder);

            dependency.VerifyAll();
            sdkDependency.VerifyAll();

            Assert.True(topLevelBuilder.First().Id.Equals(sdkDependency.Object.Id));
        }
    }
}

// Copyright © Cysharp, Inc. All rights reserved.
// This source code is licensed under the MIT License. See details at https://github.com/Cysharp/SlnMerge.

using Fody;
using Mono.Cecil;
using Mono.Cecil.Rocks;

namespace SlnMerge.Weavers.Internalize;

public class ModuleWeaver : BaseModuleWeaver
{
    public override void Execute()
    {
        // Internalize all types
        foreach (var typeDefinition in ModuleDefinition.GetAllTypes())
        {
            if (typeDefinition.IsNestedPublic)
            {
                typeDefinition.IsNestedPublic = false;
                typeDefinition.IsNestedAssembly = true;
            }

            if (typeDefinition.IsPublic)
            {
                typeDefinition.IsPublic = false;
            }
        }
    }

    public override IEnumerable<string> GetAssembliesForScanning() => [];
}

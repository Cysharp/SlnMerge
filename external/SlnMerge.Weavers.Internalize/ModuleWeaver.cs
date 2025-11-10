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

        // Add InternalsVisibleTo attributes
        var attrCtor = ModuleDefinition.ImportReference(typeof(System.Runtime.CompilerServices.InternalsVisibleToAttribute).GetConstructor([typeof(string)]));
        foreach (var target in new [] { "SlnMerge", "SlnMerge.Core" })
        {
            var customAttribute = new CustomAttribute(attrCtor);
            customAttribute.ConstructorArguments.Add(new CustomAttributeArgument(ModuleDefinition.TypeSystem.String, target));
            ModuleDefinition.CustomAttributes.Add(customAttribute);
        }
    }

    public override IEnumerable<string> GetAssembliesForScanning() => [];
}

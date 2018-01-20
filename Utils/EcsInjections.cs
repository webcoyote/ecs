// ----------------------------------------------------------------------------
// The MIT License
// Simple Entity Component System framework https://github.com/Leopotam/ecs
// Copyright (c) 2017-2018 Leopotam <leopotam@gmail.com>
// ----------------------------------------------------------------------------

using System;
using System.Reflection;

namespace LeopotamGroup.Ecs.Internals {
    /// <summary>
    /// Processes dependency injection to ecs systems. For internal use only.
    /// </summary>
    static class EcsInjections {
        public static void Inject (EcsWorld world, IEcsSystem system) {
            var worldType = world.GetType ();
            var systemType = system.GetType ();
            var ecsFilter = typeof (EcsFilter);
            var ecsIndex = typeof (int);
            var attrEcsWorld = typeof (EcsWorldAttribute);
            var attrEcsFilterInclude = typeof (EcsFilterIncludeAttribute);
            var attrEcsFilterExclude = typeof (EcsFilterExcludeAttribute);

            foreach (var f in systemType.GetFields (BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)) {
                // [EcsWorld]
                if (f.FieldType.IsAssignableFrom (worldType) && !f.IsStatic && Attribute.IsDefined (f, attrEcsWorld)) {
                    f.SetValue (system, world);
                }

                // [EcsFilterInclude]
                if (f.FieldType == ecsFilter && !f.IsStatic) {
                    EcsComponentMask includeMask = null;
                    var standardFilterIncDefined = Attribute.IsDefined (f, attrEcsFilterInclude);
                    if (standardFilterIncDefined) {
                        includeMask = new EcsComponentMask ();
                        var components = ((EcsFilterIncludeAttribute) Attribute.GetCustomAttribute (f, attrEcsFilterInclude)).Components;
                        for (var i = 0; i < components.Length; i++) {
                            var genType = typeof (EcsComponentPool<>).MakeGenericType (components[i]);
                            var poolInstance = genType.GetField ("Instance").GetValue (null) as IEcsComponentPool;
                            var bit = world.GetComponentPoolIndex (poolInstance);
                            includeMask.SetBit (bit, true);
                        }
                    }
                    EcsComponentMask excludeMask = null;
                    var standardFilterExcDefined = Attribute.IsDefined (f, attrEcsFilterExclude);
                    if (standardFilterExcDefined) {
                        excludeMask = new EcsComponentMask ();
                        var components = ((EcsFilterExcludeAttribute) Attribute.GetCustomAttribute (f, attrEcsFilterExclude)).Components;
                        for (var i = 0; i < components.Length; i++) {
                            var genType = typeof (EcsComponentPool<>).MakeGenericType (components[i]);
                            var poolInstance = genType.GetField ("Instance").GetValue (null) as IEcsComponentPool;
                            var bit = world.GetComponentPoolIndex (poolInstance);
                            excludeMask.SetBit (bit, true);
                        }
                    }
#if DEBUG && !ECS_PERF_TEST
                    if (standardFilterIncDefined && includeMask.IsEmpty ()) {
                        throw new Exception ("Include filter cant be empty at system: " + systemType.Name);
                    }
                    if (standardFilterExcDefined && excludeMask.IsEmpty ()) {
                        throw new Exception ("Exclude filter cant be empty at system: " + systemType.Name);
                    }
                    if (!standardFilterIncDefined && standardFilterExcDefined) {
                        throw new Exception ("EcsFilterExclude can be applied only as pair to EcsFilterInclude at system: " + systemType.Name);
                    }
                    if (includeMask != null && excludeMask != null && includeMask.IsIntersects (excludeMask)) {
                        throw new Exception ("Exclude and include filters are intersected at system: " + systemType.Name);
                    }
#endif
                    if (standardFilterIncDefined) {
                        f.SetValue (system, world.GetFilter (includeMask, excludeMask ?? new EcsComponentMask ()));
                    }
                }
            }
        }
    }
}
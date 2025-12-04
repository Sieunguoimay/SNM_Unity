using System;
using System.Collections.Generic;
using Snm.Runtime.Dispose;
using UnityEngine;

namespace Snm.Runtime.DIFrameworkV3
{
    public class EntitiesInstaller
    {
        public void Install(EntityAsset[] entityAssets, out IDisposable disposable)
        {
            var createdEntities = new Dictionary<EntityAsset, object>();

            foreach (var asset in entityAssets)
            {
                var entity = asset.CreateEntity(ResolveDependency);
                // Store or use the entity as needed
                createdEntities.Add(asset, entity);
            }

            object ResolveDependency(EntityAsset asset)
            {
                return createdEntities.TryGetValue(asset, out var entity) ? entity : null;
            }

            disposable = new DisposeCallback(() =>
            {
            });
        }
    }

    public class EntityAssets : ScriptableObject
    {
        public EntityAsset[] entityAssets;
    }

    public abstract class EntityAsset : ScriptableObject
    {
        public abstract object CreateEntity(Func<EntityAsset, object> resolveDependency);
    }
}
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

public class GameFO : ISetupTearDown
{
    public virtual IEnumerable<ISetupTearDown> GetChildren()
    {
        foreach (var c in GetChildrenByReflection(this))
        {
            yield return c;
        }
    }

    public virtual void Setup()
    {
        foreach (var child in GetChildren())
        {
            child?.Setup();
        }
    }

    public virtual void TearDown()
    {
        foreach (var child in GetChildren())
        {
            child?.TearDown();
        }
    }

    public static IEnumerable<ISetupTearDown> GetChildrenByReflection(object target)
    {
        var flags = BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public;
        var setupTearDownType = typeof(ISetupTearDown);
        var current = target.GetType();
        while (current != null)
        {
            foreach (var f in current.GetFields(flags).Where(f => setupTearDownType.IsAssignableFrom(f.FieldType)))
            {
                var att = f.GetCustomAttribute<SetupTearDownAttribute>();

                if (att == null)
                {
                    //if (f.GetValue(target) != null)
                    //{
                    //    Debug.LogError($"Member variable {f.Name} is not marked as SetupTearDown");
                    //}
                    continue;
                }

                if (att.Ignore)
                {
                    continue;
                }

                var value = f.GetValue(target);
                if (value != null)
                {
                    yield return (ISetupTearDown)value;
                }
            }
            current = current.BaseType;
        }
    }
}

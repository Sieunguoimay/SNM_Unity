using Supports.ViewHierachy;
using UnityEngine;

public class ViewNode_SimpleRoot : ViewNode
{
    [ObjectSelector]
    [SerializeField] private Object source;

    private void OnEnable()
    {
        this.ExecuteInNextFrame(() =>
        {
            Setup(source);
        });
    }

    private void OnDisable()
    {
        TearDown();
    }
}
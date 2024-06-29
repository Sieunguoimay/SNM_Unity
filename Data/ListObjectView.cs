using Supports.ViewHierachy;
using System;
using System.Collections.Generic;

public class ListObjectView : ViewNode<IListObject>
{
    public IEnumerable<object> List => Data.DynamicList;

    public event Action<IListObject> ListChangedEvent;

    public override void Setup(object data)
    {
        base.Setup(data);
        Data.ListChangedEvent -= OnListChanged;
        Data.ListChangedEvent += OnListChanged;
    }

    public override void TearDown()
    {
        Data.ListChangedEvent -= OnListChanged;
        base.TearDown();
    }

    private void OnListChanged(IListObject obj)
    {
        ListChangedEvent?.Invoke(obj);
    }
}

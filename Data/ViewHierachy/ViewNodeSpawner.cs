using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Supports.ViewHierachy
{
    public class ViewNodeSpawner : ViewNode
    {
        [SerializeField] private ViewNode prefab;
        [SerializeField] private Transform container;

        private readonly ListObject<ViewNode> spawnedViewNodeList = new();

        public ListObject<ViewNode> SpawnedList => spawnedViewNodeList;
        public Transform Container => container;

        public event Action<ViewNodeSpawner, ViewNode> SpawnedEvent;

        public override void Setup(object data)
        {
            base.Setup(data);
            SpawnWithDataList(data as IEnumerable<object>);
        }

        public override void TearDown()
        {
            foreach (var viewNode in spawnedViewNodeList.List)
            {
                viewNode.TearDown();
                Destroy(viewNode.gameObject);
            }

            spawnedViewNodeList.Clear();

            base.TearDown();
        }

        public void SpawnWithDataList(IEnumerable<object> dataList)
        {
            var tempList = new List<ViewNode>();

            foreach (var data in dataList)
            {
                if (spawnedViewNodeList.List.All(v => v.DynamicData != data))
                {
                    var viewNode = Instantiate(prefab, container ?? transform);

                    SpawnedEvent?.Invoke(this, viewNode);

                    viewNode.Setup(data);

                    tempList.Add(viewNode);
                }
            }

            foreach (var v in spawnedViewNodeList.List)
            {
                if (dataList.Contains(v.DynamicData))
                {
                    tempList.Add(v);
                }
                else
                {
                    v.TearDown();
                    Destroy(v.gameObject);
                }
            }

            spawnedViewNodeList.Clear();

            foreach (var viewNode in tempList)
            {
                spawnedViewNodeList.AddObject(viewNode);
            }
            //_spawnedViewNodes.AddRange(tempList);
        }
    }
}
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Entities;

public class DecisionTreeNodeConvert : MonoBehaviour, IConvertGameObjectToEntity
{
    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        // Add componnet
        var node = DecisionTreeNode.State.Default;
        if (transform.parent != null)
        {
            var parentNode = transform.parent.GetComponent<DecisionTreeNodeConvert>();
            if (parentNode != null)
                node.parent =  conversionSystem.GetPrimaryEntity(parentNode);
        }
        dstManager.AddComponentData(entity, node);

        // Generate subtree info
        var subtreeBuffer = dstManager.AddBuffer<DecisionTreeNode.SubtreeElement>(entity);
        AddSubtree(ref subtreeBuffer, conversionSystem);
    }

    int AddSubtree(ref DynamicBuffer<DecisionTreeNode.SubtreeElement> buffer, GameObjectConversionSystem conversionSystem)
    {
        int index = buffer.Length;
        var element = new DecisionTreeNode.SubtreeElement { root = conversionSystem.GetPrimaryEntity(this), count = 1 };
        buffer.Add(element);

        for (int i = 0; i < transform.childCount; i++)
        {
            var child = transform.GetChild(i).gameObject;
            if (child.GetComponent<DecisionTreeNodeConvert>() != null)
                element.count += child.GetComponent<DecisionTreeNodeConvert>().AddSubtree(ref buffer, conversionSystem);
        }

        buffer[index] = element;
        return element.count;
    }
}

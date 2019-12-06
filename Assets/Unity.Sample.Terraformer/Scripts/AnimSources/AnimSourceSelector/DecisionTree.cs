using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Sample.Core;
using UnityEngine;

public class DecisionTree
{
    public static void SetOwner(EntityManager entityManager, Entity nodeEntity, Entity owner)
    {
        var descendents = entityManager.GetBuffer<DecisionTreeNode.SubtreeElement>(nodeEntity);
        for (int i = 0; i < descendents.Length; i++)
        {
            var descendent = descendents[i].root;
            var nodeData = entityManager.GetComponentData<DecisionTreeNode.State>(descendent);
            nodeData.owner = owner;
            entityManager.SetComponentData(descendent, nodeData);
        }
    }

    public static Entity FindValidDecisionNode(
        BufferFromEntity<DecisionTreeNode.SubtreeElement> decisionTreeNodeSubtreeElementBufferFromEntity,
        ComponentDataFromEntity<DecisionTreeNode.State> decisionTreeNodeStateFromEntity,
        Entity nodeEntity)
    {
        var descendents = decisionTreeNodeSubtreeElementBufferFromEntity[nodeEntity];
        var result = Entity.Null;
        int i = 0;
        int end = descendents.Length;
        while (i < end)
        {
            var subtree = descendents[i];
            if (decisionTreeNodeStateFromEntity[subtree.root].isTrue)
            {
                result = subtree.root;
                end = i + subtree.count;
                i++;
            }
            else
            {
                i += subtree.count;
            }
        }

        return result;
    }
}

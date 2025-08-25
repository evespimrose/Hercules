using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public abstract class BTNode
{
    public enum State { None, Success, Failure, Running }
    public virtual State Tick()
    {
        return State.None;
    }
}

[System.Serializable]
public class Sequence : BTNode
{
    private List<BTNode> children;
    public Sequence(List<BTNode> nodes) => children = nodes;

    public override State Tick()
    {
        foreach (var node in children)
        {
            var result = node.Tick();
            if (result != State.Success) return result;
        }
        return State.Success;
    }
}

[System.Serializable]
public class Selector : BTNode
{
    private List<BTNode> children;
    public Selector(List<BTNode> nodes) => children = nodes;

    public override State Tick()
    {
        foreach (var node in children)
        {
            var result = node.Tick();
            if (result != State.Failure) return result;
        }
        return State.Failure;
    }
}

[System.Serializable]
public class Parallel : BTNode
{
    private List<BTNode> children;
    public enum Policy { All, Any } // 정책 옵션 추가
    public Policy policy = Policy.All; // 기본값은 All
    
    public Parallel(List<BTNode> nodes, Policy policy = Policy.All)
    {
        children = nodes;
        this.policy = policy;
    }
    
    public Parallel(List<BTNode> nodes) : this(nodes, Policy.All) { }

    public override State Tick()
    {
        if (children == null || children.Count == 0)
            return State.Success;
            
        int successCount = 0;
        int failureCount = 0;
        int runningCount = 0;
        
        // 모든 자식 노드 실행 및 결과 집계
        foreach (var node in children)
        {
            var childResult = node.Tick();
            
            switch (childResult)
            {
                case State.Success:
                    successCount++;
                    break;
                case State.Failure:
                    failureCount++;
                    break;
                case State.Running:
                    runningCount++;
                    break;
            }
        }
        
        // 정책에 따른 결과 결정
        if (policy == Policy.All)
        {
            // All 정책: 모든 자식이 성공해야 성공, 하나라도 실패하면 실패, 그 외는 Running
            if (failureCount > 0)
                return State.Failure;
            else if (successCount == children.Count)
                return State.Success;
            else
                return State.Running;
        }
        else // Policy.Any
        {
            // Any 정책: 하나라도 성공하면 성공, 모든 자식이 실패하면 실패, 그 외는 Running
            if (successCount > 0)
                return State.Success;
            else if (failureCount == children.Count)
                return State.Failure;
            else
                return State.Running;
        }
    }
}
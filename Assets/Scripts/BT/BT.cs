using UnityEngine;
using System.Collections.Generic;

public class BTNode : MonoBehaviour
{
    public enum State { None, Success, Failure, Running }
    public virtual State Tick()
    {
        return State.None;
    }
}

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

public class Parallel : BTNode
{
    private List<BTNode> children;
    public Parallel(List<BTNode> nodes) => children = nodes;

    public override State Tick()
    {
        State result = State.Success;
        foreach (var node in children)
        {
            var childResult = node.Tick();
            if (childResult == State.Failure) result = State.Failure;
        }
        return result;
    }
}
using System.Collections.Generic;

public class Selector : BTNode
{
    private readonly List<BTNode> _children;

    public Selector(List<BTNode> children) => _children = children;

    public override Status Tick()
    {
        foreach (var child in _children)
        {
            var status = child.Tick();
            if (status == Status.Success) return Status.Success;
            if (status == Status.Running) return Status.Running;
        }
        return Status.Failure;
    }
}

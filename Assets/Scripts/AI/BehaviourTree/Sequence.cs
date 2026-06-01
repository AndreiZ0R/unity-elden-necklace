using System.Collections.Generic;

public class Sequence : BTNode
{
    private readonly List<BTNode> _children;

    public Sequence(List<BTNode> children) => _children = children;

    public override Status Tick()
    {
        foreach (var child in _children)
        {
            var status = child.Tick();
            if (status == Status.Failure) return Status.Failure;
            if (status == Status.Running) return Status.Running;
        }
        return Status.Success;
    }
}

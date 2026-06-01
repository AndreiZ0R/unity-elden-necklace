using System;

public class ActionLeaf : BTNode
{
    private readonly Func<Status> _action;

    public ActionLeaf(Func<Status> action) => _action = action;

    public override Status Tick() => _action();
}

public class ConditionLeaf : BTNode
{
    private readonly Func<bool> _condition;

    public ConditionLeaf(Func<bool> condition) => _condition = condition;

    public override Status Tick() => _condition() ? Status.Success : Status.Failure;
}

namespace ReFract;
public static class DelegateExtensions
{
    public static bool HasSubscriber(this Delegate source, Delegate target)
    {
        if (source == null) return false;
        if (target == null) return false;

        foreach (var d in source.GetInvocationList())
        {
            if (d.Equals(target)) return true;
        }
        return false;
    }
}
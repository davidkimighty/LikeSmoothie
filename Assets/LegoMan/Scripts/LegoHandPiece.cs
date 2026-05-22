using UnityEngine;

public class LegoHandPiece : LegoPiece<LegoManSet>
{
    public override void Assemble(LegoManSet legoSet)
    {
        if (smoothAssembleCoroutine != null) return;
        
        if (!legoSet.AddHand(this, out Slot target)) return;
        
        transform.SetParent(target.Anchor);
        transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
    }

    public override void AssembleAfterDelay(LegoManSet legoSet, float delay)
    {
        if (smoothAssembleCoroutine != null) return;
        
        if (!legoSet.AddHand(this, out Slot target)) return;
        
        smoothAssembleCoroutine = SmoothWaypointAssemble(target, delay);
        StartCoroutine(smoothAssembleCoroutine);
    }
}

using UnityEngine;

public class LegoHipPiece : LegoPiece<LegoManSet>
{
    public Slot LegSlot;
    
    public override void Assemble(LegoManSet legoSet)
    {
        if (smoothAssembleCoroutine != null) return;
        
        if (!legoSet.AddHip(this, out Slot target)) return;
        
        body.isKinematic = true;
        transform.SetParent(target.Anchor);
        transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
    }

    public override void AssembleAfterDelay(LegoManSet legoSet, float delay)
    {
        if (smoothAssembleCoroutine != null) return;
        
        if (!legoSet.AddHip(this, out Slot target)) return;
        
        smoothAssembleCoroutine = SmoothWaypointAssemble(target, delay);
        StartCoroutine(smoothAssembleCoroutine);
    }
}

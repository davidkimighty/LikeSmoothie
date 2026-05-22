using UnityEngine;

public class LegoArmPiece : LegoPiece<LegoManSet>
{
    public Slot HandSlot;
    
    public override void Assemble(LegoManSet legoSet)
    {
        if (smoothAssembleCoroutine != null) return;
        
        if (!legoSet.AddArm(this, out Slot target)) return;
        
        transform.SetParent(target.Anchor);
        transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
    }

    public override void AssembleAfterDelay(LegoManSet legoSet, float delay)
    {
        if (smoothAssembleCoroutine != null) return;
        
        if (!legoSet.AddArm(this, out Slot target)) return;
        
        smoothAssembleCoroutine = SmoothWaypointAssemble(target, delay);
        StartCoroutine(smoothAssembleCoroutine);
    }

    public override void Disassemble()
    {
        base.Disassemble();

        HandSlot.IsFull = false;
    }
}
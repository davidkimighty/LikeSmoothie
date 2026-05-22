using UnityEngine;

public class LegoBodyPiece : LegoPiece<LegoManSet>
{
    public Slot HeadSlot;
    public Slot HipSlot;
    public Slot LeftArmSlot;
    public Slot RightArmSlot;

    public override void Assemble(LegoManSet legoSet)
    {
        if (smoothAssembleCoroutine != null) return;
        
        if (!legoSet.AddBody(this)) return;

        body.isKinematic = true;
        transform.SetParent(legoSet.transform);
        transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
    }

    public override void AssembleAfterDelay(LegoManSet legoSet, float delay)
    {
        if (smoothAssembleCoroutine != null) return;
        
        if (!legoSet.AddBody(this)) return;
        
        smoothAssembleCoroutine = SmoothAssemble(legoSet.transform, 0f, false, false);
        StartCoroutine(smoothAssembleCoroutine);
    }

    public override void Disassemble()
    {
        base.Disassemble();

        LeftArmSlot.IsFull = false;
        RightArmSlot.IsFull = false;
    }
}

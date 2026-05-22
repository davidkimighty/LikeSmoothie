using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class LegoManSet : LegoSet
{
    [SerializeField] private LegoPieceSettings settings;
    
    private LegoHeadPiece head;
    private LegoBodyPiece body;
    private LegoHipPiece hip;
    private List<LegoArmPiece> arms = new();
    private List<LegoHandPiece> hands = new();
    private List<LegoLegPiece> legs = new();

    public override void Disassemble()
    {
        head?.Disassemble();
        body?.Disassemble();
        hip?.Disassemble();
        
        foreach (LegoArmPiece arm in arms)
            arm.Disassemble();
        
        foreach (LegoHandPiece hand in hands)
            hand.Disassemble();
        
        foreach (LegoLegPiece leg in legs)
            leg.Disassemble();

        head = null; body = null; hip = null;
        arms.Clear();
        hands.Clear();
        legs.Clear();
    }

    public bool AddHead(LegoHeadPiece head, out Slot target)
    {
        target = null;
        if (this.head != null || body == null) return false;

        head.Setup(settings);
        this.head = head;
        target = body.HeadSlot;
        return true;
    }

    public bool AddBody(LegoBodyPiece body)
    {
        if (this.body != null) return false;

        body.Setup(settings);
        this.body = body;
        return true;
    }

    public bool AddHip(LegoHipPiece hip, out Slot target)
    {
        target = null;
        if (this.hip != null || body == null) return false;

        hip.Setup(settings);
        this.hip = hip;
        target = body.HipSlot;
        return true;
    }

    public bool AddArm(LegoArmPiece arm, out Slot target)
    {
        target = null;
        if (arms.Count == 2 || body == null) return false;

        arm.Setup(settings);
        arms.Add(arm);
        target = !body.LeftArmSlot.IsFull ? body.LeftArmSlot : body.RightArmSlot;
        target.IsFull = true;
        return true;
    }
    
    public bool AddHand(LegoHandPiece hand, out Slot target)
    {
        target = null;
        if (hands.Count == 2 || arms.Count == 0 ||
            arms.All(arm => arm.HandSlot.IsFull)) return false;

        hand.Setup(settings);
        hands.Add(hand);
        target = arms.First(arm => !arm.HandSlot.IsFull).HandSlot;
        target.IsFull = true;
        return true;
    }

    public bool AddLeg(LegoLegPiece leg, out Slot target)
    {
        target = null;
        if (legs.Count == 2 || hip == null) return false;

        leg.Setup(settings);
        legs.Add(leg);
        target = hip.LegSlot;
        return true;
    }
}

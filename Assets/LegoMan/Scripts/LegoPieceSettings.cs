using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Lego/Settings", fileName = "LegoSettings")]
public class LegoPieceSettings : ScriptableObject
{
    public float Speed = 5f;
    public float AssembleDuration = 0.3f;
    public AnimationCurve AssembleCurve;
    public List<AudioClip> LegoHitClips;
}

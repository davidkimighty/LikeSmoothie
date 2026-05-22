using UnityEngine;

public class BuildManager : MonoBehaviour
{
    [SerializeField] private float delayIncrement = 0.3f;
    [SerializeField] private LegoManSet _legoManSet;
    
    private LegoBuilder<LegoManSet> legoManBuilder;
    private LegoDirector<LegoManSet> legoDirector;
    private LegoManSet legoMan;
    
    private void Start()
    {
        legoManBuilder = new LegoManBuilder(_legoManSet);
        legoDirector = new LegoDirector<LegoManSet>(legoManBuilder);
        legoDirector.AddBuildSteps(legoManBuilder.GetAssembleOrder());
    }

    public void Assemble()
    {
        legoMan = legoDirector.SmoothAssemble(delayIncrement);
    }

    public void Disassemble()
    {
        if (legoMan == null) return;
        
        legoMan.Disassemble();
    }
}

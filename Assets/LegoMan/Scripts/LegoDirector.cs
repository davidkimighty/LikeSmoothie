using System.Collections.Generic;
using UnityEngine;

public class LegoDirector<T> where T : LegoSet
{
    private ILegoBuilder<T> builder;
    private List<LegoPiece<T>> buildSteps;

    public LegoDirector(ILegoBuilder<T> builder)
    {
        this.builder = builder;
        buildSteps = new List<LegoPiece<T>>();
    }

    public void AddBuildSteps(List<LegoPiece<T>> steps)
    {
        buildSteps = steps;
    }
    
    public T Assemble()
    {
        if (buildSteps == null)
        {
            Debug.Log($"[LegoDirector] Assemble fail");
            return null;
        }
        
        foreach (LegoPiece<T> piece in buildSteps)
            builder.AssemblePiece(piece);
        
        return builder.Build();
    }

    public T SmoothAssemble(float delayIncrement)
    {
        if (buildSteps == null)
        {
            Debug.Log($"[LegoDirector] Assemble fail");
            return null;
        }
        
        foreach (LegoPiece<T> piece in buildSteps)
            builder.AssemblePieceAfterIncrementalDelay(piece, delayIncrement);
        
        return builder.Build();
    }
}
 
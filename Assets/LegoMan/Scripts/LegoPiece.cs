using System;
using System.Collections;
using UnityEngine;
using Random = UnityEngine.Random;

[Serializable]
public class Slot
{
    public Transform Anchor;
    public Transform Waypoint;
    public bool IsFull;
}

public abstract class LegoPiece<T> : MonoBehaviour where T : LegoSet
{
    public int Priority;
    
    protected Rigidbody body;
    protected AudioSource audioSource;
    protected LegoPieceSettings settings;
    protected IEnumerator smoothAssembleCoroutine;

    public virtual void Setup(LegoPieceSettings settings)
    {
        body = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
        this.settings = settings;
    }
    
    public abstract void Assemble(T legoSet);

    public abstract void AssembleAfterDelay(T legoSet, float delay);

    public virtual void Disassemble()
    {
        if (smoothAssembleCoroutine != null)
            StopCoroutine(smoothAssembleCoroutine);
        smoothAssembleCoroutine = null;
        
        transform.SetParent(null);
        
        if (body == null)
        {
            body = gameObject.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.linearDamping = 5;
            body.angularDamping = 3;
        }
    }
    
    protected virtual IEnumerator SmoothWaypointAssemble(Slot target, float delay, bool removeBody = true, bool playAudio = true)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);
        
        while (Vector3.Distance(transform.position, target.Waypoint.position) > 0.1f)
        {
            transform.position = Vector3.Slerp(transform.position, target.Waypoint.position, settings.Speed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, target.Waypoint.rotation, settings.Speed * Time.deltaTime);
            yield return null;
        }
        
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        float elapsedTime = 0f;
        bool audioPlayed = false;
        
        while (elapsedTime < settings.AssembleDuration)
        {
            float fraction = settings.AssembleCurve.Evaluate(elapsedTime / settings.AssembleDuration);
            transform.position = Vector3.LerpUnclamped(startPos, target.Anchor.position, fraction);
            transform.rotation = Quaternion.LerpUnclamped(startRot, target.Anchor.rotation, fraction);
            
            if (playAudio && !audioPlayed && fraction > 0.8f)
            {
                audioSource.PlayOneShot(settings.LegoHitClips[Random.Range(0, settings.LegoHitClips.Count)]);
                audioPlayed = true;
            }
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        if (removeBody)
            Destroy(body);
        transform.SetParent(target.Anchor);
        transform.SetPositionAndRotation(target.Anchor.position, target.Anchor.rotation);
    }
    
    protected virtual IEnumerator SmoothAssemble(Transform target, float delay, bool removeBody = true, bool playAudio = true)
    {
        if (delay > 0f)
            yield return new WaitForSeconds(delay);
        
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        float elapsedTime = 0f;
        bool audioPlayed = false;
        
        while (elapsedTime < settings.AssembleDuration)
        {
            float fraction = settings.AssembleCurve.Evaluate(elapsedTime / settings.AssembleDuration);
            transform.position = Vector3.Slerp(startPos, target.position, fraction);
            transform.rotation = Quaternion.Slerp(startRot, target.rotation, fraction);

            if (playAudio && !audioPlayed && fraction > 0.8f)
            {
                audioSource.PlayOneShot(settings.LegoHitClips[Random.Range(0, settings.LegoHitClips.Count)]);
                audioPlayed = true;
            }
            
            elapsedTime += Time.deltaTime;
            yield return null;
        }
        
        if (removeBody)
            Destroy(body);
        transform.SetParent(target);
        transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
    }
}

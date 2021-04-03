using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    public CheckpointCounter counter;
    public float timeToDelete = 3f;

    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            if(counter != null)
            {
                counter.rings.Remove(transform.parent.gameObject);
                counter.UpdateRingCount();
            }
            
            Destroy(transform.parent.gameObject, timeToDelete);
        }
    }
}

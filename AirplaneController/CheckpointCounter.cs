using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class CheckpointCounter : MonoBehaviour
{
    public TMP_Text counter;
    public List<GameObject> rings = new List<GameObject>();

    private int maxRings;
    private int completedRings = 0;

    void Start()
    {
        maxRings = rings.Count;
        counter.text = "0/" + maxRings.ToString();
    }

    public void UpdateRingCount()
    {
        completedRings = maxRings - rings.Count;
        counter.text = completedRings.ToString() + "/" + maxRings.ToString();
    }
}

using System.Collections.Generic;
using CityGenerator;
using UnityEngine;

public class RoadConnection : MonoBehaviour
{
    [SerializeField] private List<CityMark> connectedMarks = new List<CityMark>();

    public List<CityMark> GetConnectedMarks() => connectedMarks;

    public void Connect(CityMark mark)
    {
        if (mark != this && !connectedMarks.Contains(mark))
            connectedMarks.Add(mark);
    }
}

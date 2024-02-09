using System.Collections.Generic;
using UnityEngine;

namespace CityGenerator
{
    public class CityMark : MonoBehaviour
    {
        public int index;
        public CityObjectType markType;
        [SerializeField] private bool isDrawn = false;
        [SerializeField] private RoadConnection _roadConnection; // when create road
        [SerializeField] private CityBuildingGenerator building; // when create building

        public bool IsDrawn => isDrawn;
        public RoadConnection RegisteredRoadConnection => _roadConnection;
        public CityBuildingGenerator Building => building;

        public (int, int) Index { get; private set; }

        public void SetIndex(int x,int y) => SetIndex((x,y));
        public void SetIndex((int, int) index) => Index = index;

        public void Clear()
        {
            markType = CityObjectType.None;
            isDrawn = false;
            _roadConnection = null;
        }

        public void RegisterRoad(RoadConnection roadConnection)
        {
            if (isDrawn)
                return;
            isDrawn = true;
            _roadConnection = roadConnection;
        }

        public void RegisterBuilding(CityBuildingGenerator obj)
        {
            if (isDrawn)
                return;
            isDrawn = true;
            building = obj;
        }

        public List<CityMark> GetNearbyRoadMarks() => CityGroupGenerator.Instance.GetCityMarks().GetNearbyMarks(this);
    }
}

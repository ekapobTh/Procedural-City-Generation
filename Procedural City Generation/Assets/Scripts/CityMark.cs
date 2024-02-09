using System.Collections.Generic;
using UnityEngine;

namespace CityGenerator
{
    public class CityMark : MonoBehaviour
    {
        public int index;
        public CityObjectType markType;
        [SerializeField] private bool isDrawn = false;
        [SerializeField] private CityBuildingGenerator building; // when create building

        public bool IsDrawn => isDrawn;
        public CityBuildingGenerator Building => building;

        public (int, int) Index { get; private set; }

        public void SetIndex(int x,int y) => SetIndex((x,y));
        public void SetIndex((int, int) index) => Index = index;

        public void Clear()
        {
            markType = CityObjectType.None;
            isDrawn = false;
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

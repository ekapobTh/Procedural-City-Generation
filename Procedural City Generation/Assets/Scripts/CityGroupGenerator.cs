using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

namespace CityGenerator
{
    public class CityGroupGenerator : MonoSingleton<CityGroupGenerator>
    {
        [Header("Customisable Attribute")]
        [SerializeField] private int seed;
        [Range(1, 5)][SerializeField] private int column; // x
        [Range(1, 5)][SerializeField] private int row; // y
        [SerializeField] private float finalScale = 1f;

        [Space(10)]
        [Range(3, 10)][SerializeField] private int lotColumn; // x
        [Range(3, 10)][SerializeField] private int lotRow; // y

        [Space(10)]
        [Range(0, 10)][SerializeField] private int maximumLotRoadCount;

        [Space(20), Header("Others Attribute")]
        [SerializeField] private GameObject cityBase;

        [Space(20), Header("Sub City Attribute")]
        [SerializeField] private Transform subCityParent;
        [SerializeField] private CityGenerator subCityPrefab;

        [SerializeField] private Transform roadParent;
        [SerializeField] private SplineContainer majorRoadPrefab;

        [Space(20), Header("Mark Attribute")]
        [SerializeField] private Transform markParent;
        [SerializeField] private CityMark markPrefab;

        [Space(20), Header("Edge Building Attribute")]
        [SerializeField] private Transform edgeParent;
        [SerializeField] private CityBuildingGenerator edgePrefab;

        public bool isRealtimeRefresh = false;
        private bool isInitial = false;

        [Header("ETC Attribute")]
        [SerializeField] private Transform cameraPosition;
        private const float CAMERA_FIELD_BASE = 30f;

        private List<CityGenerator> cities = new List<CityGenerator>();
        private List<CityMark> edgeBuildingMarks = new List<CityMark>();
        private List<CityBuildingGenerator> buildings = new List<CityBuildingGenerator>();

        private List<SplineContainer> majorRoads = new List<SplineContainer>();
        private List<SplineContainer> majorHorizontalRoads = new List<SplineContainer>();
        private List<SplineContainer> majorVerticalRoads = new List<SplineContainer>();

        private CityMark[,] cityMarks;
        public CityMark[,] GetCityMarks() => cityMarks;
        public Vector2 GetCityGroupSize() => new Vector2(column, row);

        private int minorRoadIndex = 0;
        public int GetMinorRoadIndex() => minorRoadIndex++;

        // Cache
        private int seedCache;
        private int columnCache;
        private int rowCache;
        private float finalScaleCache;

        private int subCityColumnCache; // x
        private int subCityRowCache; // y

        private int maximumSubCityRoadCountCache;

        // Pooling
        private PoolingObjects<CityMark> markPool;
        private PoolingObjects<SplineContainer> roadPool;
        private PoolingObjects<CityBuildingGenerator> buildingPool;
        private PoolingObjects<CityGenerator> cityGeneratorPool;

        protected override void Awake()
        {
            base.Awake();

            if (markPool == null) markPool = new PoolingObjects<CityMark>(markPrefab, markParent);
            if (roadPool == null) roadPool = new PoolingObjects<SplineContainer>(majorRoadPrefab, roadParent);
            if (buildingPool == null) buildingPool = new PoolingObjects<CityBuildingGenerator>(edgePrefab, edgeParent);
            if (cityGeneratorPool == null) cityGeneratorPool = new PoolingObjects<CityGenerator>(subCityPrefab, subCityParent);

        }

        private void Update()
        {
            if (isInitial && isRealtimeRefresh)
                RegenrateChecking();
        }

        void RegenrateChecking()
        {
            if(seed != seedCache ||
               column != columnCache ||
               row != rowCache ||
               finalScale != finalScaleCache ||
               lotColumn != subCityColumnCache ||
               lotRow != subCityRowCache ||
               maximumLotRoadCount != maximumSubCityRoadCountCache)
                InstantGenerating();
        }

        [ContextMenu("Instant Create")]
        public void InstantGenerating()
        {

            var estimateTime = Time.realtimeSinceStartup;

            print($"Start : {Time.realtimeSinceStartup}");
            Clear();

            //yield return new WaitForEndOfFrame();

            CityUtility.SetSeed(seed);

            ConstructEdgeBuilding();
            ConstructMainRoad();
            AdjustCityBase();
            GenerateCityGroupMark();
            //Generate();
            //LogAllCitiesMarks();

            ComputeCameraPosition();
            transform.localScale = Vector3.one * finalScale;

            estimateTime = Time.realtimeSinceStartup - estimateTime;
            print($"Finish : {Time.realtimeSinceStartup}");
            print($"Estimate Time : {estimateTime}");
            PrintTotalRoad();
            PrintTotalBuilding();

            seedCache = seed;
            columnCache = column;
            rowCache = row;
            finalScaleCache = finalScale;
            subCityColumnCache = lotColumn;
            subCityRowCache = lotRow;
            maximumSubCityRoadCountCache = maximumLotRoadCount;

            isInitial = true;
            //StartCoroutine(Generate());

            //IEnumerator Generate()
            //{
            //    var estimateTime = Time.realtimeSinceStartup;

            //    print($"Start : {Time.realtimeSinceStartup}");
            //    Clear();

            //    yield return new WaitForEndOfFrame();

            //    CityUtility.SetSeed(seed);

            //    ConstructEdgeBuilding();
            //    ConstructMainRoad();
            //    AdjustCityBase();
            //    GenerateCityGroupMark();
            //    Generate();
            //    //LogAllCitiesMarks();

            //    ComputeCameraPosition();
            //    transform.localScale = Vector3.one * finalScale;

            //    estimateTime = Time.realtimeSinceStartup - estimateTime;
            //    print($"Finish : {Time.realtimeSinceStartup}");
            //    print($"Estimate Time : {estimateTime}");
            //    PrintTotalRoad();
            //    PrintTotalBuilding();

            //    seedCache = seed;
            //    columnCache = column;
            //    rowCache = row;
            //    finalScaleCache = finalScale;
            //    subCityColumnCache = lotColumn;
            //    subCityRowCache = lotRow;
            //    maximumSubCityRoadCountCache = maximumLotRoadCount;

            //    isInitial = true;
            //}
        }

        [ContextMenu("Generate Inner City")]
        public void Generate()
        {
            cities = new List<CityGenerator>();
            for (int i = 0; i < column; i++)
            {
                for (int j = 0; j < row; j++)
                {
                    var subCity = cityGeneratorPool.GetFromPool();

                    subCity.name = $"{i} {j}";

                    var offsetX = i * (lotColumn - 1) * CityUtility.MARKS_SPACE;
                    var offsetY = j * (lotRow - 1) * CityUtility.MARKS_SPACE;

                    subCity.SetCityOffsetPosition(new Vector2(offsetX, offsetY));
                    subCity.SetCityOffsetIndex(new Vector2(i, j));
                    subCity.SetSize(lotColumn, lotRow);
                    subCity.SetMaximumRoad(maximumLotRoadCount);
                    subCity.InstantGenerating();

                    cities.Add(subCity);
                }
            }
        }

        [ContextMenu("Clear")]
        public void Clear()
        {
            //print($"CityGroupGenerator Clear");
            foreach (var obj in cities)
                cityGeneratorPool.ReturnToPool(obj);
            cities.Clear();
            foreach (var obj in edgeBuildingMarks)
                markPool.ReturnToPool(obj);
            edgeBuildingMarks.Clear();
            foreach (var obj in buildings)
                buildingPool.ReturnToPool(obj);
            buildings.Clear();
            foreach (var obj in majorRoads)
                roadPool.ReturnToPool(obj);
            majorRoads.Clear();
            foreach (var obj in majorHorizontalRoads)
                roadPool.ReturnToPool(obj);
            majorHorizontalRoads.Clear();
            foreach (var obj in majorVerticalRoads)
                roadPool.ReturnToPool(obj);
            majorVerticalRoads.Clear();

            cityMarks = new CityMark[(column * lotColumn) - (column - 1), (row * lotRow) - (row - 1)];
            minorRoadIndex = 0;
        }

        #region Edge Building
        [ContextMenu("Construct Edge Building")]
        private void ConstructEdgeBuilding()
        {
            GenerateEdgeBuilding();
        }

        private void GenerateEdgeBuilding()
        {
            var edgeColumn = (column * lotColumn) - (column - 1);
            var edgeRow = (row * lotRow) - (row - 1);

            var currentFacingSide = StepMoveDirectionType.Left;
            var x = -1;
            var y = -1;

            for (; x < edgeColumn; x++)
                ConstructEdgeBuilding(x, y, currentFacingSide);
            currentFacingSide = currentFacingSide.TurnRight();
            for (; y < edgeRow; y++)
                ConstructEdgeBuilding(x, y, currentFacingSide);
            currentFacingSide = currentFacingSide.TurnRight();
            for (; x > -1; x--)
                ConstructEdgeBuilding(x, y, currentFacingSide);
            currentFacingSide = currentFacingSide.TurnRight();
            for (; y > -1; y--)
                ConstructEdgeBuilding(x, y, currentFacingSide);

            void ConstructEdgeBuilding(int x, int y, StepMoveDirectionType facingSide)
            {
                // Mark
                var newBuildingMark = markPool.GetFromPool();

                newBuildingMark.transform.position = new Vector3(x * CityUtility.MARKS_SPACE, 0f, y * CityUtility.MARKS_SPACE);
                newBuildingMark.name = $"edge {x} {y}";

                edgeBuildingMarks.Add(newBuildingMark);

                {   // Building
                    var newBuilding = buildingPool.GetFromPool();

                    newBuilding.SetFacingDirection(facingSide);
                    newBuilding.Construct(5);
                    newBuilding.transform.position = newBuildingMark.transform.position;

                    buildings.Add(newBuilding);
                }
            }
        }
        #endregion Edge Building

        #region Main Road
        [ContextMenu("Construct Main Road")]
        private void ConstructMainRoad()
        {
            GenerateMainRoad();
            ViasualizeMainRoad();
        }

        private void GenerateMainRoad()
        {
            var edgeColumn = (column * lotColumn) - (column - 1);
            var edgeRow = (row * lotRow) - (row - 1);

            {   // Round road
                int x = 0;
                int y = 0;

                for (; x < edgeColumn - 1; x++) PlantMainRoadMark(x, y);
                for (; y < edgeRow - 1; y++) PlantMainRoadMark(x, y);
                for (; x > 0; x--) PlantMainRoadMark(x, y);
                for (; y > 0; y--) PlantMainRoadMark(x, y);
            }


            {   // Cross road
                var horizontalMajorRoadCount = column;
                var verticalMajorRoadCount = row;

                for (int i = 1; i < horizontalMajorRoadCount; i++)
                {
                    var x = (i * lotColumn) - i;

                    for (int j = 0; j < edgeRow; j++) PlantMainRoadMark(x, j);
                }

                for (int i = 1; i < verticalMajorRoadCount; i++)
                {
                    var y = (i * lotRow) - i;

                    for (int j = 0; j < edgeColumn; j++) PlantMainRoadMark(j, y);
                }
            }

            void PlantMainRoadMark(int x, int y)
            {
                if (cityMarks[x, y] != null)
                    return;

                // Mark
                var newMainRoadMark = markPool.GetFromPool();

                newMainRoadMark.transform.position = new Vector3(x * CityUtility.MARKS_SPACE, 0f, y * CityUtility.MARKS_SPACE);
                newMainRoadMark.name = $"mainroad {x} {y}";
                newMainRoadMark.markType = CityObjectType.MajorRoad;
                newMainRoadMark.SetIndex(x, y);

                cityMarks[x, y] = newMainRoadMark;
            }
        }

        private void ViasualizeMainRoad()
        {
            var edgeColumn = (column * lotColumn) - (column - 1);
            var edgeRow = (row * lotRow) - (row - 1);

            {   // Round road
                var newRoad = roadPool.GetFromPool();
                var roadMeshFilter = newRoad.GetComponent<MeshFilter>();
                var spline = new Spline();

                newRoad.RemoveSplineAt(0);
                if (!roadMeshFilter.mesh)
                    roadMeshFilter.mesh = new Mesh();
                newRoad.transform.position = Vector3.zero;

                int x = 0;
                int y = 0;

                for (; x < edgeColumn - 1; x++)
                {
                    var newKnot = new BezierKnot();

                    newKnot.Position = ComputeRoundRoadPosition(x, y);
                    spline.Add(newKnot);
                }

                for (; y < edgeRow - 1; y++)
                {
                    var newKnot = new BezierKnot();

                    newKnot.Position = ComputeRoundRoadPosition(x, y);
                    spline.Add(newKnot);
                }

                for (; x > 0; x--)
                {
                    var newKnot = new BezierKnot();

                    newKnot.Position = ComputeRoundRoadPosition(x, y);
                    spline.Add(newKnot);
                }

                for (; y > 0; y--)
                {
                    var newKnot = new BezierKnot();

                    newKnot.Position = ComputeRoundRoadPosition(x, y);
                    spline.Add(newKnot);
                }

                spline.SetTangentMode(TangentMode.AutoSmooth);
                spline.Closed = true;

                newRoad.AddSpline(spline);

                if (newRoad.TryGetComponent<SplineExtrude>(out var se))
                    se.Rebuild();

                majorRoads.Add(newRoad);
            }

            {   // Cross road
                var horizontalMajorRoadCount = column;
                var verticalMajorRoadCount = row;

                for (int i = 1; i < horizontalMajorRoadCount; i++)
                {
                    var newRoad = roadPool.GetFromPool();
                    var roadMeshFilter = newRoad.GetComponent<MeshFilter>();
                    var spline = new Spline();

                    newRoad.RemoveSplineAt(0);
                    if (!roadMeshFilter.mesh)
                        roadMeshFilter.mesh = new Mesh();
                    newRoad.transform.position = Vector3.zero;

                    var x = (i * lotColumn) - i;

                    for (int j = 0; j < edgeRow; j++)
                    {
                        var pos = ComputeRoundRoadPosition(x, j);

                        if (j == 0)
                            pos = pos.ApplyOffset(CityUtility.ROAD_OFFSET, StepMoveDirectionType.Left, false);
                        else if (j == edgeRow - 1)
                            pos = pos.ApplyOffset(CityUtility.ROAD_OFFSET, StepMoveDirectionType.Left, true);

                        var newKnot = new BezierKnot();

                        newKnot.Position = pos;
                        spline.Add(newKnot);
                    }

                    newRoad.AddSpline(spline);

                    if (newRoad.TryGetComponent<SplineExtrude>(out var se))
                        se.Rebuild();

                    majorHorizontalRoads.Add(newRoad);
                }

                for (int i = 1; i < verticalMajorRoadCount; i++)
                {
                    var newRoad = roadPool.GetFromPool();
                    var roadMeshFilter = newRoad.GetComponent<MeshFilter>();
                    var spline = new Spline();

                    newRoad.RemoveSplineAt(0);
                    if (!roadMeshFilter.mesh)
                        roadMeshFilter.mesh = new Mesh();
                    newRoad.transform.position = Vector3.zero;

                    var y = (i * lotRow) - i;

                    for (int j = 0; j < edgeColumn; j++)
                    {
                        var pos = ComputeRoundRoadPosition(j, y);

                        if (j == 0)
                            pos = pos.ApplyOffset(CityUtility.ROAD_OFFSET, StepMoveDirectionType.Back, false);
                        else if (j == edgeRow - 1)
                            pos = pos.ApplyOffset(CityUtility.ROAD_OFFSET, StepMoveDirectionType.Back, true);

                        var newKnot = new BezierKnot();

                        newKnot.Position = pos;
                        spline.Add(newKnot);
                    }

                    newRoad.AddSpline(spline);

                    if (newRoad.TryGetComponent<SplineExtrude>(out var se))
                        se.Rebuild();

                    majorVerticalRoads.Add(newRoad);
                }
            }

            Vector3 ComputeRoundRoadPosition(int x, int y) => new Vector3(x * CityUtility.MARKS_SPACE, 0f, y * CityUtility.MARKS_SPACE);
        }
        #endregion Main Road

        [ContextMenu("City Base")]
        private void AdjustCityBase()
        {
            var xScale = (column * lotColumn * CityUtility.MARKS_SPACE) + (2 * CityUtility.MARKS_SPACE) - ((column - 1) * CityUtility.MARKS_SPACE);
            var yScale = (row * lotRow * CityUtility.MARKS_SPACE) + (2 * CityUtility.MARKS_SPACE) - ((row - 1) * CityUtility.MARKS_SPACE);

            var xLocation = xScale / 2 - 7.5f;
            var yLocation = yScale / 2 - 7.5f;

            cityBase.transform.localScale = new Vector3(xScale, cityBase.transform.localScale.y, yScale);
            cityBase.transform.position = new Vector3(xLocation, cityBase.transform.position.y, yLocation);
        }

        [ContextMenu("Generate City Group Mark")]
        private void GenerateCityGroupMark()
        {
            for (int x = 0; x < cityMarks.GetLength(0); x++)
            {
                for (int y = 0; y < cityMarks.GetLength(1); y++)
                {
                    if (cityMarks[x, y] != null)
                        continue;

                    var newMainRoadMark = markPool.GetFromPool();

                    newMainRoadMark.transform.position = new Vector3(x * CityUtility.MARKS_SPACE, 0f, y * CityUtility.MARKS_SPACE);
                    newMainRoadMark.name = $"{x} {y}";
                    newMainRoadMark.markType = CityObjectType.None;
                    newMainRoadMark.SetIndex(x, y);

                    cityMarks[x, y] = newMainRoadMark;
                }
            }
        }

        private void ComputeCameraPosition() // 1920x1080
        {
            var x = (column * (lotColumn - 1) * CityUtility.MARKS_SPACE) / 2f;
            var z = (row * (lotRow - 1) * CityUtility.MARKS_SPACE) / 2f;

            cameraPosition.position = new Vector3(x, cameraPosition.position.y, z);

            Camera.main.transform.SetParent(cameraPosition);

            Camera.main.transform.localPosition = Vector3.zero;
            Camera.main.transform.rotation = cameraPosition.rotation;

            if (row >= column - 1)
            {
                var  cameraAdditional = 22.5f;
                Camera.main.orthographicSize = (CAMERA_FIELD_BASE + (cameraAdditional * (row - 1))) * finalScale;
            }
            else
            {
                var cameraAdditional = 12.5f;
                Camera.main.orthographicSize = (CAMERA_FIELD_BASE + (cameraAdditional * (column - 2))) * finalScale;
            }
        }

        private void LogAllCitiesMarks()
        {
            print($"x : {cityMarks.GetLength(0)} y : {cityMarks.GetLength(1)}");
            string markLog = "";
            for (int x = 0; x < cityMarks.GetLength(0); x++)
            {
                for (int y = 0; y < cityMarks.GetLength(1); y++)
                {
                    if(cityMarks[x, y] != null)
                    markLog += $"{(int)cityMarks[x,y].markType} ";
                }

                markLog += "\n";
            }

            print(markLog);
            LogToFile.LogToTextFile(markLog);
        }

        private void PrintTotalRoad()
        {
            var totalRoad = 0;

            totalRoad += majorRoads.Count;
            totalRoad += majorHorizontalRoads.Count;
            totalRoad += majorVerticalRoads.Count;

            foreach (var city in cities)
                totalRoad += city.MinorRoads.Count;

            print($"Total Road : {totalRoad}");
        }

        private void PrintTotalBuilding()
        {
            var totalBuilding = 0;

            totalBuilding += buildings.Count;

            foreach (var city in cities)
                totalBuilding += city.Buildings.Count;

            print($"Total Building : {totalBuilding}");
        }
    }
}
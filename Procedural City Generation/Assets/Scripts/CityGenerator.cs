using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

//   Back
// Left Right
//   Front

namespace CityGenerator
{
    public class CityGenerator : MonoBehaviour
    {
        [SerializeField] private int seed;

        [Header("City Attribute")]
        [SerializeField] private Transform markParent;
        [SerializeField] private CityMark markPrefab;
        [SerializeField] private GameObject cityBase;

        [SerializeField] private int subCityColumn; // x
        [SerializeField] private int subCityRow; // y

        private CityMark[,] marks;
        public CityMark[,] GetMarks() => marks;

        [Space(5), Header("Road Attribute")]
        [SerializeField] private Transform roadParent;
        private int maximumMinorRoadCount = 0; // 1st priority for road generation, if no room for it, it will not continue spawning
        [SerializeField] private SplineContainer minorRoadPrefab;
     
        [Space(5), Header("Building Attribute")]
        [SerializeField] private Transform buildingParent;
        [SerializeField] private CityBuildingGenerator buildingPrefab;

        [SerializeField] private int currentDrawnRoad = 0;

        private Vector2 cityOffsetPosition = Vector2.zero;
        private Vector2 cityOffsetIndex = Vector2.zero;

        // Major Road Stuff
        private SplineContainer majorRoad;

        // Minor Road Stuff
        private List<SplineContainer> minorRoads = new List<SplineContainer>();
        public List<SplineContainer> MinorRoads => minorRoads;

        private List<CityBuildingGenerator> buildings = new List<CityBuildingGenerator>();
        public List<CityBuildingGenerator> Buildings => buildings;

        private bool isInstantGenerating = false;
        public bool isAutoGenerating = false;

        // auto generating stuff
        private int seedCache;
        private int columnCache;
        private int rowCache;
        private int maximumMinorRoadCountCache;

        public void SetCityOffsetPosition(Vector2 newOffsetPosition) => cityOffsetPosition = newOffsetPosition;
        public void SetCityOffsetIndex(Vector2 newOffsetIndex) => cityOffsetIndex = newOffsetIndex;

        //private void Update()
        //{
        //    if (isInstantGenerating && isAutoGenerating)
        //    {
        //        if (seedCache != seed || columnCache != subCityColumn || rowCache != subCityRow || maximumMinorRoadCountCache != maximumMinorRoadCount)
        //            InstantGenerating();
        //    }
        //}

        [ContextMenu("Generate City Mark")]
        private void GenerateCityBaseMark()
        {
            bool isGenerateMark = false;

            if (marks == null || marks.Length == 0)
            {
                marks = new CityMark[subCityColumn, subCityRow];
                isGenerateMark = true;
            }

            for (int x = 0; x < subCityColumn; x++)
            {
                for (int y = 0; y < subCityRow; y++)
                {
                    if (isGenerateMark)
                    {
                        var newMark = Instantiate(markPrefab, markParent) as CityMark;

                        newMark.transform.position = new Vector3(cityOffsetPosition.x + (x * CityUtility.MARKS_SPACE), 0f, cityOffsetPosition.y + (y * CityUtility.MARKS_SPACE));
                        newMark.name = $"{x} {y}";
                        newMark.markType = (((x == 0 || x == subCityColumn - 1) || (y == 0 || y == subCityRow - 1)) ? CityObjectType.MajorRoad : CityObjectType.None);

                        marks[x, y] = newMark;
                    }
                }
            }
        }

        private void MapMarksFromCityGroup()
        {
            marks = new CityMark[subCityColumn, subCityRow];

            var cityGroupMarks = CityGroupGenerator.Instance.GetCityMarks();

            for (int x = 0; x < subCityColumn; x++)
            {
                for (int y = 0; y < subCityRow; y++)
                {
                    var markGroupX = x + ((int)cityOffsetIndex.x * subCityColumn) - (int)cityOffsetIndex.x;
                    var markGroupY = y + ((int)cityOffsetIndex.y * subCityRow) - (int)cityOffsetIndex.y;

                    marks[x, y] = cityGroupMarks[markGroupX, markGroupY];
                }
            }
        }

        #region Road Stuff

        private void DrawMinorRoad((int, int) step, StepMoveDirectionType drawDir, bool isStopAtIntersection = false)
        {
            var (nextStep, isMoveSuccess) = MoveStep(step, drawDir);
            var isStraight = ((int)(CityUtility.GetCurrentSeedValue() * 10)) % 7 > 0;
            var stopStep = isStraight  ? - 1 : (int)((CityUtility.GetCurrentSeedValue() * 10) + (seed % 10));
            var forceBreak = false;

            while (isMoveSuccess) // move until at the edge or intersection if isStopAtIntersection is true
            {
                step = nextStep;
                if (marks.IsStepOn(step, CityObjectType.MajorRoad, CityObjectType.MajorJunction))
                {
                    (nextStep, isMoveSuccess) = MoveStep(step, drawDir);
                    continue;
                }

                var currentMark = marks[step.Item1, step.Item2];

                if (marks.IsStepOn(step, CityObjectType.MinorRoad))
                {
                    // if step on road, convert to junction
                    // if ((currentStep.Item1 != 0 && currentStep.Item1 != column - 1) && (currentStep.Item2 != 0 || currentStep.Item2 != row - 1)) // edge check logic
                    {
                        currentMark.markType = CityObjectType.Junction;
                    }
                    if (isStopAtIntersection)
                        break;
                }
                else
                {
                    if(currentMark.markType != CityObjectType.Junction)
                        currentMark.markType = CityObjectType.MinorRoad;   
                }

                var (leftStep, _) = MoveStep(step, drawDir.TurnLeft());
                var (rightStep, _) = MoveStep(step, drawDir.TurnRight());

                forceBreak = marks.IsStepOnRoad(leftStep) || marks.IsStepOnRoad(rightStep);

                if (forceBreak)
                    break;

                (nextStep, isMoveSuccess) = MoveStep(step, drawDir);

                if ((!isStraight && --stopStep <= 0) || false)
                    break;
            }

            currentDrawnRoad++;
        }

        #region Minor Road
        [ContextMenu("Generate/Minor Road")]
        private void GenerateMinorRoad()
        {
            var currentCreatedRoad = 0;
            var currentStep = (0, 0);
            var startStep = currentStep;
            var currentDirection = StepMoveDirectionType.Right;
            bool isLoopOccur = false;

            while (!isAllRoadCreated() && !isLoopOccur)
            {
                startStep = currentStep;
                var targetStep = (int)((CityUtility.GetCurrentSeedValue() * 10)) + (seed % 20);

                //print($"GenerateMinorRoad {CityUtility.GetCurrentSeedValue()}\n{currentCreatedRoad} {targetStep}");

                while (!isLoopOccur)
                {
                    targetStep--;

                    var (nextStep, isMoveSuccess) = MoveStep(currentStep, currentDirection);

                    currentStep = nextStep;

                    LoopCheck();

                    if (!isMoveSuccess || marks.isCorner(currentStep.Item1, currentStep.Item2))
                        CornerTurnMove();

                    LoopCheck();

                    if (targetStep <= 0)
                    {
                        var (checkStep, _) = MoveStep(currentStep, currentDirection.TurnRight());
                        var checkDir = currentDirection.ToDirectionType();

                        // Debug.Log($"Current Data {currentDirection} {checkDir} {checkStep}");
                        if (!marks.IsNearRoad(checkDir, checkStep.Item1, checkStep.Item2))
                        {
                            var drawDirection = currentDirection.TurnRight();
                            var drawStepCache = currentStep;

                            marks[currentStep.Item1, currentStep.Item2].markType = CityObjectType.MajorJunction;

                            //print($"Draw {currentStep} {drawDirection}");
                            DrawMinorRoad(currentStep, drawDirection, true);
                            currentStep = drawStepCache;

                            break;
                        }
                    }

                    void CornerTurnMove()
                    {
                        currentDirection = currentDirection.TurnRight();
                        (nextStep, _) = MoveStep(currentStep, currentDirection);

                        currentStep = nextStep;
                    }

                    void LoopCheck() => isLoopOccur |= (currentStep == startStep) && targetStep <= 0;
                }

                if (isLoopOccur)
                {
                    Debug.LogWarning($"Loop occur");
                    break;
                }
                currentCreatedRoad++;
            }

            bool isAllRoadCreated() => currentCreatedRoad >= maximumMinorRoadCount;
        }

        [ContextMenu("Visualize/Minor Road")]
        private void VisualizeMinorRoad()
        {
            var roadToVisualize = currentDrawnRoad;
            var startStep = (0, 0);
            var currentStep = startStep;
            var currentStepDirection = StepMoveDirectionType.Right;

            var (nextStep, isMoveSuccess) = MoveStep(currentStep, currentStepDirection);

            currentStep = nextStep;

            while (currentStep != startStep && roadToVisualize > 0) // move around the city
            {
                var checkDir = currentStepDirection.TurnRight();
                var (checkStep, _) = MoveStep(currentStep, checkDir);

                var isNextToRead = marks.IsStepOn(checkStep, CityObjectType.MinorRoad);
                var isDrew = false;

                if (isNextToRead)
                    isDrew = marks[checkStep.Item1, checkStep.Item2].IsDrawn;

                if (isNextToRead && !isDrew)
                {
                    var constructDir = checkDir;

                    DrawRoad(currentStep, constructDir);
                    //print($"VisualizeRoad : {currentStep} {roadToVisualize}");
                    roadToVisualize--;
                }

                (nextStep, isMoveSuccess) = MoveStep(currentStep, currentStepDirection);
                if (!isMoveSuccess)
                {
                    currentStepDirection = checkDir;
                    (nextStep, _) = MoveStep(currentStep, currentStepDirection);
                }
                currentStep = nextStep;

                //print($"VisualizeRoad : {currentStep} {roadToVisualize}");
            }

            void DrawRoad((int, int) step, StepMoveDirectionType dir)
            {
                var cityGroupMarks = CityGroupGenerator.Instance.GetCityMarks();
                var newRoad = Instantiate(minorRoadPrefab, roadParent) as SplineContainer;
                var mesh = new Mesh();
                var roadMeshFilter = newRoad.GetComponent<MeshFilter>();
                var spline = new Spline();

                newRoad.name = CityGroupGenerator.Instance.GetMinorRoadIndex().ToString();

                roadMeshFilter.mesh = mesh;
                newRoad.transform.position = Vector3.zero;

                var startStep = step;
                var currentStep = startStep;
                var currentStepDirection = dir;

                var (nextStep, _) = MoveStep(currentStep, currentStepDirection);

                {   // first knot
                    var newKnot = new BezierKnot();
                    var startMark = marks[currentStep.Item1, currentStep.Item2];
                    var nextMark = marks[nextStep.Item1, nextStep.Item2];

                    newKnot.Position = startMark.transform.position.ApplyOffset(CityUtility.ROAD_OFFSET, currentStepDirection, true);
                    spline.Add(newKnot);
                }

                currentStep = nextStep;

                while (true) // move straight though current direction
                {
                    var currentMark = marks[currentStep.Item1, currentStep.Item2];

                    if (marks.IsStepOn(currentStep, CityObjectType.MajorRoad, CityObjectType.MajorJunction))
                    {   // last knot
                        var newKnot = new BezierKnot();

                        newKnot.Position = currentMark.transform.position.ApplyOffset(CityUtility.ROAD_OFFSET, currentStepDirection, false);
                        spline.Add(newKnot);

                        var (backStep, _) = MoveStep(currentStep, currentStepDirection.TurnAround());
                        var endMark = marks[currentStep.Item1, currentStep.Item2];
                        var previousMark = marks[backStep.Item1, backStep.Item2];
                        break;
                    }
                    else if (marks.IsStepOn(currentStep, CityObjectType.Junction))
                    {
                        var newKnot = new BezierKnot();

                        newKnot.Position = currentMark.transform.position.ApplyOffset(CityUtility.ROAD_OFFSET, currentStepDirection, false);
                        spline.Add(newKnot);
                        
                        var (advanceStep, _) = MoveStep(currentStep, currentStepDirection);
                        var (backStep, _) = MoveStep(currentStep, currentStepDirection.TurnAround());
                        var previousMark = marks[backStep.Item1, backStep.Item2];
                    }
                    else if (marks.IsStepOn(currentStep, CityObjectType.MinorRoad))
                    {
                        var newKnot = new BezierKnot();

                        if (currentMark.IsDrawn)
                            newKnot.Position = currentMark.transform.position.ApplyOffset(CityUtility.ROAD_OFFSET, currentStepDirection, false);
                        else
                            newKnot.Position = currentMark.transform.position;

                        spline.Add(newKnot);
                    }
                    else
                        break;

                    (nextStep, _) = MoveStep(currentStep, currentStepDirection);
                    if (!marks.IsStepOnRoad(nextStep))
                    {   // if front have no road check side
                        var (leftStep, _) = MoveStep(currentStep, currentStepDirection.TurnLeft());
                        var (rightStep, _) = MoveStep(currentStep, currentStepDirection.TurnRight());

                        var isLeftDrawn = marks[leftStep.Item1, leftStep.Item2].IsDrawn;
                        var isRightDrawn = marks[rightStep.Item1, rightStep.Item2].IsDrawn;

                        if (marks.IsStepOnRoad(leftStep) && !isLeftDrawn && !marks.IsStepOnRoad(rightStep))
                        {
                            nextStep = leftStep;
                            currentStepDirection = currentStepDirection.TurnLeft();
                        }
                        else if (marks.IsStepOnRoad(rightStep) && !isRightDrawn && !marks.IsStepOnRoad(leftStep))
                        {
                            nextStep = rightStep;
                            currentStepDirection = currentStepDirection.TurnRight();
                        }
                        else
                            break;
                    }
                    currentStep = nextStep;

                }
                spline.SetTangentMode(TangentMode.AutoSmooth);
                newRoad.AddSpline(spline);

                minorRoads.Add(newRoad);
            }
        }
        #endregion Minor Road

        #endregion Road Stuff

        #region Building Stuff
        [ContextMenu("Generate/City Base")]
        private void GenerateCityBase()
        {
            var xScale = (subCityColumn * CityUtility.MARKS_SPACE) + (2 * CityUtility.MARKS_SPACE);
            var yScale = (subCityRow * CityUtility.MARKS_SPACE) + (2 * CityUtility.MARKS_SPACE);

            var xLocation = xScale / 2 - 7.5f;
            var yLocation = yScale / 2 - 7.5f;

            cityBase.transform.localScale = new Vector3(xScale, cityBase.transform.localScale.y, yScale);
            cityBase.transform.position = new Vector3(xLocation, cityBase.transform.position.y, yLocation);
        }

        [ContextMenu("Generate/Building")]
        private void GenerateBuilding()
        {
            //print($"GenerateBuilding {marks.GetLength(0)} {marks.GetLength(1)}");
            for (int x = 0; x < marks.GetLength(0); x++)
                for (int y = 0; y < marks.GetLength(1); y++)
                {
                    (int,int) currentStep = (x, y);

                    if (marks.IsStepOn(currentStep, CityObjectType.None))
                    {
                        var isBuildingValid = true;// Rand.value > 0.15;

                        if (!isBuildingValid || !marks.IsMarkAvailableToDraw(currentStep))
                            continue;

                        var newBuilding = Instantiate(buildingPrefab, buildingParent) as CityBuildingGenerator;

                        var isMerge = (CityUtility.GetCurrentSeedValue() * 100) > 75;

                        marks[x, y].markType = CityObjectType.Building;

                        var facingDirection = StepMoveDirectionType.Front;

                        {
                            var step1 = (x, y + 1); // Left
                            var step2 = (x, y - 1); // Right
                            var step3 = (x + 1, y); // Front
                            var step4 = (x - 1, y); // Back

                            var facingList = new List<StepMoveDirectionType>();

                            if (marks.IsMarkAvailable(step1) && marks.IsStepOnRoad(step1))
                                facingList.Add(StepMoveDirectionType.Left);
                            if (marks.IsMarkAvailable(step2) && marks.IsStepOnRoad(step2))
                                facingList.Add(StepMoveDirectionType.Right);
                            if (marks.IsMarkAvailable(step3) && marks.IsStepOnRoad(step3))
                                facingList.Add(StepMoveDirectionType.Front);
                            if (marks.IsMarkAvailable(step4) && marks.IsStepOnRoad(step4))
                                facingList.Add(StepMoveDirectionType.Back);

                            var facingSeed = CityUtility.GetCurrentSeedValue() * 100f;

                            if (facingList.Count > 0)
                                facingDirection = facingList[(int)(facingSeed % facingList.Count)];
                            else
                            {
                                if (facingSeed >= 0 && facingSeed <= 25f) facingDirection = StepMoveDirectionType.Left;
                                else if (facingSeed > 25 && facingSeed <= 50f) facingDirection = StepMoveDirectionType.Right;
                                else if (facingSeed > 50f && facingSeed <= 75f) facingDirection = StepMoveDirectionType.Back;
                            }
                        }

                        var buildingPosition = marks[x, y].transform.position;
                        if (isMerge)
                        {
                            (int, int) mergeStep1 = (x, y + 1);
                            (int, int) mergeStep2 = (x + 1, y);
                            (int, int) mergeStep3 = (x + 1, y + 1);

                            var amplifySize = 2f;

                            if ((marks.IsMarkAvailable(mergeStep1) && marks.IsMarkAvailableToDraw(mergeStep1) && !marks.IsStepOnRoad(mergeStep1)) &&
                               (marks.IsMarkAvailable(mergeStep2) && marks.IsMarkAvailableToDraw(mergeStep2) && !marks.IsStepOnRoad(mergeStep2)) &&
                               (marks.IsMarkAvailable(mergeStep3) && marks.IsMarkAvailableToDraw(mergeStep3) && !marks.IsStepOnRoad(mergeStep3)))
                            {
                                marks[mergeStep1.Item1, mergeStep1.Item2].RegisterBuilding(newBuilding);
                                marks[mergeStep2.Item1, mergeStep2.Item2].RegisterBuilding(newBuilding);
                                marks[mergeStep3.Item1, mergeStep3.Item2].RegisterBuilding(newBuilding);
                                newBuilding.Resize(new Vector3(amplifySize, 1f, amplifySize));
                                buildingPosition = CityUtility.CalculateCentroid(
                                    marks[currentStep.Item1, currentStep.Item2].transform.position,
                                    marks[mergeStep1.Item1, mergeStep1.Item2].transform.position,
                                    marks[mergeStep2.Item1, mergeStep2.Item2].transform.position,
                                    marks[mergeStep3.Item1, mergeStep3.Item2].transform.position);
                            }
                            else if (marks.IsMarkAvailable(mergeStep1) && marks.IsMarkAvailableToDraw(mergeStep1) && !marks.IsStepOnRoad(mergeStep1))
                            {
                                var newSize = facingDirection switch
                                {
                                    StepMoveDirectionType.Left => new Vector3(amplifySize, 1f, 1f),
                                    StepMoveDirectionType.Right => new Vector3(amplifySize, 1f, 1f),
                                    StepMoveDirectionType.Front => new Vector3(1f, 1f, amplifySize),
                                    StepMoveDirectionType.Back => new Vector3(1f, 1f, amplifySize),
                                };

                                marks[mergeStep1.Item1, mergeStep1.Item2].RegisterBuilding(newBuilding);
                                newBuilding.Resize(newSize);
                                buildingPosition = CityUtility.CalculateCentroid(
                                    marks[currentStep.Item1, currentStep.Item2].transform.position,
                                    marks[mergeStep1.Item1, mergeStep1.Item2].transform.position);
                            }
                            else if (marks.IsMarkAvailable(mergeStep2) && marks.IsMarkAvailableToDraw(mergeStep2) && !marks.IsStepOnRoad(mergeStep2))
                            {
                                var newSize = facingDirection switch
                                {
                                    StepMoveDirectionType.Left => new Vector3(1f, 1f, amplifySize),
                                    StepMoveDirectionType.Right => new Vector3(1f, 1f, amplifySize),
                                    StepMoveDirectionType.Front => new Vector3(amplifySize, 1f, 1f),
                                    StepMoveDirectionType.Back => new Vector3(amplifySize, 1f, 1f),
                                };

                                marks[mergeStep2.Item1, mergeStep2.Item2].RegisterBuilding(newBuilding);
                                newBuilding.Resize(newSize);
                                buildingPosition = CityUtility.CalculateCentroid(
                                    marks[currentStep.Item1, currentStep.Item2].transform.position,
                                    marks[mergeStep2.Item1, mergeStep2.Item2].transform.position);
                            }
                        }

                        newBuilding.SetFacingDirection(facingDirection);
                        newBuilding.Construct();
                        newBuilding.transform.position = buildingPosition;
                    }
                }
        }
        #endregion Building Stuff

        private ((int, int), bool) MoveStep((int, int) step, StepMoveDirectionType stepMove, bool isLog = false)
        {
            bool isMoveSuccess = true;
            var newStep = (0, 0);

            switch (stepMove)
            {
                case StepMoveDirectionType.Left:
                    {
                        var nextStep = step.Item2 - 1;

                        if (nextStep < 0)
                            isMoveSuccess = false;
                        else
                            newStep = (step.Item1, nextStep);
                        if (isLog)
                            Debug.Log($"Left {step}");
                    }
                    break;
                case StepMoveDirectionType.Right:
                    {
                        var nextStep = step.Item2 + 1;

                        if (nextStep >= subCityRow)
                            isMoveSuccess = false;
                        else
                            newStep = (step.Item1, nextStep);
                        if (isLog)
                            Debug.Log($"Right {step}");
                    }
                    break;
                case StepMoveDirectionType.Front:
                    {
                        var nextStep = step.Item1 + 1;

                        if (nextStep >= subCityColumn)
                            isMoveSuccess = false;
                        else
                            newStep = (nextStep, step.Item2);
                        if (isLog)
                            Debug.Log($"Front {step}");
                    }
                    break;
                case StepMoveDirectionType.Back:
                    {
                        var nextStep = step.Item1 - 1;

                        if (nextStep < 0)
                            isMoveSuccess = false;
                        else
                            newStep = (nextStep, step.Item2);
                        if (isLog)
                            Debug.Log($"Back {step}");
                    }
                    break;
                default:
                    throw new NotImplementedException($"{stepMove}");
            }

            return (newStep, isMoveSuccess);
        }

        [ContextMenu("Clear")]
        private void Clear()  => ClearCheckCache();

        private void ClearCheckCache(bool checkCache = false)
        {
            currentDrawnRoad = 0;

            if (majorRoad != null)
                Destroy(majorRoad.gameObject);

            if (minorRoads.Count > 0)
            {
                foreach (var minorRoad in minorRoads)
                    Destroy(minorRoad.gameObject);
                minorRoads.Clear();
            }

            if (buildings.Count > 0)
            {
                foreach (var building in buildings)
                    Destroy(building.gameObject);
                buildings.Clear();
            }

            if (checkCache && (columnCache == subCityColumn && rowCache == subCityRow))
            {
                if (marks != null && marks.Length > 0)
                    for (int x = 0; x < marks.GetLength(0); x++)
                        for (int y = 0; y < marks.GetLength(1); y++)
                        {
                            if (marks[x, y].markType == CityObjectType.MajorJunction)
                                marks[x, y].markType = CityObjectType.MajorRoad;
                            if (marks[x, y].markType != CityObjectType.MajorRoad)
                                marks[x, y].Clear();
                        }
            }
            else
            {
                if (marks != null && marks.Length > 0)
                    for (int x = 0; x < marks.GetLength(0); x++)
                        for (int y = 0; y < marks.GetLength(1); y++)
                            Destroy(marks[x, y].gameObject);

                marks = null;
            }
        }

        public void SetSize(int column, int row)
        {
            subCityColumn = column;
            subCityRow = row;
        }

        public void SetMaximumRoad(int maximumRoad)
        {
            maximumMinorRoadCount = maximumRoad;
        }

        [ContextMenu("Instant Create")]
        public void InstantGenerating()
        {
            ClearCheckCache(true);
            //GenerateCityBaseMark();
            MapMarksFromCityGroup();
            GenerateMinorRoad();
            VisualizeMinorRoad();
            GenerateBuilding();
            GenerateCityBase();

            seedCache = seed;
            columnCache = subCityColumn;
            rowCache = subCityRow;
            maximumMinorRoadCountCache = maximumMinorRoadCount;
            isInstantGenerating = true;
        }
    }
}
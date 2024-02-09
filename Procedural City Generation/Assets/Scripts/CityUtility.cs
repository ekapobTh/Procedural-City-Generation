using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityGenerator
{
    public static class CityUtility
    {
        public const float MARKS_SPACE = 5f;
        public const float ROAD_OFFSET = 0.8f;
        public const int NULL_INDEX = -1;

        #region Turn Function
        public static StepMoveDirectionType TurnRight(this StepMoveDirectionType dir) // find right direction of current direction
             => dir switch
             {
                 StepMoveDirectionType.Left => StepMoveDirectionType.Back,
                 StepMoveDirectionType.Right => StepMoveDirectionType.Front,
                 StepMoveDirectionType.Front => StepMoveDirectionType.Left,
                 StepMoveDirectionType.Back => StepMoveDirectionType.Right,
                 _ => throw new NotImplementedException($"{dir}"),
             };

        public static StepMoveDirectionType TurnLeft(this StepMoveDirectionType dir) // find left direction of current direction
             => dir switch
             {
                 StepMoveDirectionType.Left => StepMoveDirectionType.Front,
                 StepMoveDirectionType.Right => StepMoveDirectionType.Back,
                 StepMoveDirectionType.Front => StepMoveDirectionType.Right,
                 StepMoveDirectionType.Back => StepMoveDirectionType.Left,
                 _ => throw new NotImplementedException($"{dir}"),
             };

        public static StepMoveDirectionType TurnAround(this StepMoveDirectionType dir)
             => dir switch
             {
                 StepMoveDirectionType.Left => StepMoveDirectionType.Right,
                 StepMoveDirectionType.Right => StepMoveDirectionType.Left,
                 StepMoveDirectionType.Front => StepMoveDirectionType.Back,
                 StepMoveDirectionType.Back => StepMoveDirectionType.Front,
                 _ => throw new NotImplementedException($"{dir}"),
             };
        #endregion Turn Function

        public static DirectionType ToDirectionType(this StepMoveDirectionType dir)
             => dir switch
             {
                 StepMoveDirectionType.Left => DirectionType.Horizontal,
                 StepMoveDirectionType.Right => DirectionType.Horizontal,
                 StepMoveDirectionType.Front => DirectionType.Vertical,
                 StepMoveDirectionType.Back => DirectionType.Vertical,
                 _ => throw new NotImplementedException($"{dir}"),
             };

        public static Vector3 ApplyOffset(this Vector3 position, float offset, StepMoveDirectionType dir, bool isStartPosition)
        {
            var newPosition = Vector3.zero;

            switch (dir)
            {
                case StepMoveDirectionType.Left:
                    {
                        newPosition = isStartPosition
                            ? new Vector3(position.x, position.y, position.z - offset)
                            : new Vector3(position.x, position.y, position.z + offset);
                    }
                    break;
                case StepMoveDirectionType.Right:
                    {
                        newPosition = isStartPosition
                            ? new Vector3(position.x, position.y, position.z + offset)
                            : new Vector3(position.x, position.y, position.z - offset);
                    }
                    break;
                case StepMoveDirectionType.Front:
                    {
                        newPosition = isStartPosition
                            ? new Vector3(position.x + offset, position.y, position.z)
                            : new Vector3(position.x - offset, position.y, position.z);
                    }
                    break;
                case StepMoveDirectionType.Back:
                    {
                        newPosition = isStartPosition
                            ? new Vector3(position.x - offset, position.y, position.z)
                            : new Vector3(position.x + offset, position.y, position.z);
                    }
                    break;
            }

            return newPosition;
        }

        public static bool IsStepOn(this CityMark[,] marks, (int, int) step, params CityObjectType[] cityObjectType)
        {
            bool isStepOn = false;

            foreach (var item in cityObjectType)
            {
                isStepOn |= marks[step.Item1, step.Item2].markType == item;
                if (isStepOn)
                    break;
            }

            return isStepOn;
        }

        public static bool IsMarkAvailableToDraw(this CityMark[,] marks, (int, int) step)
        {
            bool isDrawAble = false;

            try
            {
                var mark = marks[step.Item1, step.Item2];

                isDrawAble = !mark.IsDrawn;
            }
            catch (Exception e)
            {
            }

            return isDrawAble;
        }

        public static bool IsMarkAvailable(this CityMark[,] marks, (int, int) step)
        {
            bool isAvailable = false;

            try
            {
                var mark = marks[step.Item1, step.Item2];

                isAvailable = true;
            }
            catch (Exception e)
            {
            }

            return isAvailable;
        }

        public static bool IsNearRoad(this CityMark[,] marks, DirectionType dir, int x, int y) // check front and back
        {
            bool isNearRoad = IsStepOnRoad(x, y);

            switch (dir)
            {
                case DirectionType.Horizontal:
                    {
                        var left = y - 1;
                        var right = y + 1;

                        isNearRoad |= (IsStepOnRoad(x, left) || IsStepOnRoad(x, right));
                    }
                    break;
                case DirectionType.Vertical:
                    {
                        var front = x + 1;
                        var back = x - 1;

                        isNearRoad |= (IsStepOnRoad(front, y) || IsStepOnRoad(back, y));

                    }
                    break;
                default:
                    throw new NotImplementedException($"{dir}");
            }

            return isNearRoad;

            bool IsStepOnRoad(int x, int y) => marks.IsStepOn((x, y), CityObjectType.MajorRoad, CityObjectType.MajorJunction, CityObjectType.MinorRoad, CityObjectType.Junction);
        }

        public static bool isCorner(this CityMark[,] marks, int x, int y) =>
            (x == 0 && y == 0) ||
            (x == marks.GetLength(0) - 1 && y == marks.GetLength(1) - 1) ||
            (x == marks.GetLength(0) - 1 && y == 0) ||
            (x == 0 && y == marks.GetLength(1) - 1);

        public static StepMoveDirectionType ComputeDirectionByIndex(this StepMoveDirectionType dir, int index)
        {
            var currentDirection = dir;
            var newDirIndex = index % 3; // 0 -> left, 1 -> front, 2 -> right

            switch (newDirIndex)
            {
                case 0: // left
                    {
                        currentDirection = dir.TurnLeft();
                    }
                    break;
                case 1: // straight
                    {
                    }
                    break;
                case 2: // right
                    {
                        currentDirection = dir.TurnRight();
                    }
                    break;
                default:
                    throw new NotImplementedException($"{newDirIndex}");
            }

            return currentDirection;
        }

        public static Vector2 ToRotation(this StepMoveDirectionType dir) => dir switch
        {
            StepMoveDirectionType.Left => new Vector3(0f, 270f, 0f),
            StepMoveDirectionType.Right => new Vector3(0f, 90f, 0f),
            StepMoveDirectionType.Front => Vector3.zero,
            StepMoveDirectionType.Back => new Vector3(0f, 180, 0f),
            _ => throw new NotImplementedException($"{dir}"),
        };

        public static Vector3 CalculateCentroid(params Vector3[] v)
        {
            float x = 0f;
            float y = 0f;
            float z = 0f;

            for (int i = 0; i < v.Length; i++)
                x += v[i].x;
            x /= v.Length;
            for (int i = 0; i < v.Length; i++)
                y += v[i].y;
            y /= v.Length;
            for (int i = 0; i < v.Length; i++)
                z += v[i].z;
            z /= v.Length;

            return new Vector3(x, y, z);
        }

        public static List<CityMark> GetNearbyMarks(this CityMark[,] marks, CityMark mark)
        {
            List<CityMark> returnMarks = new List<CityMark>();

            var left = (mark.Index.Item1 , mark.Index.Item2 - 1);
            var right = (mark.Index.Item1, mark.Index.Item2 + 1);
            var front = (mark.Index.Item1 + 1, mark.Index.Item2);
            var back = (mark.Index.Item1 - 1, mark.Index.Item2);


            if (marks.IsMarkAvailable(left) && marks.IsStepOnRoad(left))
                returnMarks.Add(marks[left.Item1,left.Item2]);
            if (marks.IsMarkAvailable(right) && marks.IsStepOnRoad(right))
                returnMarks.Add(marks[right.Item1, right.Item2]);
            if (marks.IsMarkAvailable(front) && marks.IsStepOnRoad(front))
                returnMarks.Add(marks[front.Item1, front.Item2]);
            if (marks.IsMarkAvailable(back) && marks.IsStepOnRoad(back))
                returnMarks.Add(marks[back.Item1, back.Item2]);

            return returnMarks;
        }

        public static bool IsStepOnRoad(this CityMark[,] marks, (int, int) step) => marks.IsStepOn(step, CityObjectType.MajorRoad, CityObjectType.MajorJunction, CityObjectType.Junction, CityObjectType.MinorRoad);

        #region Seed
        public static float GetCurrentSeedValue() => UnityEngine.Random.value;
        public static void SetSeed(int seed) => UnityEngine.Random.InitState(seed);
        #endregion Seed
    }

    #region Enum
    public enum CityObjectType { None, MajorRoad, MajorJunction, MinorRoad, Junction, Building, }
    public enum StepMoveDirectionType { Left, Right, Front, Back, }
    public enum DirectionType { Horizontal, Vertical, }
    public enum CornerType { FrontRight, FrontLeft, BackRight, BackLeft, }
    #endregion Enum
}


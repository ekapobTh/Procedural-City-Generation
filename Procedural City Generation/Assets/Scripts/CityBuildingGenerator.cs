using UnityEngine;

namespace CityGenerator
{
    public class CityBuildingGenerator : MonoBehaviour
    {
        private float buildingPositionOffset = 0.5f;
        [SerializeField] private Transform buildingParent;
        [SerializeField] private BoxCollider _collider;
        [SerializeField] private float scale;
        [SerializeField] private StepMoveDirectionType facingDirection;

        [Space(20), Header("Building Parts")]
        [SerializeField] private GameObject[] baseParts;
        [SerializeField] private GameObject[] bodyParts;
        [SerializeField] private GameObject[] ceilingParts;

        [ContextMenu("Construct")]
        public void InstantConstruct() => Construct();

        public void SetFacingDirection(StepMoveDirectionType facingDirection) => this.facingDirection = facingDirection;

        private Vector3 amplifySize = Vector3.one;
        private float totalHeightOffset = 0f;

        public void Construct(int piece = CityUtility.NULL_INDEX)
        {
            Clear();

            var targetPieces = piece == CityUtility.NULL_INDEX ? (int)(CityUtility.GetCurrentSeedValue() * 5) : piece;
            var heightOffset = 0f;

            heightOffset += SpawnPieceLayer(heightOffset, baseParts);

            for (int i = 2; i < targetPieces; i++)
                heightOffset += SpawnPieceLayer(heightOffset, bodyParts);

            heightOffset += SpawnPieceLayer(heightOffset, ceilingParts);

            totalHeightOffset = heightOffset;

            buildingParent.localScale = GetActualScale();
            buildingParent.position = new Vector3(buildingPositionOffset * scale * amplifySize.x, 0f, buildingPositionOffset * scale * amplifySize.z);

            transform.rotation = Quaternion.Euler(facingDirection.ToRotation());
            RefreshCollision();
        }

        private float SpawnPieceLayer(float inputHeight, params GameObject[] pieceArray)
        {
            var percentIndex = (int)(pieceArray.Length * CityUtility.GetCurrentSeedValue());
            var layer = Instantiate(pieceArray[Mathf.Min(percentIndex, pieceArray.Length - 1)], buildingParent);

            layer.transform.localPosition = new Vector3(0f, transform.position.y + (inputHeight), 0f);
            
            return layer.GetComponentInChildren<MeshFilter>().mesh.bounds.size.y;
        }

        public void Clear()
        {
            for (int i = buildingParent.childCount - 1; i > 0; i--)
                Destroy(buildingParent.GetChild(i).gameObject);

            buildingParent.localScale = Vector3.one;
            buildingParent.position = Vector3.zero;
            transform.rotation = Quaternion.Euler(Vector3.zero);
        }

        public void Resize(Vector3 size)
        {
            amplifySize = size;
            buildingParent.localScale = new Vector3(
                buildingParent.localScale.x * size.x,
                buildingParent.localScale.y * size.y,
                buildingParent.localScale.z * size.z);
        }

        private void RefreshCollision()
        {
            _collider.transform.localScale = GetActualScale();
            _collider.size = new Vector3(_collider.size.x, totalHeightOffset, _collider.size.z);
            _collider.transform.position += new Vector3(0f, totalHeightOffset * 2.375f, 0f);
        }

        private Vector3 GetActualScale() => new Vector3(
                1f * scale * amplifySize.x,
                1f * scale * amplifySize.y,
                1f * scale * amplifySize.z);
    }
}
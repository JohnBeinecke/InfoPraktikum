using UnityEngine;

namespace Util
{
    [System.Serializable]
    struct Vector3Int
    {
        [SerializeField][Range(0,50)]public int x;
        [SerializeField][Range(0,50)]public int y;
        [SerializeField][Range(0,50)]public int z;
    }
}
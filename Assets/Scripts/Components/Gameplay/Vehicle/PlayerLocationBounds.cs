using Unity.Entities;

namespace Unity.Megacity.Gameplay
{
    public struct PlayerLocationBounds : IComponentData
    {
        public bool IsInside;
    }
}
using Unity.Entities;
using Unity.Physics.Systems;

namespace Unity.Megacity.Gameplay
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
    [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
    public partial class ClientOnlyPhysicsSystemGroup : CustomPhysicsSystemGroup
    {
        public ClientOnlyPhysicsSystemGroup() : base(1, true)
        {}

        protected override void OnCreate()
        {
            base.OnCreate();
            RequireForUpdate<PlayerSpawner>();
            RequireForUpdate<SinglePlayer>();
        }
    }
}
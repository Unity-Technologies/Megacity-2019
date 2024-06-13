using static Unity.Entities.SystemAPI;

namespace Unity.Entities.Simulation
{
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateBefore(typeof(UpdateGameState))]
    public partial struct CompleteGameState : ISystem
    {
        private EntityQuery m_DependenciesQuery;
        public void OnCreate(ref SystemState state)
        {
            m_DependenciesQuery = state.GetEntityQuery(ComponentType.ReadOnly<GameStateDependency>());
            state.RequireForUpdate(m_DependenciesQuery);
            state.RequireForUpdate<GameStateData>();
            state.RequireForUpdate<GameStateElement>();
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            var moveToNextState = true;
            if (!m_DependenciesQuery.IsEmpty)
            {
                var dependenciesCompleted = 0;
                var numOfDependencies = m_DependenciesQuery.CalculateEntityCount();
                foreach (var dependency in Query<GameStateDependency>())
                {
                    if (dependency.IsCompleted) 
                        dependenciesCompleted++;
                    
                }

                moveToNextState = dependenciesCompleted.Equals(numOfDependencies);
                
                if (moveToNextState) 
                {
                    var updateState = new UpdateGameStateJob();
                    state.Dependency = updateState.ScheduleParallel(state.Dependency);
                }
            }

            if(moveToNextState)
            {
                var updateStateIndexJob = new UpdateStateIndexJob
                {
                    Buffer = GetSingletonBuffer<GameStateElement>(),
                };
                state.Dependency = updateStateIndexJob.Schedule(state.Dependency);
                state.EntityManager.DestroyEntity(m_DependenciesQuery);
            }
        }
    }
}
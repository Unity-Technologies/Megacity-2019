namespace Unity.Entities.Simulation
{
    public class GameStateAccess 
    {
        public static void AddDependency(EntityManager entityManager, out Entity entity) 
        {
            entity = entityManager.CreateEntity(typeof(GameStateDependency));
            entityManager.SetName(entity, "GameState - Dependency Linker");
        }

        public static void CompleteDependency (EntityManager entityManager, ref Entity reference) 
        {
            var data = new GameStateDependency { IsCompleted = true };
            entityManager.SetComponentData(reference, data);
        }
    }
}
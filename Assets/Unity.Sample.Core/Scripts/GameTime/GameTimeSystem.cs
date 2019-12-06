using Unity.Entities;

public class GameTimeSystem : ComponentSystem
{
    
    // TODO (mogensh) THIS IS CURRENTLY UNUSED. SHOULD BE USED INSTEAD OF GAMEWORLD
    protected override void OnCreate()
    {
        base.OnCreate();
        globalTimeEntity = EntityManager.CreateEntity(typeof(GlobalGameTime));
        worldTime = new GameTime(60);
    }

    public GameTime GetWorldTime()
    {
        return worldTime;
    }

    public void SetWorldTime(GameTime time)
    {
        worldTime = time;
        var globalTime = EntityManager.GetComponentData<GlobalGameTime>(globalTimeEntity);
        globalTime.gameTime = worldTime;
        EntityManager.SetComponentData(globalTimeEntity, globalTime);
    }

    public float frameDuration
    {
        get { return m_frameDuration; }
        set
        {
            m_frameDuration = value;

            var globalTime = EntityManager.GetComponentData<GlobalGameTime>(globalTimeEntity);
            globalTime.frameDuration = m_frameDuration;
            EntityManager.SetComponentData(globalTimeEntity, globalTime);
        }
    }
    
    protected override void OnUpdate()
    {
    }
    
    GameTime worldTime;
    float m_frameDuration;
    Entity globalTimeEntity;

}


using Unity.Collections;
using Unity.Entities;
using Random = Unity.Mathematics.Random;

// TODO (mogensh) this will hopefully become full features way to have customizable random properties in authoring
// (e.g. choose distribution type)  that are converted to simple lists of values for use at runtime.
public class RandomValueList
{
    public static BlobAssetReference<RandomFloat> CreateRandomFloat(int runtimeBufferSize)
    {
        var blobBuilder = new BlobBuilder(Allocator.Temp);
        ref var root = ref blobBuilder.ConstructRoot<RandomFloat>();

        var values = blobBuilder.Allocate(ref root.Values, runtimeBufferSize);
        var rnd = new Random();
        rnd.InitState();
        for (int i = 0; i < runtimeBufferSize; i++)
        {
            values[i] = rnd.NextFloat();
        }
        var rootRef =  blobBuilder.CreateBlobAssetReference<RandomFloat>(Allocator.Persistent);
        return rootRef;
    }

    public struct RandomFloat
    {
        public BlobArray<float> Values;
    }
}
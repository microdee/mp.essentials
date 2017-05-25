using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimMetrics.Net.Metric;
using VVVV.PluginInterfaces.V2;

namespace VVVV.Nodes
{
    [PluginInfo(
        Version = "Distance",
        Category = "String",
        Name = "Block",
        Author = "microdee"
    )]
    public class StringBlockDistanceNode : AbstractStringDistanceNode<BlockDistance> { }

    [PluginInfo(
        Version = "Distance",
        Category = "String",
        Name = "ChapmanLengthDeviation",
        Author = "microdee"
    )]
    public class StringChapmanLengthDeviationNode : AbstractStringDistanceNode<ChapmanLengthDeviation> { }

    [PluginInfo(
        Version = "Distance",
        Category = "String",
        Name = "ChapmanMeanLength",
        Author = "microdee"
    )]
    public class StringChapmanMeanLengthNode : AbstractStringDistanceNode<ChapmanMeanLength> { }

    [PluginInfo(
        Version = "Distance",
        Category = "String",
        Name = "CosineSimilarity",
        Author = "microdee"
    )]
    public class StringCosineSimilarityNode : AbstractStringDistanceNode<CosineSimilarity> { }

    [PluginInfo(
        Version = "Distance",
        Category = "String",
        Name = "DiceSimilarity",
        Author = "microdee"
    )]
    public class StringDiceSimilarityNode : AbstractStringDistanceNode<DiceSimilarity> { }

    [PluginInfo(
        Version = "Distance",
        Category = "String",
        Name = "Euclidean",
        Author = "microdee"
    )]
    public class StringEuclideanDistanceNode : AbstractStringDistanceNode<EuclideanDistance>
    {
        [Output("Euclid Distance")]
        public ISpread<double> FEuclidDist;

        public override void AuxOperation(string a, string b, int i)
        {
            FEuclidDist[i] = FAlg[0].GetEuclidDistance(a, b);
        }
    }

    [PluginInfo(
        Version = "Distance",
        Category = "String",
        Name = "JaccardSimilarity",
        Author = "microdee"
    )]
    public class StringJaccardSimilarityNode : AbstractStringDistanceNode<JaccardSimilarity> { }

    [PluginInfo(
        Version = "Distance",
        Category = "String",
        Name = "Jaro",
        Author = "microdee"
    )]
    public class StringJaroNode : AbstractStringDistanceNode<Jaro> { }

    [PluginInfo(
        Version = "Distance",
        Category = "String",
        Name = "JaroWinkler",
        Author = "microdee"
    )]
    public class StringJaroWinklerNode : AbstractStringDistanceNode<JaroWinkler> { }

    [PluginInfo(
        Version = "Distance",
        Category = "String",
        Name = "Levenstein",
        Author = "microdee"
    )]
    public class StringLevensteinNode : AbstractStringDistanceNode<Levenstein> { }

    [PluginInfo(
        Version = "Distance",
        Category = "String",
        Name = "MatchingCoefficient",
        Author = "microdee"
    )]
    public class StringMatchingCoefficientNode : AbstractStringDistanceNode<MatchingCoefficient> { }

    [PluginInfo(
        Version = "Distance",
        Category = "String",
        Name = "MongeElkan",
        Author = "microdee"
    )]
    public class StringMongeElkanNode : AbstractStringDistanceNode<MongeElkan> { }

    [PluginInfo(
        Version = "Distance",
        Category = "String",
        Name = "NeedlemanWunch",
        Author = "microdee"
    )]
    public class StringNeedlemanWunchNode : AbstractStringDistanceNode<NeedlemanWunch> { }

    [PluginInfo(
        Version = "Distance",
        Category = "String",
        Name = "OverlapCoefficient",
        Author = "microdee"
    )]
    public class StringOverlapCoefficientNode : AbstractStringDistanceNode<OverlapCoefficient> { }

    [PluginInfo(
        Version= "Distance",
        Category = "String",
        Name = "QGramsDistance",
        Author = "microdee"
    )]
    public class StringQGramsDistanceNode : AbstractStringDistanceNode<QGramsDistance> { }

    [PluginInfo(
        Version = "Distance",
        Category = "String",
        Name = "SmithWaterman",
        Author = "microdee"
    )]
    public class StringSmithWatermanNode : AbstractStringDistanceNode<SmithWaterman> { }
}

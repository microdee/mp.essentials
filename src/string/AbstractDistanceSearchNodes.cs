using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SimMetrics.Net.API;
using mp.pddn;
using VVVV.PluginInterfaces.V2;

namespace mp.essentials.Nodes.Strings
{
    public abstract class AbstractStringDistanceNode<TMetrics> : IPluginEvaluate where TMetrics : AbstractStringMetric, new()
    {
        [Input("Input 1")]
        public ISpread<string> FInput1;
        [Input("Input 2")]
        public ISpread<string> FInput2;

        [Output("Similarity")]
        public ISpread<double> FSim;
        [Output("Similarity Explained")]
        public ISpread<string> FSimexplained;
        [Output("Unnormalized Similarity")]
        public ISpread<double> FSimUnnorm;
        [Output("Algorithm")]
        public ISpread<TMetrics> FAlg;

        [Output("Short Description", Visibility = PinVisibility.Hidden)]
        public ISpread<string> FShortDesc;
        [Output("Long Description", Visibility = PinVisibility.Hidden)]
        public ISpread<string> FLongDesc;

        public virtual void AuxOperation(string a, string b, int i) { }

        public void Evaluate(int SpreadMax)
        {
            if (FInput1.IsChanged || FInput2.IsChanged)
            {
                this.SetSliceCountForAllOutput(SpreadMax, ignore: new []{ "FShortDesc", "FLongDesc", "FAlg"});
                var alg = new TMetrics();
                FAlg[0] = alg;
                FShortDesc[0] = alg.ShortDescriptionString;
                FLongDesc[0] = alg.LongDescriptionString;

                for (int i = 0; i < SpreadMax; i++)
                {
                    FSim[i] = alg.GetSimilarity(FInput1[i], FInput2[i]);
                    FSimUnnorm[i] = alg.GetUnnormalisedSimilarity(FInput1[i], FInput2[i]);
                    try
                    {
                        FSimexplained[i] = alg.GetSimilarityExplained(FInput1[i], FInput2[i]);
                    }
                    catch { }
                    AuxOperation(FInput1[i], FInput2[i], i);
                }
            }
        }
    }
}
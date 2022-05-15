
using System;
using System.Collections.Generic;
using System.Linq;
using md.stdl.Mathematics;
using VVVV.PluginInterfaces.V2;
using VVVV.PluginInterfaces.V2.NonGeneric;
using VVVV.Utils.VMath;

namespace mp.essentials.Nodes.Values
{
    [PluginInfo(
        Name = "BinarySearch",
        Category = "Value",
        Author = "microdee"
    )]
    public class ValueBinarySearchNode : IPluginEvaluate
    {
        [Input("Ordered Input")]
        public IDiffSpread<ISpread<double>> OrderedInput;
        
        [Input("Reference")]
        public IDiffSpread<double> ReferenceIn;
        
        [Output("First")]
        public ISpread<ISpread<int>> FirstOut;
        
        [Output("Weight of Second")]
        public ISpread<ISpread<double>> WeightOfNextOut;
        
        [Output("Distance")]
        public ISpread<ISpread<double>> DistanceOut;

        void SetResult(int bin, int id, double refvalue, double? overrideWeight = null)
        {
            FirstOut[bin][0] = id;
            WeightOfNextOut[bin][0] = overrideWeight
                ?? VMath.Map(refvalue, OrderedInput[bin][id], OrderedInput[bin][id + 1], 0.0, 1.0, TMapMode.Clamp);
            DistanceOut[bin][0] = Math.Abs(OrderedInput[bin][id + 1] - OrderedInput[bin][id]);
        }

        public void Evaluate(int SpreadMax)
        {
            if (!SpreadUtils.AnyChanged(OrderedInput, ReferenceIn))
            {
                return;
            }
            
            foreach (var spread in new ISpread[]{ FirstOut, WeightOfNextOut, DistanceOut})
                spread.SliceCount = SpreadUtils.SpreadMax(OrderedInput, ReferenceIn);

            for (int i = 0; i < SpreadUtils.SpreadMax(OrderedInput, ReferenceIn); i++)
            {
                var outSpreads = new ISpread[]{ FirstOut[i], WeightOfNextOut[i], DistanceOut[i]};
                if (OrderedInput[i].SliceCount == 0)
                {
                    foreach (var spread in outSpreads) spread.SliceCount = 0;
                    continue;
                }
                foreach (var spread in outSpreads) spread.SliceCount = 1;

                if (OrderedInput[i].SliceCount == 1)
                {
                    FirstOut[i][0] = 0;
                    WeightOfNextOut[i][0] = 0;
                    DistanceOut[i][0] = 0;
                    continue;
                }
                var refvalue = ReferenceIn[i];

                if (refvalue <= OrderedInput[i][0])
                {
                    SetResult(i, 0, refvalue, 0.0);
                    continue;
                }

                if (refvalue >= OrderedInput[i][-1])
                {
                    SetResult(i, OrderedInput[i].SliceCount - 2, refvalue, 1.0);
                    continue;
                }

                if (OrderedInput[i].SliceCount == 2)
                {
                    SetResult(i, 0, refvalue);
                    continue;
                }
                
                int minId = 0;
                int maxId = OrderedInput[i].SliceCount - 1;

                while (maxId - minId > 1)
                {
                    int midId = (int)Math.Floor((float)(minId + maxId) * 0.5);
                    if (refvalue.Eq(OrderedInput[i][midId]))
                    {
                        SetResult(i, midId, refvalue, 0.0);
                        break;
                    }

                    if (refvalue > OrderedInput[i][midId])
                    {
                        minId = midId;
                    }
                    else
                    {
                        maxId = midId;
                    }
                }
                
                SetResult(i, minId, refvalue);
            }
        }
    }
}
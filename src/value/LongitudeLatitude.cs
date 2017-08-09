using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VVVV.Core.Logging;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VMath;

namespace mp.essentials.Nodes.Values
{
    [PluginInfo(Name = "Distance",
                Category = "2d",
                Version = "LongLat",
                Author = "microdee")]
    public class LongLatDistanceNode : IPluginEvaluate
    {
        [Input("Input 1")]
        public ISpread<Vector2D> FIn1;
        [Input("Input 2")]
        public ISpread<Vector2D> FIn2;

        [Output("Output", AllowFeedback = true)]
        public ISpread<double> FOutput;
        
        public void Evaluate(int SpreadMax)
        {
            FOutput.SliceCount = SpreadMax;


            for (int i = 0; i < SpreadMax; i++)
            {
                var lon1 = FIn1[i].x;
                var lat1 = FIn1[i].y;
                var lon2 = FIn2[i].x;
                var lat2 = FIn2[i].y;

                var R = 6371e3; // metres
                var φ1 = lat1 * VMath.DegToRad;
                var φ2 = lat2 * VMath.DegToRad;
                var Δφ = (lat2 - lat1) * VMath.DegToRad;
                var Δλ = (lon2 - lon1) * VMath.DegToRad;

                var a = Math.Sin(Δφ / 2) * Math.Sin(Δφ / 2) +
                        Math.Cos(φ1) * Math.Cos(φ2) *
                        Math.Sin(Δλ / 2) * Math.Sin(Δλ / 2);
                var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

                FOutput[i] = R * c;
            }
        }
    }
}

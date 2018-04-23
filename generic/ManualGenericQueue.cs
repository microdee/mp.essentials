#region usings
using System;
using System.ComponentModel.Composition;
using mp.pddn;
using VVVV.PluginInterfaces.V1;
using VVVV.PluginInterfaces.V2;
using VVVV.Utils.VColor;
using VVVV.Utils.VMath;

using VVVV.Core.Logging;
using VVVV.PluginInterfaces.V2.NonGeneric;

#endregion usings

namespace mp.essentials.Nodes.Generic
{
	[PluginInfo(
        Name = "Queue",
        Category = "Generic",
        Version = "Manual",
        Author = "microdee"
        )]
	public class ManualGenericQueue : IPluginEvaluate, IPartImportsSatisfiedNotification
	{
	    [Import] protected IPluginHost2 FPluginHost;
	    [Import] protected IIOFactory FIOFactory;
	    [Import] protected IHDEHost Hde;

        private ConfigurableTypePinGroup _pg;
	    private bool _typeChanged;
	    private bool _pgready;
        private DiffSpreadPin _input;
	    private SpreadPin _output;
	    private SpreadPin _first;
	    private SpreadPin _last;

		[Input("Enqueue", IsSingle=true, DefaultBoolean=false)]
		public ISpread<bool> FAdd;
		[Input("Dequeue", IsSingle=true, DefaultBoolean=true)]
		public ISpread<bool> FDelete;
		[Input("Maximum", IsSingle=true, DefaultValue=-1)]
		public ISpread<int> FMax;

		[Import()]
		public ILogger FLogger;
		
		public void OnImportsSatisfied()
		{
		    _pg = new ConfigurableTypePinGroup(FPluginHost, FIOFactory, Hde.MainLoop, "Input");
		    _pg.OnTypeChangeEnd += (sender, args) =>
		    {
		        _typeChanged = true;
		        if (_pgready) return;
		        _pgready = true;

		        _input = _pg.AddInput(new InputAttribute("Input"));
		        _output = _pg.AddOutput(new OutputAttribute("Output"));
		        _first = _pg.AddOutput(new OutputAttribute("Dequeued"));
		        _last = _pg.AddOutput(new OutputAttribute("Enqueued"));
		        _output.Spread.SliceCount = 0;
		        _first.Spread.SliceCount = _last.Spread.SliceCount = 1;
		    };
		}

		//called when data for any output pin is requested
		public void Evaluate(int SpreadMax)
		{
		    if (_typeChanged) _typeChanged = false;
		    if (!_pgready) return;

		    if (FDelete[0] && _output.Spread.SliceCount > 0)
		    {
		        _first[0] = _output[0];
                _output.Spread.RemoveAt(0);
		    }
			if(FAdd[0])
			{
			    _last.Spread.SliceCount = _input.Spread.SliceCount;
			    var slc = 0;
				for (int i = 0; i < _input.Spread.SliceCount; i++)
				{
                    if (FMax[0] >= _output.Spread.SliceCount || FMax[0] < 0)
					{
					    _output.Spread.Add(_input[i]);
					    _last[i] = _input[i];
					    slc++;
					}
				}
			    _last.Spread.SliceCount = slc;

			}

            //FLogger.Log(LogType.Debug, "hi tty!");
        }
	}

    // TODO: move this to mp.pddn
    public static class TempSpreadHelper
    {
        public static void RemoveAt(this ISpread spread, int i)
        {
            if(spread.SliceCount == 0) return;
            if (spread.SliceCount == 1)
            {
                spread.SliceCount = 0;
                return;
            }
            var mi = i % spread.SliceCount;
            if(mi == spread.SliceCount - 1)
            {
                spread.SliceCount--;
                return;
            }
            for (int j = mi; j < spread.SliceCount - 1; j++)
            {
                spread[j] = spread[j + 1];
            }
            spread.SliceCount--;
        }

        public static void Add(this ISpread spread, object o)
        {
            spread.SliceCount++;
            spread[spread.SliceCount - 1] = o;
        }
    }
}

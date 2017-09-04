using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PowerArgs;
using VVVV.PluginInterfaces.V2;
using VVVV.Nodes.PDDN;
using VVVV.Utils.Linq;

namespace mp.essentials.Nodes.Shaders
{
    public class PreProcOptionsHelper
    {
        public static PreProcOptionsHelper Instance => _instance ?? (_instance = new PreProcOptionsHelper());
        private static PreProcOptionsHelper _instance;

        public string VVVVDir { get; private set; }
        public Regex RelativeInclude { get; private set; }
        public Regex AbsoluteInclude { get; private set; }
        public Regex DefinedInline { get; private set; }
        public Regex DefinedArgs { get; private set; }
        public Regex DefaultDefines { get; private set; }

        public PreProcOptionsHelper()
        {
            VVVVDir = AppDomain.CurrentDomain.BaseDirectory;
            const RegexOptions regexoptions = RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled;
            RelativeInclude = new Regex(@"^#include\s+""(?<file>.+)""\s*?$", regexoptions);
            AbsoluteInclude = new Regex(@"^#include\s+\<(?<file>.+)\>\s*?$", regexoptions);
            DefinedInline = new Regex(@"defined\((?<name>[^\(\)]*)\)", regexoptions);
            DefinedArgs = new Regex(@"\/{3}(?<args>.+)$", regexoptions);
            DefaultDefines = new Regex(@"#define\s+(?<name>[^\(\)]+?)\s+(?<value>.*?)$", regexoptions);
        }
    }
    public enum PreProcOptionType
    {
        Switch,
        Token,
        String,
        Bool,
        Int,
        Float
    }
    public class ShaderFile
    {
        public readonly List<ShaderFile> FlatShaderList = new List<ShaderFile>();
        public readonly List<PreProcOption> FlatOptions = new List<PreProcOption>();
        public string FilePath { get; private set; }
        public List<ShaderFile> Includes { get; set; } = new List<ShaderFile>();
        public List<PreProcOption> Options { get; set; } = new List<PreProcOption>();
        public ShaderFile Root { get; private set; }
        public bool IsRoot { get; private set; }

        public ShaderFile(string path, ShaderFile root = null)
        {
            if (root == null)
            {
                FlatShaderList.Clear();
                FlatOptions.Clear();
                Root = this;
                IsRoot = true;
            }
            else
            {
                IsRoot = false;
                Root = root;
            }

            FilePath = path;
            var reldir = Path.GetDirectoryName(path);
            if(reldir == null) throw new Exception("Problem with getting relative directory.");
            var content = File.ReadAllText(path);

            var inlinedefs = PreProcOptionsHelper.Instance.DefinedInline.Matches(content);
            foreach (Match match in inlinedefs)
            {
                var defname = match.Groups["name"].Value;
                if(Root.FlatOptions.Any(o => o.Name == defname)) continue;

                var linerange = content.LineRangeFromCharIndex(match.Index);
                var linelength = linerange.Item2 - linerange.Item1 + 1;
                var defargsmatch = PreProcOptionsHelper.Instance.DefinedArgs.Match(content, linerange.Item1, linelength);
                if(!defargsmatch.Success) continue;
                PreProcOptionArgs defargs;

                try
                {
                    var defargstext = Args.Convert(defargsmatch.Groups["args"].Value.Trim());
                    defargs = Args.Parse<PreProcOptionArgs>(defargstext);
                }
                catch (Exception e)
                {
                    continue;
                }

                var option = new PreProcOption
                {
                    Name = defname,
                    Value = "0",
                    Arguments = defargs,
                    OrderInGroup = 0,
                    Type = defargs.GetTypeAt(0)
                };
                if (option.Type == PreProcOptionType.Switch || option.Type == PreProcOptionType.Bool)
                {
                    option.Arguments.Pin.Min = 0;
                    option.Arguments.Pin.Max = 1;
                }

                Root.FlatOptions.Add(option);
                Options.Add(option);

                var defGroupContent = content.Remove(0, linerange.Item1);
                defGroupContent = defGroupContent.Remove(content.IndexOf("#endif", linerange.Item1, StringComparison.InvariantCultureIgnoreCase) - linerange.Item1);
                var defGroup = PreProcOptionsHelper.Instance.DefaultDefines.Matches(defGroupContent);
                int i = 0;
                foreach (Match groupmember in defGroup)
                {
                    var gdefname = groupmember.Groups["name"].Value;
                    if (gdefname == defname)
                    {
                        option.OrderInGroup = i;
                        option.Type = defargs.GetTypeAt(i);
                        option.Value = groupmember.Groups["value"].Value;
                        if (option.Type == PreProcOptionType.String) option.Value = option.Value.Trim('"');
                    }
                    else
                    {
                        if (Root.FlatOptions.Any(o => o.Name == gdefname)) continue;

                        var goption = new PreProcOption
                        {
                            Name = gdefname,
                            Value = groupmember.Groups["value"].Value,
                            Arguments = defargs,
                            OrderInGroup = i,
                            Type = defargs.GetTypeAt(i)
                        };
                        if (goption.Type == PreProcOptionType.String) goption.Value = goption.Value.Trim('"');
                        if (goption.Type == PreProcOptionType.Switch || goption.Type == PreProcOptionType.Bool)
                        {
                            goption.Arguments.Pin.Min = 0;
                            goption.Arguments.Pin.Max = 1;
                        }
                        Root.FlatOptions.Add(goption);
                        Options.Add(goption);
                    }
                    i++;
                }
            }

            var absincludes = PreProcOptionsHelper.Instance.AbsoluteInclude.Matches(content);
            foreach (Match match in absincludes)
            {
                var filename = match.Groups["file"].Value.Replace('/', '\\');
                filename = Path.Combine(PreProcOptionsHelper.Instance.VVVVDir, filename);
                if(!File.Exists(filename)) continue;
                if(Root.FlatShaderList.Any(f => f.FilePath == filename)) continue;

                var include = new ShaderFile(filename, Root);
                Includes.Add(include);
                Root.FlatShaderList.Add(include);
            }
            var relincludes = PreProcOptionsHelper.Instance.RelativeInclude.Matches(content);
            foreach (Match match in relincludes)
            {
                var filename = match.Groups["file"].Value.Replace('/', '\\');
                filename = Path.Combine(reldir, filename);
                if (!File.Exists(filename)) continue;
                if (Root.FlatShaderList.Any(f => f.FilePath == filename)) continue;

                var include = new ShaderFile(filename, Root);
                Includes.Add(include);
                Root.FlatShaderList.Add(include);
            }
        }
    }

    public class PreProcOption
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public int OrderInGroup { get; set; }
        public PreProcOptionArgs Arguments { get; set; }
        public PreProcOptionType Type { get; set; }
    }

    [ArgIgnoreCase(true)]
    [ArgExceptionBehavior(ArgExceptionPolicy.DontHandleExceptions)]
    public class PreProcOptionArgs
    {
        private PreProcOptionPinArgs _pin;

        [ArgReviver]
        public static PreProcOptionPinArgs Revive(string key, string val)
        {
            return Args.Parse<PreProcOptionPinArgs>(Args.Convert(val));
        }

        [ArgRequired]
        public string Type { get; set; }

        public PreProcOptionPinArgs Pin
        {
            get => _pin ?? (_pin = new PreProcOptionPinArgs()
            {
                Name = "",
                Visibility = PinVisibility.True,
                Min = -99999,
                Max = 99999,
                StepSize = 1
            });
            set => _pin = value;
        }

        public PreProcOptionType GetTypeAt(int i)
        {
            var types = Type.Trim('"').Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries);
            Enum.TryParse(types[i % types.Length], true, out PreProcOptionType res);
            return res;
        }
    }

    [ArgIgnoreCase(true)]
    [ArgExceptionBehavior(ArgExceptionPolicy.DontHandleExceptions)]
    public class PreProcOptionPinArgs
    {
        [ArgDefaultValue("")]
        public string Name { get; set; }

        [ArgDefaultValue(PinVisibility.True)]
        [ArgIgnoreCase(true)]
        public PinVisibility Visibility { get; set; }

        [ArgDefaultValue(-99999.0)]
        public double Min { get; set; }

        [ArgDefaultValue(99999.0)]
        public double Max { get; set; }

        [ArgDefaultValue(1.0)]
        public double StepSize { get; set; }
    }
    [PluginInfo(
        Name = "PreprocessorOptions",
        Category = "File",
        Tags = "Shader, HLSL",
        Author = "Microdee",
        Help = "Creates dynamic pins for '#if defined(...)' preprocessors",
        AutoEvaluate = true
        )]
    public class PreprocessorOptionsNode : ConfigurableDynamicPinNode<string>, IPluginEvaluate
    {
        [Import] protected IPluginHost2 FPluginHost;
        [Import] protected IIOFactory FIOFactory;

        [Config("Shader Path Config")]
        public IDiffSpread<string> FShaderPathConf;
        [Input("Defines Input")]
        public IDiffSpread<string> FDefinesInput;
        [Input("Shader Path")]
        public IDiffSpread<string> FShaderPath;

        [Output("Defines", AllowFeedback = true)]
        public ISpread<string> FDefineOut;

        protected PinDictionary Pd;
        protected ShaderFile CurrentShader;
        protected bool Invalidate = false;
        protected bool Init = true;

        protected override void PreInitialize()
        {
            this.ConfigPinCopy = FShaderPathConf;
            Pd = new PinDictionary(FIOFactory);
        }

        protected override bool IsConfigDefault()
        {
            return string.IsNullOrWhiteSpace(FShaderPathConf[0]);
        }

        protected override void OnConfigPinChanged()
        {
            FShaderPathConf.Stream.IsChanged = false;
            if (IsConfigDefault()) return;
            if (!File.Exists(FShaderPathConf[0])) FShaderPathConf[0] = FShaderPath[0];
            if (!File.Exists(FShaderPathConf[0])) return;
            CurrentShader = new ShaderFile(FShaderPathConf[0]);
            Pd.BeginInputExchange();
            foreach (var option in CurrentShader.FlatOptions)
            {
                Type pintype;
                InputAttribute pinattr;
                var pinname = option.Name;
                var validdefval = double.TryParse(option.Value, out double defval);
                if (option.Arguments.Pin != null)
                {
                    if (!string.IsNullOrWhiteSpace(option.Arguments.Pin.Name)) pinname = option.Arguments.Pin.Name;
                    pinattr = new InputAttribute(pinname)
                    {
                        Visibility = option.Arguments.Pin.Visibility,
                        MinValue = option.Arguments.Pin.Min,
                        MaxValue = option.Arguments.Pin.Max,
                        StepSize = option.Arguments.Pin.StepSize,
                        DefaultString = option.Value,
                        DefaultValue = validdefval ? defval : 0.0
                    };
                }
                else
                {
                    pinattr = new InputAttribute(pinname)
                    {
                        StepSize = 1.0,
                        DefaultString = option.Value,
                        DefaultValue = validdefval ? defval : 0.0
                    };
                }
                switch (option.Type)
                {
                    case PreProcOptionType.Bool:
                        pintype = typeof(bool);
                        break;
                    case PreProcOptionType.Switch:
                        pintype = typeof(bool);
                        break;
                    case PreProcOptionType.Token:
                        pintype = typeof(string);
                        break;
                    case PreProcOptionType.String:
                        pintype = typeof(string);
                        break;
                    case PreProcOptionType.Float:
                        pintype = typeof(double);
                        break;
                    case PreProcOptionType.Int:
                        pintype = typeof(int);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
                Pd.AddInput(pintype, pinattr, option);
                Invalidate = true;
            }
            Pd.EndInputExchange();
        }

        public void Evaluate(int SpreadMax)
        {
            if ((FShaderPath.IsChanged || Init) && !string.IsNullOrWhiteSpace(FShaderPath[0]))
            {
                FShaderPathConf[0] = FShaderPath[0];
                FShaderPathConf.Stream.IsChanged = true;
                Init = false;
                //OnConfigPinChanged();
            }
            if (Pd.InputChanged || Invalidate || FDefinesInput.IsChanged)
            {
                Invalidate = false;
                FDefineOut.SliceCount = 0;
                if (FDefinesInput.SliceCount > 0 && !string.IsNullOrWhiteSpace(FDefinesInput[0]))
                {
                    foreach (var define in FDefinesInput)
                    {
                        FDefineOut.Add(define);
                    }
                }
                foreach (var pin in Pd.InputPins.Values)
                {
                    var option = (PreProcOption) pin.CustomData;
                    if (FDefinesInput.Any(d => d.Contains(option.Name))) continue;
                    if (option.Type == PreProcOptionType.Switch)
                    {
                        if((bool)pin.Spread[0]) FDefineOut.Add(option.Name + "=1");
                        continue;
                    }
                    var val = pin.Spread[0].ToString();
                    if (option.Type == PreProcOptionType.String)
                        val = "\"" + val + "\"";
                    FDefineOut.Add(option.Name + "=" + val);
                }
            }
        }
    }
}

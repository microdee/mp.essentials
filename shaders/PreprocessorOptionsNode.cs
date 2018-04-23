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
using md.stdl.String;
using PowerArgs;
using VVVV.PluginInterfaces.V2;
using mp.pddn;
using VVVV.PluginInterfaces.V1;
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
        public string DefineExtract = "";
        public string FilePath { get; private set; }
        public string ShaderText { get; private set; }
        public List<ShaderFile> Includes { get; set; } = new List<ShaderFile>();
        public List<PreProcOption> Options { get; set; } = new List<PreProcOption>();
        public ShaderFile Root { get; private set; }
        public bool IsRoot { get; private set; }

        private void CreateOptions()
        {
            var inlinedefs = PreProcOptionsHelper.Instance.DefinedInline.Matches(ShaderText);
            foreach (Match match in inlinedefs)
            {
                var defname = match.Groups["name"].Value;
                if (Root.FlatOptions.Any(o => o.Name == defname)) continue;

                var linerange = ShaderText.LineRangeFromCharIndex(match.Index);

                var defargsmatch = PreProcOptionsHelper.Instance.DefinedArgs.Match(ShaderText, linerange.Start, linerange.Length);
                if (!defargsmatch.Success) continue;
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

                Root.DefineExtract += ShaderText.Substring(linerange.Start, linerange.Length).Trim() + "\n";

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

                var defGroupContent = ShaderText.Remove(0, linerange.Start);
                defGroupContent = defGroupContent.Remove(ShaderText.IndexOf("#endif", linerange.Start, StringComparison.InvariantCultureIgnoreCase) - linerange.Start);
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

                    var gdefline = defGroupContent.LineRangeFromCharIndex(groupmember.Groups["name"].Index);
                    Root.DefineExtract += defGroupContent.Substring(gdefline.Start, gdefline.Length).Trim() + "\n";

                    i++;
                }
                Root.DefineExtract += "#endif\n";
            }
        }

        public ShaderFile(string shadercontent)
        {
            FlatShaderList.Clear();
            FlatOptions.Clear();
            Root = this;
            IsRoot = true;
            DefineExtract = "";
            ShaderText = shadercontent;

            CreateOptions();
        }

        public ShaderFile(string path, ShaderFile root)
        {
            if (root == null)
            {
                FlatShaderList.Clear();
                FlatOptions.Clear();
                DefineExtract = "";
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
            ShaderText = File.ReadAllText(path);

            CreateOptions();

            var absincludes = PreProcOptionsHelper.Instance.AbsoluteInclude.Matches(ShaderText);
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
            var relincludes = PreProcOptionsHelper.Instance.RelativeInclude.Matches(ShaderText);
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
    public class PreprocessorOptionsNode : ConfigurableDynamicPinNode<string>, IPluginEvaluate, IPluginFeedbackLoop
    {
        [Import] protected IPluginHost2 FPluginHost;
        [Import] protected IIOFactory FIOFactory;

        [Config("Defines Extract Config")]
        public IDiffSpread<string> FDefineExtractConf;
        [Input("Defines Input")]
        public IDiffSpread<string> FDefinesInput;
        [Input("Shader Path")]
        public Pin<string> FShaderPath;

        [Output("Defines")]
        public ISpread<string> FDefineOut;

        [Output("Defines Extract")]
        public ISpread<string> FDefineExtract;

        protected PinDictionary Pd;
        protected ShaderFile CurrentShader;
        protected bool Invalidate = false;
        protected bool Init = true;

        protected override void PreInitialize()
        {
            this.ConfigPinCopy = FDefineExtractConf;
            Pd = new PinDictionary(FIOFactory);
        }

        protected override bool IsConfigDefault()
        {
            return string.IsNullOrWhiteSpace(FDefineExtractConf[0]);
        }

        protected override void Initialize()
        {
            OnShaderPathChange();
        }

        protected override void OnConfigPinChanged() { }

        protected void OnShaderPathChange()
        {
            if (FShaderPath.SliceCount > 0 && !string.IsNullOrWhiteSpace(FShaderPath[0]))
            {
                CurrentShader = File.Exists(FShaderPath[0]) ? new ShaderFile(FShaderPath[0], null) : new ShaderFile(FDefineExtractConf[0]);
            }
            else CurrentShader = new ShaderFile(FDefineExtractConf[0]);

            FDefineExtract[0] = CurrentShader.DefineExtract;
            FDefineExtractConf[0] = CurrentShader.DefineExtract;
            FDefineExtractConf.Stream.IsChanged = true;

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
                Pd.AddInput(pintype, pinattr, obj: option);
                Invalidate = true;
            }
            Pd.EndInputExchange();
        }

        public void Evaluate(int SpreadMax)
        {
            if (FShaderPath.IsChanged || Init)
            {
                OnShaderPathChange();
                Init = false;
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
                    if(pin.Spread.SliceCount == 0) continue;

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

        public bool OutputRequiresInputEvaluation(IPluginIO inputPin, IPluginIO outputPin)
        {
            return !(inputPin.Name == "Shader Path" && outputPin.Name == "Defines");
        }
    }
}

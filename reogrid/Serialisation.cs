using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

using unvell.ReoGrid;
using unvell.ReoGrid.IO;
using VVVV.Core.Logging;
using VVVV.PluginInterfaces.V2;

namespace mp.essentials.reogrid
{

    [PluginInfo(
    Name = "Writer",
    Category = "ReoGrid",
    Author = "velcrome",
    AutoEvaluate = true,
    Version = "Excel"
    )]
    public class WriterNode : IPluginEvaluate
    {
        [Input("Workbook")]
        public ISpread<IWorkbook> FWorksheet;

        [Input("Filename", IsSingle = true, StringType = StringType.Filename, DefaultString = "reogrid.xlsx")]
        public ISpread<string> FFilename;

//        [Input("Format", IsSingle = true, DefaultEnumEntry = "Excel2007")]
//        public ISpread<FileFormat> FFormat;


        [Input("Write", IsSingle = true, IsBang = true)]
        public ISpread<bool> FWrite;

        [Output("Success", IsBang = true)]
        public ISpread<bool> FSuccess;


        [Import()]
        public ILogger FLogger;

        //called when data for any output pin is requested
        public void Evaluate(int SpreadMax)
        {
            FSuccess.AssignFrom(Enumerable.Repeat(false, 1));

            if (!FWrite[0]) return;

            try
            {
                var workbook = FWorksheet[0];

//                Directory.CreateDirectory(path);
                workbook.Save(FFilename[0], FileFormat.Excel2007);

                FSuccess[0] = true;
            }
            catch (Exception e)
            {
                FLogger.Log(e, LogType.Error);
            }
        }
    }

    [PluginInfo(
    Name = "Reader",
    Category = "ReoGrid",
    Author = "velcrome",
    AutoEvaluate = true, 
    Version = "Excel"
    )]
    public class ReaderNode : IPluginEvaluate
    {
        [Input("Workbook")]
        public ISpread<IWorkbook> FWorksheet;

        [Input("Filename", IsSingle = true, StringType = StringType.Filename, DefaultString = "reogrid.xlsx")]
        public ISpread<string> FFilename;

        [Input("Load", IsSingle = true, IsBang = true)]
        public ISpread<bool> FLoad;

        [Output("Success", IsBang = true)]
        public ISpread<bool> FSuccess;
        
        [Import()]
        public ILogger FLogger;

        //called when data for any output pin is requested
        public void Evaluate(int SpreadMax)
        {
            FSuccess.AssignFrom(Enumerable.Repeat(false, 1));

            if (!FLoad[0]) return;

            try
            {
                var workbook = FWorksheet[0];
                workbook.Load(FFilename[0], FileFormat.Excel2007);

                FSuccess[0] = true;
            }
            catch (Exception e)
            {
                FLogger.Log(e, LogType.Error);
            }
        }
    }

}

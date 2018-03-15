using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using VVVV.PluginInterfaces.V2;
using OfficeOpenXml;
using OfficeOpenXml.Table;
using VVVV.Utils.VMath;

namespace mp.essentials.Nodes.Excel
{
    public abstract class AbstractOoxmlExcelReaderNode : IPluginEvaluate
    {
        protected abstract ExcelPackage CreatePackage(int i);
        protected abstract int SpreadMax();

        [Input("Parse", Order = 100, IsBang = true)]
        public IDiffSpread<bool> FParse;

        [Output("Data Reader")]
        public ISpread<ExcelPackage> FPackage;
        [Output("Workbook")]
        public ISpread<ExcelWorkbook> FWorkbook;
        [Output("Worksheets")]
        public ISpread<ISpread<ExcelWorksheet>> FWorksheets;
        [Output("Data XML")]
        public ISpread<XElement> FXmlOut;
        [Output("Total Columns/Rows", AsInt = true)]
        public ISpread<ISpread<Vector2D>> FColRows;
        [Output("Error")]
        public ISpread<string> FError;

        public void Evaluate(int spreadmax)
        {
            FPackage.Stream.IsChanged = false;
            FWorksheets.Stream.IsChanged = false;
            FXmlOut.Stream.IsChanged = false;
            if (FParse[0])
            {
                int slc = SpreadMax();
                FPackage.SliceCount =
                    FWorksheets.SliceCount =
                    FWorkbook.SliceCount =
                    FXmlOut.SliceCount =
                    FError.SliceCount =
                    FColRows.SliceCount = slc;
                for (int i = 0; i < slc; i++)
                {
                    try
                    {
                        var result = CreatePackage(i);
                        FPackage[i] = result;
                        FWorkbook[i] = result.Workbook;
                        FWorksheets[i].SliceCount = FColRows[i].SliceCount = result.Workbook.Worksheets.Count;
                        for (int j = 0; j < FWorksheets[i].SliceCount; j++)
                        {
                            FWorksheets[i][j] = result.Workbook.Worksheets[j + 1];
                            FColRows[i][j] = new Vector2D(FWorksheets[i][j].Dimension.Columns,
                                FWorksheets[i][j].Dimension.Rows);
                        }
                        FXmlOut[i] = XDocument.Parse(result.Workbook.WorkbookXml.OuterXml).Root;
                    }
                    catch (Exception e)
                    {
                        FError[i] = e.Message;
                    }
                }
                FPackage.Stream.IsChanged = true;
                FWorksheets.Stream.IsChanged = true;
                FXmlOut.Stream.IsChanged = true;
            }
        }
    }

    [PluginInfo(
        Name = "Excel",
        Category = "OoXml",
        Version = "Raw",
        Author = "microdee",
        Tags = "office, document"
    )]
    public class StreamOoXmlNode : AbstractOoxmlExcelReaderNode
    {
        [Input("Input Stream")]
        public IDiffSpread<Stream> FInStream;

        [Input("Password")]
        public IDiffSpread<string> FPassword;

        protected override ExcelPackage CreatePackage(int i)
        {
            return string.IsNullOrWhiteSpace(FPassword[i]) ?
                new ExcelPackage(FInStream[i]) :
                new ExcelPackage(FInStream[i], FPassword[i]);
        }

        protected override int SpreadMax()
        {
            return FInStream.SliceCount;
        }
    }

    [PluginInfo(
        Name = "Excel",
        Category = "OoXml",
        Version = "File",
        Author = "microdee",
        Tags = "office, document"
    )]
    public class FileOoXmlNode : AbstractOoxmlExcelReaderNode
    {
        [Input("Input File", StringType = StringType.Filename, FileMask = "*.xlsx")]
        public IDiffSpread<string> FInFile;

        [Input("Password")]
        public IDiffSpread<string> FPassword;

        protected override ExcelPackage CreatePackage(int i)
        {
            var fileinfo = new FileInfo(FInFile[i]);
            return string.IsNullOrWhiteSpace(FPassword[i]) ?
                new ExcelPackage(fileinfo) :
                new ExcelPackage(fileinfo, FPassword[i]);
        }

        protected override int SpreadMax()
        {
            return FInFile.SliceCount;
        }
    }

    public enum TextEncoding
    {
        Default,
        ASCII,
        Unicode,
        BigEndianUnicode,
        UTF32,
        UTF8,
        UTF7
    }

    [PluginInfo(
        Name = "Excel",
        Category = "OoXml",
        Version = "Csv",
        Author = "microdee",
        Tags = "office, document"
    )]
    public class CsvOoXmlNode : AbstractOoxmlExcelReaderNode
    {
        [Input("Input Csv")]
        public ISpread<ISpread<string>> FInCsv;
        [Input("Sheet Title")]
        public ISpread<ISpread<string>> FSheetTitle;
        [Input("Encoding")]
        public ISpread<TextEncoding> FEncoding;
        [Input("Delimiter", DefaultString = ",")]
        public ISpread<string> FDelimiter;
        [Input("Qualifier", DefaultString = "\"")]
        public ISpread<string> FQualifier;
        [Input("First Row is Header")]
        public ISpread<bool> FFirstRowHeader;

        Dictionary<TextEncoding, Encoding> EncodingSelector = new Dictionary<TextEncoding, Encoding>
        {
            {TextEncoding.ASCII, Encoding.ASCII },
            {TextEncoding.BigEndianUnicode, Encoding.BigEndianUnicode },
            {TextEncoding.Default, Encoding.Default },
            {TextEncoding.UTF8, Encoding.UTF8 },
            {TextEncoding.UTF7, Encoding.UTF7 },
            {TextEncoding.UTF32, Encoding.UTF32 },
            {TextEncoding.Unicode, Encoding.Unicode }
        };

        protected override ExcelPackage CreatePackage(int i)
        {
            var package = new ExcelPackage();
            for (int j = 0; j < FInCsv[i].SliceCount; j++)
            {
                var sheetname = string.IsNullOrWhiteSpace(FSheetTitle[i][j]) ? $"Sheet {j + 1}" : FSheetTitle[i][j];
                var worksheet = package.Workbook.Worksheets.Add(sheetname);
                worksheet.Cells.LoadFromText(FInCsv[i][j], new ExcelTextFormat
                {
                    Encoding = EncodingSelector[FEncoding[0]],
                    TextQualifier = FQualifier[0][0],
                    Delimiter = FDelimiter[0][0],
                    Culture = CultureInfo.InvariantCulture
                }, TableStyles.None, FFirstRowHeader[0]);
            }
            return package;
        }

        protected override int SpreadMax()
        {
            return FInCsv.SliceCount;
        }
    }
}

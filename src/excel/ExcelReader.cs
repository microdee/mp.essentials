using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Excel;
using VVVV.PluginInterfaces.V2;

namespace mp.essentials.Nodes.Excel
{
    public enum ExcelReaderMode
    {
        OpenXml_2007,
        Binary_97
    }
    [PluginInfo(
        Name = "Excel",
        Category = "Raw",
        Author = "microdee",
        Tags = "office, document"
    )]
    public class ExcelReaderNode : IPluginEvaluate
    {
        [Input("Spreadsheet Binary")]
        public IDiffSpread<Stream> FSpreadsheetIn;
        [Input("Excel Mode")]
        public IDiffSpread<ExcelReaderMode> FExcelMode;
        [Input("First Row is Column Names")]
        public IDiffSpread<bool> FFirstRowIsColumns;

        [Output("Data Reader")]
        public ISpread<IExcelDataReader> FDataReaderOut;
        [Output("Data Set")]
        public ISpread<DataSet> FDataSetOut;
        [Output("Data XML")]
        public ISpread<string> FXmlOut;
        [Output("Tables")]
        public ISpread<ISpread<DataTable>> FTables;

        public void Evaluate(int SpreadMax)
        {
            FDataReaderOut.Stream.IsChanged = false;
            FDataSetOut.Stream.IsChanged = false;
            FTables.Stream.IsChanged = false;
            if (FSpreadsheetIn.IsChanged || FExcelMode.IsChanged || FFirstRowIsColumns.IsChanged)
            {
                FDataSetOut.SliceCount = FSpreadsheetIn.SliceCount;
                FDataReaderOut.SliceCount = FSpreadsheetIn.SliceCount;
                FXmlOut.SliceCount = FSpreadsheetIn.SliceCount;
                FTables.SliceCount = FSpreadsheetIn.SliceCount;
                for (int i = 0; i < FSpreadsheetIn.SliceCount; i++)
                {
                    FDataReaderOut[i]?.Close();
                    FDataSetOut[i]?.Dispose();
                    DataSet result;
                    IExcelDataReader excelReader;
                    switch (FExcelMode[i])
                    {
                        case ExcelReaderMode.OpenXml_2007:
                            excelReader = ExcelReaderFactory.CreateOpenXmlReader(FSpreadsheetIn[i]);
                            break;
                        case ExcelReaderMode.Binary_97:
                            excelReader = ExcelReaderFactory.CreateBinaryReader(FSpreadsheetIn[i]);
                            break;
                        default:
                            excelReader = null;
                            break;
                    }
                    excelReader.IsFirstRowAsColumnNames = FFirstRowIsColumns[i];
                    result = excelReader.AsDataSet();
                    FDataReaderOut[i] = excelReader;
                    FDataSetOut[i] = result;
                    FXmlOut[i] = result.GetXml();
                    FTables[i].SliceCount = 0;
                    foreach (DataTable table in result.Tables)
                    {
                        FTables[i].Add(table);
                    }
                }
                FDataReaderOut.Stream.IsChanged = true;
                FDataSetOut.Stream.IsChanged = true;
                FTables.Stream.IsChanged = false;
            }
        }
    }

    [PluginInfo(
        Name = "Table",
        Category = "Excel",
        Author = "microdee",
        Tags = "office, document"
    )]
    public class ExcelTableNode : IPluginEvaluate
    {
        [Input("Spreadsheet Binary")]
        public IDiffSpread<DataTable> FTable;

        [Output("Rows")]
        public ISpread<ISpread<DataRow>> FRows;
        [Output("Columns")]
        public ISpread<ISpread<DataColumn>> FCols;

        public void Evaluate(int SpreadMax)
        {
            FRows.Stream.IsChanged = false;
            FCols.Stream.IsChanged = false;
            if (FTable.IsChanged)
            {
                FRows.SliceCount = FTable.SliceCount;
                FCols.SliceCount = FTable.SliceCount;
                for (int i = 0; i < FTable.SliceCount; i++)
                {
                    FRows[i].SliceCount = 0;
                    foreach (DataRow row in FTable[i].Rows)
                    {
                        FRows[i].Add(row);
                    }
                    FCols[i].SliceCount = 0;
                    foreach (DataColumn col in FTable[i].Columns)
                    {
                        FCols[i].Add(col);
                    }
                }
                FRows.Stream.IsChanged = true;
                FCols.Stream.IsChanged = true;
            }
        }
    }

    [PluginInfo(
        Name = "QueryColumn",
        Category = "Excel",
        Author = "microdee",
        Tags = "office, document"
    )]
    public class ExcelQueryColumnNode : IPluginEvaluate
    {
        [Input("Row")]
        public IDiffSpread<DataRow> FRow;
        [Input("Column Label")]
        public IDiffSpread<ISpread<string>> FCol;
        [Input("Label Row", DefaultValue = -1)]
        public IDiffSpread<int> FLabelRow;

        [Output("Result")]
        public ISpread<ISpread<string>> FResult;

        public void Evaluate(int SpreadMax)
        {
            if (FRow.IsChanged || FCol.IsChanged)
            {
                FResult.SliceCount = FRow.SliceCount;
                for (int i = 0; i < FRow.SliceCount; i++)
                {
                    if (string.IsNullOrWhiteSpace(FCol[i][0]))
                    {
                        FResult[i].SliceCount = FRow[i].ItemArray.Length;
                        for (int j = 0; j < FResult[i].SliceCount; j++)
                        {
                            FResult[i][j] = FRow[i][j].ToString();
                        }
                    }
                    else
                    {
                        FResult[i].SliceCount = FCol[i].SliceCount;
                        for (int j = 0; j < FResult[i].SliceCount; j++)
                        {

                            if (FLabelRow[i] < 0)
                            {
                                int colnum = 0;
                                foreach (DataColumn column in FRow[i].Table.Columns)
                                {
                                    if (column.ColumnName == FCol[i][j])
                                    {
                                        FResult[i][j] = FRow[i][colnum].ToString();
                                        break;
                                    }
                                    colnum++;
                                }
                            }
                            else
                            {
                                int colnum = 0;
                                foreach (object column in FRow[i].Table.Rows[FLabelRow[i]].ItemArray)
                                {
                                    if (column.ToString() == FCol[i][j])
                                    {
                                        FResult[i][j] = FRow[i][colnum].ToString();
                                        break;
                                    }
                                    colnum++;
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}

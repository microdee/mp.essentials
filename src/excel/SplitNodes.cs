using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OfficeOpenXml;
using VVVV.Nodes.PDDN;
using VVVV.PluginInterfaces.V2;

namespace mp.essentials.Nodes.Excel
{
    [PluginInfo(
        Name = "Expand",
        Category = "OoXml",
        Author = "microdee",
        Version = "ExcelRangeBase",
        Tags = "office, document, split"
    )]
    public class ExcelRangeSplitNode : ObjectSplitNode<ExcelRangeBase> { }

    [PluginInfo(
        Name = "Expand",
        Category = "OoXml",
        Author = "microdee",
        Version = "ExcelAddressBase",
        Tags = "office, document, split"
    )]
    public class ExcelAddressBaseSplitNode : ObjectSplitNode<ExcelAddressBase> { }

    [PluginInfo(
        Name = "Expand",
        Category = "OoXml",
        Author = "microdee",
        Version = "ExcelWorksheet",
        Tags = "office, document, split"
    )]
    public class ExcelWorksheetSplitNode : ObjectSplitNode<ExcelWorksheet> { }

    [PluginInfo(
        Name = "Expand",
        Category = "OoXml",
        Author = "microdee",
        Version = "ExcelWorkbook",
        Tags = "office, document, split"
    )]
    public class ExcelWorkbookSplitNode : ObjectSplitNode<ExcelWorkbook> { }

    [PluginInfo(
        Name = "Expand",
        Category = "OoXml",
        Author = "microdee",
        Version = "ExcelPackage",
        Tags = "office, document, split"
    )]
    public class ExcelPackageSplitNode : ObjectSplitNode<ExcelPackage> { }
}

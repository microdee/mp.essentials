using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using unvell.ReoGrid;
using unvell.ReoGrid.CellTypes;
using unvell.ReoGrid.IO;
using VVVV.PluginInterfaces.V2;

namespace mp.essentials.reogrid
{
    public partial class SpreadsheetEditorNode
    {
        public string Base64Save()
        {
            var res = "";

            foreach (var sheet in Grid.Worksheets)
            {
                if (res != "") res += ";";
                res += $"{sheet.Name}:";

                var datastream = new MemoryStream();
                sheet.SaveRGF(datastream);
                datastream.Position = 0;
                var databytes = new byte[datastream.Length];
                datastream.Read(databytes, 0, (int)datastream.Length);
                var xmltext = Encoding.UTF8.GetString(databytes);
                var xel = XElement.Parse(xmltext);

                var metael = xel.XPathSelectElement("//head/meta");
                metael?.SetElementValue(XName.Get("decimalchar"), CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator);

                AddWorksheetData(sheet, data => data.Xml = xel);
                
                var wsxd = _wsxdata[sheet];
                foreach (var element in xel.XPathSelectElements("//cell[@body-type]"))
                {
                    var btxattr = element.Attribute(XName.Get("body-type"));
                    if (btxattr == null) continue;

                    var rc = ParseRowCol(element);
                    if (rc.r < 0 || rc.c < 0) continue;

                    var cell = sheet.Cells[rc.r, rc.c];

                    if (btxattr.Value.Equals("RadioButtonCell"))
                    {
                        RangePosition crange = RangePosition.Empty;
                        foreach (var range in wsxd.RadioGroups.Keys)
                        {
                            if (range.Contains(rc.r, rc.c))
                            {
                                crange = range;
                                break;
                            }
                        }
                        if (crange == RangePosition.Empty) continue;
                        element.SetAttributeValue(XName.Get("radio-group-range"), crange.ToAddress());
                    }
                    if (btxattr.Value.Equals("DropdownListCell"))
                    {
                        if (!wsxd.EnumCells.ContainsKey(rc)) continue;
                        element.SetAttributeValue(XName.Get("enum-name"), wsxd.EnumCells[rc]);
                    }
                }

                res += Convert.ToBase64String(Encoding.UTF8.GetBytes(xel.ToString()));
            }
            return res;
        }

        public void Base64Load(string data)
        {
            var sheets = data.Split(';');

            foreach (var sheetdata in sheets)
            {
                var nameAndSheet = sheetdata.Split(':');
                var name = nameAndSheet[0];
                var sheetbase64 = nameAndSheet[1];
                var sheet = Grid.CreateWorksheet(name);
                var databytes = Convert.FromBase64String(sheetbase64);

                var xmltext = Encoding.UTF8.GetString(databytes);
                var xel = XElement.Parse(xmltext);

                AddWorksheetData(sheet, xdata => xdata.Xml = xel);

                var xml = new XmlDocument();
                xml.LoadXml(xmltext);

                var origdecsep = xml["grid"]?["head"]?["meta"]?["decimalchar"]?.InnerText;
                if (origdecsep == null)
                {
                    var origcultureid = xml["grid"]?["head"]?["meta"]?["culture"]?.InnerText ?? "en-US";
                    var origculture = new CultureInfo(origcultureid);
                    origdecsep = origculture.NumberFormat.NumberDecimalSeparator;
                }

                ConvertCulture(xml, origdecsep);

                databytes = Encoding.UTF8.GetBytes(xml.InnerXml);

                var datastream = new MemoryStream(databytes);
                sheet.LoadRGF(datastream);
                Grid.AddWorksheet(sheet);

                var wsxd = _wsxdata[sheet];

                foreach (var element in xel.XPathSelectElements("//cell[@body-type]"))
                {
                    var btxattr = element.Attribute(XName.Get("body-type"));
                    if (btxattr == null) continue;

                    if (btxattr.Value.Equals("VerticalProgressCell"))
                    {
                        SetCellBodyFromXml(sheet, element, new VerticalProgressCell());
                    }
                    if (btxattr.Value.Equals("HorizontalProgressCell"))
                    {
                        SetCellBodyFromXml(sheet, element, new HorizontalProgressCell());
                    }

                    if (btxattr.Value.Equals("RadioButtonCell"))
                    {
                        var rc = ParseRowCol(element);
                        var celldata = sheet.Cells[rc.r, rc.c].Data?
                            .ToString()
                            .Equals("True", StringComparison.InvariantCultureIgnoreCase) ?? false;

                        var button = new RadioButtonCell
                        {
                            IsChecked = celldata
                        };

                        var groupattr = element.Attribute(XName.Get("radio-group-range"));
                        if (groupattr != null && RangePosition.IsValidAddress(groupattr.Value))
                        {
                            var range = new RangePosition(groupattr.Value);
                            if (wsxd.RadioGroups.ContainsKey(range))
                            {
                                button.RadioGroup = wsxd.RadioGroups[range];
                            }
                            else
                            {
                                var radiogroup = new RadioButtonGroup();
                                button.RadioGroup = radiogroup;
                                wsxd.RadioGroups.Add(range, radiogroup);
                            }
                        }
                        button.Click += (sender, eventArgs) => SaveChanges();
                        SetCellBodyFromXml(sheet, element, button);
                    }
                    if (btxattr.Value.Equals("CheckBoxCell"))
                    {
                        var rc = ParseRowCol(element);
                        var celldata = sheet.Cells[rc.r, rc.c].Data?
                            .ToString()
                            .Equals("True", StringComparison.InvariantCultureIgnoreCase) ?? false;

                        var button = new CheckBoxCell
                        {
                            IsChecked = celldata
                        };
                        button.Click += (sender, eventArgs) => SaveChanges();
                        SetCellBodyFromXml(sheet, element, button);
                    }
                    if (btxattr.Value.Equals("DropdownListCell"))
                    {
                        var enumattr = element.Attribute(XName.Get("enum-name"));
                        if (enumattr != null)
                        {
                            var rc = ParseRowCol(element);
                            if (rc.r >= 0 && rc.c >= 0)
                            {
                                var button = new DropdownListCell(GetEnumEntries(enumattr.Value));
                                button.SelectedItemChanged += (sender, args) => SaveChanges();
                                sheet.Cells[rc.r, rc.c].Body = button;
                                if (wsxd.EnumCells.ContainsKey(rc)) wsxd.EnumCells[rc] = enumattr.Value;
                                else wsxd.EnumCells.Add(rc, enumattr.Value);
                            }
                        }
                    }

                    var formulael = element.Element(XName.Get("formula"));
                    if (formulael != null)
                    {
                        var rc = ParseRowCol(element);
                        if (rc.r >= 0 && rc.c >= 0)
                        {
                            sheet.SetCellFormula(rc.r, rc.c, formulael.Value);
                        }
                    }
                }
            }
        }

        public (int r, int c) ParseRowCol(XElement cellel)
        {
            var rowattr = cellel.Attribute(XName.Get("row"));
            var colattr = cellel.Attribute(XName.Get("col"));
            if (rowattr == null || colattr == null) return (-1, -1);
            var row = int.Parse(rowattr.Value);
            var col = int.Parse(colattr.Value);
            return (row, col);
        }

        public void SetCellBodyFromXml(Worksheet sheet, XElement cellel, CellBody body)
        {
            var rc = ParseRowCol(cellel);
            if(rc.r < 0 || rc.c < 0) return;
            sheet.Cells[rc.r, rc.c].Body = body;
        }

        public void ConvertCulture(XmlNode node, string decsep)
        {
            var ccds = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;

            if (node is XmlText textnode)
            {
                if(Regex.IsMatch(textnode.Value, $"^\\d*?\\{decsep}\\d*?$"))
                {
                    textnode.Value = textnode.Value.Replace(decsep, ccds);
                }
            }

            if (node.Attributes != null)
            {
                for (int i = 0; i < node.Attributes.Count; i++)
                {
                    var attr = node.Attributes[i];
                    if (attr.Name == "font-size") continue;

                    if (Regex.IsMatch(attr.Value, $"^\\d*?\\{decsep}\\d*?$"))
                    {
                        attr.Value = attr.Value.Replace(decsep, ccds);
                    }
                }
            }

            for (int i = 0; i < node.ChildNodes.Count; i++)
            {
                ConvertCulture(node.ChildNodes[i], decsep);
            }
        }
    }

    public static class Styles
    {
        public static ControlAppearanceStyle VvvvStyle()
        {
            var rgcs = new ControlAppearanceStyle();
            rgcs[ControlAppearanceColors.LeadHeadNormal] = Color.FromArgb(255, 238, 238, 238);
            rgcs[ControlAppearanceColors.LeadHeadHover] = Color.FromArgb(255, 208, 208, 208);
            rgcs[ControlAppearanceColors.LeadHeadSelected] = Color.FromArgb(255, 0, 0, 0);
            rgcs[ControlAppearanceColors.LeadHeadIndicatorStart] = Color.FromArgb(0, 0, 0, 0);
            rgcs[ControlAppearanceColors.LeadHeadIndicatorEnd] = Color.FromArgb(0, 0, 0, 0);
            rgcs[ControlAppearanceColors.ColHeadSplitter] = Color.FromArgb(255, 0, 0, 0);
            rgcs[ControlAppearanceColors.ColHeadNormalStart] = Color.FromArgb(255, 238, 238, 238);
            rgcs[ControlAppearanceColors.ColHeadNormalEnd] = Color.FromArgb(255, 238, 238, 238);
            rgcs[ControlAppearanceColors.ColHeadHoverStart] = Color.FromArgb(255, 238, 238, 238);
            rgcs[ControlAppearanceColors.ColHeadHoverEnd] = Color.FromArgb(255, 238, 238, 238);
            rgcs[ControlAppearanceColors.ColHeadSelectedStart] = Color.FromArgb(255, 120, 120, 120);
            rgcs[ControlAppearanceColors.ColHeadSelectedEnd] = Color.FromArgb(255, 120, 120, 120);
            rgcs[ControlAppearanceColors.ColHeadFullSelectedStart] = Color.FromArgb(255, 0, 0, 0);
            rgcs[ControlAppearanceColors.ColHeadFullSelectedEnd] = Color.FromArgb(255, 0, 0, 0);
            rgcs[ControlAppearanceColors.ColHeadInvalidStart] = Color.FromArgb(255, 255, 255, 255);
            rgcs[ControlAppearanceColors.ColHeadInvalidEnd] = Color.FromArgb(255, 255, 255, 255);
            rgcs[ControlAppearanceColors.ColHeadText] = Color.FromArgb(255, 208, 208, 208);
            rgcs[ControlAppearanceColors.RowHeadSplitter] = Color.FromArgb(255, 208, 208, 208);
            rgcs[ControlAppearanceColors.RowHeadNormal] = Color.FromArgb(255, 238, 238, 238);
            rgcs[ControlAppearanceColors.RowHeadHover] = Color.FromArgb(255, 182, 182, 182);
            rgcs[ControlAppearanceColors.RowHeadSelected] = Color.FromArgb(255, 120, 120, 120);
            rgcs[ControlAppearanceColors.RowHeadFullSelected] = Color.FromArgb(255, 0, 0, 0);
            rgcs[ControlAppearanceColors.RowHeadInvalid] = Color.FromArgb(255, 255, 255, 255);
            rgcs[ControlAppearanceColors.RowHeadText] = Color.FromArgb(255, 208, 208, 208);
            rgcs[ControlAppearanceColors.SelectionBorder] = Color.FromArgb(180, 145, 145, 145);
            rgcs[ControlAppearanceColors.SelectionFill] = Color.FromArgb(30, 120, 120, 120);
            rgcs[ControlAppearanceColors.GridBackground] = Color.FromArgb(255, 255, 255, 255);
            rgcs[ControlAppearanceColors.GridText] = Color.FromArgb(255, 0, 0, 0);
            rgcs[ControlAppearanceColors.GridLine] = Color.FromArgb(255, 208, 208, 208);
            rgcs[ControlAppearanceColors.OutlinePanelBorder] = Color.FromArgb(255, 208, 208, 208);
            rgcs[ControlAppearanceColors.OutlinePanelBackground] = Color.FromArgb(255, 255, 255, 255);
            rgcs[ControlAppearanceColors.OutlineButtonBorder] = Color.FromArgb(255, 208, 208, 208);
            rgcs[ControlAppearanceColors.OutlineButtonText] = Color.FromArgb(180, 94, 94, 94);
            rgcs[ControlAppearanceColors.SheetTabBorder] = Color.FromArgb(255, 208, 208, 208);
            rgcs[ControlAppearanceColors.SheetTabBackground] = Color.FromArgb(255, 255, 255, 255);
            rgcs[ControlAppearanceColors.SheetTabText] = Color.FromArgb(255, 0, 0, 0);
            rgcs[ControlAppearanceColors.SheetTabSelected] = Color.FromArgb(255, 255, 255, 255);
            rgcs.SelectionBorderWidth = 3;
            return rgcs;
        }
    }
}

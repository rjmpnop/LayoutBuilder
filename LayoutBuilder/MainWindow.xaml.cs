using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;

namespace LayoutBuilder
{
    public partial class MainWindow : Window
    {
        string[] arr_sLangFileNames; //все файлы языков
        List<string> list_sLayouts = new List<string>() { "ansi104", "iso105" };
        List<string> list_sBorderedKeys = new List<string>() { "SC1E", "SC1F", "SC20", "SC21", "SC24", "SC25", "SC26", "SC27" };
        Dictionary<string, XElement> dictSC_Lays;
        Dictionary<string, Dictionary<string, string>> dictVCodes_Symbols;


        public MainWindow()
        {
            InitializeComponent();
            arr_sLangFileNames = Directory.GetFiles("xml/langs");
            string sLang;
            foreach (string s in arr_sLangFileNames)
            {
                sLang = s.Split(new string[] { "\\" }, StringSplitOptions.None)[1];
                sLang = sLang.Replace(".xml", "");
                lbxLangs.Items.Add(sLang);
            }
        }

        private void InflateScanCodesAndLays()
        {
            //цикл по выбранным файлам языков ENG FRA
            foreach (string sLang in lbxLangs.SelectedItems)
            {
                //цикл по раскладкам ANSI ISO
                foreach (string sLayout in list_sLayouts)
                {
                    //загрузить файл раскладки
                    XDocument xDoc = XDocument.Load("xml/" + sLayout + ".xml");
                    XElement xelmSettings = xDoc.Element("settings");
                    //словарь dictSC_Lays для текущей раскладки
                    dictSC_Lays = new Dictionary<string, XElement>();
                    foreach (XElement xelmElement in xelmSettings.Elements())
                    {
                        dictSC_Lays.Add(xelmElement.Name.LocalName, xelmElement);
                    }

                    //загрузить файл языка                    
                    dictVCodes_Symbols = new Dictionary<string, Dictionary<string, string>>();
                    xDoc = XDocument.Load("xml/langs/" + sLang + ".xml");
                    foreach (var item in dictSC_Lays)
                    {
                        var element = (from m in xDoc.Root.Descendants("PK")
                                       where ("SC" + (string)(m.Attribute("SC"))) == item.Key
                                       select m).First();

                        string sVCode = element.Attribute("VK").Value;
                        Dictionary<string, string> dict = new Dictionary<string, string>();
                        dict.Add("SC", item.Key);
                        dict.Add("normal", (string)element.Element("Result").Attribute("Text"));
                        dict.Add("shift", (string)(from m in element.Elements()
                                                   where (string)m.Attribute("With") == "VK_SHIFT"
                                                   select m.Attribute("Text")).FirstOrDefault());
                        dict.Add("control_menu", (string)(from m in element.Elements()
                                                          where (string)m.Attribute("With") == "VK_CONTROL VK_MENU"
                                                          select m.Attribute("Text")).FirstOrDefault());

                        dict.Add("dead_normal", (string)(from m in element.Descendants("DeadKeyTable")
                                                         where !m.Parent.HasAttributes
                                                         select m.Attribute("Accent")).FirstOrDefault());
                        dict.Add("dead_shift", (string)(from m in element.Descendants("DeadKeyTable")
                                                         where (string)m.Parent.Attribute("With") == "VK_SHIFT"
                                                         select m.Attribute("Accent")).FirstOrDefault());
                        dict.Add("dead_control_menu", (string)(from m in element.Descendants("DeadKeyTable")
                                                        where (string)m.Parent.Attribute("With") == "VK_CONTROL VK_MENU"
                                                               select m.Attribute("Accent")).FirstOrDefault());
                        dictVCodes_Symbols.Add(sVCode, dict);
                    }

                    XDocument xDocOut = new XDocument
                    (
                       new XDeclaration("1.0", "utf-8", null),
                       new XElement("settings")
                    );

                    foreach (var item in dictVCodes_Symbols)
                    {
                        var elemMain = new XElement(item.Key);
                        var elemShow = new XElement("show", "true");
                        var elemKey = new XElement("key",
                                                      new XElement("left", dictSC_Lays[item.Value["SC"]].Element("left").Value),
                                                      new XElement("top", dictSC_Lays[item.Value["SC"]].Element("top").Value),
                                                      new XElement("width", dictSC_Lays[item.Value["SC"]].Element("width").Value),
                                                      new XElement("height", dictSC_Lays[item.Value["SC"]].Element("height").Value),
                                                      new XElement("rotation", dictSC_Lays[item.Value["SC"]].Element("rotation").Value)
                                                      );
                        var elemBorder = new XElement("border",
                                                               new XElement("show", list_sBorderedKeys.Contains(item.Value["SC"]) ? "true" : "false"),
                                                               new XElement("thickness", txtBorderThickness.Text),
                                                               new XElement("radius", txtBorderRadius.Text),
                                                               new XElement("red", txtBorderRed.Text),
                                                               new XElement("green", txtBorderGreen.Text),
                                                               new XElement("blue", txtBorderBlue.Text),
                                                               new XElement("background",
                                                                                         new XElement("show", "false"),
                                                                                         new XElement("red", txtBackgroundRed.Text),
                                                                                         new XElement("green", txtBackgroundGreen.Text),
                                                                                         new XElement("blue", txtBackgroundBlue.Text)
                                                                           )
                                                     );
                        elemMain.Add(elemShow);
                        elemMain.Add(elemKey);
                        elemMain.Add(elemBorder);

                        foreach (var symbol in item.Value)
                        {
                            if (symbol.Key == "SC") continue;
                            if (symbol.Value is null) continue;

                            var list_sModificators = new List<string>()
                            {
                                "normal",
                                "shift",
                                "control_menu",
                                "dead_normal",
                                "dead_shift",
                                "dead_control_menu"
                            };
                            int i = list_sModificators.IndexOf(symbol.Key);
                            string sModificator = symbol.Key;
                            if (i == 3)
                            {
                                i = 0;
                                sModificator = "normal";
                            }
                            if (i == 4)
                            {
                                i = 1;
                                sModificator = "shift";
                            }
                            if (i == 5)
                            {
                                i = 2;
                                sModificator = "control_menu";
                            }
                            var elemSymbol = new XElement("symbol", new XAttribute("modificator", sModificator),
                                                                   new XElement("show", "true"),
                                                                   new XElement("unicode", dictVCodes_Symbols[item.Key][symbol.Key]),
                                                                   new XElement("left", txtSymbolLeft.Text.Split('/')[i]),
                                                                   new XElement("top", txtSymbolTop.Text.Split('/')[i]),
                                                                   new XElement("size", txtSymbolSize.Text.Split('/')[i]),
                                                                   new XElement("font", txtSymbolFontName.Text),
                                                                   new XElement("weight", txtSymbolFontWeight.Text),
                                                                   new XElement("red", txtSymbolRed.Text),
                                                                   new XElement("green", txtSymbolGreen.Text),
                                                                   new XElement("blue", txtSymbolBlue.Text)
                                                         );
                            elemMain.Add(elemSymbol);
                        }
                        xDocOut.Element("settings").Add(elemMain);
                    }

                    foreach (var elem in xDocOut.Element("settings").Elements())
                    {
                        string sNormalSymbol = (from p in elem.Elements("symbol")
                                                where
                                                        (string)p.Attribute("modificator") == "normal"
                                                select p.Element("unicode").Value).FirstOrDefault();

                        string sShiftSymbol = (from p in elem.Elements("symbol")
                                               where
                                                       (string)p.Attribute("modificator") == "shift"
                                               select p.Element("unicode").Value).FirstOrDefault();
                        if (sNormalSymbol == null || sShiftSymbol == null) continue;
                        if (sNormalSymbol.ToUpper() == sShiftSymbol)
                        {
                            //(from p in elem.Elements("symbol")
                            // where
                            //         (string)p.Attribute("modificator") == "shift"
                            // select p).Remove();
                            elem.Elements("symbol")
                           .Where(r => (string)r.Attribute("modificator").Value == "shift")
                           .Remove();
                        }
                    }
                    xDocOut.Save("xml/out/" + sLayout + "_" + sLang + ".xml");
                }
            }
            
        }

        private void btnBuild_Click(object sender, RoutedEventArgs e)
        {
            InflateScanCodesAndLays();
        }
    }
}



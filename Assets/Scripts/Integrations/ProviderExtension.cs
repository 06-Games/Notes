﻿using HtmlAgilityPack;
using System.IO;
using System.Linq;
using UnityEngine;

namespace Integrations
{
    public static class ProviderExtension
    {
        static string[] whiteList = new[] { "Auth" };
        public static bool TryGetModule<T>(this Provider provider, out T module)
        {
            module = GetModule<T>(provider);
            return !module?.Equals(default) ?? false;
        }
        public static T GetModule<T>(this Provider provider)
        {
            var moduleName = typeof(T).ToString().Substring("Integrations.".Length);
            if (!whiteList.Contains(moduleName) && (!Manager.Data.ActiveChild.modules?.Contains(moduleName) ?? false)) return default;
            try { return (T)provider; }
            catch { return default; }
        }

        public static string HtmlToRichText(string Html)
        {
            var htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(Html);
            return AnalyseNode(htmlDoc.DocumentNode) ?? "";

            string AnalyseNode(HtmlNode node)
            {
                string NodeName(HtmlNode n) => n.Name.ToLower();
                var result = new System.Text.StringBuilder();
                if (!node.HasChildNodes) return System.Net.WebUtility.HtmlDecode(node.InnerText);
                foreach (var item in node.ChildNodes)
                {
                    var itemName = NodeName(item);
                    if (itemName == "p" || itemName == "div") result.AppendLine(AnalyseNode(item));
                    else if (itemName == "strong" || itemName == "b") result.Append($"<b>{AnalyseNode(item)}</b>");
                    else if (itemName == "em" || itemName == "i") result.Append($"<i>{AnalyseNode(item)}</i>");
                    else if (itemName == "u") result.Append($"<u>{AnalyseNode(item)}</u>");
                    else if (itemName == "a") result.Append($"<link={item.Attributes["href"].Value}>{AnalyseNode(item)}</link>");
                    else if (itemName == "br") result.AppendLine("");
                    else if (itemName == "ul") result.Append(AnalyseNode(item));
                    else if (itemName == "li") result.AppendLine($"• {AnalyseNode(item)}");
                    else if (itemName == "span" || itemName == "body")
                    {
                        string style = item.Attributes["style"]?.Value.ToLower() ?? "";
                        if (style.StartsWith("font-size:"))
                        {
                            var value = style.Substring("font-size:".Length).TrimStart(' ').TrimEnd(';');
                            var medium = 20F;
                            var parsedValue = medium;
                            if (value.EndsWith("px")) parsedValue = float.TryParse(value.Replace("px", ""), out var vFloat) ? vFloat : medium;
                            else if (value == "xx-small") parsedValue = 0.6F * medium;
                            else if (value == "x-small") parsedValue = 0.75F * medium;
                            else if (value == "small") parsedValue = 8F / 9F * medium;
                            else if (value == "medium") parsedValue = 1F * medium;
                            else if (value == "large") parsedValue = 1.2F * medium;
                            else if (value == "x-large") parsedValue = 1.5F * medium;
                            else if (value == "xx-large") parsedValue = 2F * medium;
                            result.Append($"<size={parsedValue}>{AnalyseNode(item)}</size>");
                        }
                        else if (style.StartsWith("color:"))
                        {
                            var value = style.Substring("color:".Length).TrimStart(' ').TrimEnd(';');
                            if (value.StartsWith("rgb("))
                                value = "#" + string.Join("", value.Substring("rgb(".Length).TrimEnd(')').Replace(" ", "").Split(',').Select(d => byte.TryParse(d, out var b) ? b.ToString("X2") : "00"));
                            else if (value.StartsWith("rgba("))
                                value = "#" + string.Join("", value.Substring("rgba(".Length).TrimEnd(')').Split(',').Cast<byte>().Select(d => d.ToString("X2")));
                            result.Append($"<color={value}>{AnalyseNode(item)}</color>");
                        }
                        else result.Append(AnalyseNode(item));
                    }
                    else if (itemName == "table")
                    {
                        try
                        {
                            var ITEM = item.FirstChild;
                            if (ITEM.Name != "tr") ITEM = ITEM.FirstChild;
                            var cellNumber = ITEM.ChildNodes.Sum(c => int.TryParse(c.Attributes["colspan"]?.Value, out var colspan) ? colspan : 1);

                            var firstLine = true;
                            var tableLenght = 150;
                            var cellSize = (tableLenght - cellNumber - 1) / cellNumber;
                            tableLenght = cellSize * cellNumber + cellNumber + 1; //Adjust table size

                            result.AppendLine("<font=\"Courier New SDF\"><size=60%>");
                            result.AppendLine($"┌{new string('─', tableLenght - 2)}┐");
                            foreach (var child in item.ChildNodes)
                            {
                                var childName = NodeName(child);
                                bool thead = childName == "thead";
                                foreach (var row in new[] { "thead", "tbody", "tfoot" }.Contains(childName) ? child.ChildNodes : item.ChildNodes)
                                {
                                    var cells = row.ChildNodes.Select(cell =>
                                    {
                                        int.TryParse(cell.Attributes["colspan"]?.Value, out var colspan);
                                        var _cellSize = cellSize * (colspan > 1 ? colspan : 1);

                                        var innerText = System.Net.WebUtility.HtmlDecode(cell.InnerText);

                                        var startWhite = whiteSpaces((_cellSize - innerText.Length) / 2);
                                        var text = AnalyseNode(cell).Replace("\n", "");
                                        var endWhite = whiteSpaces(_cellSize - startWhite.Length - innerText.Length);

                                        return $"{startWhite}{(thead ? "<b>" : "")}{text}{(thead ? "</b>" : "")}{endWhite}";

                                        string whiteSpaces(float size) => new string(' ', size < 0 ? 0 : UnityEngine.Mathf.FloorToInt(size));
                                    });
                                    var line = $"│{string.Join("│", cells)}│";
                                    if (!firstLine) result.AppendLine($"├{new string('─', tableLenght - 2)}┤");
                                    result.AppendLine(line);
                                    firstLine = false;
                                }
                            }
                            result.AppendLine($"└{new string('─', tableLenght - 2)}┘");
                            result.Append("</font><size=100%>");
                        }
                        catch (System.Exception e)
                        {
                            Logging.Log("Experimental display of tables failed with the following error\n\n" + e + "\n\n At: " + item.OuterHtml, UnityEngine.LogType.Error);
                            result.Append(AnalyseNode(item));
                        }
                    }
                    else if (itemName.StartsWith("o:")) result.Append(AnalyseNode(item)); //MS Office related tag
                    else if (item.NodeType == HtmlNodeType.Text) result.Append(AnalyseNode(item));
                    else if (item.NodeType == HtmlNodeType.Comment) return "";
                    else
                    {
                        Logging.Log("Unknown HTML element: " + itemName + "\nAt: " + item.OuterHtml, UnityEngine.LogType.Warning);
                        result.Append(AnalyseNode(item));
                    }
                }
                return result.ToString();
            }
        }
        public static string RemoveEmptyLines(string lines) => lines == null ? "" : System.Text.RegularExpressions.Regex.Replace(lines, @"^\s*$\n|\r", string.Empty, System.Text.RegularExpressions.RegexOptions.Multiline).TrimEnd();


        public static System.Collections.IEnumerator DownloadDoc(UnityEngine.Networking.UnityWebRequest request, Data.Document doc) => DownloadDoc(request, DocPath(doc));
        public static System.Collections.IEnumerator DownloadDoc(UnityEngine.Networking.UnityWebRequest request, FileInfo path)
        {
            request.SendWebRequest();
            while (!request.isDone)
            {
                Manager.UpdateLoadingStatus("homeworks.downloading", "Downloading: [0]%", false, (request.downloadProgress * 100).ToString("0"));
                yield return new WaitForEndOfFrame();
            }
            if (request.error == null) Manager.HideLoadingPanel();
            else { Manager.FatalErrorDuringLoading("Error downloading file", request.error); yield break; }

            File.WriteAllBytes(path.FullName, request.downloadHandler.data);
            OpenDoc(path);
        }
        public static FileInfo DocPath(Data.Document doc)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            var path = "/storage/emulated/0/Download/Suivi-Scolaire/";
#else
            var path = Application.temporaryCachePath + Path.DirectorySeparatorChar;
#endif
            if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            return new FileInfo(path + doc.name);
        }
        public static void OpenDoc(FileInfo path)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            UnityAndroidOpenUrl.AndroidOpenUrl.OpenFile(path.FullName);
#elif UNITY_IOS && !UNITY_EDITOR
            drstc.DocumentHandler.DocumentHandler.OpenDocument(path.FullName);
#else
            Application.OpenURL(path.FullName);
#if !UNITY_STANDALONE && !UNITY_EDITOR
            Debug.LogWarning($"Unsupported platform ({Application.platform}), we are unable to certify that the opening worked. The file has been saved at \"{path.FullName}\"");
#endif
#endif
        }

        public static void GenerateSubjectColors()
        {
            var rnd = new System.Random();
            var colorPalette = new System.Collections.Generic.List<Color32> {
                new Color32(100, 140, 200, 255), // #648CC8: Blue
                new Color32(165, 170, 190, 255), // #A5AABE: Gray
                new Color32(112, 162, 136, 255), // #70A288: Green
                new Color32(218, 183, 133, 255), // #DAB785: Gold Ravenne
                new Color32(213, 137, 111, 255), // #D5896F: Dark Salmon
                new Color32(255, 188, 010, 255), // #FFBC0A: Orange Creamsicle
                new Color32(174, 118, 166, 255), // #AE76A6: Purple
                new Color32(204, 214, 235, 255), // #CCD6EB: Very Light Blue
                new Color32(163, 195, 217, 255), // #A3C3D9: Cerulean Pigeon Post (Sky blue)
                new Color32(170, 229, 153, 255), // #AAE599: Pale Green
                new Color32(223, 229, 192, 255), // #DFE5C0: Light Oatmeal
                new Color32(202, 175, 234, 255), // #CAAFEA: Pale Purple
                new Color32(249, 215, 194, 255), // #F9D7C2: Peachy Creaminess
                new Color32(217, 244, 146, 255), // #D9F492: Pale Light Green
                new Color32(241, 227, 243, 255)  // #F1E3F3: Light Purple
            };
            var defaultColor = colorPalette.First();

            foreach (var subject in Manager.Data.ActiveChild.Subjects.Where(s => s.color != Color.black)) colorPalette.Remove(subject.color);
            foreach (var subject in Manager.Data.ActiveChild.Subjects.Where(s => s.color == Color.black))
            {
                var index = rnd.Next(colorPalette.Count);
                subject.color = index < colorPalette.Count ? colorPalette[index] : defaultColor;
                colorPalette.Remove(subject.color);
            }
        }
    }
}

﻿using System.Collections.Generic;
using System.IO;
using System.Linq;
using Tools;

public class LangueAPI
{
    /// <summary> Get a text in the current language </summary>
    /// <param name="id">ID of the text</param>
    /// <param name="dontExists">Text to display if the ID doesn't exists</param>
    public static string Get(string id, string dontExists) { return Get(id, dontExists, new string[0]); }
    /// <summary> Get a text in the current language </summary>
    /// <param name="id">ID of the text</param>
    /// <param name="dontExists">Text to display if the ID doesn't exists</param>
    /// <param name="arg">Text parsing arguments</param>
    public static string Get(string id, string dontExists, params double[] arg) { return Get(id, dontExists, arg.Select(x => x.ToString()).ToArray()); }
    /// <summary> Get a text in the current language </summary>
    /// <param name="id">ID of the text</param>
    /// <param name="dontExists">Text to display if the ID doesn't exists</param>
    /// <param name="arg">Text parsing arguments</param>
    public static string Get(string id, string dontExists, params float[] arg) { return Get(id, dontExists, arg.Select(x => x.ToString()).ToArray()); }
    /// <summary> Get a text in the current language </summary>
    /// <param name="id">ID of the text</param>
    /// <param name="dontExists">Text to display if the ID doesn't exists</param>
    /// <param name="arg">Text parsing arguments</param>
    public static string Get(string id, string dontExists, params string[] arg)
    {
        if (!Load().TryGetValue(id, out string c)) c = dontExists; //If nothing is found, return the text of the variable "dontExists"
        for (int i = 0; i < arg.Length; i++) c = c?.Replace("[" + i + "]", arg[i]); //Insert the arguments in the text
        return Format(c); //Returns the text after formatting (line breaks, tabs, ...)
    }

    /// <summary>Format a string</summary>
    /// <param name="str">The string to format</param>
    static string Format(string str)
    {
        if (!string.IsNullOrEmpty(str))
        {
            return str.Replace("\\n", "\n")
                .Replace("\\r", "\r")
                .Replace("\\t", "\t");
        }
        else return str;
    }

    static Dictionary<string, string> data;
    public static Dictionary<string, string> Load(string persistentPath = null)
    {
        if (data != null) return data;

        var dic = new Dictionary<string, string>();
        for (int i = 0; i < 2; i++)
        {
            string path = (persistentPath ?? UnityEngine.Application.streamingAssetsPath) + "/Languages/" + "/" + (i == 0 ? UnityEngine.Application.systemLanguage.ToString() : "English") + ".lang";
            if (File.Exists(path))
            {
                StreamReader file = new StreamReader(@path);
                string line;
                while ((line = file.ReadLine()) != null)
                {
                    int equalIndex = line.IndexOf(" = ");
                    if (equalIndex < 0) continue;

                    string key = line.Substring(0, equalIndex);
                    string value = line.Substring(equalIndex + 3);
                    if (!dic.ContainsKey(key)) dic.Add(key, value);
                }
                file.Close();
            }
        }
        data = dic;
        return dic;
    }
}
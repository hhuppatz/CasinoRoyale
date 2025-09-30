// An adaptation of Java's Properties class for .NET
// By user "Nick Rimmer" on StackOverflow.com
// Found at "https://stackoverflow.com/questions/485659" 
using System;
using System.Linq;
using System.Collections.Generic;

namespace CasinoRoyale.Utils
{
    public class Properties
{
    private Dictionary<string, string> list;
    private string filename;

    public Properties(string file)
    {
        reload(file);
    }
        
    public string get(string field, string defValue)
    {
        return (get(field) == null) ? (defValue) : (get(field));
    }

    public string get(string field)
    {
        return (list.ContainsKey(field))?(list[field]):(null);
    }

    public void set(string field, Object value)
    {
        if (!list.ContainsKey(field))
            list.Add(field, value.ToString());
        else
            list[field] = value.ToString();
    }

    public void Save()
    {
        Save(this.filename);
    }

    public void Save(string filename)
    {
        this.filename = filename;

        if (!System.IO.File.Exists(filename))
            System.IO.File.Create(filename);

        System.IO.StreamWriter file = new System.IO.StreamWriter(filename);

        foreach(string prop in list.Keys.ToArray())
            if (!string.IsNullOrWhiteSpace(list[prop]))
                file.WriteLine(prop + "=" + list[prop]);

        file.Close();
    }

    public void reload()
    {
        reload(this.filename);
    }

    public void reload(string filename)
    {
        this.filename = filename;
        list = new Dictionary<string, string>();

        if (System.IO.File.Exists(filename))
            loadFromFile(filename);
        else
            System.IO.File.Create(filename);
    }

    private void loadFromFile(string file)
    {
        foreach (string line in System.IO.File.ReadAllLines(file))
        {
            if ((!string.IsNullOrEmpty(line)) &&
                (!line.StartsWith(";")) &&
                (!line.StartsWith("#")) &&
                (!line.StartsWith("'")) &&
                (line.Contains('=')))
            {
                int index = line.IndexOf('=');
                string key = line.Substring(0, index).Trim();
                string value = line.Substring(index + 1).Trim();

                if ((value.StartsWith("\"") && value.EndsWith("\"")) ||
                    (value.StartsWith("'") && value.EndsWith("'")))
                {
                    value = value.Substring(1, value.Length - 2);
                }

                try
                {
                    //ignore dublicates
                    list.Add(key, value);
                }
                catch { }
            }
        }
    }


    }
}
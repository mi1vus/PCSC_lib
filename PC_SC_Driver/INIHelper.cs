using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;

public class INIHelper
{
    public string this[string index] 
        => (m_sections?.ContainsKey(index)??false)? m_sections[index] : null;

    private Dictionary<string, string> m_sections =
            new Dictionary<string, string>();
    private const bool SeparateSections = true;
    public char SectionSeparator = ';';

    public INIHelper(string file)
    {
        Init(File.ReadAllText(file));
    }

    protected void Init(string a_text)
    {
        //configText = a_text;
        m_sections.Clear();
        StringReader a_reader = new StringReader(a_text);
        // Наименование текущей секции
        string a_section = string.Empty;
        while (a_reader.Peek() != -1)
        {
            string a_line = a_reader.ReadLine();
            a_line = a_line.Trim('\t', ' ');

            // если строка - секция
            if ((a_line.StartsWith("[")) &&
                (a_line.EndsWith("]")))
            {
                // получить наименование секции
                a_section = a_line.Replace("[", string.Empty);
                a_section = a_section.Replace("]", string.Empty);
                if (SeparateSections)
                {
                    string tmp_section = a_section;
                    for (int z = 1; m_sections.ContainsKey(a_section); z++)
                    {
                        a_section = tmp_section + SectionSeparator + z.ToString();
                    }

                }
                continue;
            }
            // если секции нет - пропустить строку
            if (a_section.Trim() == string.Empty)
                continue;
            if (a_line.Trim() == string.Empty)
                continue;

            // если строка - "ключ"="значение"
            string[] a_lineParts = a_line.Split('=');
            if (a_lineParts.Length >= 2)
            {
                string a_key = a_lineParts[0].Trim();
                string a_value = a_line.Remove(0, a_line.IndexOf('=') + 1).Trim();

                m_sections[a_key] = a_value;
            }
            if (a_lineParts.Length == 1)
            {
                string a_key = a_lineParts[0].Trim();
                string a_value = "";

                m_sections[a_key] = a_value;
            }

        }
    }
};
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.IO;
using System.Net.Http;
using System.Text.RegularExpressions;
using HtmlAgilityPack;

namespace KoreanBuilds
{
    class Program
    {
        private static readonly string path = "C:\\Riot Games\\League of Legends\\Config\\Champions";
        private static readonly HttpClient client = new HttpClient();
        private static readonly string patch = "9.8";
        static void Main(string[] args)
        {
            List<Champion> champ_list;
            Console.WriteLine("Getting champions list");
            champ_list = GetChampionsList();
            Console.WriteLine("Getting each champion roles");
            GetChampionsRoles(champ_list);
            Console.WriteLine("Getting each champion builds");
            GetBuilds(champ_list);
        }

        private static List<Champion> GetChampionsList()
        {
            string html = string.Empty;
            string url = @"http://koreanbuilds.net";
            string name, id;
            List<Champion> champ_list = new List<Champion>();

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.AutomaticDecompression = DecompressionMethods.GZip;

            using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream))
            {
                html = reader.ReadToEnd();
            }
            string[] lines = html.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            foreach (string line in lines)
                if (line.Contains("champIcon"))
                {
                    int i1 = line.IndexOf("name") + 6;
                    int i2 = line.IndexOf("title") - 2;
                    name = line.Substring(i1, i2 - i1).Replace("&#x27;", "'").Replace("&amp;", " ");
                    id = Regex.Match(line, @"\d+").Value;
                    Champion champ = new Champion(id, name);
                    champ_list.Add(champ);
                }
            return champ_list;
        }
        private static void GetChampionsRoles(List<Champion> champ_list)
        {
            string html = string.Empty;
            string url = @"http://koreanbuilds.net/roles?championid=";

            for (int i = 0; i < champ_list.Count; i++)
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url+champ_list[i].Id);
                request.AutomaticDecompression = DecompressionMethods.GZip;

                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                using (StreamReader reader = new StreamReader(stream))
                {
                    html = reader.ReadToEnd();
                }
                string[] lines = html.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

                Console.Write("Getting " + champ_list[i].Name + " roles.");

                foreach (string line in lines)
                {
                    if (line.Length != 0)
                    {
                        int i1 = line.IndexOf(")\">") + 3;
                        int i2 = line.IndexOf("</button>");
                        string role = line.Substring(i1, i2 - i1);
                        champ_list[i].Roles.Add(role);
                    }
                }

                Console.Write(" Done!\n");
            }        
        }
        private static void GetBuilds(List<Champion> champ_list)
        {
            string html = string.Empty;
            string url = @"http://koreanbuilds.net/champion/Ahri/Mid/9.8/enc/NA";

            for (int i = 0; i < champ_list.Count; i++)
            {
                for (int j = 0; j < champ_list[i].Roles.Count; j++)
                {
                    url = "http://koreanbuilds.net/champion/" + champ_list[i].Name.Replace(" & ", "%26") + "/" + champ_list[i].Roles[j] + "/" + patch + "/enc/NA";
                    Console.WriteLine(url);
                    try
                    {
                        HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

                        using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
                        using (Stream stream = response.GetResponseStream())
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            html = reader.ReadToEnd();
                        }
                        HtmlDocument doc = new HtmlDocument();
                        doc.LoadHtml(html);
                        //Console.WriteLine(html);
                        string[] lines = html.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
                        //string line = doc.GetElementbyId("items").SelectNodes("div")[0].SelectNodes("div")[0].SelectNodes("img")[0].OuterHtml;
                        HtmlNodeCollection full_items = doc.GetElementbyId("items").SelectNodes("div")[0].SelectNodes("div");
                        HtmlNodeCollection starter_items = doc.GetElementbyId("items").SelectNodes("div")[1].SelectNodes("div");
                        StringBuilder str = new StringBuilder();
                        str.AppendLine("{").AppendLine("\t\"map\": \"any\",").AppendLine("\t\"blocks\": [").AppendLine("\t\t{").AppendLine("\t\t\t\"items\": [");
                        HtmlNode last_full = full_items.Last();
                        HtmlNode last_start = starter_items.Last();
                        foreach (HtmlNode item in starter_items)
                        {
                            string line = item.SelectNodes("img").First().OuterHtml;
                            str.AppendLine("\t\t\t\t{").AppendLine("\t\t\t\t\t\"id\": \"" + GetItemId(line) + "\",").AppendLine("\t\t\t\t\t\"count\": 1");
                            if (item != last_start)
                                str.AppendLine("\t\t\t\t},");
                            else
                                str.AppendLine("\t\t\t\t}");
                        }

                        str.AppendLine("\t\t\t],").AppendLine("\t\t\t\"type\": \"Starter items\"").AppendLine("\t\t},");
                        str.AppendLine("\t\t{").AppendLine("\t\t\t\"items\": [");
                        foreach (HtmlNode item in full_items)
                        {
                            string line = item.SelectNodes("img").First().OuterHtml;
                            str.AppendLine("\t\t\t\t{").AppendLine("\t\t\t\t\t\"id\": \"" + GetItemId(line).Replace("3040","3003").Replace("3042","3004") + "\",").AppendLine("\t\t\t\t\t\"count\": 1");
                            if (item != last_full)
                                str.AppendLine("\t\t\t\t},");
                            else
                                str.AppendLine("\t\t\t\t}");
                        }
                        str.AppendLine("\t\t\t],").AppendLine("\t\t\t\"type\": \"Full build\"").AppendLine("\t\t},");
                        str.AppendLine("\t\t{").AppendLine("\t\t\t\"items\": [");
                        str.AppendLine("\t\t\t\t{").AppendLine("\t\t\t\t\t\"id\": \"2003\",").AppendLine("\t\t\t\t\t\"count\": 1").AppendLine("\t\t\t\t},");
                        str.AppendLine("\t\t\t\t{").AppendLine("\t\t\t\t\t\"id\": \"2004\",").AppendLine("\t\t\t\t\t\"count\": 1").AppendLine("\t\t\t\t},");
                        str.AppendLine("\t\t\t\t{").AppendLine("\t\t\t\t\t\"id\": \"2055\",").AppendLine("\t\t\t\t\t\"count\": 1").AppendLine("\t\t\t\t},");
                        str.AppendLine("\t\t\t\t{").AppendLine("\t\t\t\t\t\"id\": \"2031\",").AppendLine("\t\t\t\t\t\"count\": 1").AppendLine("\t\t\t\t},");
                        str.AppendLine("\t\t\t\t{").AppendLine("\t\t\t\t\t\"id\": \"2032\",").AppendLine("\t\t\t\t\t\"count\": 1").AppendLine("\t\t\t\t},");
                        str.AppendLine("\t\t\t\t{").AppendLine("\t\t\t\t\t\"id\": \"2033\",").AppendLine("\t\t\t\t\t\"count\": 1").AppendLine("\t\t\t\t},");
                        str.AppendLine("\t\t\t\t{").AppendLine("\t\t\t\t\t\"id\": \"2138\",").AppendLine("\t\t\t\t\t\"count\": 1").AppendLine("\t\t\t\t},");
                        str.AppendLine("\t\t\t\t{").AppendLine("\t\t\t\t\t\"id\": \"2140\",").AppendLine("\t\t\t\t\t\"count\": 1").AppendLine("\t\t\t\t},");
                        str.AppendLine("\t\t\t\t{").AppendLine("\t\t\t\t\t\"id\": \"2139\",").AppendLine("\t\t\t\t\t\"count\": 1").AppendLine("\t\t\t\t}");
                        str.AppendLine("\t\t\t],").AppendLine("\t\t\t\"type\": \"Consumables\"").AppendLine("\t\t}");
                        str.AppendLine("\t],").AppendLine("\t\"title\": \"KRB " + champ_list[i].Roles[j] + "\",").AppendLine("\t\"priority\": false,").AppendLine("\t\"mode\": \"any\",").AppendLine("\t\"type\": \"custom\",").AppendLine("\t\"sortrank\": 1,").AppendLine("\t\"champion\": \"" + champ_list[i].Name + "\"").AppendLine("}");
                        //Console.WriteLine(str.ToString());


                        bool exists = Directory.Exists(path + "\\" + champ_list[i].Name.Replace(" ", "").Replace("'", "").Replace(".", "").Replace("Wukong", "MonkeyKing") + "\\Recommended\\");

                        if (!exists)
                            Directory.CreateDirectory(path + "\\" + champ_list[i].Name.Replace(" ", "").Replace("'", "").Replace(".", "").Replace("Wukong", "MonkeyKing") + "\\Recommended\\");

                        File.WriteAllText(path + "\\" + champ_list[i].Name.Replace(" ", "").Replace("'", "").Replace(".", "").Replace("Wukong", "MonkeyKing") + "\\Recommended\\" + champ_list[i].Name + "_" + champ_list[i].Roles[j] + ".json", str.ToString());
                    }
                    catch(Exception ex) {
                        Console.WriteLine(ex);
                    }
                }
                


                //Console.Write(" Done!\n");
            }
        }
        private static string GetItemId(string line)
        {
            int i1 = line.LastIndexOf("/") + 1;
            int i2 = line.IndexOf(".png");
            return line.Substring(i1, i2 - i1);
        }
    }
    //http://koreanbuilds.net/champion/Ahri/Mid/9.8/enc/NA
    class Champion
    {
        private string name;
        private string id;
        private List<string> roles;

        public string Name { get => name; set => name = value; }
        public string Id { get => id; set => id = value; }
        public List<string> Roles { get => roles; set => roles = value; }

        public Champion(string id, string name)
        {
            this.name = name;
            this.id = id;
            this.roles = new List<string>();
        }

    }

    
}

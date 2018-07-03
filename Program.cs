using System;
using System.Collections.Generic;
using System.Collections;
using System.Xml;
using System.Linq;
using System.Text.RegularExpressions;

namespace test
{
    class Program
    {
        const string xml_file_path = "ZTB.scd";
        static private List<string[]> IEDsInfo = new List<string[]>();
        static private XmlDocument xmlDoc = new XmlDocument();

        static void Main(string[] args)
        {
            System.Diagnostics.Stopwatch stop = new System.Diagnostics.Stopwatch();
            stop.Start();
            
            try
            {
                xmlDoc.Load(xml_file_path);

                GetIEDsInfo(xmlDoc);

                // GetTransformers();
                // GetBuses();
                // lines = GetLines();
                                
                // var a = GetBusRelation();
                
                Console.WriteLine("Done.");
            }
            catch(Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }

            stop.Stop();
            Console.WriteLine(stop.Elapsed.TotalMilliseconds);
            Console.ReadLine();
        }

        /// <summary>
        /// 返回所有IED信息的列表，
        /// 返回包含IED的name和desc信息,
        /// [["PL1101","主变110kV侧保护装置"],["MM2202":"220kV巴福线合并单元"],...]
        /// </summary>
        /// <returns>IED信息列表，每个列表项为string数组：[ name , desc ]</returns>
        private static List<string[]> GetIEDsInfo(XmlDocument xmlDocument)
        {
            // 取得所有IED节点
            var IEDsList = xmlDocument.GetElementsByTagName("IED");

            // 提取每个IED节点的name,desc属性信息
            foreach (var item in IEDsList)
            {
                var ied = (XmlElement)item;
                string name = ied.GetAttribute("name");
                string desc = ied.GetAttribute("desc");
                IEDsInfo.Add(new[] { name, desc });
            }
            return IEDsInfo;
        }

        /// <summary>
        /// 获取主变信息，{1:"1#",2:"2#",...}
        /// </summary>
        /// <return>
        /// 主变的编号，描述
        /// </return>
        private static IDictionary<int,string> GetTransformers()
        {
            Regex reg = new Regex(@"(\d{3,})");

            var m_trans = new SortedDictionary<int, string>();

            var trans = IEDsInfo.Where(ied => ied[1].Contains("主变") && ied[1].Contains("测控")).Select(ied => ied);
            foreach (var info in trans)
            {
                Match m = reg.Match(info[0]);
                var key = Convert.ToInt32(m.Groups[1].Value.Last())-48;
                if (m.Groups[1].Value != "" && !m_trans.ContainsKey(key))
                {
                    m_trans[key] = key.ToString()+"#";
                    Console.WriteLine(m_trans[key]);
                }
            }

            if (m_trans.Count == 0)
            {
                Console.WriteLine("没有查找到主变相关信息！");
                return null;
            }
            else
            {
                return m_trans;
            }
        }

        /// <summary>
        /// 获取母线信息，{110:[1,2],220:[1,2,3,4],...}
        /// </summary>
        /// <returns>
        /// 母线电压等级，编号
        /// </returns>
        private static IDictionary<int,List<int>> GetBuses()
        {
            Regex reg = new Regex(@"(\d{3,})");

            var buses = IEDsInfo.Where(ied => ied[0].StartsWith("CM")).Select(ied=>ied);
            if (buses.Count()==0)
            {
                Console.WriteLine("未找到母线相关信息");
                return null;
            }

            var m_buses = new SortedDictionary<int, List<int>>();

            foreach (var bus in buses)
            {
                Match m = reg.Match(bus[0]);
                var value = m.Groups[1].Value;

                int n = Convert.ToInt32(value.Last()) - 48;
                
                int level = int.Parse(value.Substring(0,2));
                
                // 电压等级的处理
                var low_evel = new[] { 10, 35, 66 };
                level = low_evel.Contains(level) ? level : level * 10;
                
                // 将处理后的数据存放到数据结构中，并返回
                if (!m_buses.ContainsKey(level))
                {
                    List<int> lst = new List<int>();
                    m_buses[level] = lst;
                    m_buses[level].Add(n);
                    Console.WriteLine(n);
                }
                else
                {
                    m_buses[level].Add(n);
                    Console.WriteLine(n);
                }                
            }
            return m_buses;
        }

        /// <summary>
        /// 获取线路信息,{110:{"1101":"110kV大海I线","1102":"110kV海东线",...},220:{"2201":"xxx",...}}
        /// </summary>
        /// <returns>
        /// 按电压等级分类，存储该等级下的所有线路
        /// </returns>
        private static SortedDictionary<int,SortedDictionary<string,string>> GetLines()
        {
            Regex line_no = new Regex(@"^[PS].*L(\d{4})");
            Regex line_name = new Regex(@"(\d{2,4}\D{2,}\d?\D*?线路?\d*)|(\D*线\d*)");

            var lines = IEDsInfo.Where(ied=>line_no.IsMatch(ied[0])&&line_name.IsMatch(ied[1])).Select(ied=>ied);
            if (lines.Count() == 0)
            {
                return null;
            }

            var m_lines = new SortedDictionary<int,SortedDictionary<string,string>>();
            var low_level = new[] { 10, 35, 66 };

            foreach (var item in lines)
            {
                var l_no = line_no.Match(item[0]).Groups[1].Value;
                var l_name = line_name.Match(item[1]).Value;

                // 低压的去除
                var level = int.Parse(l_no.Substring(0, 2));
                level = low_level.Contains(level) ? level : level * 10;

                if (!m_lines.ContainsKey(level))
                {
                    var dic = new SortedDictionary<string, string>();
                    m_lines[level] = dic;
                }
                m_lines[level][l_no] = l_name;
                
            }
            return m_lines;
        }

        /// <summary>
        /// 获取母线连接关系，{110:{ "分段":{1101: [1,2] ,1102: [3,4] } , "母联":{1103: [1,3] ,1104: [2,4] } },35:...... }
        /// </summary>
        /// <returns>按电压等级，分段，母联关系列出所有连接关系</returns>
        private static SortedDictionary<int, Dictionary<string,Dictionary<string,IList>>> GetBusRelation()
        {
            Regex relation = new Regex(@"([1-9]|[IVX]+)-([1-9]|[IVX]+)|([123567][012356][1-9]{2})");
            Regex relation_no = new Regex(@"(\d{3,})");

            var d_index = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
            var c_index = new[] { "O", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX" };

            var bus_relation = IEDsInfo.Where(ied => ied[1].Contains("母联") || ied[1].Contains("分段")).Select(ied=>ied);

            if (bus_relation.Count() == 0)
            {
                return null;
            }

            var m_relation = new SortedDictionary<int, Dictionary<string,Dictionary<string,IList>>>();
            var low_level = new[] {10,35,66 };

            foreach (var item in bus_relation)
            {
                var m_part = relation.Match(item[1]).Value;
                var no = relation_no.Match(item[0]).Value;
                var level = int.Parse(no.Substring(0, 2));
                int[] seg_arr=null;

                Console.WriteLine(no+" : "+item[1]);
                if (m_part == "" && no[2] == '0')
                    m_part = null;
                else
                    m_part = no;

                if(m_part!=null)
                {
                    seg_arr = new int[2];
                    
                    if (m_part.Contains("-"))
                    {
                        seg_arr[0] = Array.IndexOf(c_index ,m_part.Split('-')[0]);
                        seg_arr[1] = Array.IndexOf(c_index, m_part.Split('-')[1]);
                    }
                    else
                    {
                        seg_arr[0] = Array.IndexOf(d_index,m_part[2]);
                        seg_arr[1] = Array.IndexOf(d_index,m_part[3]);
                    }
                }
                level = low_level.Contains(level) ? level : level * 10;
                
                if (!m_relation.ContainsKey(level))
                {
                    m_relation[level] = new Dictionary<string, Dictionary<string,IList>>();                    
                }

                var r = item[1].Contains("分段") ? "分段" : "母联";
                switch (r)
                {
                    case "分段":
                        if (!m_relation[level].ContainsKey("分段"))
                        {
                            m_relation[level]["分段"] = null;
                        }
                        if (m_part is null)
                        {
                            break;
                        }
                        if (m_relation[level]["分段"]== null)
                        {
                            m_relation[level]["分段"] = new Dictionary<string,IList>();
                        }

                        if (m_relation[level]["分段"].ContainsKey(m_part))
                        {
                            break;
                        }
                        m_relation[level]["分段"][m_part] = seg_arr;
                        break;

                    case "母联":
                        if (!m_relation[level].ContainsKey("母联"))
                        {
                            m_relation[level]["母联"] = null;
                        }
                        if (m_part is null)
                        {
                            break;
                        }
                        if (m_relation[level]["母联"] == null)
                        {
                            m_relation[level]["母联"] = new Dictionary<string, IList>();
                        }
                        if (m_relation[level]["母联"].ContainsKey(m_part))
                        {
                            break;
                        }
                        m_relation[level]["母联"][m_part] = seg_arr;
                        break;

                    default:
                        break;
                }   
            }
            return m_relation;
        }

        private static void GetLineToBus()
        {
            var lines = GetLines();
            var keys = lines.SelectMany(line => line.Value.Keys);

            xmlDoc.where();
        }
    }
}

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
        const string xml_file_path = "BZB.scd";
        static private List<string[]> IEDsInfo = new List<string[]>();
        static private List<XmlElement> IEDList = new List<XmlElement>();
        static private XmlNamespaceManager nsmgr;

        static string[] d_index = new[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
        static string[] c_index = new[] { "O", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX" };

        static void Main(string[] args)
        {
            System.Diagnostics.Stopwatch stop = new System.Diagnostics.Stopwatch();
            stop.Start();
            
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(xml_file_path);
                nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
                nsmgr.AddNamespace("ns", "http://www.iec.ch/61850/2003/SCL");
                GetIEDsInfo(xmlDoc);

                // GetTransformers();
                // GetBuses();
                // lines = GetLines();

                // var a = GetBusRelation();
                // GetLineToBus();
                GetTransToBus();
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
            var IEDs = xmlDocument.GetElementsByTagName("IED");
            
            // 提取每个IED节点的name,desc属性信息
            foreach (var item in IEDs)
            {
                var ied = (XmlElement)item;
                string name = ied.GetAttribute("name");
                string desc = ied.GetAttribute("desc");
                IEDsInfo.Add(new[] { name, desc });
                IEDList.Add(ied);
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
                        seg_arr[0] = Array.IndexOf(d_index,m_part[2].ToString());
                        seg_arr[1] = Array.IndexOf(d_index,m_part[3].ToString());
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

        /// <summary>
        /// 获取线路与母线的连接关系，{"2201":[1,2],"1102":[1],"1103":[2],...}
        /// </summary>
        /// <returns>按线路名称，所连接母线段组成键值对</returns>
        private static IDictionary GetLineToBus()
        {
            //var lines = GetLines();
            //var keys = lines.SelectMany(line => line.Value.Keys);
            //foreach (var i in keys)
            //{
            //    Console.WriteLine(i);
            //}

            Regex reg = new Regex(@"M.*L(\d{4})");


            // 获得一个包含[ied名称,AccessPoint为M1的节点]列表的可迭代对象
            var mu_name_extref = IEDList.Where(ele => reg.IsMatch(ele.GetAttribute("name"))).Select(ied =>
                                  {
                                      var name = ied.GetAttribute("name");
                                      var ext_ref = ied.SelectNodes("//ns:IED[@name='" + ied.GetAttribute("name") + "']/ns:AccessPoint[contains(@name,'M')]/ns:Server/ns:LDevice[contains(@inst,'MUSV')]/ns:LN0/ns:Inputs/ns:ExtRef", nsmgr).OfType<XmlNode>().ToArray();
                                      ArrayList ref_list = new ArrayList();
                                      ref_list.Add(name);
                                      ref_list.Add(ext_ref);
                                      return ref_list;
                                  });
            Regex bus_no = new Regex(@"([1-9]|[IVX]+)");
            Dictionary<string,ISet<int>> line_bus_dic = new Dictionary<string,ISet<int>>();
            
            foreach (var e in mu_name_extref)
            {
                // MU 的 IED name和自身节点
                var name = (string)e[0];
                var ext_ref = (IEnumerable)e[1];

                var line = reg.Match(name).Value;
                line = reg.Split(line)[1];

                if (line == "")
                    continue;
                if (line_bus_dic.ContainsKey(line))
                    continue;
                line_bus_dic[line] = line_bus_dic.ContainsKey(line)? line_bus_dic[line]:new SortedSet<int>();
                
                // 获取该线路下的 LDevice/LN0/Inputs/ExtRef 节点
                foreach (var ele in ext_ref) {
                    var node_ref = (XmlElement)ele;
                    var desc = FindReference(node_ref);
                    var m = bus_no.Match(desc);
                    if (m.Value == "")
                    {
                        continue;
                    }
                    else
                    {
                        // 获取对应的母线编号
                        var index = c_index.Contains(m.Value) ? Array.IndexOf(c_index, m.Value) : Array.IndexOf(d_index,m.Value);
                        line_bus_dic[line].Add(index);
                    }
                }
                Console.WriteLine(name);
            }

            //Console.WriteLine("pass");
            return line_bus_dic;
        }

        /// <summary>
        /// 传入Inputs节点下的ExtRef元素
        /// 解析其对应的引用节点的字符串描述
        /// </summary>
        /// <param name="element"></param>
        /// <returns>返回引用所对应输入的 desc 信息</returns>
        private static string FindReference(XmlElement element) {
           
            // 该 ExtRef 所引用的外部 LN 节点
            XmlElement target_ln;
            string desc = "";
            try
            {
                // ExtRef 的属性信息
                var ied_name = element.GetAttribute("iedName");
                var ldInst = element.GetAttribute("ldInst");
                var lnClass = element.GetAttribute("lnClass");
                var lnInst = element.GetAttribute("lnInst");

                // 对于非提供电压信息的ExtRef，直接跳过
                if (lnInst == "")
                    return desc;
                target_ln = (XmlElement)IEDList.Where(ele => ele.GetAttribute("name") == ied_name).
                                                Select(ele => ele.SelectSingleNode("//ns:IED[@name='"+ied_name+"']//ns:LDevice[@inst='" + ldInst + "']/ns:LN[@lnClass='" + lnClass + "' and @inst='"+lnInst+"']", nsmgr)).ToArray()[0];
                
                // 获取对应 LN 节点的描述信息
                desc = target_ln.GetAttribute("desc");
            }

            catch (Exception)
            {
                Console.WriteLine("Not reqired ExtRef node.");
            }

            return desc;
        }


        private static void GetTransToBus()
        {
            Regex reg = new Regex(@"(\d{4})");
            Regex trans_no = new Regex(@"(\d{1})");

            var trans = IEDsInfo.Where(e=>e[0].StartsWith("M")&&reg.IsMatch(e[0])&&e[1].Contains("主变")).Select(e=>e);

            var trans_dic = new Dictionary<string,Dictionary<int,IList>>();

            foreach (var item in trans)
            {
                var name = reg.Match(item[0]).Value;
                var level = int.Parse(name.Substring(0, 2));
                
                // 属于低压部分，直接跳过
                var low_evel = new[] {0, 10, 35, 66 };
                if (low_evel.Contains(level))
                    continue;

                level = level * 10;
                var t_no = trans_no.Match(item[1]).Value;

                if (!trans_dic.ContainsKey(t_no))
                    trans_dic[t_no] = new Dictionary<int, IList>();

                if (!trans_dic[t_no].ContainsKey(level))
                    trans_dic[t_no][level] = new ArrayList();

                var mt_node = IEDList.Where(ied => ied.GetAttribute("name").Equals(item[0])).Select(ied=>ied);
                //trans_dic[t_no][level].Add(1);

                Console.WriteLine(name+" : "+item[1]);
            }
        }

    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Text;

namespace SCDVisual
{
    class SCDResolver2
    {
        struct IEDTYPES
        {
            public const string PROT = "PROT";
            public const string MEAS = "MEAS";
            public const string RPIT = "RPIT";
            public const string MU = "MU";
            public const string type_P = "保护";
            public const string type_C = "测控";
            public const string type_I = "智能终端";
            public const string type_M = "合并单元";
            public const string type_X = "其他类型";
        }

        //// 节点结构
        //struct Node
        //{
        //    public string name;                // 节点名称
        //    public bool visited ;              // 访问标识
        //    // public ArrayList weight_list ;     // 边的权重列表
        //    public ArrayList link_list ;       // 有连接的其他节点列表
        //    public int seg_num;                // 所连的不同间隔个数

        //    // 添加与本节点相连的节点到列表中
        //    public void add(string v)
        //    {
        //        if (link_list == null)
        //            link_list = new ArrayList();
        //        link_list.Add(v);
        //        if (LinkTable.edges.ContainsKey(name + "$" + v) || LinkTable.edges.ContainsKey(v + "$" + name))
        //            return;

        //        LinkTable.edges[name + "$" + v] = 1;
        //    }

        //    // 修改本节点的边的权重
        //    public void change_weight(Node self)
        //    {
        //        IEnumerable tEdges = LinkTable.edges.Where(edge => edge.Key.StartsWith(self.name) || edge.Key.EndsWith(self.name)).Select(edge=>edge.Key);
        //        foreach(string edge in tEdges)
        //        {
        //            LinkTable.edges[edge] = 10;
        //        }
        //    }

        //}

        //class LinkTable
        //{
        //    private List<Node> nodes;
        //    public static Dictionary<string, int> edges = new Dictionary<string, int>();

        //    // 创建新节点，初始化节点信息
        //    public Node init_node(string name)
        //    {
        //        Node n = new Node();
        //        n.name = name;
        //        n.visited = false;
        //        return n;
        //    }

        //    public LinkTable(string name)
        //    {
        //        Node node = init_node(name);

        //    }

        //    // 广度优先搜索
        //    public void BFS()
        //    {

        //    }

        //}
        ///
        
        // 公共正则表达式
        static Regex reg_IEDType = new Regex(@"([合智]并?智?能?[\u4e00-\u9fa5]{2})|(保护)|(测控)(测控)?");
        static Regex reg_no = new Regex(@"\d{3,}");
        static Regex bus_seg_no = new Regex(@"([1-9]|[IVX]+|[\u2160-\u2169])");

        // 编号对比查询数组
        public static string[] d_index = new[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
        public static string[] c_index = new[] { "O", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX" };
        public static string[] roma_index = new[] {"0","Ⅰ","Ⅱ","Ⅲ","Ⅳ","Ⅴ","Ⅵ","Ⅶ","Ⅷ","Ⅸ","Ⅹ"};

        public static XDocument xmldoc;
        public static string err_msg;
        public static XNamespace ns;
        public static Dictionary<string, int> Volts = new Dictionary<string,int>();
        public static IDictionary<int, ISet<string>> Buses;
        public static IDictionary<int, IDictionary<string, string>> Lines;
        public static ISet<string> Trans;
        public static IDictionary<int, IDictionary<string, IDictionary<string, int[]>>> Buses_Relation;
        public static IDictionary<string, ISet<string>> Line_Breaker_Relation = new SortedDictionary<string, ISet<string>>();
        public static ISet<int> Breaker_Suffix = new SortedSet<int>();
        public static IDictionary<string, Dictionary<string, List<string>>> Device_IEDs;

        public static void read(string filepath)
        {
            try
            {
                xmldoc = XDocument.Load(filepath);
                ns = SCDResolver2.xmldoc.Root.Attribute("xmlns").Value;
                IEDs = xmldoc.Root.Elements(ns + "IED").ToList();
            }
            catch(Exception e)
            {
                err_msg = e.Message;
            }
        }

        public static List<XElement> IEDs;

        public static List<XElement> ExtRefs(XElement ied)
        {
            return ied.Descendants(ns+"ExtRef").ToList();
        }

        public static List<string> relatedIEDs(XElement ied)
        {
            Regex ied_mode = new Regex(@"^\w(\w+)\d+");
            var inputs = ExtRefs(ied);
            var iedNames = inputs.Select(ele => ele.Attribute("iedName").Value).Where(name => ied_mode.IsMatch(name));
            List<string> mList = new List<string>();
            mList.Add(ied.Attribute("name").Value);
            foreach (string name in iedNames)
            {
                if (mList.Contains(name))
                    continue;
                mList.Add(name);
            }
            return mList;
        }

        /// <summary>
        /// 根据IED的desc属性和内部的LDevice的inst描述，
        /// 判断该IED所属的类型，保护，测控，合并单元，智能终端 ... ，
        /// 并返回类型的字符串形式。
        /// </summary>
        /// <param name="ied">一个IED节点元素，e.g：PL2201A 节点</param>
        /// <returns>返回该IED所属的类型的字符串表示，保护、测控、合并单元、智能终端 ...</returns>
        private static string getIEDType(XElement ied)
        {
            string type = IEDTYPES.type_X;

            string name = ied.Attribute("name").Value;
            string desc = ied.Attribute("desc").Value;
            IEnumerable<XElement> LDevices = ied.Descendants(ns + "LDevice");

            var match = reg_IEDType.Match(desc);
            var a = match.Groups[1].Value;
            if (LDevices.Any(ld => ld.Attribute("inst").Value == IEDTYPES.PROT))
                type = IEDTYPES.type_P;
            else if (LDevices.Any(ld => ld.Attribute("inst").Value == IEDTYPES.MEAS) && LDevices.All(ld => ld.Attribute("inst").Value != IEDTYPES.PROT))
                type = IEDTYPES.type_C;
            else if (LDevices.Any(ld => ld.Attribute("inst").Value == IEDTYPES.RPIT))
                type = IEDTYPES.type_I;
            else if (LDevices.Any(ld => ld.Attribute("inst").Value.StartsWith(IEDTYPES.MU)))
                type = IEDTYPES.type_M;

            return match.Success ? match.Value : type;
        }

        public static SortedDictionary<int, IDictionary<string, string>> GetLines()
        {
            // 线路匹配正则表达式
            Regex line_no = new Regex(@"^[PSC]X?L(\d{4})");
            Regex line_name = new Regex(@"([\w\u4e00-\u9fa5\u2160-\u2169]+?线)");

            // 线路IEDs
            var lines = IEDs.Where(ied => line_no.IsMatch(ied.Attribute("name").Value)).Select(ied => ied).AsParallel();
            if (lines.Count() == 0)
                return null;

            var m_lines = new SortedDictionary<int, IDictionary<string, string>>();
            var low_level = new[] { 10, 35, 66 };

            // 遍历线路IED
            Parallel.ForEach(lines, (item) => {
                // 获得线路编号和名称
                var l_no = line_no.Match(item.Attribute("name").Value).Groups[1].Value;
                var l_name = line_name.Match(item.Attribute("desc").Value).Value;

                // 去除低压线路
                var level = int.Parse(l_no.Substring(0, 2));
                if (low_level.Contains(level))
                    return;
                level = level * 10;

                if (l_name == "")
                    l_name = level.ToString() + "kV线路" + l_no.Substring(1, 3);

                // 添加线路信息
                lock (m_lines)
                {
                    if (!m_lines.ContainsKey(level))
                    {
                        // 新增线路到存储结构中
                        var dic = new SortedDictionary<string, string>();
                        m_lines[level] = dic;
                    }
                    m_lines[level][l_no] = l_name;
                }

            });

            return m_lines;
        }

        public static IDictionary<int, ISet<string>> GetBuses()
        {
            Regex reg = new Regex(@"[PCI]MX?(\d\w*)");
            Regex suffix = new Regex(@"(\D\w*)");

            string no;
            int level;
            SortedSet<int> sLevel = new SortedSet<int>();
            int[] low_level = new [] {0,10,35,66 };
            Volts["H"] = 0;
            Volts["M"] = 0;
            Volts["L"] = 0;

            var buses = IEDs.Where(ied => reg.IsMatch(ied.Attribute("name").Value)).Select(ied=>ied.Attribute("name").Value).AsParallel();
            
            if (buses.Count() == 0)
                return null;

            foreach(string bus in buses)
            {
                level = int.Parse(reg.Match(bus).Groups[1].Value.Substring(0, 2));

                if (low_level.Contains(level))
                    sLevel.Add(level);
                else
                    sLevel.Add(level * 10);
            }
            Volts["H"] = sLevel.Max();
            sLevel.Remove(Volts["H"]);

            if (sLevel.Count == 1)
                Volts["M"] = sLevel.Max();
            else if (sLevel.Count == 2)
            {
                Volts["M"] = sLevel.Max();
                Volts["L"] = sLevel.Min();
            }

            var C_ieds = buses.Where(name => name.StartsWith("CM")).Select(name => reg.Match(name).Groups[1].Value);
            var I_ieds = buses.Where(name => name.StartsWith("IM")).Select(name => reg.Match(name).Groups[1].Value);
            
            var m_buses = new Dictionary<int, ISet<string>>();
            var tmp = buses;
            foreach(var kv in Volts)
            {
                
                if (kv.Value>220)
                {
                    tmp = buses.Where(name => name.Contains(kv.Value.ToString())).Select(name => reg.Match(name).Groups[1].Value);
                    var s = tmp.ToArray();
                    foreach ( string bus in tmp)
                    {
                        no = reg_no.Match(bus).Value.Last().ToString();
                        if (!m_buses.ContainsKey(kv.Value))
                        {
                            ISet<string> lst = new SortedSet<string>();
                            m_buses[kv.Value] = lst;
                        }
                        m_buses[kv.Value].Add(no);
                    }
                    continue;
                }
                if (C_ieds.Where(name => name.StartsWith(kv.Value.ToString())).Count() >= I_ieds.Where(name => name.StartsWith(kv.Value.ToString())).Count())
                    tmp = C_ieds.Where(name => name.StartsWith(kv.Value.ToString()));
                else
                    tmp = I_ieds.Where(name => name.StartsWith(kv.Value.ToString()));

                // 遍历母线，获取各段母线，并按电压等级归类
                foreach(string bus in tmp)
                {
                    if (suffix.IsMatch(bus))
                        no = suffix.Match(bus).Groups[1].Value;
                    else
                        no = bus.Last().ToString();
                
                    if (!m_buses.ContainsKey(kv.Value))
                    {
                        ISet<string> lst = new SortedSet<string>();
                        m_buses[kv.Value] = lst;
                    }
                    m_buses[kv.Value].Add(no);
                }
            }
            return m_buses;
        }

        public static ISet<string> GetTransformers()
        {
            // 存储主变的数据结构

            SortedSet<string> m_trans = new SortedSet<string>();
            string no = null;
            string reg_str = "^C(T|(ZB))"+Volts["H"].ToString();
            Regex trans_reg = new Regex(reg_str);

            // 包含主变信息的IED节点
            var trans = IEDs.Select(ied => ied.Attribute("name").Value).Where(name => trans_reg.IsMatch(name)).AsParallel();
            foreach(string t in trans)
            {
                // 获取主变的编号信息
                no = reg_no.Match(t).Value.Last().ToString();
                // 存储器中是否包含该编号的主变信息。没有==>则添加
                if (no == "")
                    continue;
                m_trans.Add(no);
            }
            return m_trans;
        }

        public static IDictionary<int, IDictionary<string, IDictionary<string, int[]>>> GetBusRelation()
        {
            Regex reg_seg = new Regex(@"^[PS]((FD)|(ML)|E|F)\d+");
            Regex reg_type = new Regex(@"(分段)|(母联)");
            int level, no;
            string type;
            var bus_relation = IEDs.Where(ied => reg_seg.IsMatch(ied.Attribute("name").Value)).
                GroupBy(ied=>ied.Attribute("name").Value).
                Select(ied => new string[] { reg_no.Match(ied.Last().Attribute("name").Value).Value,ied.Last().Attribute("desc").Value });
            
            var m_relation = new SortedDictionary<int, IDictionary<string, IDictionary<string, int[]>>>();
            m_relation[Volts["H"]] = null;
            m_relation[Volts["M"]] = null;
            m_relation[Volts["L"]] = null;

            int[] low_level = new[] { 10, 35, 66 };

            foreach(string[] info in bus_relation)
            {
                try
                {
                    no = int.Parse(info[0].Substring(2));
                    level = int.Parse(info[0].Substring(0,2));
                    type = reg_type.Match(info[1]).Value;
                }
                catch(Exception e)
                {
                    System.Console.WriteLine(e.Message);
                    continue;
                }
                
                if (!low_level.Contains(level))
                    level = level * 10;

                // 母线关系的数组，存储有关系的母线段
                int[] seg_arr = null;

                if (no > 10)
                    seg_arr = new int[] { no / 10, no % 10 };
                else if (Buses[level].Count==2)
                    seg_arr = new int[] { 1, 2 };
                else if(Buses[level].Count == 2 * bus_relation.Where(data =>data[1].Contains(type)).Select(data=>data[0]).Distinct().Count())
                {
                    switch (type)
                    {
                        case "分段":
                            seg_arr = new int[] { no, no + 2 };
                            break;
                        case "母联":
                            seg_arr = new int[] { no * 2 - 1, no * 2 };
                            break;
                        default:
                            break;
                    }
                }
                else
                    seg_arr = new int[] { no, no + 1 };

                if (m_relation[level]==null)
                    m_relation[level] = new Dictionary<string,IDictionary<string, int[]>>();
                if(!m_relation[level].ContainsKey(type))
                    m_relation[level][type] = new Dictionary<string, int[]>();
                m_relation[level][type][info[0]] = seg_arr;
            }

            return m_relation;
        }

        public static IDictionary<string, ISet<int>> GetLinkToBus()
        {
            SortedDictionary<string, ISet<int>> line_bus_dic = new SortedDictionary<string, ISet<int>>();

            Regex M_reg, P_reg;
            string part,no;
            IEnumerable<XElement> M_P_ieds;
            IEnumerable<string> all_components = Lines.SelectMany(line => line.Value.Keys).Select(name => "X?L" + name).AsParallel();
            List<string> all_trans = new List<string>();
            foreach (var t in Trans)
            {
                if (Volts["M"] > 100)
                {
                    all_trans.Add(@"(T|(ZB))" + Volts["M"].ToString() + t);
                }
                all_trans.Add(@"(T|(ZB))" + Volts["H"].ToString() + t);
            }
            all_components = all_components.Concat(all_trans);
            foreach(var item in all_components)
            {
                no = reg_no.Match(item).Value;
                part = item.Contains("L") ? "L"+no: "T"+no;
                M_reg = new Regex(@"^M" + item);

                // 获得一个线路合并单元|保护装置的可迭代对象
                M_P_ieds = IEDs.Where(ele => M_reg.IsMatch(ele.Attribute("name").Value) && !ele.Attribute("desc").Value.Contains("电压")).
                                Select(ied => ied).AsParallel();
                if (M_P_ieds.Count() == 0)
                {
                    P_reg = new Regex(@"^[PS]" + part);
                    M_P_ieds = IEDs.Where(ele => P_reg.IsMatch(ele.Attribute("name").Value) && !ele.Attribute("desc").Value.Contains("电压")).
                                    Select(ied => ied).AsParallel();
                    if (M_P_ieds.Count() == 0)
                        continue;
                }
                if (int.Parse(part.Substring(1, 2)) * 10 >= 330)
                {
                    GetLinkToBreaker(item);
                    continue;
                }
                // 新线路，生成新的存储结构
                if (!line_bus_dic.ContainsKey(part) || line_bus_dic[part].Count == 0)
                {
                    lock (line_bus_dic)
                    {
                        if (!line_bus_dic.ContainsKey(part))
                            line_bus_dic[part] = new SortedSet<int>();
                    }
                    // 获取该线路P|M装置对应的外部母线引用
                    // M_P_ieds = M_P_ieds.Where(ele => !ele.Attribute("desc").Value.Contains("电压"));
                    FindReference(M_P_ieds.First(), line_bus_dic,item);
                }
            }
            return line_bus_dic;
        }

        private static void FindReference(XElement node, IDictionary<string, ISet<int>> line_bus_dic,string part)
        {
            string node_name = node.Attribute("name").Value;
            string type = node_name.Substring(0,1);
            string component = node_name.Contains("L")?"L"+reg_no.Match(node_name).Value:"T"+ reg_no.Match(node_name).Value;
            Regex reg = new Regex(@"(\d[0-9A-Z]{3})");

            if (type == "M")
            {
                // 线路对应的ExtRef节点
                var mu_ext_refs = node.Descendants(ns+"AccessPoint").
                        Where(ele => ele.Attribute("name").Value.StartsWith("M")).
                        Descendants(ns+"ExtRef").
                        Where(ele=>ele.Attribute("iedName").Value.StartsWith("MM"));
                if (mu_ext_refs.Count() == 0)
                {
                    var P_reg = new Regex(@"^[PS]" + part);
                    var nodes = IEDs.Where(ele => P_reg.IsMatch(ele.Attribute("name").Value)).Select(ied => ied);
                    if (nodes.Count() == 0)
                    {
                        P_reg = new Regex(@"^P(T|(ZB))\d*" + part.Last().ToString());
                        nodes = IEDs.Where(ele => P_reg.IsMatch(ele.Attribute("name").Value)).Select(ied => ied);
                    }
                    if(nodes.Count()==0)
                        return;
                    node = nodes.First();
                    type = node.Attribute("name").Value.Substring(0, 1);
                } 
                else
                {
                    // 该 ExtRef 所引用的外部 LN 节点
                    string ied_name, ldInst, lnClass, lnInst;

                    // 遍历所有ExtRef节点，获得线路连接的母线
                    foreach(var element in mu_ext_refs)
                    {
                        // ExtRef 的属性信息
                        try
                        {
                            ied_name = element.Attribute("iedName").Value;
                            ldInst = element.Attribute("ldInst").Value;
                            lnClass = element.Attribute("lnClass").Value;
                            lnInst = element.Attribute("lnInst").Value;
                            if (lnInst == "")
                                continue;
                        }
                        catch (Exception)
                        {
                            continue;
                        }
                        // 获取对应的母线编号
                        int bus_no,index=0;
                        try
                        {
                            bus_no = int.Parse(reg.Match(ied_name).Value);
                        }
                        catch(Exception)
                        {
                            string name = reg.Match(ied_name).Value;
                            char[] ch = new char[] {'A','B','C','D','E','F','G','H' };
                            bus_no = int.Parse(name.Substring(0, 3))*10+ Array.IndexOf(ch,name[3])+1; 
                        }
                        if (bus_no % 100 > 10)
                        {
                            index = bus_no % 10;
                            line_bus_dic[component].Add(index);

                            index = (bus_no % 100 - bus_no % 10) / 10;
                            line_bus_dic[component].Add(index);
                            continue;
                        }
                        else
                        {
                            var target_IED = IEDs.Where(ele => ele.Attribute("name").Value == ied_name).AsParallel().First();
                            var target_ln = target_IED.Descendants(ns + "LDevice").Where(ele => ele.Attribute("inst").Value==ldInst).First();
                            target_ln = target_ln.Descendants(ns + "LN").
                                            Where(ele => ele.Attribute("lnClass").Value == lnClass && ele.Attribute("inst").Value == lnInst).First();

                            if (target_ln == null)
                                continue;
                            // 获取对应 LN 节点的描述信息
                            string desc = target_ln.Attribute("desc").Value;
                            desc = bus_seg_no.Match(desc).Value;

                            if (desc == "")
                                continue;

                            index = c_index.Contains(desc) ? Array.IndexOf(c_index, desc) + (bus_no % 10 - 1) * 2 : Array.IndexOf(roma_index, desc) + (bus_no % 10 - 1) * 2;
                            if (index < 0)
                                index = Array.IndexOf(d_index, desc);
                            line_bus_dic[component].Add(index);
                        }
                    }
                }
            }
            if(type == "P" || type == "S")
            {
                // 线路对应的ExtRef节点
                var p_ext_refs = node.Descendants(ns + "AccessPoint").
                                    Where(ele => ele.Attribute("name").Value.StartsWith("G")).
                                    Descendants(ns + "ExtRef").
                                    Where(ele=>ele.Attribute("iedName").Value.StartsWith("PM")).First();

                int bus_no = 0, index;
                string ied_name = p_ext_refs.Attribute("iedName").Value;
                ied_name = reg.Match(ied_name).Groups[1].Value;
                try
                {
                    bus_no = int.Parse(reg.Match(ied_name).Value);
                }
                catch (Exception)
                {
                    string name = reg.Match(ied_name).Value;
                    char[] ch = new char[] { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H' };
                    bus_no = int.Parse(name.Substring(0, 3)) * 10 + Array.IndexOf(ch, name[3]) + 1;
                }
                if (bus_no % 100 > 10)
                {
                        index = bus_no % 10;
                        line_bus_dic[component].Add(index);

                        index = (bus_no % 100 - bus_no % 10) / 10;
                        line_bus_dic[component].Add(index);
                    return;
                }
                else
                {
                    if (Buses_Relation[bus_no / 10] != null && Buses_Relation[bus_no / 10].ContainsKey("母联"))
                    {
                            line_bus_dic[component].Add(2 * bus_no % 10 - 1);
                            line_bus_dic[component].Add(2 * bus_no % 10);
                    }
                    else
                            line_bus_dic[component].Add(bus_no % 10);
                }
            }
        }

        public static void GetLinkToBreaker(string part)
        {
            Regex prot_reg = new Regex(@"^P" + part);
            string no;
            string component = part.Contains("L") ? "L" + reg_no.Match(part).Value : "T" + reg_no.Match(part).Value;
            try
            {
                // 过滤出线路保护IED
                var line_prot_ied = IEDs.Where(ied => prot_reg.IsMatch(ied.Attribute("name").Value));
                if (line_prot_ied.Count()==0)
                {
                    prot_reg = new Regex(@"^P(T|(ZB))\d*"+part.Last().ToString());
                    line_prot_ied = IEDs.Where(ied => prot_reg.IsMatch(ied.Attribute("name").Value));
                    if (line_prot_ied == null)
                        return;
                }
                // 线路保护IED的引用Ext_Ref
                var breaker_ext_refs = line_prot_ied.First().Descendants(ns+"AccessPoint").
                                        Where(ele=>ele.Attribute("name").Value.StartsWith("G")).
                                        Descendants(ns+"ExtRef").AsParallel();

                // 遍历线路对应的ExtRef节点
                foreach(var ele in breaker_ext_refs)
                {
                    // 引用IED名称，编号
                    var ied_name = ele.Attribute("iedName").Value;
                    if (!ied_name.StartsWith("P"))
                        continue;
                    no = reg_no.Match(ied_name).Value;
                    if (!no.StartsWith(component.Substring(1,2))||no[2]=='0')
                        continue;
                    // 还没存放过该线路-断路器关系信息
                    if (!Line_Breaker_Relation.ContainsKey(component))
                        Line_Breaker_Relation[component] = new SortedSet<string>();
                    // 添加断路器编号到关系字典中
                    Line_Breaker_Relation[component].Add(no);
                }
            }
            catch (Exception)
            {
                Line_Breaker_Relation = null;
            }
        }

        public static ISet<string> GetBreakers()
        {
            // 匹配规则
            string str_volt = (Volts["H"] / 10).ToString();
            Regex breaker_reg = new Regex(@"^P(B|(DL))" + str_volt + @"[1-9]\d");
            string break_no;

            // 高压IED信息
            var breakers = IEDs.Where(ied => breaker_reg.IsMatch(ied.Attribute("name").Value)).Select(ied=>ied.Attribute("name").Value);

            ISet<string> break_seg = new SortedSet<string>();
            foreach( var info in breakers)
            {
                break_no = reg_no.Match(info).Value;
                break_seg.Add(break_no.Substring(0, 3));
                Breaker_Suffix.Add(int.Parse(break_no.Substring(3, 1)));
            }
            return break_seg;
        }

        public static void ClassifyIEDs()
        {
            Regex line_reg = new Regex(@"^\wX?L(\d{4})\w?");          // 线路
            Regex trans_reg = new Regex(@"^\w(ZB)|T\w*\d+\w?");       // 主变
            Regex union_or_seg_reg = new Regex(@"(母联)|(分段)");     // 母联
            Regex breaker_reg = new Regex(@"^\w((DL)|B)\w*\d+\w?");   // 断路器 PW220B1
            Regex no_reg = new Regex(@"(\d+)");                       // 数字

            Device_IEDs = new Dictionary<string, Dictionary<string, List<string>>>();
            string[] low_level = new string[] { "10", "35", "66" };
            string type;
            // 分类IED
            foreach (var item in IEDs)
            {
                type = getIEDType(item);  // IED类型
                // 线路 IED 的处理
                string ied = line_reg.Match(item.Attribute("name").Value).Groups[1].Value;
                if (ied != "")
                {
                    ied = "L" + ied;
                    if (low_level.Contains(ied.Substring(1, 2)))
                        continue;
                    if (!Device_IEDs.ContainsKey(ied))
                        Device_IEDs[ied] = new Dictionary<string, List<string>>();

                    if (!Device_IEDs[ied].ContainsKey(type))
                        Device_IEDs[ied][type] = new List<string>();

                    Device_IEDs[ied][type].Add(item.Attribute("name").Value);
                    continue;
                }
                // 主变 IED 的处理
                Boolean is_trans = trans_reg.IsMatch(item.Attribute("name").Value);
                if (is_trans)
                {
                    ied = no_reg.Match(item.Attribute("name").Value).Groups[1].Value;

                    // 对于某些SCD文件，兼容性处理，变成4位长度的字符串数字：0001,3502，...
                    if (4 - ied.Length > 0)
                        ied = ied.Substring(0, ied.Length - 1) + new string('0', 4 - ied.Length) + ied.Last().ToString();

                    string level = ied.Substring(0, 2);
                    if (low_level.Contains(level) || ied.Last() == '0')
                        continue;

                    if (int.Parse(level) * 10 == Volts["H"])
                        ied = "T" + ied.Last().ToString() + "H";

                    else if (int.Parse(level) * 10 == Volts["M"])
                        ied = "T" + ied.Last().ToString() + "M";
                    else
                        ied = "T" + ied.Last().ToString() + "B";

                    if (type == "保护" || type == "保护测控")
                        ied = ied.Substring(0, 2) + "B";

                    if (!Device_IEDs.ContainsKey(ied))
                        Device_IEDs[ied] = new Dictionary<string, List<string>>();

                    if (!Device_IEDs[ied].ContainsKey(type))
                        Device_IEDs[ied][type] = new List<string>();

                    Device_IEDs[ied][type].Add(item.Attribute("name").Value);
                    continue;
                }
                // 母联|分段 IED 的处理
                ied = union_or_seg_reg.Match(item.Attribute("desc").Value).Value;
                if (ied != "")
                {
                    string no = reg_no.Match(item.Attribute("name").Value).Value;
                    ied = ied == "母联" ? "U" + no : "S" + no;
                    if (low_level.Contains(ied.Substring(1, 2)))
                        continue;
                    if (!Device_IEDs.ContainsKey(ied))
                        Device_IEDs[ied] = new Dictionary<string, List<string>>();

                    if (!Device_IEDs[ied].ContainsKey(type))
                        Device_IEDs[ied][type] = new List<string>();

                    Device_IEDs[ied][type].Add(item.Attribute("name").Value);
                    continue;
                }
                // 断路器的处理
                Boolean is_breaker = breaker_reg.IsMatch(item.Attribute("name").Value);
                if (is_breaker)
                {
                    string no = reg_no.Match(item.Attribute("name").Value).Value;
                    ied = "B" + no;
                    if (low_level.Contains(ied.Substring(1, 2)))
                        continue;
                    if (!Device_IEDs.ContainsKey(ied))
                        Device_IEDs[ied] = new Dictionary<string, List<string>>();

                    if (!Device_IEDs[ied].ContainsKey(type))
                        Device_IEDs[ied][type] = new List<string>();

                    Device_IEDs[ied][type].Add(item.Attribute("name").Value);
                    continue;
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Xml;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace SCDVisual
{
    class SCDResolver
    {
        // xml文件名称
        const string xml_file_path = "STHB.scd";
        // IED的name，desc信息
        static private List<string[]> IEDsInfo = new List<string[]>();
        // IED节点信息
        static private List<XmlElement> IEDList = new List<XmlElement>();
        // xml的namespace管理器
        static private XmlNamespaceManager nsmgr;
        // 编号对比查询数组
        public static string[] d_index = new[] { "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
        public static string[] c_index = new[] { "O", "I", "II", "III", "IV", "V", "VI", "VII", "VIII", "IX" };
        // 公用正则表达式
        static Regex bus_seg_no = new Regex(@"([1-9]|[IVX]+)");
        static Regex ied_no = new Regex(@"(\d{4})");

        // 高压侧、中压侧电压
        public static int High_Volt;
        public static int Mid_Volt;

        // 3/2断路器尾号
        public static ISet<int> breaker_no = new SortedSet<int>();

        // 主变信息
        public static IDictionary<int,string> transformers;

        // 母线信息
        public static IDictionary<int, ISet<int>> buses;

        // 线路信息
        public static IDictionary<int, IDictionary<string, string>> lines;

        // 断路器信息
        public static ISet<string> breakers;

        // 母线关系信息
        public static IDictionary<int, IDictionary<string, IDictionary<string, int[]>>> buses_relation;

        // 母线-线路连接关系信息
        public static IDictionary<string, ISet<int>> line_bus_relation;

        // 线路-断路器连接关系信息
        public static IDictionary<string, ISet<string>> line_breaker_relation = new SortedDictionary<string, ISet<string>>();

        // 主变-断路器连接关系信息
        public static IDictionary<int, ISet<string>> trans_breaker_relation;

        // 主变-母线连接关系信息
        public static IDictionary<string, ISet<int>> trans_bus_relation;

        /// <summary>
        /// 启动初始化，获取各参数信息
        /// </summary>
        public static void init()
        {

            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                // 加载xml文件
                xmlDoc.Load(xml_file_path);
                // 添加namespace
                nsmgr = new XmlNamespaceManager(xmlDoc.NameTable);
                nsmgr.AddNamespace("ns", "http://www.iec.ch/61850/2003/SCL");

                // 解析出IED的信息
                GetIEDsInfo(xmlDoc);
                // 获取线路
                lines = GetLines();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
                
            }
    
            // 获取主变
            transformers = Task<Object>.Run(() => GetTransformers()).Result;

            // 获取母线
            buses = Task<Object>.Run(() => GetBuses()).Result;

            // 获取`母线-母线`关系
            buses_relation = Task<Object>.Run(() => GetBusRelation()).Result;
                
            // 获取`线路-母线`关系
            line_bus_relation =  Task<Object>.Run(() => GetLineToBus()).Result;

            // 获取`主变-母线`的关系
            trans_bus_relation = Task<Object>.Run(() => GetTransToBus()).Result;

            if (High_Volt >= 500)
            {
                // 500kV及以上`线路-断路器`的关系，500kV及以上使用
                // var line_breker = line_breaker_relation;
                
                // 获取500kV及以上高压侧`主变-断路器`的关系，500kV及以上使用
                trans_breaker_relation = Task<Object>.Run(() => GetTransToBreaker()).Result;

                // 获取断路器
                breakers = Task<Object>.Run(() => GetBreakers()).Result;
            }

            Task.WaitAll();
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
            var IEDs = xmlDocument.GetElementsByTagName("IED").OfType<XmlElement>().AsParallel();

            // 提取每个IED节点的name,desc属性信息
            Parallel.ForEach(IEDs, (ied) => {
                string name = ied.GetAttribute("name");
                string desc = ied.GetAttribute("desc");
                lock (IEDsInfo)
                {
                    IEDsInfo.Add(new[] { name, desc });
                }
                lock (IEDList)
                {
                    IEDList.Add(ied);
                }
            });
            
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
            // 存储主变的数据结构
            var m_trans = new SortedDictionary<int, string>();
            var trans_reg = new Regex(@"(CT.*\d{4})|(CZB.*\d{4})");
            // 包含主变信息的IED节点
            var trans = IEDsInfo.Where(ied => trans_reg.IsMatch(ied[0])).Select(ied => ied).AsParallel();
            try
            {
                // 遍历主变信息IED节点
                Parallel.ForEach(trans, (info) => {
                    // 获取主变的编号信息
                    string m = ied_no.Match(info[0]).Value;
                    var key = int.Parse(m.Last().ToString());
                    // 存储器中是否包含该编号的主变信息。没有==>则添加
                    if (m == "")
                        return;

                    lock (m_trans)
                    {
                        if (!m_trans.ContainsKey(key))
                            m_trans[key] = key.ToString() + "#";
                    }
                });
            }
            catch(Exception)
            {
                m_trans = null;
            }

            return m_trans;
        }

        /// <summary>
        /// 获取母线信息，{110:[1,2],220:[1,2,3,4],...}
        /// </summary>
        /// <returns>
        /// 母线电压等级，编号
        /// </returns>
        private static IDictionary<int,ISet<int>> GetBuses()
        {
            Regex reg = new Regex(@"(\d{3,})");

            var buses = IEDsInfo.Where(ied => ied[0].StartsWith("CM")).Select(ied => ied).AsParallel();
            
            // 没有线路信息，直接返回
            // if (buses.Count() == 0)
            //    return null;

            var m_buses = new ConcurrentDictionary<int, ISet<int>>();
            m_buses[High_Volt] = null;
            m_buses[Mid_Volt] = null;

            // 电压等级的处理
            var low_level = new[] {0, 10, 35, 66 };

            try
            {
                // 遍历母线，获取各段母线，并按电压等级归类
                Parallel.ForEach(buses,(bus) => {
                    var value = reg.Match(bus[0]).Value;

                    int n = int.Parse(value.Last().ToString());

                    int level = int.Parse(value.Substring(0, 2));

                    if (low_level.Contains(level))
                        return;
                    level = level * 10;

                    // 将处理后的数据存放到数据结构中，并返回
                    lock (m_buses)
                    {
                        if (m_buses[level] == null)
                        {
                            ISet<int> lst = new SortedSet<int>();
                            m_buses[level] = lst;
                        }
                        m_buses[level].Add(n);
                    }
                });
            }
            catch(Exception)
            {
            }
            return m_buses;
        }

        /// <summary>
        /// 获取线路信息,{110:{"1101":"110kV大海I线","1102":"110kV海东线",...},220:{"2201":"xxx",...}}
        /// </summary>
        /// <returns>
        /// 按电压等级分类，存储该等级下的所有线路
        /// </returns>
        private static SortedDictionary<int, IDictionary<string, string>> GetLines()
        {
            // 线路匹配正则表达式
            Regex line_no = new Regex(@"^MX?L(\d{4})");
            Regex line_name = new Regex(@"(\d{2,4}\D{2,}\d?\D?线路?)|(\D*线)");
            
            // 线路IEDs
            var lines = IEDsInfo.Where(ied => line_no.IsMatch(ied[0])).Select(ied => ied).AsParallel();
            if (lines.Count() == 0)
                return null;

            var m_lines = new SortedDictionary<int, IDictionary<string, string>>();
            var low_level = new[] { 10, 35, 66 };

            // 遍历线路IED
            Parallel.ForEach(lines,(item) => {
                // 获得线路编号和名称
                var l_no = line_no.Match(item[0]).Groups[1].Value;
                var l_name = line_name.Match(item[1]).Value;

                // 去除低压线路
                var level = int.Parse(l_no.Substring(0, 2));
                if (low_level.Contains(level))
                    return;
                level = level * 10;

                if(l_name=="") 
                    l_name = level.ToString()+"kV线路" + l_no.Substring(1, 3);

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

            List<int> volts = m_lines.Keys.ToList();
            // 获取高，中压等级电压
            High_Volt = volts.Max();
            volts.Remove(High_Volt);
            if (volts.Count == 0)
                Mid_Volt = 35;
            else
                Mid_Volt = volts.Max();

            return m_lines;
        }

        /// <summary>
        /// 获取母线连接关系，{110:{ "分段":{"1101": [1,2] ,"1102": [3,4] } , "母联":{"1103": [1,3] ,"1104": [2,4] } },35:...... }
        /// </summary>
        /// <returns>按电压等级，分段，母联关系列出所有连接关系</returns>
        private static IDictionary<int,IDictionary<string,IDictionary<string,int[]>>> GetBusRelation()
        {
            Regex relation = new Regex(@"([1-9]|[IVX]+)-([1-9]|[IVX]+)|([123567][012356][1-9]{2})");
            Regex relation_no = new Regex(@"(\d{3,})");

            var bus_relation = IEDsInfo.Where(ied => ied[1].Contains("母联") || ied[1].Contains("分段")).Select(ied => ied).AsParallel();

            if (bus_relation.Count() == 0)
                return null;

            var m_relation = new SortedDictionary<int, IDictionary<string, IDictionary<string, int[]>>>();
            m_relation[High_Volt] = null;
            m_relation[Mid_Volt] = null;

            var low_level = new[] { 10, 35, 66 };

            try
            {
                // 遍历包含母线连接关系的IED，分类出母联和分段的母线关系
                Parallel.ForEach(bus_relation, (item) =>
                {
                    // 包含母线关系的的desc文字信息提取部分，I-II, 2212 ...
                    var m_part = relation.Match(item[1]).Value;
                    // 母线编号及母线电压等级
                    var no = relation_no.Match(item[0]).Value;
                    var level = int.Parse(no.Substring(0, 2));
                    if (low_level.Contains(level))
                    {
                        return;
                    }
                    level = level * 10;
                    // 母线关系的数组，存储有关系的母线段
                    int[] seg_arr = null;

                    // 不是有效的母线关系
                    if (no == "")
                        return;
                    
                    // 母线关系存在，对seg_arr数组进行赋值，包含两段关联的母线段
                    if (no[2] != '0')
                    {
                        seg_arr = new int[2];
                        seg_arr[0] = Array.IndexOf(d_index, no[2].ToString());
                        seg_arr[1] = Array.IndexOf(d_index, no[3].ToString());
                    }
                    else
                    {
                        if (m_part.Contains("-"))
                        {
                            seg_arr = new int[2];
                            seg_arr[0] = Array.IndexOf(c_index, m_part.Split('-')[0].Trim());
                            seg_arr[1] = Array.IndexOf(c_index, m_part.Split('-')[1].Trim());
                        }   
                    }

                    // 若是新的电压等级，创建新的存储结构
                    if (m_relation[level] == null)
                    {
                        lock (m_relation)
                        {
                            m_relation[level] = new SortedDictionary<string, IDictionary<string, int[]>>();
                        }
                    }
                    // 判断关联类型，存储关联部分到关系数据结构中
                    var r = item[1].Contains("分段") ? "分段" : "母联";
                    switch (r)
                    {
                        // 分段关系的存储
                        case "分段":
                            lock (m_relation)
                            {
                                if (!m_relation[level].ContainsKey("分段"))
                                {
                                    m_relation[level]["分段"] = null;
                                }
                                if (seg_arr is null)
                                    break;
                                if (m_relation[level]["分段"] == null)
                                {
                                    m_relation[level]["分段"] = new SortedDictionary<string, int[]>();
                                }
                                m_relation[level]["分段"][no] = seg_arr;
                            }
                            break;

                        // 母联关系的存储
                        case "母联":
                            lock (m_relation)
                            {
                                if (!m_relation[level].ContainsKey("母联"))
                                {
                                    m_relation[level]["母联"] = null;
                                }
                                if (seg_arr is null)
                                    break;
                                if (m_relation[level]["母联"] == null)
                                {
                                    m_relation[level]["母联"] = new SortedDictionary<string, int[]>();
                                }
                                m_relation[level]["母联"][no] = seg_arr;
                            }
                            break;
                        // 默认情况
                        default:
                            break;
                    }
                });
            }
            catch(Exception)
            {
                m_relation = null;
            }
            return m_relation;
        }

        /// <summary>
        /// 获取线路与母线的连接关系，{"2201":[1,2],"1102":[1],"1103":[2],...}
        /// </summary>
        /// <returns>按线路名称，所连接母线段组成键值对</returns>
        private static IDictionary<string,ISet<int>> GetLineToBus()
        {
            Regex reg = new Regex(@"^M.*L(\d{4})");

            var all_lines = lines.SelectMany(line => line.Value.Keys).Select(name => name).ToArray();

            // 获得一个线路合并单元的可迭代对象
            var mu_ieds = IEDList.Where(ele => reg.IsMatch(ele.GetAttribute("name"))).Select(ied => ied).AsParallel();

            SortedDictionary<string, ISet<int>> line_bus_dic = new SortedDictionary<string, ISet<int>>();

            Parallel.ForEach(mu_ieds, (e) => {
                // MU 的 IED 的 `name`
                var name = e.GetAttribute("name");
                // 线路编号, e.g `2202`
                var line = reg.Match(name).Value;
                line = reg.Split(line)[1];

                // 过滤掉非线路和过掉以处理过的线路
                if (!all_lines.Contains(line))
                    return;
                // 500kV及以上高压部分，获取其与断路器的关系
                if (int.Parse(line.Substring(0, 2)) * 10 >= 500)
                {
                    GetLineToBreaker(line);
                    return;
                }
                // 新线路，生成新的存储结构
                if (!line_bus_dic.ContainsKey(line) || line_bus_dic[line].Count == 0)
                {
                    lock (line_bus_dic)
                    {
                        if(!line_bus_dic.ContainsKey(line))
                            line_bus_dic[line] = new SortedSet<int>();
                    }
                    // 获取该线路MU对应的外部母线引用
                    FindReference(e, line_bus_dic);
                }
            });

            return line_bus_dic;
        }

        /// <summary>
        /// 解析得到线路连接到的母线段
        /// </summary>
        /// <param name="line">对应线路名称</param>
        /// <param name="ext_refs">线路对应的引用节点可迭代对象</param>
        /// <param name="line_bus_dic">要存储的字典</param>
        private static void FindReference(XmlElement node, IDictionary<string, ISet<int>> line_bus_dic)
        {

            // 线路名称
            var mu_name = node.GetAttribute("name");
            // 线路编号
            var line = ied_no.Match(node.GetAttribute("name")).Value;
            // 线路对应的ExtRef节点
            var mu_ext_refs = node.SelectNodes("//ns:IED[@name='" + mu_name + "']/ns:AccessPoint[starts-with(@name,'M')]/ns:Server/ns:LDevice[starts-with(@inst,'MU')]/ns:LN0/ns:Inputs/ns:ExtRef", nsmgr).OfType<XmlElement>().AsParallel();

            // 该 ExtRef 所引用的外部 LN 节点
            XmlElement target_ln;
            string desc = "";

            // 遍历所有ExtRef节点，获得线路连接的母线
            Parallel.ForEach(mu_ext_refs, (element) => {
                // ExtRef 的属性信息
                var ied_name = element.GetAttribute("iedName");
                var ldInst = element.GetAttribute("ldInst");
                var lnClass = element.GetAttribute("lnClass");
                var lnInst = element.GetAttribute("lnInst");

                // 对于非提供电压信息的ExtRef，直接跳过
                if (lnInst == "")
                    return;

                // 获取对应的母线编号
                var bus_no = int.Parse(ied_no.Match(ied_name).Value);
                if (bus_no % 100 > 10)
                {
                    lock (line_bus_dic)
                    {
                        int index = bus_no % 10;
                        line_bus_dic[line].Add(index);

                        index = (bus_no % 100 - bus_no % 10) / 10;
                        line_bus_dic[line].Add(index);
                    }
                    return;
                }

                try
                {
                    var target_IED = IEDList.Where(ele => ele.GetAttribute("name") == ied_name).AsParallel().First<XmlNode>();

                    target_ln = (XmlElement)target_IED.SelectSingleNode("//ns:IED[@name='" + ied_name + "']/ns:AccessPoint/ns:Server/ns:LDevice[@inst='" + ldInst + "']/ns:LN[@lnClass='" + lnClass + "' and @inst='" + lnInst + "']", nsmgr);

                    // 获取对应 LN 节点的描述信息
                    desc = target_ln.GetAttribute("desc");
                    desc = bus_seg_no.Match(desc).Value;

                    if (desc == "")
                        return;

                    var index = c_index.Contains(desc) ? Array.IndexOf(c_index, desc) + (bus_no % 10 - 1) * 2 : Array.IndexOf(d_index, desc) + (bus_no % 10 - 1) * 2;
                    if (index < 0)
                        return;
                    lock (line_bus_dic)
                    {
                        line_bus_dic[line].Add(index);
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine("Not reqired ExtRef node.");
                }
            });
        }

        /// <summary>
        /// 获取主变中高压侧与母线的连接关系，{"2201":[1,2],"1102":[2],"1103":[3],...}
        /// </summary>
        /// <returns>按主变各侧，所连接母线段组成键值对</returns>
        private static IDictionary<string,ISet<int>> GetTransToBus()
        {
            Regex reg = new Regex(@"^M.*(\d{4})");
            var trans = IEDList.Where(e => reg.IsMatch(e.GetAttribute("name")) && e.GetAttribute("desc").Contains("主变") && !e.GetAttribute("desc").Contains("电压")).Select(ied => ied).AsParallel();

            var trans_dic = new SortedDictionary<string, ISet<int>>();

            // 获取主变各侧的ExtRef节点，解析得到所连接的母线
            Parallel.ForEach(trans, (item) =>
            {
                var name = ied_no.Match(item.GetAttribute("name")).Value;
                var level = int.Parse(name.Substring(0, 2));

                // 低压部分，已存在字典中，直接跳过
                var low_evel = new[] { 0, 10, 35, 66 };
                if (low_evel.Contains(level))
                    return;

                // 关系字典中存在该主变，跳过
                if (!trans_dic.ContainsKey(name) || trans_dic[name].Count==0)
                {
                    lock (trans_dic)
                    {
                        if(!trans_dic.ContainsKey(name))
                        {
                            // 添加主变到存储结构中
                            trans_dic[name] = new SortedSet<int>();
                        }
                    }
                    // 查找该主变关联的母线
                    FindReference(item, trans_dic);
                }
            });
            return trans_dic;
        }

        /// <summary>
        /// 获取500kV及以上线路与断路器的关系，{"5001":["5011","5012"], "5002":["5022","5023"],...}
        /// </summary>
        /// <param name="line">线路编号</param>
        private static void GetLineToBreaker(string line)
        {
            Regex prot_reg = new Regex(@"^P.*L"+line);

            try
            {
                // 过滤出线路保护IED
                var line_prot_ied = IEDList.Where(ied => prot_reg.IsMatch(ied.GetAttribute("name"))).OfType<XmlElement>().First();
                // 线路保护IED的引用Ext_Ref
                var breaker_ext_refs =line_prot_ied.SelectNodes("//ns:IED[@name='" + line_prot_ied.GetAttribute("name") + "']/ns:AccessPoint[starts-with(@name,'M')]/ns:Server/ns:LDevice[contains(@inst,'SV')]/ns:LN0/ns:Inputs/ns:ExtRef", nsmgr).OfType<XmlElement>().AsParallel();
                
                // 遍历线路对应的ExtRef节点
                Parallel.ForEach(breaker_ext_refs, (ele) => {
                    // 引用IED名称，编号
                    var ied_name = ele.GetAttribute("iedName");
                    var no = ied_no.Match(ied_name).Value;
                    // 匹配IED编号
                    if (no == line)
                        return;

                    lock (line_breaker_relation)
                    {
                        // 还没存放过该线路-断路器关系信息
                        if (!line_breaker_relation.ContainsKey(line))
                        {
                                line_breaker_relation[line] = new SortedSet<string>();
                        }
                        // 添加断路器编号到关系字典中
                        line_breaker_relation[line].Add(no);
                    }
                });

                // 只找到一个外部IED，就再加上自身
                if (line_breaker_relation[line].Count == 1)
                    line_breaker_relation[line].Add(line);
            }
            catch (Exception)
            {
                line_breaker_relation = null;
            }            
        }

        /// <summary>
        /// 获取500kV及以上变压器与断路器的连接关系，{1:["5021","5022"],2:["5042","5043"],...}
        /// </summary>
        /// <returns>按 `主变序号-所连接断路器编号` 组成键值对</returns>
        private static IDictionary<int,ISet<string>> GetTransToBreaker()
        {
            // 正则匹配表达式
            Regex prot_reg = new Regex(@"P[T|(ZB)].*\d{1,4}");
            Regex reg_no = new Regex(@"\d{1,}");
            // 主变保护IED过滤列表
            var trans = IEDList.Where(e => prot_reg.IsMatch(e.GetAttribute("name")) && e.GetAttribute("desc").Contains("主变") && e.GetAttribute("desc").Contains("保护")).Select(ied => ied).AsParallel();

            var trans_breaker_relation = new SortedDictionary<int, ISet<string>>();
            // 遍历主变保护IED
            Parallel.ForEach(trans, (item) => {
                try
                {
                    // 保护IED名称，编号
                    string ied_name = item.GetAttribute("name");
                    var trans_no = int.Parse(reg_no.Match(ied_name).Value.Last().ToString());

                    if (trans_breaker_relation.ContainsKey(trans_no))
                        return;
                    lock (trans_breaker_relation)
                    {
                        trans_breaker_relation[trans_no] = new SortedSet<string>();
                    }
                    // 保护IED下面的对应引用Ext_Refs节点
                    var breaker_ext_refs = item.SelectNodes("//ns:IED[@name='" + ied_name + "']/ns:AccessPoint[starts-with(@name,'G')]/ns:Server/ns:LDevice[starts-with(@inst,'PI')]/ns:LN0/ns:Inputs/ns:ExtRef", nsmgr).OfType<XmlElement>().AsParallel();
                    // 遍历对应的ExtRef节点
                    Parallel.ForEach(breaker_ext_refs, (ele) => {
                        // 获取引用IED名称，编号
                        var ext_ied = ele.GetAttribute("iedName");
                        var no = ied_no.Match(ext_ied).Value;

                        if (int.Parse(no.Substring(0, 2)) < 50 || no[2] == '0')
                            return;
                        // 添加到对应主变高压侧关系对象中
                        lock (trans_breaker_relation)
                        {
                            trans_breaker_relation[trans_no].Add(no);
                        }
                    });
                }
                catch (Exception)
                {
                    trans_breaker_relation = null;
                }
            });
           
            return trans_breaker_relation;
        }

        /// <summary>
        /// 获取500kV及以上断路器间隔，["501","502","503",...]
        /// </summary>
        /// <param name="volt">电压等级</param>
        /// <returns>断路器间隔编号前3位的集合</returns>
        private static ISet<string> GetBreakers()
        {
            // 匹配规则
            string str_volt = High_Volt.ToString().Substring(0, 2);
            Regex breaker_reg = new Regex(str_volt + @"[1-9]\d");
            
            // 高压IED信息
            var breakers = IEDsInfo.Where(info => breaker_reg.IsMatch(info[0])).Select(info=>info[0]).AsParallel();

            ISet<string> break_seg = new SortedSet<string>();
            Parallel.ForEach(breakers, (info) => {
                // 获取断路器间隔编号，并添加到集合中
                var b_no = breaker_reg.Match(info).Value;
                lock (break_seg)
                {
                    break_seg.Add(b_no.Substring(0, 3));
                    breaker_no.Add(int.Parse(b_no.Substring(3, 1)));
                }
            });
            
            return break_seg;
        }
    }
}

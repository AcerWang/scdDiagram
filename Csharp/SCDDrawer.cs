using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace SCDVisual
{
    class SCDDrawer
    {
        // 要使用的svg模板和处理的SVG节点
        private static XmlDocument html = new XmlDocument();
        private static XmlNode svg;

        // 保存已输出的主变位置信息
        private static IDictionary<int,int[]> trans_location = new Dictionary<int,int[]>();

        // 保存已输出的母线位置信息
        private static IDictionary<int, IDictionary<int,int[]>> buses_location = new Dictionary<int,IDictionary<int,int[]>>();

        // 保存已输出的线路位置信息
        private static IDictionary<string, int[]> lines_location = new Dictionary<string, int[]>();

        // 记录某电压等级的某母线上已有多少条线路
        private static IDictionary<int,IDictionary<int,int>> bus_line_num = new Dictionary<int,IDictionary<int,int>>();

        // 保存电压等级
        private static int High_volt;
        private static int Mid_volt;

        public static void Main(string[] args)
        {
            SCDResolver.init();
            try
            {
                html.Load("base.html");
                svg = html.SelectSingleNode("/html/body/svg");

            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
            }

            DrawTransformer();
            html.Save("index.html");
            Console.WriteLine("Draw done.");


            Console.ReadLine();
        }

        /// <summary>
        /// 通过解析得到的主变信息画出对应主变位置图，保存主变位置信息
        /// </summary>
        private static void DrawTransformer()
        {
            var trans = SCDResolver.transformers;

            if (trans == null)
                return;

            // 主变的起始坐标
            int x = 0, y = 450;
            // 遍历所有主变信息，生成各主变元素
            foreach (int trans_no in trans.Keys)
            {
                // 确定主变位置坐标
                x = 300 + 400 * (trans_no-1);

                // 画主变
                draw_component(x,y,"#Trans");

                // 显示主变对应的文字信息
                draw_text((string)trans[trans_no], x - 20, y + 25);

                // 保存此主变位置信息
                trans_location[trans_no] = new int[] {x,y };
            }
        }

        /// <summary>
        /// 通过解析得到的母线信息画出对应母线的母联关系图，保存母线位置信息
        /// </summary>
        private static void Draw_Bus_Union()
        {
            var buses = SCDResolver.buses;
            int[] volts = buses.Keys.OfType<int>().ToArray();
            var num = volts.Count();
            if (num < 2)
                return;

            // 获取高，中压等级电压
            High_volt = volts[num - 1];
            Mid_volt  = volts[num - 2];

            // 高压侧是500kV及以上时，按照3/2接线方式处理
            if (High_volt >= 500)
            {
                int x = 50, y = 200;
                // 画3/2接线母线
                foreach (int i in SCDResolver.buses[High_volt])
                {
                    // 画一条母线
                    draw_single_line(High_volt.ToString() + "kV", i, x, y, x + 1200, y);
                    draw_text(SCDResolver.c_index[i], x - 20, y + 5);
                    // 存储位置信息
                    buses_location[High_volt] = new Dictionary<int, int[]>();
                    buses_location[High_volt][i] = new int[] { x, y };

                    // 更新y坐标
                    y = y + 150;
                }
            }
            // 高压侧是220kV及以下时，按照正常逻辑处理
            else
            {
                draw_single_side_bus_union(High_volt);
            }

            // 中压侧母线
            if (Mid_volt < 100)
                return;

            // 普通接线，按照上述高压侧操作
            draw_single_side_bus_union(Mid_volt);
        }

        /// <summary>
        /// 通过解析得到的母线信息画出对应母线的分段关系图，保存母线位置信息
        /// </summary>
        private static void Draw_Bus_Seg()
        {   
            // 高压侧存在分段
            if(SCDResolver.buses_relation.ContainsKey(High_volt) && SCDResolver.buses_relation[High_volt].ContainsKey("分段"))
            {
                draw_single_side_bus_seg(High_volt);
            }

            // 中压侧存在分段
            if (SCDResolver.buses_relation.ContainsKey(Mid_volt) && SCDResolver.buses_relation[Mid_volt].ContainsKey("分段"))
            {
                draw_single_side_bus_seg(Mid_volt);
            }
        }

        /// <summary>
        /// 通过解析得到的线路信息画出响应线路，保存线路位置信息
        /// </summary>
        private static void Draw_Lines()
        {
            // 高压侧在500kV 及以上，用 3/2 画法
            if (High_volt >= 500)
            {
                var iter = SCDResolver.lines[High_volt];
                foreach(var line in iter)
                {
                    draw_one_half_line(line.Key);
                }
            }
            // 否则，高压侧是普通接线方式
            else
            {

                var iter = SCDResolver.lines[High_volt];
                foreach(var line in iter)
                {
                    draw_normal_line(line.Key,High_volt);
                }
            }
            // 中压侧小于 100kV, 退出
            if (Mid_volt < 100)
                return;
            // 中压侧是普通接线
            else
            {
                var iter = SCDResolver.lines[Mid_volt];
                foreach (var line in iter)
                {
                    draw_normal_line(line.Key);
                }
            }
        }


        private static void draw_normal_line(string line, int volt_level)
        {
            // `线路-母线` 关系不存在，由母线数量确定其位置
            if (!SCDResolver.line_bus_relation.ContainsKey(line))
            {
                // 存在该电压等级的母线，则线路画在该母线上
                int i = (buses_location[volt_level].Count == 1) ? 1 : 2;
                int x = buses_location[volt_level][i][0];
                int y = buses_location[volt_level][i][1];
            }
        }

        /// <summary>
        /// 画中压、高压侧母联接线
        /// </summary>
        /// <param name="Side">Hight_volt / Mid_volt</param>
        private static void draw_single_side_bus_union(int Side)
        {
            string color = (Side == High_volt) ? "red" : "blue";
            string href = (Side == High_volt) ? "#BusUnion" : "#BusUnion-down";
            // 不存在关联关系的母线，直接画单独分段线段
            if (SCDResolver.buses_relation[Side].Count() == 0)
            {
                int x = 50, y = (Side == High_volt) ? 350 : 600;
                // 每一段母线的长度
                int seg_length = line_seg_length(SCDResolver.buses[Side].Count);
                // 每一段，单独画，水平排列
                foreach (var i in SCDResolver.buses[Side])
                {
                    // 画一条母线
                    draw_single_line(Side.ToString() + "kV", i, x, y, x + seg_length, y,color);
                    draw_text(SCDResolver.c_index[i], x - 20, y + 5);
                    // 存储坐标
                    if (!buses_location.ContainsKey(Side))
                        buses_location[Side] = new Dictionary<int, int[]>();
                    buses_location[Side][i] = new int[] { x, y };

                    // 更新x坐标
                    x = x + seg_length + 50;
                }
            }

            // 这一侧有母联的情况
            if (SCDResolver.buses_relation[Side].ContainsKey("母联"))
            {
                // 有母联关系，但各段母线未给出，此情况直接按并联两条母线画图
                if (SCDResolver.buses_relation[Side]["母联"] == null)
                {
                    // 之前的解析结果只有一条母线，则补齐另一条
                    if (SCDResolver.buses[Side].Count == 1)
                        SCDResolver.buses[Side].Add(SCDResolver.buses[Side].Last() + 1);

                    int x = 50, y = (Side == High_volt) ? 350 : 600;
                    int dy = (Side == High_volt) ? -40 : 40;
                    // 画两条并联母线
                    foreach (int i in SCDResolver.buses[Side])
                    {
                        // 画一条母线
                        draw_single_line(Side.ToString() + "kV", i, x, y, x + 1200, y, color);
                        draw_text(SCDResolver.c_index[i], x - 20, y + 5);
                        // 存储母线位置
                        if (!buses_location.ContainsKey(Side))
                            buses_location[Side] = new Dictionary<int, int[]>();
                        buses_location[Side][i] = new int[] { x, y };

                        // 调整坐标
                        y = y + dy;
                    }

                    // 并联两母线，画出母联
                    draw_component(x + 20, y + 40, href, color);
                }

                // 多组母联
                else
                {
                    var relation = SCDResolver.buses_relation[Side]["母联"].Values;
                    var relation_res = union_seg(relation);
                    // 初始坐标，及每段母线的长度
                    int seg_length = line_seg_length(relation_res.Count), x = 50, y = (Side == High_volt) ? 350 : 600;
                    int dy = (Side == High_volt) ? 40 : -40;
                    // 画母线
                    foreach (var item in relation_res)
                    {
                        // 画内侧的base母线
                        draw_single_line(Side.ToString() + "kV", item.Key, x, y, x + seg_length, y, color);
                        draw_text(SCDResolver.c_index[item.Key], x - 20, y + 5);
                        // 保存母线位置信息
                        if (!buses_location.ContainsKey(Side))
                            buses_location[Side] = new Dictionary<int, int[]>();
                        buses_location[Side][item.Key] = new int[] { x, y };
                        // 调整坐标
                        y = y - 40;
                        int partLen = (seg_length - (item.Value.Count - 1) * 50) / item.Value.Count;
                        int x2 = x + partLen;

                        // 画与base母线对应的外侧母线
                        foreach (var i in item.Value)
                        {
                            // 画母线
                            draw_single_line(Side.ToString() + "kV", i, x, y, x2, y, color);
                            draw_text(SCDResolver.c_index[i], x - 20, y + 5);
                            // 保存母线位置信息
                            buses_location[Side][i] = new int[] { x, y };
                            // 调整坐标
                            x = x2 + 50;
                            x2 = x + partLen;
                        }
                        x = x2 - x + 100;
                        x2 = x + seg_length;
                        y = y + dy;
                    }
                }
            }
        }

        /// <summary>
        /// 画中高压侧分段接线
        /// </summary>
        /// <param name="Side">电压等级</param>
        private static void draw_single_side_bus_seg(int Side)
        {
            string color = (Side == High_volt) ? "red" : "blue";
            string href = "#BusSeg";
            int x = 50, y = (Side == High_volt) ? 350 : 600;
            int dy = (Side == High_volt) ? 40 : -40;

            // 默认二分段
            if (SCDResolver.buses_relation[Side]["分段"] == null)
            {
                // 之前解析结果只有一条母线，补齐另一条
                if (SCDResolver.buses[Side].Count == 1)
                    SCDResolver.buses[Side].Add(SCDResolver.buses[Side].Last() + 1);
                // 并联画两条母线
                foreach (int i in SCDResolver.buses[Side])
                {
                    // 记录位置信息
                    if (!buses_location.ContainsKey(Side))
                        buses_location[Side] = new Dictionary<int, int[]>();
                    if (!buses_location[Side].ContainsKey(i))
                    {
                        // 画母线
                        draw_single_line(Side.ToString() + "kV", i, x, y, x + 600, y, color);
                        draw_text(SCDResolver.c_index[i], x - 20, y + 5);
                        // 记录位置信息
                        buses_location[Side][i] = new int[] { x, y };
                    }
                    // 调整坐标
                    x += 650;
                }
                // 画分段开关
                draw_component(640, 330, href, color);
            }

            // 多分段情况
            else
            {
                // 分段组数
                int seg_num = SCDResolver.buses_relation[Side]["分段"].Count;

                List<int> longList = new List<int>();
                foreach (int[] arr in SCDResolver.buses_relation[Side]["分段"].Values)
                {
                    longList.AddRange(arr);
                }
                // 总的合并段数，去除重复段
                ISet<int> set = new SortedSet<int>(longList);
                var total_seg_num = set.Count;

                int part_len = 1200 ;

                // 并联型分段
                // -----------^^^----------^^^-----------^^^---------
                // -----------^^^----------^^^-----------^^^---------
                if (total_seg_num - seg_num == 2)
                {
                    part_len = line_seg_length(seg_num);
                    // 记录画的组数
                    int n = 0;
                    // 画分段母线，并联型画法
                    foreach (var item in SCDResolver.buses_relation[Side]["分段"].Values)
                    {
                        n++;
                        foreach (var i in item)
                        {   // 初次画该电压等级的母线
                            if (!buses_location.ContainsKey(Side))
                                buses_location[Side] = new Dictionary<int, int[]>();

                            // 存在该电压等级，判断该段母线是否存在记录中
                            if (!buses_location[Side].ContainsKey(i))
                            {
                                // 画母线
                                draw_single_line(Side + "kV", i, x, y, x + part_len, y, "red");
                                draw_text(SCDResolver.c_index[i], x - 20, y + 5, "red");

                                // 记录位置信息
                                buses_location[Side][i] = new int[] { x, y };
                            }
                            // 调整x坐标
                            x = x + part_len + 50;
                        }
                        // 画出分段开关
                        draw_component(buses_location[Side][item[0]][0] + part_len - 25, y - 25, "#BusSeg", "red");
                        if (n % 2 == 1)
                        {
                            // 调整坐标
                            y = y - dy;
                            x = buses_location[Side][item[0]][0];
                        }
                        else
                            y = y + dy;
                    }
                }
                // 串联型分段
                // ----------^^^-----------^^^-----------^^^---------
                if (total_seg_num - seg_num == 1)
                {
                    part_len = line_seg_length(total_seg_num);
                    // 画分段母线，串联型画法
                    foreach (var item in SCDResolver.buses_relation[Side]["分段"].Values)
                    {
                        foreach (var i in item)
                        {   // 初次画该电压等级的母线
                            if (!buses_location.ContainsKey(Side))
                                buses_location[Side] = new Dictionary<int, int[]>();

                            // 存在该电压等级，判断该段母线是否存在记录中
                            if (!buses_location[Side].ContainsKey(i))
                            {
                                // 画母线
                                draw_single_line(Side + "kV", i, x, y, x + part_len, y, "red");
                                draw_text(SCDResolver.c_index[i], x - 20, y + 5, "red");

                                // 记录位置信息
                                buses_location[Side][i] = new int[] { x, y };
                            }
                            // 调整坐标
                            x = x + part_len + 50;
                        }
                        // 画出分段开关
                        draw_component(buses_location[Side][item[0]][0] + part_len - 25, y - 25, "#BusSeg", "red");
                    }
                }

            }
        }

        /// <summary>
        /// 按所给参数，画一条母线
        /// </summary>
        /// <param name="prefix">母线id前缀</param>
        /// <param name="id">母线编号</param>
        /// <param name="x1">母线起点横坐标</param>
        /// <param name="y1">母线起点纵坐标</param>
        /// <param name="x2">母线终点横坐标</param>
        /// <param name="y2">母线终点纵坐标</param>
        /// <param name="color">母线颜色</param>
        private static void draw_single_line(string prefix, int id, int x1, int y1, int x2, int y2, string color="red")
        {
            Dictionary<string, string> ele_attrs = new Dictionary<string, string>()
                            {
                                { "id", prefix+"_"+id.ToString() } ,
                                { "x1", x1.ToString() } ,
                                { "y1", y1.ToString() } ,
                                { "x2", x2.ToString() } ,
                                { "y2", y2.ToString() } ,
                                { "stroke", color } ,
                                { "stroke-width", "5" }
                            };
            // 创建新节点
            XmlElement element = NewElement("line", ele_attrs);
            // 添加元素节点到svg节点后面
            svg.AppendChild(element);

            // 创建对应的文字信息
            Dictionary<string, string> text_attrs = new Dictionary<string, string>()
                            {
                                { "dy", "0" } ,
                                { "stroke", "black" } ,
                                { "stroke-width", "0.5" } ,
                                { "x", (x1-20).ToString() } ,
                                { "y", (y1+5).ToString()}
                            };
            // 添加文字节点大svg节点后面
            XmlElement text = NewElement("text", text_attrs);
            text.InnerText = SCDResolver.c_index[id];
            svg.AppendChild(text);
        }

        /// <summary>
        /// 根据TagName新建一个XmlElement,并为其设置各项属性值信息
        /// </summary>
        /// <param name="name">XmlElement的TagName，字符串类型</param>
        /// <param name="attrs">XmlElement的属性集，字典类型</param>
        /// <returns>返回创建好的XmlElement元素</returns>
        private static XmlElement NewElement(string name, Dictionary<string,string> attrs)
        {
            // 创建子节点元素
            XmlElement element = html.CreateElement(name);
            // 设置元素的属性
            foreach (var attr in attrs)
            {
                element.SetAttribute(attr.Key,attr.Value);
            }
            return element;

        }

        /// <summary>
        /// 确定分段母线每段的长度
        /// </summary>
        /// <returns>返回每段的长度</returns>
        private static int line_seg_length(int n)
        {
            int seg_len = 0;
            switch (n)
            {
                case 1:
                    seg_len = 1200;
                    break;
                case 2:
                    seg_len = 600;
                    break;
                case 3:
                    seg_len = 400;
                    break;
                case 4:
                    seg_len = 300;
                    break;
                default:
                    seg_len = 200;
                    break;
            }
            return seg_len;
        }

        /// <summary>
        /// 创建元件节点
        /// </summary>
        /// <param name="x">元件显示的横坐标</param>
        /// <param name="y">元件显示的纵坐标</param>
        /// <param name="href">引用的元件</param>
        /// <param name="color">元件颜色</param>
        private static void draw_component(int x, int y,string href, string color="red")
        {
            // 元件的基本属性信息
            Dictionary<string,string> attrs = new Dictionary<string, string> {
                { "x", x.ToString() } ,
                { "y", y.ToString() } ,
                { "href", href },
                { "stroke", color }
            };
            // 添加元件节点到svg节点后面
            XmlElement ele = NewElement("use",attrs);
            svg.AppendChild(ele);

        }

        /// <summary>
        /// 创建文字节点
        /// </summary>
        /// <param name="text">要显示的文字信息</param>
        /// <param name="x">文字显示的起点横坐标</param>
        /// <param name="y">文字显示的起点纵坐标</param>
        /// <param name="color"></param>
        private static void draw_text(string text, int x,int y, string color = "black")
        {
            // 文字元素的基本信息
            Dictionary<string, string> text_attrs = new Dictionary<string, string>()
                {
                    { "dy", "0" } ,
                    { "stroke", color } ,
                    { "stroke-width", "0.5" } ,
                    { "x", x.ToString() } ,
                    { "y", y.ToString()}
                };
            // 添加文字节点大svg节点后面
            XmlElement ele_text = NewElement("text", text_attrs);
            ele_text.InnerText = text;
            svg.AppendChild(ele_text);
        }

        /// <summary>
        /// 处理母联很多组的情况，{1:[2], 3:[4], 5:[6,7]}
        /// </summary>
        /// <param name="bus_relation">传入的原始母联分组情况</param>
        /// <returns>返回分好的类别</returns>
        private static Dictionary<int,List<int>> union_seg(ICollection<int[]> bus_relation)
        {
            // 传入的量拷贝一份副本供操作
            List<int[]> relation_copy = new List<int[]>();
            foreach(int[] arr in bus_relation)
            {
                int[] dest = new int[2];
                Array.Copy(arr,dest,2);
                relation_copy.Add(dest);
            }
            
            // 要返回的对象
            Dictionary<int, List<int>> res = new Dictionary<int, List<int>>();
            // 分离处理逻辑
            while (relation_copy.Count > 0)
            {
                int a0 = relation_copy[0][0], a1 = relation_copy[0][1];
                relation_copy.Remove(relation_copy[0]);
                Boolean flag = false;
                    
                for(int i=0; i<relation_copy.Count;i++)
                {
                    var item = relation_copy[i];
                    if (item.Contains(a0))
                    {
                        int target = a0 == item[0] ? item[1] : item[0];
                        if (res.ContainsKey(a0))
                            res[a0].Add(target);
                        else
                        {
                            res[a0] = new List<int>();
                            res[a0].Add(a1);
                            res[a0].Add(target);
                        }
                        relation_copy.Remove(item);
                        flag = true;
                        continue;
                    }
                    if (item.Contains(a1))
                    {
                        int target = a1 == item[0] ? item[1] : item[0];
                        if (res.ContainsKey(a1))
                            res[a1].Add(target);
                        else
                        {
                            res[a1] = new List<int>();
                            res[a1].Add(a0);
                            res[a1].Add(target);
                        }
                        relation_copy.Remove(item);
                        flag = true;
                        continue;
                    }
                }
                if (!flag)
                {
                    res[a0] = new List<int> { a1 };
                }
            }
            return res;
        }
    }
}

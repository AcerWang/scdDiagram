using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Text;

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

        // 记录3/2断路器位置
        private static IDictionary<string, int[]> breaker_location = new Dictionary<string,int[]>();

        // 记录断路器间隔所接线路数
        private static IDictionary<string, int> line_num_of_breaker = new Dictionary<string, int>();

        // 记录每段母线上线路数量
        private static IDictionary<string, int> dic_busline_num = null;

        // 保存电压等级
        private static int High_volt;
        private static int Mid_volt;

        public static void Main(string[] args)
        {
            SCDResolver.init();
            High_volt = SCDResolver.High_Volt;
            Mid_volt = SCDResolver.Mid_Volt;
            try
            {
                Check();
                // 加载模板
                html.Load("base.html");
                svg = html.SelectSingleNode("/html/body/div/svg");

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine(e.StackTrace);
                return;
            }

            // 画主变
            DrawTransformer();

            // 画母联母线
            Draw_Bus_Union();

            // 画分段母线
            Draw_Bus_Seg();

            // 画线路
            Draw_Lines();

            // 画连接线
            Draw_Connector();
           
            // 输出到HTML文件
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
                x = 300 + 300 * (trans_no-1);

                // 画主变
                draw_component(x,y,"#Trans");

                // 显示主变对应的文字信息
                draw_text((string)trans[trans_no], x, y);

                // 保存此主变位置信息
                trans_location[trans_no] = new int[] {x,y };

                // 标注主变对应的IED
                if (trans.ContainsKey(trans_no))
                    draw_trans_ieds(trans[trans_no]+"B",x,y);
            }
        }

        /// <summary>
        /// 通过解析得到的母线信息画出对应母线的母联关系图，保存母线位置信息
        /// </summary>
        private static void Draw_Bus_Union()
        {
            // 高压侧是500kV及以上时，按照3/2接线方式处理
            if (High_volt >= 500)
            {
                int x = 50, y = 350;
                if (SCDResolver.buses[High_volt] == null)
                    SCDResolver.buses[High_volt] = new SortedSet<int>(new int[] { 1, 2 });
                int length = SCDResolver.breakers.Count * 250 + 100;
                // 画3/2接线母线
                foreach (int i in SCDResolver.buses[High_volt])
                {
                    // 画一条母线
                    draw_single_bus(High_volt.ToString() + "kV", i, x, y, x + length, y,"red");
                    // 存储位置信息
                    buses_location[High_volt] = new Dictionary<int, int[]>();
                    buses_location[High_volt][i] = new int[] { x, y,x+1200 };

                    // 更新y坐标
                    y = y - 150;
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
            if(SCDResolver.buses_relation[High_volt]!=null && SCDResolver.buses_relation[High_volt].ContainsKey("分段"))
            {
                draw_single_side_bus_seg(High_volt);
            }

            // 中压侧存在分段
            if (SCDResolver.buses_relation[Mid_volt]!=null && SCDResolver.buses_relation[Mid_volt].ContainsKey("分段"))
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
                var breaker = SCDResolver.breakers.ToArray();
                // 先画断路器间隔
                for (int i = 0; i < breaker.Count(); i++)
                {
                    // 画断路器
                    draw_breaker(150 + 250 * i, 200);
                    draw_text(SCDResolver.breakers.ToArray()[i]+SCDResolver.breaker_no.ToArray()[2], 170 + 250 * i, 230);
                    draw_text(SCDResolver.breakers.ToArray()[i] + SCDResolver.breaker_no.ToArray()[1], 170 + 250 * i, 280);
                    draw_text(SCDResolver.breakers.ToArray()[i] + SCDResolver.breaker_no.ToArray()[0], 170 + 250 * i, 330);
                    // 记录断路器位置信息
                    breaker_location[breaker[i]] = new int[] { 150 + 250 * i, 200 };

                    // 断路器对应的IEDs
                    int k = 0;
                    foreach(int j in SCDResolver.breaker_no)
                    {
                        k++;
                        string b_no = "B" + SCDResolver.breakers.ToArray()[i] + j.ToString();
                        if (!SCDResolver.line_ieds.ContainsKey(b_no))
                            continue;
                        draw_breakers_ieds("B"+SCDResolver.breakers.ToArray()[i]+j.ToString() , 155 + 250 * i, 350-k*50);
                    }
                }
                
                // 画线路
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
                    draw_normal_line(line.Key,Mid_volt);
                }
            }
        }

        /// <summary>
        /// 画主变到母线或断路器的连接线
        /// </summary>
        private static void Draw_Connector()
        {
            // 高压侧是500kV及以上，则画连接的path
            if (High_volt >= 500)
            {
                int[] breaker_no = SCDResolver.breaker_no.ToArray();
                // 纵坐标偏移量
                int dy = 0;
                foreach(var kv in SCDResolver.trans_breaker_relation)
                {
                    // 将断路器编号的最后一位转化为数字
                    var b_no = kv.Value.Select(e => int.Parse(e.Last().ToString())).ToArray();
                    // 确定纵坐标偏移量
                    if (b_no[0] == breaker_no[0])
                        dy = b_no[1] == breaker_no[1] ? 100 : 50;
                    else
                        dy = 50;
                    int x1 = breaker_location[kv.Value.First().Substring(0, 3)][0];
                    int y1 = 200 + dy;
                    int x2 = trans_location[kv.Key][0], y2 = trans_location[kv.Key][1];
                    draw_trans_path(x1+15, y1, x2+20, y2+5);
                }
            }
            // 按普通画法
            else
            {
                draw_single_side_connector(High_volt);
            }

            if (Mid_volt < 100)
                return;
            // 中压侧做法和高压侧一致
            draw_single_side_connector(Mid_volt);
        }

        /// <summary>
        /// 画主变本体的IEDs
        /// </summary>
        /// <param name="ied_no">主变编号</param>
        /// <param name="x">主变位置横坐标</param>
        /// <param name="y">主变位置纵坐标</param>
        private static void draw_trans_ieds(string ied_no,int x, int y)
        {
            foreach (var item in SCDResolver.line_ieds[ied_no])
            {
                int i = 0;
                switch (item.Key)
                {
                    case "合并单元":
                        draw_text(item.Key, x + 60, y-15,"black","8");
                        foreach (string ied_name in item.Value)
                        {
                            draw_image(x + 40 + i * 40, y - 20, ied_name);
                            draw_text(ied_name, x + 40 + i * 45, y + 10, "black", "8");
                            i++;
                        }
                        break;
                    case "合智一体":
                        draw_text(item.Key, x + 60, y -15,"black","8");
                        foreach (string ied_name in item.Value)
                        {
                            draw_image(x + 40 + i * 40, y -20, ied_name);
                            draw_text(ied_name, x + 40 + i * 45, y + 10, "black", "8");
                            i++;
                        }
                        break;
                    case "智能终端":
                        draw_text(item.Key, x + 60, y + 20,"black","8");
                        foreach (string ied_name in item.Value)
                        {
                            draw_image(x + 40 + i * 40, y + 15, ied_name);
                            draw_text(ied_name, x + 40 + i * 45, y + 45, "black", "8");
                            i++;
                        }
                        break;
                    case "保护测控":
                        draw_text(item.Key, x - 50, y +20,"black","8");
                        foreach (string ied_name in item.Value)
                        {
                            draw_image(x - 30 - i * 40, y+15 , ied_name);
                            draw_text(ied_name, x - 30 - i * 45, y + 45, "black", "8");
                            i++;
                        }
                        break;
                    case "保护":
                        draw_text(item.Key, x - 50, y +20,"black","8");
                        foreach (string ied_name in item.Value)
                        {
                            draw_image(x - 30 - i * 40, y+15, ied_name);
                            draw_text(ied_name, x - 30 - i * 45, y + 45, "black", "8");
                            i++;
                        }
                        break;
                    case "测控":
                        draw_text(item.Key, x - 45, y-10,"black","8");
                        foreach (string ied_name in item.Value)
                        {
                            draw_image(x - 30 - i * 40, y -20, ied_name);
                            draw_text(ied_name, x - 30 - i * 40, y + 10, "black", "8");
                            i++;
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        /// <summary>
        /// 画断路器的IEDs
        /// </summary>
        /// <param name="ied_no">断路器编号</param>
        /// <param name="x">断路器位置横坐标</param>
        /// <param name="y">断路器位置纵坐标</param>
        private static void draw_breakers_ieds(string ied_no, int x, int y)
        {
            foreach (var item in SCDResolver.line_ieds[ied_no])
            {
                int i = 0;
                switch (item.Key)
                {
                    case "合并单元":
                        draw_text(item.Key, x + 65, y +10, "black", "6");
                        foreach (string ied_name in item.Value)
                        {
                            draw_image(x + 50 + i * 35, y+5, ied_name,"20","20");
                            draw_text(ied_name, x + 45 + i * 35, y + 25, "black", "6");
                            i++;
                        }
                        break;
                    case "合智一体":
                        draw_text(item.Key, x + 65, y + 10, "black", "6");
                        foreach (string ied_name in item.Value)
                        {
                            draw_image(x + 50 + i * 35, y + 5, ied_name, "20", "20");
                            draw_text(ied_name, x + 45 + i * 35, y + 25, "black", "6");
                            i++;
                        }
                        break;
                    case "智能终端":
                        draw_text(item.Key, x + 65, y + 30, "black", "6");
                        foreach (string ied_name in item.Value)
                        {
                            draw_image(x + 50 + i * 35, y + 25, ied_name, "20", "20");
                            draw_text(ied_name, x + 45 + i * 40, y + 45, "black", "6");
                            i++;
                        }
                        break;
                    case "保护测控":
                        draw_text(item.Key, x - 55, y + 20, "black", "6");
                        foreach (string ied_name in item.Value)
                        {
                            draw_image(x - 40 - i * 30, y + 15, ied_name, "20", "20");
                            draw_text(ied_name, x - 40 - i * 35, y + 45, "black", "6");
                            i++;
                        }
                        break;
                    case "保护":
                        draw_text(item.Key, x - 55, y + 30, "black", "6");
                        foreach (string ied_name in item.Value)
                        {
                            draw_image(x - 40 - i * 30, y + 25, ied_name, "20", "20");
                            draw_text(ied_name, x - 40 - i * 35, y + 45, "black", "6");
                            i++;
                        }
                        break;
                    case "测控":
                        draw_text(item.Key, x - 55, y+15, "black", "6");
                        foreach (string ied_name in item.Value)
                        {
                            draw_image(x - 40 - i * 30, y +5, ied_name, "20", "20");
                            draw_text(ied_name, x - 40 - i * 30, y + 25, "black", "6");
                            i++;
                        }
                        break;
                    default:
                        break;
                }
            }
        }
        
        /// <summary>
                ///  画单侧主变到母线的连接
                /// </summary>
                /// <param name="volt">电压等级</param>
        private static void draw_single_side_connector(int volt)
        {
            string color = volt==High_volt? "red":"blue";
            string href = volt == High_volt ? "#Join-U-" : "#Join-D-";
            string ied_side = volt == High_volt ? "H" : "M";
            int dy = volt == High_volt ? 0 : -110;
            int ty = volt == High_volt ? 100 : 60;
            int vy = volt == High_volt ? 5 : 35;
            int dy_ied = volt == High_volt ? -100 : 140;
            int j = 0;
            foreach (var i in SCDResolver.transformers.Keys)
            {
                j++;
                // 包含主变-母线关系
                string trans_no = (volt * 10 + i).ToString();

                // 找到关系，直接连接到母线数量的母线上
                if (SCDResolver.trans_bus_relation.ContainsKey(trans_no))
                {
                    var bus_arr = SCDResolver.trans_bus_relation[trans_no];
                    if (bus_arr.Count == 0)
                    {
                        int num = SCDResolver.buses[volt].Count;
                        int[] cordination = buses_location[volt].First().Value;
                        int seg_length = (cordination[0] + cordination[2]) / (SCDResolver.transformers.Count + 1);
                        int x = 50 + seg_length * j;
                        int y = num == 1 ? cordination[1] : cordination[1] - 42;
                        string tmp_href = num == 1 ? href+"1" : href+"2";
                        draw_join(x, y, color, href);
                        draw_text(trans_no.Substring(1,3),x+17,y+ty);
                        draw_path(trans_location[i][0],trans_location[i][1],x,y+dy,color);

                        // 标注连接器上的IED
                        draw_trans_ieds(SCDResolver.transformers[i] + ied_side, x, trans_location[i][1]+dy_ied);
                    }
                    // 连接到一段母线，或并联的母线上
                    else
                    {
                        var m = calc_bus_num_of_trans(volt);
                        int[] cord = buses_location[volt][bus_arr.Max()];
                        int x = 0;
                        int y = cord[1];
                        string tmp_href = bus_arr.Count == 1 ? href + "1" : href + "2";
                        if (m[bus_arr.First()] == 1)
                        {
                            int part_len = (cord[2] - cord[0]) / 2;
                            x = cord[0] + part_len;
                            j = 0;
                        }
                        else
                        {
                            int part_len = (cord[2] - cord[0]) / (m[bus_arr.First()] + 1);
                            x = cord[0] + part_len * j;
                        }

                        int len_conn = bus_arr.Count == 1 ? 110 : 150;
                        int off_y = 0;
                        if (volt == High_volt)
                        {
                            off_y = len_conn;
                        }
                        else
                        {
                            y = y - len_conn;
                        }
                        draw_join(x, y, color, tmp_href);
                        draw_text(trans_no.Substring(1,3), x + 17, y + ty);
                        draw_path(trans_location[i][0]+20, trans_location[i][1]+vy, x+10, y+off_y, color);
                        // 标注连接器上的IED
                        draw_trans_ieds(SCDResolver.transformers[i] + ied_side, x, trans_location[i][1] + dy_ied);
                    }
                }
                // 无母线-主变连接关系
                else
                {
                    int num = SCDResolver.buses[volt].Count;
                    int[] cordination = buses_location[volt].First().Value;
                    int seg_length = (cordination[0] + cordination[2]) / (SCDResolver.transformers.Count + 1);
                    int x = 50 + seg_length * j;
                    int y = num == 1 ? cordination[1] : cordination[1] - 42;
                    string tmp_href = num == 1 ? href + "1" : href + "2";
                    draw_join(x, y, color, tmp_href);
                    draw_text(trans_no, x + 17, y + ty);
                    draw_path(trans_location[i][0], trans_location[i][1], x, y + dy, color);
                    // 标注连接器上的IED
                    draw_trans_ieds(SCDResolver.transformers[i] + ied_side, x, trans_location[i][1] + dy_ied);
                }
            }
        }

        /// <summary>
        /// 计算某电压侧各母线上接的主变数量
        /// </summary>
        /// <param name="volt">电压等级</param>
        /// <returns>返回`母线-主变数量`的字典</returns>
        private static IDictionary<int,int> calc_bus_num_of_trans(int volt)
        {
            IDictionary<int, int> dict = new SortedDictionary<int, int>();
            foreach (var t in SCDResolver.transformers.Keys)
            {
                var trans_no = (volt * 10 + t).ToString();
                // 500kV及以上直接采用母线段
                if (volt >= 500)
                {
                    SCDResolver.trans_bus_relation[trans_no] = SCDResolver.buses[volt];
                }
                var bus = SCDResolver.trans_bus_relation[trans_no].First();
                if (!dict.ContainsKey(bus))
                    dict[bus] = 1;
                else
                    dict[bus]++;
            }
            return dict;
        }

        /// <summary>
        /// 画主变到母线的连接线
        /// </summary>
        /// <param name="x">连接线的x坐标</param>
        /// <param name="y">连接线的y坐标</param>
        /// <param name="color">连接线的颜色</param>
        /// <param name="href">引用的连接线</param>
        private static void draw_join(int x, int y, string color, string href)
        {
            Dictionary<string, string> attrs = new Dictionary<string, string>() {
                { "x",x.ToString() },
                { "y",y.ToString() },
                { "stroke",color},
                { "stroke-width","1" },
                { "href",href }
            };
            XmlElement ele = NewElement("use", attrs);
            svg.AppendChild(ele);
        }

        /// <summary>
        /// 画3/2接线
        /// </summary>
        /// <param name="line">线路编号</param>
        private static void draw_one_half_line(string line)
        {
            // 线路所处的两个断路器
            var line_breaker = SCDResolver.line_breaker_relation[line].ToArray();
            var breaker = line_breaker[0].Substring(0, 3);
            // 线路所联断路器的x坐标
            int x = breaker_location[breaker][0];
            string href = "#Line-3/2-L";
            int offset_r_l = 0;

            // 所处两个断路器编号的最后一位数字
            int[] b_no = new int[] {int.Parse(line_breaker[0].Substring(3,1)),int.Parse(line_breaker[1].Substring(3,1))};
            int[] breaker_no = SCDResolver.breaker_no.ToArray();
            // 纵坐标偏移量
            int dy = 0;
            // 确定纵坐标偏移量
            if (b_no[0] == breaker_no[0])
                dy = b_no[1] == breaker_no[1] ? 50 : 0;
            else
                dy = 0;
            
            // 判断线路在断路器左侧还是右侧
            if(line_num_of_breaker.ContainsKey(breaker))
            {
                x = x + 10;
                href = "#Line-3/2-R"; // 右侧
                line_num_of_breaker[breaker] = 2;
                offset_r_l = 85;
            }
            else   // 左侧
            {
                line_num_of_breaker[breaker] = 1;
                x = x - 10;
            }
            // 画线路
            draw_3_2_line(line,x,50+dy,href);

            // 标注线路IEDs
            int m = 1;
            foreach (var it in SCDResolver.line_ieds["L" + line])
            {
                draw_text(it.Key, x - 35+offset_r_l, 53+m*25, "black", "6");
                int j = 1;
                foreach (string name in it.Value)
                {
                    draw_image(x - 80 + j * 30+offset_r_l, 50+m*25, name, "20", "20");
                    draw_text(name, x - 85 + j * 30+offset_r_l, 50 + m * 25 +22, "black", "6");
                    j++;
                }
                m++;
            }

        }

        /// <summary>
        /// 画3/2接线
        /// </summary>
        /// <param name="x">线路的起点x坐标</param>
        /// <param name="y">线路的起点y坐标</param>
        /// <param name="href">引用的线路</param>
        private static void draw_3_2_line(string line,int x, int y, string href)
        {
            Dictionary<string, string> attrs = new Dictionary<string, string>() {
                { "x",x.ToString() },
                { "y",y.ToString() },
                { "stroke","red"},
                { "stroke-width","1"},
                { "href",href }
            };
            XmlElement ele = NewElement("use", attrs);
            svg.AppendChild(ele);

            // 线路文字节点属性
            Dictionary<string, string> txt_attrs = new Dictionary<string, string>() {
                { "dy", "0" } ,
                { "stroke", "black" } ,
                { "stroke-width", "0.3" } ,
                { "style", "writing-mode:tb;"} ,
                { "x", (x+15).ToString() } ,
                { "y", y.ToString()}
            };
            // 文字信息节点
            XmlElement txt = NewElement("text", txt_attrs);
            txt.InnerText = SCDResolver.lines[High_volt][line];
            svg.AppendChild(txt);
        }

        /// <summary>
        /// 画3/2接线断路器间隔
        /// </summary>
        /// <param name="x">断路器间隔横坐标</param>
        /// <param name="y">断路器间隔纵坐标</param>
        private static void draw_breaker(int x,int y)
        {
            Dictionary<string, string> attrs = new Dictionary<string, string>() {
                { "x",x.ToString() },
                { "y",y.ToString() },
                { "href","#Breaker-3" }
            };
            XmlElement ele = NewElement("use",attrs);

            svg.AppendChild(ele);
        }

        /// <summary>
        /// 画一条线路，包含文字部分
        /// </summary>
        /// <param name="line">线路编号</param>
        /// <param name="x">线路起点横坐标</param>
        /// <param name="y">线路起点纵坐标</param>
        /// <param name="color">线路颜色</param>
        /// <param name="href">线路模板</param>
        private static void draw_single_line(string line, int x, int y, string color, string href)
        {
            // 设置线路节点属性
            Dictionary<string, string> attrs = new Dictionary<string, string>() {
                { "id", line } ,
                { "x", x.ToString() } ,
                { "y", y.ToString() } ,
                { "stroke", color } ,
                { "href", href }
            };
            // 引用线路模板
            XmlElement ele = NewElement("use", attrs);
            // 追加到svg节点中
            svg.AppendChild(ele);

            if (href.Contains("Up"))
                x = x;
            else if (href.Contains("Down"))
                y = y + 150;
            else if (href == "#Line-3/2-R")
                x = x + 15;
            else
                x = x - 5;
            // 线路文字节点属性
            Dictionary<string, string> txt_attrs = new Dictionary<string, string>() {
                { "dy", "0" } ,
                { "stroke", color } ,
                { "stroke-width", "0.3" } ,
                { "style", "writing-mode:tb;"} ,
                { "x", x.ToString() } ,
                { "y", y.ToString()}
            };
            // 文字信息节点
            XmlElement txt = NewElement("text", txt_attrs);
            var volt = int.Parse(line.Substring(0, 2)) * 10;
            // 要显示的线路文字信息
            txt.InnerText = SCDResolver.lines[volt][line];
            // 追加到svg节点中
            svg.AppendChild(txt);
        }

        /// <summary>
        /// 画线路
        /// </summary>
        /// <param name="line">线路名称</param>
        /// <param name="volt_level">电压等级</param>
        private static void draw_normal_line(string line, int volt_level)
        {
            int dy = volt_level == High_volt ? -200 : 0;
            int ty = volt_level== High_volt ? -50 : 60;
            string color = volt_level == High_volt ? "red" : "blue";
            string href = volt_level == High_volt ? "#Line-Up-" : "#Line-Down-";
            int one_or_two = 1;  // 线路联到1条或2条母线上
            int i, x, y;
            // `线路-母线` 关系不存在，由母线数量确定其位置
            if (!SCDResolver.line_bus_relation.ContainsKey(line))    // 有几段该等级的母线，只有一段，直接画在上面，有两段则接在两段上
            {
                i = (buses_location[volt_level].Count == 1) ? 1 : 2;
                one_or_two = i;
            }
            else    // 存在`线路-母线`关系，根据关系确定其位置
            {
                one_or_two = SCDResolver.line_bus_relation[line].Count;
                // 取用某侧标号的母线
                if(volt_level==High_volt)
                    i = SCDResolver.line_bus_relation[line].Max();
                else
                    i = SCDResolver.line_bus_relation[line].Min();
            }
            // 记录母线上线路条数
            if (!bus_line_num.ContainsKey(volt_level))
                bus_line_num[volt_level] = new Dictionary<int, int>();
            if (!bus_line_num[volt_level].ContainsKey(i))
                bus_line_num[volt_level][i] = 0;
            bus_line_num[volt_level][i] += 1;
            // debug
            x = buses_location[volt_level][i][0];
            y = buses_location[volt_level][i][1];
            
            x = x + 75 + (bus_line_num[volt_level][i]-1)*125;
            y = y + dy;
            // 画出线路  debug
            draw_single_line(line,x,y,color,href+one_or_two.ToString());
            ty = buses_location[volt_level][SCDResolver.line_bus_relation[line].Max()][1] + ty;
            draw_text(line,x-25,ty);

            // 标注出线路IED
            draw_ieds(line, x, y);
        }

        /// <summary>
        /// 画线路IEDs
        /// </summary>
        /// <param name="line">线路编号</param>
        /// <param name="x">线路位置横坐标</param>
        /// <param name="y">线路位置横坐标</param>
        private static void draw_ieds(string line, int x, int y)
        {
            int dy = 0;
            foreach (var item in SCDResolver.line_ieds["L" + line])
            {
                int i = 0;
                switch (item.Key)
                {
                    case "合并单元":
                        dy = int.Parse(line.Substring(0, 2)) * 10 == High_volt ? 50 : 160;
                        draw_text(item.Key,x+35,y+dy+2,"black","8");
                        foreach (string ied_name in item.Value)
                        {
                            draw_image(x+20+i*30, y+dy, ied_name);
                            draw_text(ied_name, x + 15 + i * 38, y + dy + 35, "black","8");
                            i++;
                        }
                        break;
                    case "合智一体":
                        dy = int.Parse(line.Substring(0, 2)) * 10 == High_volt ? 130 : 80;
                        draw_text(item.Key, x+35, y + dy+2,"black","8");
                        foreach (string ied_name in item.Value)
                        {
                            draw_image(x+20+i*30, y+dy+5, ied_name);
                            draw_text(ied_name, x + 18 + i * 38, y + dy + 30, "black", "8");
                            i++;
                        }
                        break;
                    case "智能终端":
                        dy = int.Parse(line.Substring(0, 2)) * 10 == High_volt ? 120 : 80;
                        draw_text(item.Key, x+35, y + dy+2, "black", "8");
                        foreach (string ied_name in item.Value)
                        {
                            draw_image(x+20+i*30, y + dy, ied_name);
                            draw_text(ied_name, x + 18 + i * 35, y + dy + 30, "black", "8");
                            i++;
                        }
                        break;
                    case "保护测控":
                        dy = int.Parse(line.Substring(0,2))*10 == High_volt ? -20 : 240;
                        draw_text(item.Key, x+20, y + dy+5, "black", "8");
                        foreach (string ied_name in item.Value)
                        {
                            draw_image(x+20+i*30, y + dy, ied_name);
                            draw_text(ied_name, x + 15 + i * 38, y + dy + 30, "black", "8");
                            i++;
                        }
                        break;
                    case "保护":
                        dy = int.Parse(line.Substring(0, 2)) * 10 == High_volt ? -10 : 220;
                        draw_text(item.Key, x+40, y + dy+5, "black", "8");
                        foreach (string ied_name in item.Value)
                        {
                            draw_image(x + 20 + i * 30, y + dy, ied_name);
                            draw_text(ied_name, x + 15 + i * 38, y + dy + 30, "black", "8");
                            i++;
                        }
                        break;
                    case "测控":
                        dy = int.Parse(line.Substring(0, 2)) * 10 == High_volt ? -50 : 270;
                        draw_text(item.Key, x+20, y + dy, "black", "8");
                        foreach (string ied_name in item.Value)
                        {
                            draw_image(x + 20 + i * 30, y + dy, ied_name);
                            draw_text(ied_name, x + 45 + i * 38, y + dy+20, "black", "8");
                            i++;
                        }
                        break;
                    default:
                        break;
                }
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
            int dy_ied = (Side == High_volt) ? 20 : -20;  // IED放置方向
            int ty = (Side == High_volt) ? -26 : 26;

            // 不存在关联关系的母线，直接画单独分段线段
            if (SCDResolver.buses_relation[Side] == null)
            {
                int x = 50, y = (Side == High_volt) ? 300 : 650;
                // 为比较老的，特殊的scd文件兼容，没有母线的情况
                if (SCDResolver.buses[Side] == null)
                {
                    // 默认添加一条母线
                    SCDResolver.buses[Side] = new SortedSet<int>(new int[] { 1 });
                }
                // 每一段母线的长度
                // int seg_length = line_seg_length(SCDResolver.buses[Side].Count);

                // 每一段，单独画，水平排列
                foreach (int i in SCDResolver.buses[Side])
                {
                    string k = Side.ToString() + i.ToString();
                    int seg_length = get_bus_line_num()[k] * 150 ;
                    // 画一条母线
                    draw_single_bus(Side.ToString() + "kV", i, x, y, x + seg_length, y,color);
                    // 存储坐标
                    if (!buses_location.ContainsKey(Side))
                        buses_location[Side] = new SortedDictionary<int, int[]>();
                    buses_location[Side][i] = new int[] { x, y, x+seg_length };

                    // 更新x坐标
                    x = x + seg_length + 50;
                }

                return;
            }

            // 这一侧有母联的情况
            if (SCDResolver.buses_relation[Side].ContainsKey("母联"))
            {
                // 有母联关系，但各段母线未给出，此情况直接按并联两条母线画图
                if (SCDResolver.buses_relation[Side]["母联"] == null)
                {
                    // 为兼容老的，特殊scd文件，之前的解析没有母线
                    if (SCDResolver.buses[Side] == null)
                    {
                        // 默认添加两段母线
                        SCDResolver.buses[Side] = new SortedSet<int>(new int[] { 1, 2 });
                    }
                    // 之前的解析结果只有一条母线，则补齐另一条
                    else if (SCDResolver.buses[Side].Count == 1)
                        SCDResolver.buses[Side].Add(SCDResolver.buses[Side].Last() + 1);
                    // SCDResolver.buses_relation[Side]["母联"] = SCDResolver.buses[Side];
                    int x = 50, y = (Side == High_volt) ? 300 : 650;
                    int dy = (Side == High_volt) ? -40 : 40;
                    // 画两条并联母线
                    foreach (int i in SCDResolver.buses[Side])
                    {
                        string k = Side.ToString() + i.ToString();
                        int seg_length = get_bus_line_num()[k] * 150;
                        // 画一条母线
                        draw_single_bus(Side.ToString() + "kV", i, x, y, x + seg_length, y, color);
                        // 存储母线位置
                        if (!buses_location.ContainsKey(Side))
                            buses_location[Side] = new Dictionary<int, int[]>();
                        buses_location[Side][i] = new int[] { x, y,x+seg_length };

                        // 调整坐标
                        y = y + dy;
                    }

                    int u_x = buses_location[Side][SCDResolver.buses[Side].Min()][0] + 70, u_y = buses_location[Side][SCDResolver.buses[Side].Min()][1]+40;
                    // 并联两母线，画出母联
                    draw_component(buses_location[Side][SCDResolver.buses[Side].Max()][0] + 40, buses_location[Side][SCDResolver.buses[Side].Max()][1], href, color);
                    // 母联编号
                    draw_text((Side / 100).ToString() + SCDResolver.buses[Side].First().ToString() + SCDResolver.buses[Side].Last().ToString(), u_x+5, u_y+ty);
                    // 画母联IEDs
                    var ieds = SCDResolver.line_ieds.Where(ied => ied.Key.StartsWith("U" + (Side / 10).ToString())).Select(ied=>ied).First();
                    int j = (Side == High_volt) ? 0 :1;
                    u_y = u_y + (j - 1) * 15;
                    foreach (var item in ieds.Value)
                    {
                        draw_text(item.Key, u_x -25, u_y + j * dy_ied + 5, "black", "5");
                        int i = 0;
                        foreach(string name in item.Value)
                        {
                            draw_image(u_x -40 + i * 35, u_y + j * dy_ied, name, "20", "20");
                            draw_text(name, u_x - 40 + i * 35, u_y + j * dy_ied + 20, "black", "6");
                            i++;
                        }
                        j++;
                    }
                }

                // 多组母联
                else
                {
                    var relation = SCDResolver.buses_relation[Side]["母联"].Values;
                    var relation_res = union_seg(relation);
                    // 初始坐标，及每段母线的长度
                    int x = 50, y = (Side == High_volt) ? 300 : 650;
                    // int seg_length = line_seg_length(relation_res.Count);
                    int dy = (Side == High_volt) ? -40 : 40;
                    // 画母线
                    foreach (var item in relation_res)
                    {
                        string k = Side.ToString() + item.Key.ToString();
                        int seg_length = get_bus_line_num()[k] * 150;
                        // 画内侧的base母线
                        draw_single_bus(Side.ToString() + "kV", item.Key, x, y, x + seg_length, y, color);
                        // 保存母线位置信息
                        if (!buses_location.ContainsKey(Side))
                            buses_location[Side] = new Dictionary<int, int[]>();
                        buses_location[Side][item.Key] = new int[] { x, y,x+seg_length };
                        // 调整坐标
                        y = y + dy;
                        int partLen = (seg_length - (item.Value.Count - 1) * 50) / item.Value.Count;
                        int x2 = x + partLen;

                        // 画与base母线对应的外侧母线
                        foreach (var i in item.Value)
                        {
                            // 画母线
                            draw_single_bus(Side.ToString() + "kV", i, x, y, x2, y, color);
                            // 保存母线位置信息
                            buses_location[Side][i] = new int[] { x, y, x2 };
                            
                            int u_x = buses_location[Side][item.Key][0] + 70, u_y = buses_location[Side][item.Key] [1] - dy+10;
                            // 画母联
                            draw_component(buses_location[Side][item.Key][0]+40, buses_location[Side][item.Key][1] - dy + 10, href, color);
                            // 母联编号
                            draw_text((Side / 100).ToString() + item.Key.ToString() + i.ToString(), u_x+5, u_y+ty);
                            // 标注母联IEDs
                            string uni_no = SCDResolver.buses_relation[Side]["母联"].Where(it => it.Value[0] == item.Key && it.Value[1] == i).Select(it => it.Key).First();
                            int j = (Side == High_volt) ? 0:1;
                            u_y = u_y + (j - 1) * 15;
                            foreach (var it in SCDResolver.line_ieds["U"+uni_no])
                            {
                                draw_text(it.Key, u_x -25, u_y + j*dy_ied+5, "black", "5");
                                int n = 0;
                                foreach (string name in it.Value)
                                {
                                    draw_image(u_x - 40 + n * 35, u_y + j * dy_ied, name, "20", "20");
                                    draw_text(name, u_x - 40 + n * 35, u_y + j * dy_ied+20, "black", "6");
                                    n++;
                                }
                                j++;
                            }
                            // 调整坐标
                            x = x2 + 50;
                            x2 = x + partLen;
                        }
                        // 调整坐标
                        x = x2 - x + 100;
                        x2 = x + seg_length;
                        y = y - dy;
                    }
                }
            }
        }

        /// <summary>
        /// 画中、高压侧分段接线
        /// </summary>
        /// <param name="Side">电压等级</param>
        private static void draw_single_side_bus_seg(int Side)
        {
            string color = (Side == High_volt) ? "red" : "blue";
            string href = "#BusSeg";
            int x = 50, y = (Side == High_volt) ? 300 : 650;
            int dy = (Side == High_volt) ? 40 : -40;
            // 不存在分段
            if (!SCDResolver.buses_relation[Side].ContainsKey("分段"))
                return;

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
                        draw_single_bus(Side.ToString() + "kV", i, x, y, x + 650, y, color);
                        draw_text(SCDResolver.c_index[i], x - 20, y + 5);
                        // 记录位置信息
                        buses_location[Side][i] = new int[] { x, y, x+600};
                    }
                    // 调整坐标
                    x += 650;
                }
                // 画分段开关
                draw_component(640, 280, href, color);
                draw_text((Side/10).ToString()+SCDResolver.buses[Side].First().ToString()+SCDResolver.buses.Last().ToString(),690,300);
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

                // 并联型分段
                // -----------^^^----------^^^-----------^^^---------
                // -----------^^^----------^^^-----------^^^---------
                if (total_seg_num - seg_num == 2)
                {
                    // part_len = line_seg_length(seg_num);
                    // 记录画的组数
                    int n = 0;
                    // 画分段母线，并联型画法
                    foreach (var item in SCDResolver.buses_relation[Side]["分段"].Values)
                    {
                        int seg_length = 0;
                        n++;
                        foreach (int i in item)
                        {
                            string k = Side.ToString() + i.ToString();
                            seg_length = get_bus_line_num()[k] * 150;

                            // 初次画该电压等级的母线
                            if (!buses_location.ContainsKey(Side))
                                buses_location[Side] = new Dictionary<int, int[]>();

                            // 存在该电压等级，判断该段母线是否存在记录中
                            if (!buses_location[Side].ContainsKey(i))
                            {
                                // 画母线
                                draw_single_bus(Side + "kV", i, x, y, x + seg_length, y, color);

                                // 记录位置信息
                                buses_location[Side][i] = new int[] { x, y, x + seg_length };
                            }
                            // 已经划过该母线了，转到下一条
                            x = buses_location[Side][i][2] + 50;
                        }
                        // 画出分段开关
                        int s_x = buses_location[Side][item[0]][2], s_y = y;
                        draw_component(s_x - 25, s_y - 25, href, color);
                        // 分段编号
                        draw_text((Side / 100).ToString() + item[0].ToString() + item[1].ToString(), s_x + 10, y);
                        // 标注分段IEDs
                        string ied_no = SCDResolver.buses_relation[Side]["分段"].Where(ied => ied.Value[0] == item[0] && ied.Value[1] == item[1]).Select(ied => ied.Key).First();
                        int directions = (n % 2 == 1) ? -1 : 1;  // IED放置方向
                        int dy_ied = (1-n % 2) * 20;
                        int m = 1;
                        foreach (var it in SCDResolver.line_ieds["S"+ied_no])
                        {
                            draw_text(it.Key, s_x+15, s_y+m*20*directions-25+dy_ied,"black","5");
                            int j = 1;
                            foreach(string name in it.Value)
                            {
                                draw_image(s_x-30 + j * 30, s_y + m * 20*directions-30+dy_ied, name, "20", "20");
                                draw_text(name, s_x-40 + j * 35, s_y + m*20*directions-10+dy_ied, "black", "6");
                                j++;
                            }
                            m++;
                        }
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
                    // part_len = line_seg_length(total_seg_num);
                    // 画分段母线，串联型画法
                    foreach (var item in SCDResolver.buses_relation[Side]["分段"].Values)
                    {
                        int seg_length = 0;
                        foreach (var i in item)
                        {
                            string k = Side.ToString() + i.ToString();
                            seg_length = get_bus_line_num()[k] * 150;
                            // 初次画该电压等级的母线
                            if (!buses_location.ContainsKey(Side))
                                buses_location[Side] = new Dictionary<int, int[]>();

                            // 存在该电压等级，判断该段母线是否存在记录中
                            if (!buses_location[Side].ContainsKey(i))
                            {
                                // 画母线
                                draw_single_bus(Side + "kV", i, x, y, x + seg_length, y,color);

                                // 记录位置信息
                                buses_location[Side][i] = new int[] { x, y, x+seg_length};
                            }
                            // 调整坐标
                            x = buses_location[Side][i][2] + 50;
                        }
                        // 画出分段开关
                        int s_x = buses_location[Side][item[0]][2], s_y = y;
                        draw_component(s_x - 25, y - 25, href, color);
                        // 分段编号
                        draw_text((Side / 100).ToString() + item[0].ToString() + item[1].ToString() , s_x +10, y);
                        // 标注分段IEDs
                        string ied_no = SCDResolver.buses_relation[Side]["分段"].Where(ied => ied.Value[0] == item[0] && ied.Value[1] == item[1]).Select(ied => ied.Key).First();
                        int directions = -1;  // IED放置方向
                        int m = 1;
                        foreach (var it in SCDResolver.line_ieds["S" + ied_no])
                        {
                            draw_text(it.Key, s_x +15, s_y + m * 20 * directions - 25, "black", "5");
                            int j = 1;
                            foreach (string name in it.Value)
                            {
                                draw_image(s_x - 30 + j * 30, s_y + m * 20 * directions - 30 , name, "20", "20");
                                draw_text(name, s_x - 40 + j * 35, s_y + m * 20 * directions - 10 , "black", "6");
                                j++;
                            }
                            m++;
                        }
                    }
                }

            }
        }

        /// <summary>
        /// 按所给参数，画一条母线,包含文字部分
        /// </summary>
        /// <param name="prefix">母线id前缀</param>
        /// <param name="id">母线编号</param>
        /// <param name="x1">母线起点横坐标</param>
        /// <param name="y1">母线起点纵坐标</param>
        /// <param name="x2">母线终点横坐标</param>
        /// <param name="y2">母线终点纵坐标</param>
        /// <param name="color">母线颜色</param>
        private static void draw_single_bus(string prefix, int id, int x1, int y1, int x2, int y2, string color)
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
                                { "x", x1.ToString() } ,
                                { "y", (y1+15).ToString()}
                            };
            // 添加文字节点大svg节点后面
            XmlElement text = NewElement("text", text_attrs);
            text.InnerText = prefix+" "+SCDResolver.c_index[id];
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
        /// 创建IED的图像节点
        /// </summary>
        /// <param name="x">放置的横坐标</param>
        /// <param name="y">放置的纵坐标</param>
        /// <param name="alt">显示的提示信息</param>
        /// <param name="href">引用的源文件</param>
        private static void draw_image(int x,int y, string alt, string width = "25", string height = "25",string href = "ied1.svg")
        {
            Dictionary<string, string> attrs = new Dictionary<string, string> {
                { "x", x.ToString() } ,
                { "y", y.ToString() } ,
                { "href", href },
                { "alt", alt },
                { "width", width },
                { "height", height }
            };
            XmlElement ele = NewElement("image",attrs);
            svg.AppendChild(ele);
        }

        /// <summary>
        /// 创建文字节点
        /// </summary>
        /// <param name="text">要显示的文字信息</param>
        /// <param name="x">文字显示的起点横坐标</param>
        /// <param name="y">文字显示的起点纵坐标</param>
        /// <param name="color"></param>
        private static void draw_text(string text, int x,int y, string color = "black", string font_size="12")
        {
            // 文字元素的基本信息
            Dictionary<string, string> text_attrs = new Dictionary<string, string>()
                {
                    { "dy", "0" } ,
                    { "stroke", color } ,
                    { "stroke-width", "0.5" } ,
                    { "font-size", font_size } ,
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

        /// <summary>
        /// 画主变到母线路径
        /// </summary>
        /// <param name="x1">起点横坐标</param>
        /// <param name="y1">起点纵坐标</param>
        /// <param name="x2">终点横坐标</param>
        /// <param name="y2">终点纵坐标</param>
        private static void draw_path(int x1, int y1, int x2, int y2, string color)
        {
            int dy = y1 < y2 ? (y1 + y2) / 2 + 10 : (y1 + y2) / 2 - 10;

            StringBuilder sb = new StringBuilder("M");
            sb.Append(x1.ToString() + " " + y1.ToString()+" ");
            // sb.Append("L" + x1.ToString() + " " + y2.ToString() + " ");
            sb.Append("L"+x1.ToString()+" "+dy.ToString()+" ");
            sb.Append("L " + x2.ToString() + " " + dy.ToString()+" ");
            sb.Append("L " + x2.ToString() + " " + y2.ToString());

            Dictionary<string, string> attrs = new Dictionary<string, string>() {
                { "d", sb.ToString() },
                { "fill-opacity","0.0" },
                { "stroke",color }
            };
            XmlElement ele = NewElement("path", attrs);
            svg.AppendChild(ele);
        }

        /// <summary>
        /// 画主变到断路器的路径
        /// </summary>
        /// <param name="x1">起点横坐标</param>
        /// <param name="y1">起点纵坐标</param>
        /// <param name="x2">终点横坐标</param>
        /// <param name="y2">终点纵坐标</param>
        private static void draw_trans_path(int x1, int y1, int x2, int y2)
        {
            int dy = (y1 + y2) / 2 + 20;

            StringBuilder sb = new StringBuilder("M");
            sb.Append(x1.ToString() + " " + y1.ToString());
            sb.Append(" L" + (x1+110).ToString() + " " + y1.ToString());
            sb.Append(" L" + (x1+110).ToString() + " " + dy.ToString());
            sb.Append(" L" + x2.ToString() + " " + dy.ToString());
            sb.Append(" L" + x2.ToString() + " " + y2.ToString());

            Dictionary<string, string> attrs = new Dictionary<string, string>() {
                { "d", sb.ToString() },
                { "fill-opacity","0.0" },
                { "stroke","red" }
            };
            XmlElement ele = NewElement("path", attrs);
            svg.AppendChild(ele);
        }

        /// <summary>
        /// 检查解析结果的有效性
        /// </summary>
        private static void Check()
        {
            if (SCDResolver.buses[High_volt] == null && SCDResolver.buses[Mid_volt] == null)
                throw new Exception("解析到SCD配置文件，不符合“六统一”规范的格式");
        }

        /// <summary>
        /// 获取每条母线上所连线路数量，["2201":2,"2202":2,"1101":3,"1102":2,...]
        /// </summary>
        private static IDictionary<string,int> get_bus_line_num()
        {
            if (dic_busline_num != null)
                return dic_busline_num;
            var m = new Dictionary<string, int>();

            foreach(var item in SCDResolver.line_bus_relation)
            {
                foreach(int i in item.Value)
                {
                    var m_key = item.Key.Substring(0, 2)+ "0" + i.ToString();
                    if (!m.ContainsKey(m_key))
                        m[m_key] = 1;
                    else
                        m[m_key] += 1;
                }
            }
            dic_busline_num = m;
            return dic_busline_num;
        }
    }
}

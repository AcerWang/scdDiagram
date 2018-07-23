using System;
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
            catch(Exception e)
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
            // 主变的起始坐标
            int x = 0, y = 450;

            if (trans == null)
                return;
            // 遍历所有主变信息，生成各主变元素
            foreach (int trans_no in trans.Keys)
            {
                x = 300 + 400 * (trans_no-1);
                Dictionary<string, string> ele_attrs = new Dictionary<string, string>()
                {
                    { "id", (string)trans[trans_no] } ,
                    { "x", x.ToString() } ,
                    { "y", y.ToString() } ,
                    { "href", "#Trans" }
                };
                // 创建新节点
                XmlElement element = NewElement("use",ele_attrs);
                // 添加元素节点到svg节点后面
                svg.AppendChild(element);

                // 创建对应的文字信息
                Dictionary<string, string> text_attrs = new Dictionary<string, string>()
                {
                    { "dy", "0" } ,
                    { "stroke", "black" } ,
                    { "stroke-width", "0.5" } ,
                    { "x", (x-20).ToString() } ,
                    { "y", (y+25).ToString()}
                };
                // 添加文字节点大svg节点后面
                XmlElement text = NewElement("text",text_attrs);
                text.InnerText = (string)trans[trans_no];
                svg.AppendChild(text);

                // 保存此主变位置信息
                trans_location[trans_no] = new int[] {x,y };
            }
        }

        /// <summary>
        /// 通过解析得到的母线信息画出对应母线的位置图，保存母线位置信息
        /// </summary>
        private static void DrawBus()
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
                // 不存在关联关系的母线，直接画单独分段线段
                if(SCDResolver.buses_relation[High_volt].Count() == 0)
                {
                    int x = 50, y = 350;
                    // 每一段母线的长度
                    int seg_length = line_seg_length(SCDResolver.buses[High_volt].Count);

                    // 每一段，单独画，水平排列
                    foreach(var i in SCDResolver.buses[High_volt])
                    {
                        // 画一条母线
                        draw_single_line(High_volt.ToString()+"kV",i,x,y,x+seg_length,y);

                        // 存储坐标
                        buses_location[High_volt] = new Dictionary<int,int[]>();
                        buses_location[High_volt][i] = new int[] { x, y };

                        // 更新x坐标
                        x = x + seg_length + 50;
                    }
                }

                // 高压侧有母联的情况
                if (SCDResolver.buses_relation[High_volt].ContainsKey("母联"))
                {
                    // 有母联关系，但各段母线未给出，此情况直接按并联两条母线画图
                    if (SCDResolver.buses_relation[High_volt]["母联"] == null)
                    {
                        // 之前的解析结果只有一条母线，则补齐另一条
                        if (SCDResolver.buses[High_volt].Count == 1)
                            SCDResolver.buses[High_volt].Add(SCDResolver.buses[High_volt].Last()+1);

                        int x = 50, y = 350;
                        // 画两条并联母线
                        foreach(int i in SCDResolver.buses[High_volt])
                        {
                            // 画一条母线
                            draw_single_line(High_volt.ToString()+"kV",i,x,y,x+1200,y);

                            // 存储母线位置
                            buses_location[High_volt] = new Dictionary<int, int[]>();
                            buses_location[High_volt][i] = new int[] { x,y };

                            // 调整坐标
                            y = y - 40;
                        }
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

        private static void draw_bus_union(int id,int x, int y,string href, string color="red")
        {
            var attrs = new Dictionary<string, string> {
                { "x", x.ToString() } ,
                { "y", y.ToString() } ,
                { "href", href },
                { "stroke", color }
            };
            var ele = NewElement("use",attrs);

        }
    }
}
